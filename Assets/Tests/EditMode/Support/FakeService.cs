using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MinimalChat;

namespace MinimalChat.Tests
{
    /// <summary>
    /// Simple in-memory IChatService for tests. Backlog + live via AsyncQueue.
    /// Provides SendCount and EnqueueForTest helpers for tests.
    /// </summary>
    public sealed class FakeService : IChatService
    {
        private readonly object _gate = new object();
        private readonly List<ChatMessage> _messages = new List<ChatMessage>(256);
        private readonly List<AsyncQueue<ChatMessage>> _subs =
            new List<AsyncQueue<ChatMessage>>(8);

        private long _nextId = 1;

        /// <summary>
        /// How many sends were accepted. Useful for assertions.
        /// </summary>
        public int SendCount { get; private set; }

        public Task<SendMessageAck> SendMessageAsync(
            SendMessageRequest req,
            CancellationToken ct
        )
        {
            if (req == null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            ValidateAscii(req.Sender);
            ValidateAscii(req.Text);

            if (req.Text.Length > 1024)
            {
                throw new ArgumentException("Text exceeds 1024 characters.", nameof(req));
            }

            ChatMessage msg;

            lock (_gate)
            {
                var id = _nextId;
                _nextId = _nextId + 1;

                var nowMs = NowMs();

                msg = new ChatMessage
                {
                    Id = id,
                    Sender = req.Sender,
                    Text = req.Text,
                    CreatedAt = nowMs
                };

                _messages.Add(msg);

                var i = 0;

                while (i < _subs.Count)
                {
                    _subs[i].Enqueue(msg);
                    i = i + 1;
                }

                SendCount = SendCount + 1;
            }

            var ack = new SendMessageAck
            {
                Id = msg.Id,
                CreatedAt = msg.CreatedAt
            };

            return Task.FromResult(ack);
        }

        public async IAsyncEnumerable<ChatMessage> SubscribeMessagesAsync(
            long sinceId,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct
        )
        {
            var q = new AsyncQueue<ChatMessage>();
            List<ChatMessage> backlog = new List<ChatMessage>(64);

            lock (_gate)
            {
                var i = 0;

                while (i < _messages.Count)
                {
                    var m = _messages[i];

                    if (m.Id > sinceId)
                    {
                        backlog.Add(m);
                    }

                    i = i + 1;
                }

                _subs.Add(q);
            }

            try
            {
                var j = 0;

                while (j < backlog.Count)
                {
                    ct.ThrowIfCancellationRequested();
                    q.Enqueue(backlog[j]);
                    j = j + 1;
                }

                await foreach (var m in q.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    yield return m;
                }
            }
            finally
            {
                lock (_gate)
                {
                    var k = 0;

                    while (k < _subs.Count)
                    {
                        if (ReferenceEquals(_subs[k], q))
                        {
                            _subs.RemoveAt(k);
                            break;
                        }

                        k = k + 1;
                    }
                }
            }
        }

        // --- Test helpers -----------------------------------------------------

        /// <summary>
        /// Push a message into backlog/live for tests (auto-assign id and CreatedAt now).
        /// </summary>
        public void EnqueueForTest(string sender, string text)
        {
            var msg = new ChatMessage
            {
                Id = 0, // will be assigned below
                Sender = sender ?? string.Empty,
                Text = text ?? string.Empty,
                CreatedAt = NowMs()
            };

            EnqueueForTest(msg);
        }

        /// <summary>
        /// Push a preconstructed message (if Id==0, assigns next id).
        /// </summary>
        public void EnqueueForTest(ChatMessage msg)
        {
            if (msg == null)
            {
                throw new ArgumentNullException(nameof(msg));
            }

            lock (_gate)
            {
                if (msg.Id <= 0)
                {
                    msg.Id = _nextId;
                    _nextId = _nextId + 1;
                }

                if (msg.CreatedAt <= 0)
                {
                    msg.CreatedAt = NowMs();
                }

                _messages.Add(msg);

                var i = 0;

                while (i < _subs.Count)
                {
                    _subs[i].Enqueue(msg);
                    i = i + 1;
                }
            }
        }

        // --- Internals --------------------------------------------------------

        private static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static void ValidateAscii(string value)
        {
            if (value == null)
            {
                throw new ArgumentException("Value cannot be null.");
            }

            var i = 0;

            while (i < value.Length)
            {
                var c = value[i];

                if (c == '\n' || c == '\t')
                {
                    i = i + 1;
                    continue;
                }

                if (c < 0x20 || c > 0x7E)
                {
                    throw new ArgumentException("Only ASCII characters are allowed.");
                }

                i = i + 1;
            }
        }
    }
}
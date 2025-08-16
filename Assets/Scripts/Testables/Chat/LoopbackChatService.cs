using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalChat
{
    /// <summary>
    /// In-memory "server" for loopback. Retains last 1024 messages. No external deps.
    /// </summary>
    public sealed class LoopbackChatService : IChatService
    {
        private readonly object _gate = new object();
        private readonly List<ChatMessage> _messages = new List<ChatMessage>(1024);
        private readonly List<AsyncQueue<ChatMessage>> _subscribers =
            new List<AsyncQueue<ChatMessage>>();

        private long _nextId = 1;

        public async Task<SendMessageAck> SendMessageAsync(
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

                // Epoch milliseconds for consistency with backend/proto.
                var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                msg = new ChatMessage
                {
                    Id = id,
                    Sender = req.Sender,
                    Text = req.Text,
                    CreatedAt = timestampMs
                };

                _messages.Add(msg);

                while (_messages.Count > 1024)
                {
                    _messages.RemoveAt(0);
                }

                var i = 0;

                while (i < _subscribers.Count)
                {
                    _subscribers[i].Enqueue(msg);
                    i = i + 1;
                }
            }

            await Task.Yield();

            var ack = new SendMessageAck
            {
                Id = msg.Id,
                CreatedAt = msg.CreatedAt
            };

            return ack;
        }

        public async IAsyncEnumerable<ChatMessage> SubscribeMessagesAsync(
            long sinceId,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct
        )
        {
            var queue = new AsyncQueue<ChatMessage>();

            List<ChatMessage> backlog = new List<ChatMessage>();

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

                _subscribers.Add(queue);
            }

            try
            {
                var j = 0;

                while (j < backlog.Count)
                {
                    ct.ThrowIfCancellationRequested();
                    queue.Enqueue(backlog[j]);
                    j = j + 1;
                }

                await foreach (var m in queue.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    yield return m;
                }
            }
            finally
            {
                lock (_gate)
                {
                    var k = 0;

                    while (k < _subscribers.Count)
                    {
                        if (ReferenceEquals(_subscribers[k], queue))
                        {
                            _subscribers.RemoveAt(k);
                            break;
                        }

                        k = k + 1;
                    }
                }
            }
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
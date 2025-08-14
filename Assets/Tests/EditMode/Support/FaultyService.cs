using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace MinimalChat.Tests
{
    /// <summary>
    /// IChatService fake that:
    /// - Emits backlog > sinceId
    /// - Emits some live messages
    /// - Then throws once to simulate a drop
    /// - On next Subscribe, continues emitting live messages
    /// </summary>
    internal sealed class FaultyService : IChatService
    {
        private long _nextId;
        private readonly List<ChatMessage> _sent = new List<ChatMessage>(64);
        private readonly Queue<ChatMessage> _live = new Queue<ChatMessage>(64);
        private bool _shouldFailOnce;

        public FaultyService(bool failOnce)
        {
            _shouldFailOnce = failOnce;
        }

        public Task<SendMessageAck> SendMessageAsync(SendMessageRequest req, CancellationToken ct)
        {
            _nextId = _nextId + 1;
            var created = DateTimeOffset.UtcNow.ToString("o");

            var m = new ChatMessage
            {
                Id = _nextId,
                Sender = req.Sender,
                Text = req.Text,
                CreatedAt = created
            };

            _sent.Add(m);
            _live.Enqueue(m);

            var ack = new SendMessageAck { Id = m.Id, CreatedAt = created };
            return Task.FromResult(ack);
        }

        public async IAsyncEnumerable<ChatMessage> SubscribeMessagesAsync(
            long sinceId,
            [EnumeratorCancellation] CancellationToken ct)
        {
            // Backlog
            for (var i = 0; i < _sent.Count; i++)
            {
                var m = _sent[i];
                if (m.Id > sinceId)
                {
                    yield return m;
                }
            }

            // Live: emit up to 2, then fail once if configured
            int emitted = 0;

            while (!ct.IsCancellationRequested)
            {
                if (_live.Count > 0)
                {
                    var m = _live.Dequeue();
                    yield return m;
                    emitted = emitted + 1;

                    if (_shouldFailOnce && emitted >= 2)
                    {
                        _shouldFailOnce = false;
                        throw new Exception("Simulated drop");
                    }
                }
                else
                {
                    await Task.Delay(1, ct);
                }
            }
        }

        public long EnqueueForTest(string sender, string text)
        {
            _nextId = _nextId + 1;
            var created = DateTimeOffset.UtcNow.ToString("o");

            var m = new ChatMessage
            {
                Id = _nextId,
                Sender = sender,
                Text = text,
                CreatedAt = created
            };

            _sent.Add(m);
            _live.Enqueue(m);

            return m.Id;
        }
    }
}

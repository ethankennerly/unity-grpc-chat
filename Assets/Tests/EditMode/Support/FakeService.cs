using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace MinimalChat.Tests
{
    /// <summary>
    /// Test-only chat service fake that simulates backlog + live streaming.
    /// Matches IChatService: SendMessageAsync returns SendMessageAck.
    /// </summary>
    internal sealed class FakeService : IChatService
    {
        private long _nextId;
        private readonly List<ChatMessage> _sent = new List<ChatMessage>(64);
        private readonly Queue<ChatMessage> _live = new Queue<ChatMessage>(64);

        public int SendCount;

        public Task<SendMessageAck> SendMessageAsync(SendMessageRequest req, CancellationToken ct)
        {
            SendCount = SendCount + 1;

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

            var ack = new SendMessageAck
            {
                Id = m.Id,
                CreatedAt = created
            };

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

            // Live
            while (!ct.IsCancellationRequested)
            {
                if (_live.Count > 0)
                {
                    var m = _live.Dequeue();
                    yield return m;
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

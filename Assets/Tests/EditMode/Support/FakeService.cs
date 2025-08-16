using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalChat.Tests
{
    /// <summary>
    /// In-memory IChatService for tests (normal behavior).
    /// Uses shared core for storage + broadcast; exposes SendCount and test enqueue helpers.
    /// </summary>
    public sealed class FakeService : IChatService
    {
        private readonly InMemoryChatCore _core = new InMemoryChatCore();

        /// <summary>
        /// Number of accepted sends.
        /// </summary>
        public int SendCount { get; private set; }

        public Task<SendMessageAck> SendMessageAsync(SendMessageRequest req, CancellationToken ct)
        {
            if (req == null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            InMemoryChatCore.ValidateAscii(req.Sender);
            InMemoryChatCore.ValidateAscii(req.Text);

            if (req.Text.Length > 1024)
            {
                throw new ArgumentException("Text exceeds 1024 characters.", nameof(req));
            }

            ChatMessage msg = _core.AppendAndBroadcast(req.Sender, req.Text);
            SendCount++;

            var ack = new SendMessageAck
            {
                Id = msg.Id,
                CreatedAt = msg.CreatedAt
            };

            return Task.FromResult(ack);
        }

        public async IAsyncEnumerable<ChatMessage> SubscribeMessagesAsync(
            long sinceId,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            var backlog = new List<ChatMessage>(64);
            _core.SnapshotBacklog(sinceId, backlog);

            var q = _core.AddSubscriber();

            try
            {
                foreach (ChatMessage m in backlog)
                {
                    ct.ThrowIfCancellationRequested();
                    q.Enqueue(m);
                }

                await foreach (ChatMessage m in q.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    yield return m;
                }
            }
            finally
            {
                _core.RemoveSubscriber(q);
            }
        }

        /// <summary>
        /// Push a message into backlog/live for tests (auto id/CreatedAt).
        /// </summary>
        public void EnqueueForTest(string sender, string text)
        {
            var msg = new ChatMessage
            {
                Id = 0,
                Sender = sender ?? string.Empty,
                Text = text ?? string.Empty,
                CreatedAt = InMemoryChatCore.NowMs()
            };

            EnqueueForTest(msg);
        }

        /// <summary>
        /// Push a preconstructed message (assign id/CreatedAt if missing).
        /// </summary>
        public void EnqueueForTest(ChatMessage msg)
        {
            _core.EnqueueExternal(msg);
        }
    }
}
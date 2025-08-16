using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalChat.Tests
{
    /// <summary>
    /// In-memory IChatService that drops the stream once after ≥1 emission.
    /// Uses shared core; only adds the failure behavior.
    /// </summary>
    public sealed class FaultyService : IChatService
    {
        private readonly InMemoryChatCore _core = new InMemoryChatCore();
        private bool _shouldDropOnce;

        public FaultyService()
            : this(failOnce: true)
        {
        }

        public FaultyService(bool failOnce)
        {
            _shouldDropOnce = failOnce;
        }

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
            int emitted = 0;

            try
            {
                foreach (ChatMessage m in backlog)
                {
                    ct.ThrowIfCancellationRequested();
                    q.Enqueue(m);
                    emitted++;

                    if (_shouldDropOnce && emitted >= 1)
                    {
                        _shouldDropOnce = false;
                        throw new Exception("Simulated stream failure.");
                    }
                }

                await foreach (ChatMessage m in q.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    yield return m;
                    emitted++;

                    if (_shouldDropOnce && emitted >= 1)
                    {
                        _shouldDropOnce = false;
                        throw new Exception("Simulated stream failure.");
                    }
                }
            }
            finally
            {
                _core.RemoveSubscriber(q);
            }
        }
    }
}
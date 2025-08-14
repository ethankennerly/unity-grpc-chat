using System;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalChat
{
    /// <summary>
    /// Reconnects with backoff, resumes from last delivered id, and dedupes.
    /// No Unity API usage; ideal for EditMode tests.
    /// </summary>
    public sealed class ReliableSubscriber : IReliableSubscriber
    {
        private readonly IChatService _service;
        private readonly IRetryBackoff _backoff;
        private readonly IReliableClock _clock;

        public ReliableSubscriber(IChatService service, IRetryBackoff backoff, IReliableClock clock)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _backoff = backoff ?? throw new ArgumentNullException(nameof(backoff));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task RunAsync(
            long sinceId,
            Func<ChatMessage, Task> onMessage,
            CancellationToken ct)
        {
            if (onMessage == null)
            {
                throw new ArgumentNullException(nameof(onMessage));
            }

            var lastId = sinceId;
            _backoff.Reset();

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var stream = _service.SubscribeMessagesAsync(lastId, ct);

                    await foreach (var msg in stream)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            break;
                        }

                        if (msg.Id <= lastId)
                        {
                            continue;
                        }

                        await onMessage(msg);
                        lastId = msg.Id;
                        _backoff.Reset();
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                    // Transient error → backoff delay then retry.
                }

                var delay = _backoff.NextDelayMs();
                try
                {
                    await _clock.Delay(delay, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}

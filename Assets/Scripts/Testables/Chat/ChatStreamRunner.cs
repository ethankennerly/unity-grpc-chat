using System;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalChat
{
    /// <summary>
    /// Runs the subscription stream and calls back for new messages > sinceId.
    /// No LINQ, no MonoBehaviour, no UI access here.
    /// </summary>
    public sealed class ChatStreamRunner
    {
        private readonly IChatService _service;

        public ChatStreamRunner(IChatService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
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

            try
            {
                var stream = _service.SubscribeMessagesAsync(lastId, ct);

                await foreach (var msg in stream.ConfigureAwait(false))
                {
                    if (msg.Id <= lastId)
                    {
                        continue;
                    }

                    await onMessage(msg).ConfigureAwait(false);
                    lastId = msg.Id;
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalChat
{
    /// <summary>
    /// Placeholder for future gRPC client. Kept to minimal interface for easy swap.
    /// </summary>
    public sealed class RemoteGrpcChatService : IChatService
    {
        public Task<SendMessageAck> SendMessageAsync(
            SendMessageRequest req,
            CancellationToken ct
        )
        {
            throw new System.NotImplementedException("Remote gRPC not implemented.");
        }

        public async IAsyncEnumerable<ChatMessage> SubscribeMessagesAsync(
            long sinceId,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct
        )
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}

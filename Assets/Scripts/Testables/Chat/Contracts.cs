// Assets/Scripts/Testables/Contracts.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalChat
{
    /// <summary>
    /// Request to send a chat message.
    /// </summary>
    public sealed class SendMessageRequest
    {
        public string Sender = string.Empty;
        public string Text = string.Empty;
    }

    /// <summary>
    /// Acknowledgement for a sent message (id + created-at epoch ms).
    /// </summary>
    public sealed class SendMessageAck
    {
        public long Id;
        public long CreatedAt;
    }

    /// <summary>
    /// A chat message DTO (epoch ms timestamp).
    /// </summary>
    public sealed class ChatMessage
    {
        public long Id;
        public long CreatedAt;
        public string Sender = string.Empty;
        public string Text = string.Empty;
    }

    /// <summary>
    /// Chat service abstraction for presenter/tests.
    /// </summary>
    public interface IChatService
    {
        Task<SendMessageAck> SendMessageAsync(SendMessageRequest req, CancellationToken ct);

        IAsyncEnumerable<ChatMessage> SubscribeMessagesAsync(
            long sinceId,
            CancellationToken ct);
    }
}
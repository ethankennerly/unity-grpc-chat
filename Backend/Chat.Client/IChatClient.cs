using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Chat.Proto;

namespace Chat.Client
{
    /// <summary>
    /// Minimal client surface for Unity and tests.
    /// </summary>
    public interface IChatClient
    {
        Task<long> SendAsync(string sender, string text, CancellationToken ct);
        IAsyncEnumerable<ChatMessage> StreamAsync(long sinceId, CancellationToken ct);
    }
}

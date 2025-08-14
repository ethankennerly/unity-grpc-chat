using System.Threading;
using System.Threading.Tasks;

namespace MinimalChat
{
    /// <summary>
    /// Delay abstraction for testability.
    /// </summary>
    public interface IReliableClock
    {
        Task Delay(int milliseconds, CancellationToken ct);
    }
}

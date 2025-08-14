using System.Threading;
using System.Threading.Tasks;

namespace MinimalChat
{
    /// <summary>
    /// Real clock implementation.
    /// </summary>
    public sealed class SystemClock : IReliableClock
    {
        public Task Delay(int milliseconds, CancellationToken ct)
        {
            return Task.Delay(milliseconds, ct);
        }
    }
}

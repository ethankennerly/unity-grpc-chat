using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalChat
{
    /// <summary>
    /// Minimal async queue: Enqueue items, consume via ReadAllAsync(ct).
    /// No LINQ, no Channels, thread-safe, no allocations beyond items.
    /// </summary>
    public sealed class AsyncQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

        /// <summary>
        /// Enqueue a value and wake one waiter.
        /// </summary>
        public void Enqueue(T item)
        {
            _queue.Enqueue(item);
            _signal.Release();
        }

        /// <summary>
        /// Endless async stream of items until cancellation.
        /// </summary>
        public async IAsyncEnumerable<T> ReadAllAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            while (true)
            {
                T item;

                if (_queue.TryDequeue(out item))
                {
                    yield return item;
                    continue;
                }

                Task wait = _signal.WaitAsync(ct);

                try
                {
                    await wait.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }
            }
        }
    }
}
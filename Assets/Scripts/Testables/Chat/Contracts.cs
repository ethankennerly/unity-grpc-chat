using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalChat
{
    /// <summary>
    /// Message payload. ASCII only. Max 1024 chars. ISO-8601 UTC timestamp.
    /// </summary>
    [Serializable]
    public sealed class ChatMessage
    {
        public long Id;
        public string Sender = "";
        public string Text = "";
        public string CreatedAt = "";

        public override string ToString()
        {
            var inner = CreatedAt + " " + Sender + ": " + Text;
            var boxed = "[" + inner + "]";
            return boxed;
        }
    }

    /// <summary>
    /// Send request.
    /// </summary>
    public sealed class SendMessageRequest
    {
        public string Sender = "";
        public string Text = "";
    }

    /// <summary>
    /// Send acknowledgment.
    /// </summary>
    public sealed class SendMessageAck
    {
        public long Id;
        public string CreatedAt = "";
    }

    /// <summary>
    /// Minimal service contract. Only operations that need async.
    /// </summary>
    public interface IChatService
    {
        Task<SendMessageAck> SendMessageAsync(
            SendMessageRequest req,
            CancellationToken ct
        );

        IAsyncEnumerable<ChatMessage> SubscribeMessagesAsync(
            long sinceId,
            CancellationToken ct
        );
    }

    /// <summary>
    /// Tiny async queue to avoid System.Threading.Channels dependency in Unity.
    /// </summary>
    public sealed class AsyncQueue<T>
    {
        private readonly object _gate = new object();
        private readonly Queue<T> _queue;
        private TaskCompletionSource<bool> _tcs;

        public AsyncQueue(int capacity = 256)
        {
            if (capacity <= 0)
            {
                capacity = 1;
            }

            _queue = new Queue<T>(capacity);
            _tcs = NewTcs();
        }

        public void Enqueue(T item)
        {
            TaskCompletionSource<bool> release = null;

            lock (_gate)
            {
                _queue.Enqueue(item);

                if (!_tcs.Task.IsCompleted)
                {
                    release = _tcs;
                    _tcs = NewTcs();
                }
            }

            if (release != null)
            {
                release.TrySetResult(true);
            }
        }

        public async IAsyncEnumerable<T> ReadAllAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct
        )
        {
            while (true)
            {
                while (true)
                {
                    T item;

                    lock (_gate)
                    {
                        if (_queue.Count == 0)
                        {
                            break;
                        }

                        item = _queue.Dequeue();
                    }

                    yield return item;
                }

                Task wait;

                lock (_gate)
                {
                    wait = _tcs.Task;
                }

                if (wait.IsCompleted)
                {
                    continue;
                }

                var cont = await WaitAsync(wait, ct).ConfigureAwait(false);

                if (!cont)
                {
                    yield break;
                }
            }
        }

        private static TaskCompletionSource<bool> NewTcs()
        {
            return new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
        }

        private static async Task<bool> WaitAsync(Task task, CancellationToken ct)
        {
            try
            {
                var delay = Task.Delay(-1, ct);
                await Task.WhenAny(task, delay).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}

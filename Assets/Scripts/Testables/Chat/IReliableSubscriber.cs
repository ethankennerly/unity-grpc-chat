namespace MinimalChat
{
    /// <summary>
    /// Runs a resilient stream from sinceId. Returns when canceled.
    /// </summary>
    public interface IReliableSubscriber
    {
        System.Threading.Tasks.Task RunAsync(
            long sinceId,
            System.Func<ChatMessage, System.Threading.Tasks.Task> onMessage,
            System.Threading.CancellationToken ct);
    }
}

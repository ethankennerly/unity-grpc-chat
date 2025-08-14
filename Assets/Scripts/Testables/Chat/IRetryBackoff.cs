namespace MinimalChat
{
    /// <summary>
    /// Backoff policy (deterministic).
    /// </summary>
    public interface IRetryBackoff
    {
        void Reset();
        int NextDelayMs();
    }
}

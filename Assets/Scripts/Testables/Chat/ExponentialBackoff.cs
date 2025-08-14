namespace MinimalChat
{
    /// <summary>
    /// Bounded exponential backoff (e.g., 50 -> 100 -> 200 -> ... -> max).
    /// </summary>
    public sealed class ExponentialBackoff : IRetryBackoff
    {
        private readonly int _initialMs;
        private readonly int _maxMs;
        private int _current;

        public ExponentialBackoff(int initialMs, int maxMs)
        {
            if (initialMs < 1) { initialMs = 1; }
            if (maxMs < initialMs) { maxMs = initialMs; }
            _initialMs = initialMs;
            _maxMs = maxMs;
            _current = _initialMs;
        }

        public void Reset()
        {
            _current = _initialMs;
        }

        public int NextDelayMs()
        {
            var d = _current;
            var next = _current << 1;
            _current = next > _maxMs ? _maxMs : next;
            return d;
        }
    }
}

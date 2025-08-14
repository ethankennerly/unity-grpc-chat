namespace MinimalChat.Tests
{
    /// <summary>
    /// Test-only factory fake to flip between loopback and "remote".
    /// Accepts any IChatService (e.g., FakeService or FaultyService).
    /// </summary>
    internal sealed class FakeFactory : IChatServiceFactory
    {
        private readonly IChatService _loop;
        private readonly IChatService _remote;

        public int CreateLoopCount;
        public int CreateRemoteCount;

        public FakeFactory(IChatService loop, IChatService remote)
        {
            _loop = loop;
            _remote = remote;
        }

        public IChatService Create(bool useLoopback)
        {
            if (useLoopback)
            {
                CreateLoopCount = CreateLoopCount + 1;
                return _loop;
            }

            CreateRemoteCount = CreateRemoteCount + 1;
            return _remote;
        }
    }
}

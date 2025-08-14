namespace MinimalChat.Tests
{
    /// <summary>
    /// Test-only factory fake to flip between loopback and "remote".
    /// </summary>
    internal sealed class FakeFactory : IChatServiceFactory
    {
        private readonly FakeService _loop;
        private readonly FakeService _remote;

        public int CreateLoopCount;
        public int CreateRemoteCount;

        public FakeFactory(FakeService loop, FakeService remote)
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

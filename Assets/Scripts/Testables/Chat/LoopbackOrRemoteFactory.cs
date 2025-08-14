namespace MinimalChat
{
    /// <summary>
    /// Returns a shared LoopbackChatService so multiple clients in the scene
    /// receive the same broadcasts. Remote remains per-call for now.
    /// </summary>
    public sealed class LoopbackOrRemoteFactory : IChatServiceFactory
    {
        private static readonly LoopbackChatService _sharedLoopback = new LoopbackChatService();

        public IChatService Create(bool useLoopback)
        {
            if (useLoopback)
            {
                return _sharedLoopback;
            }

            return new RemoteGrpcChatService();
        }
    }
}

namespace MinimalChat
{
    /// <summary>
    /// Factory isolates how services are created so the presenter stays testable.
    /// </summary>
    public interface IChatServiceFactory
    {
        IChatService Create(bool useLoopback);
    }

    /// <summary>
    /// Default factory: Loopback for debug, Remote for integration.
    /// </summary>
    public sealed class LoopbackOrRemoteFactory : IChatServiceFactory
    {
        public IChatService Create(bool useLoopback)
        {
            if (useLoopback)
            {
                return new LoopbackChatService();
            }

            return new RemoteGrpcChatService();
        }
    }
}

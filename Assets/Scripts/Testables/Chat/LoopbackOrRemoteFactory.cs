using System;

namespace MinimalChat
{
    /// <summary>
    /// Minimal factory that switches loopback vs remote.
    /// Defaults to the same base URL as ChatServiceFactory.
    /// </summary>
    public sealed class LoopbackOrRemoteFactory : IChatServiceFactory
    {
        private readonly string _remoteBaseUrl;

        public LoopbackOrRemoteFactory()
            : this(ChatServiceFactory.DefaultRemoteBaseUrl)
        {
        }

        public LoopbackOrRemoteFactory(string remoteBaseUrl)
        {
            if (string.IsNullOrEmpty(remoteBaseUrl))
            {
                throw new ArgumentException("remoteBaseUrl required",
                    nameof(remoteBaseUrl));
            }

            _remoteBaseUrl = remoteBaseUrl;
        }

        public IChatService Create(bool useLoopback)
        {
            if (useLoopback)
            {
                return new LoopbackChatService();
            }

            return new RemoteGrpcChatService(_remoteBaseUrl);
        }
    }
}
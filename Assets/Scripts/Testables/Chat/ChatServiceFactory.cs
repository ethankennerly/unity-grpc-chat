using System;

namespace MinimalChat
{
    /// <summary>
    /// Produces chat services for loopback and remote.
    /// Presenter stays URL-agnostic; URL lives here.
    /// </summary>
    public sealed class ChatServiceFactory : IChatServiceFactory
    {
        /// <summary>
        /// Default local backend URL for dev/review.
        /// </summary>
        public const string DefaultRemoteBaseUrl = "http://127.0.0.1:5000";

        private readonly string _remoteBaseUrl;

        /// <summary>
        /// Uses DefaultRemoteBaseUrl for remote.
        /// </summary>
        public ChatServiceFactory() : this(DefaultRemoteBaseUrl)
        {
        }

        /// <summary>
        /// Allows overriding the remote base URL.
        /// </summary>
        public ChatServiceFactory(string remoteBaseUrl)
        {
            if (string.IsNullOrEmpty(remoteBaseUrl))
            {
                throw new ArgumentException("remoteBaseUrl required",
                    nameof(remoteBaseUrl));
            }

            _remoteBaseUrl = remoteBaseUrl;
        }

        /// <summary>
        /// Creates either a loopback or remote gRPC service.
        /// </summary>
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
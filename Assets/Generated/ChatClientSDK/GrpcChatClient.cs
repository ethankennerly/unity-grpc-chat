using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Chat.Proto;
using Grpc.Net.Client;

namespace Chat.Client
{
    /// <summary>
    /// gRPC-based implementation of IChatClient.
    /// </summary>
    public sealed class GrpcChatClient : IChatClient
    {
        private readonly ChatService.ChatServiceClient _client;

        private GrpcChatClient(ChatService.ChatServiceClient client)
        {
            _client = client;
        }

        public static GrpcChatClient Create(HttpClient httpClient, string baseAddress)
        {
            var channel = GrpcChannel.ForAddress(baseAddress, new GrpcChannelOptions
            {
                HttpClient = httpClient
            });

            return new GrpcChatClient(new ChatService.ChatServiceClient(channel));
        }

        public async Task<long> SendAsync(string sender, string text, CancellationToken ct)
        {
            var ack = await _client.SendMessageAsync(
                new SendMessageRequest { Sender = sender, Text = text },
                cancellationToken: ct);

            return ack.Id;
        }

        public async IAsyncEnumerable<ChatMessage> StreamAsync(
            long sinceId,
            [EnumeratorCancellation] CancellationToken ct)
        {
            using var call = _client.StreamMessages(new StreamRequest { SinceId = sinceId }, cancellationToken: ct);
            var stream = call.ResponseStream;

            while (await stream.MoveNext(ct))
            {
                yield return stream.Current;
            }
        }
    }
}

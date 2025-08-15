using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Chat.Proto;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;

namespace MinimalChat
{
    /// <summary>
    /// Real gRPC client for Unity using gRPC-Web (HTTP/1.1). Works in Editor.
    /// Converts between MinimalChat DTOs and Chat.Proto DTOs.
    /// </summary>
    public sealed class RemoteGrpcChatService : IChatService
    {
        private readonly string _baseUrl;
        private readonly ChatService.ChatServiceClient _client;

        public RemoteGrpcChatService(string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentException("baseUrl required", nameof(baseUrl));
            }

            // gRPC-Web handler chain (binary mode). Text mode also works if needed:
            // new GrpcWebHandler(GrpcWebMode.GrpcWebText, inner)
            var inner = new HttpClientHandler();
            var grpcWeb = new GrpcWebHandler(GrpcWebMode.GrpcWeb, inner);
            var http = new HttpClient(grpcWeb)
            {
                BaseAddress = new Uri(baseUrl)
            };

            var channel = GrpcChannel.ForAddress(baseUrl, new GrpcChannelOptions
            {
                HttpClient = http
            });

            _baseUrl = baseUrl;
            _client = new ChatService.ChatServiceClient(channel);
        }

        public async Task<SendMessageAck> SendMessageAsync(
            SendMessageRequest req,
            CancellationToken ct)
        {
            // Map MinimalChat → Proto
            var pReq = new Chat.Proto.SendMessageRequest
            {
                Sender = req.Sender ?? string.Empty,
                Text = req.Text ?? string.Empty
            };

            var ack = await _client.SendMessageAsync(pReq, cancellationToken: ct);

            // Map Proto → MinimalChat
            return new SendMessageAck
            {
                Id = ack.Id,
                CreatedAt = ack.CreatedAt
            };
        }

        public async IAsyncEnumerable<ChatMessage> SubscribeMessagesAsync(
            long sinceId,
            [EnumeratorCancellation] CancellationToken ct)
        {
            using var call = _client.StreamMessages(
                new StreamRequest { SinceId = sinceId },
                cancellationToken: ct);

            var stream = call.ResponseStream;

            while (await stream.MoveNext(ct))
            {
                var m = stream.Current;

                yield return new ChatMessage
                {
                    Id = m.Id,
                    Sender = m.Sender,
                    Text = m.Text,
                    CreatedAt = m.CreatedAt
                };
            }
        }
    }
}

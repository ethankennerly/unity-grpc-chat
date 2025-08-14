using System.Threading.Tasks;
using Chat.Proto;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Chat.Server
{
    /// <summary>
    /// gRPC chat service with persistence and backlog replay.
    /// </summary>
    public sealed class GrpcChatService : ChatService.ChatServiceBase
    {
        private readonly IChatRepo _repo;
        private readonly ILogger<GrpcChatService> _logger;

        public GrpcChatService(IChatRepo repo, ILogger<GrpcChatService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public override async Task<SendMessageAck> SendMessage(
            SendMessageRequest request,
            ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "request required"));
            }

            ValidateAscii(request.Sender);
            ValidateAscii(request.Text);

            if (request.Text.Length > 1024)
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "message too long"));
            }

            var (id, createdAt) =
                await _repo.InsertAsync(request.Sender, request.Text,
                                        context.CancellationToken);

            return new SendMessageAck { Id = id, CreatedAt = createdAt };
        }

        public override async Task StreamMessages(
            StreamRequest request,
            IServerStreamWriter<ChatMessage> responseStream,
            ServerCallContext context)
        {
            if (request == null)
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "request required"));
            }

            var sinceId = request.SinceId;
            var ct = context.CancellationToken;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var rows = await _repo.ReadSinceAsync(sinceId, ct);

                    for (int i = 0; i < rows.Count; i++)
                    {
                        var row = rows[i];
                        await responseStream.WriteAsync(new ChatMessage
                        {
                            Id = row.Item1,
                            Sender = row.Item2,
                            Text = row.Item3,
                            CreatedAt = row.Item4
                        });

                        sinceId = row.Item1;
                    }

                    await Task.Delay(100, ct);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("StreamMessages canceled by client.");
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                _logger.LogInformation("StreamMessages RPC canceled by client.");
            }
        }

        private static void ValidateAscii(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return;
            }

            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c == '\n' || c == '\t')
                {
                    continue;
                }

                if (c < 0x20 || c > 0x7E)
                {
                    throw new RpcException(
                        new Status(StatusCode.InvalidArgument, "ascii only"));
                }
            }
        }
    }
}

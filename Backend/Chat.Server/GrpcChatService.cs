using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Chat.Proto;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Chat.Server
{
    /// <summary>
    /// Minimal gRPC chat service:
    /// - SendMessage: insert and ack id/createdAt.
    /// - StreamMessages: backlog first, then live via repo's OnInserted.
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

            // NOTE: property names depend on your generated C# from proto.
            // If it's 'CreatedAt' instead of 'Created_at', adjust here.
            return new SendMessageAck { Id = id, CreatedAt = createdAt };
        }

        public override async Task StreamMessages(
            StreamRequest request,
            IServerStreamWriter<ChatMessage> responseStream,
            ServerCallContext context)
        {
            var ct = context.CancellationToken;

            // 1) Backlog: fully flush rows with id > sinceId.
            var backlog =
                await _repo.ReadBacklogAsync(request.SinceId, ct);

            for (int i = 0; i < backlog.Count; i++)
            {
                var m = backlog[i];

                await responseStream.WriteAsync(new ChatMessage
                {
                    Id        = m.Id,
                    CreatedAt = m.CreatedAt, // adjust to 'Created_at' if your generator uses snake case
                    Sender    = m.Sender,
                    Text      = m.Text
                });
            }

            // 2) Live tail: subscribe to repo inserts and forward until client cancels.
            var queue = new BlockingCollection<RepoMessage>(
                new ConcurrentQueue<RepoMessage>());

            void OnInsert(RepoMessage m)
            {
                // Early-out: ignore messages at/before the last seen id.
                if (m.Id <= request.SinceId)
                {
                    return;
                }

                // Non-blocking add; if closed, ignore.
                try { queue.TryAdd(m); } catch { /* ignore */ }
            }

            _repo.OnInserted += OnInsert;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // Wait for next live message or cancellation.
                    RepoMessage m;
                    try
                    {
                        if (!queue.TryTake(out m!, 250, ct))
                        {
                            // Periodic check for cancellation; keep loop simple.
                            continue;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    // Forward to client.
                    await responseStream.WriteAsync(new ChatMessage
                    {
                        Id        = m.Id,
                        CreatedAt = m.CreatedAt, // or Created_at
                        Sender    = m.Sender,
                        Text      = m.Text
                    });

                    // Advance sinceId so we never resend.
                    request.SinceId = m.Id;
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                _logger.LogInformation(ex, "StreamMessages ended with exception.");
                // gRPC will translate thrown exceptions; we log and complete gracefully.
            }
            finally
            {
                // Unsubscribe and drain.
                _repo.OnInserted -= OnInsert;
                queue.Dispose();

                _logger.LogInformation("StreamMessages canceled by client.");
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

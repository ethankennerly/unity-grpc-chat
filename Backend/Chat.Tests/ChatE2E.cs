using System.Threading;
using System.Threading.Tasks;
using Chat.Proto;
using Grpc.Net.Client;
using Xunit;

namespace Chat.Tests
{
    public sealed class ChatE2E : IClassFixture<TestChatFactory>
    {
        private readonly TestChatFactory _factory;

        public ChatE2E(TestChatFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Send_Then_Stream_Returns_Persisted_Messages()
        {
            using var http = _factory.CreateDefaultClient();
            using var channel = GrpcChannel.ForAddress(http.BaseAddress!, new GrpcChannelOptions
            {
                HttpClient = http
            });

            var chat = new ChatService.ChatServiceClient(channel);

            var a = await chat.SendMessageAsync(
                new SendMessageRequest { Sender = "Alice", Text = "Hello" });
            var b = await chat.SendMessageAsync(
                new SendMessageRequest { Sender = "Bob", Text = "Hi" });

            Assert.True(a.Id > 0 && b.Id > a.Id);

            using var cts = new CancellationTokenSource(2000);
            using var call = chat.StreamMessages(
                new StreamRequest { SinceId = a.Id - 1 },
                cancellationToken: cts.Token);

            int seen = 0;
            long lastId = 0;

            while (await call.ResponseStream.MoveNext(cts.Token))
            {
                var msg = call.ResponseStream.Current;
                lastId = msg.Id;
                seen++;
                if (seen >= 2)
                {
                    break;
                }
            }

            Assert.Equal(b.Id, lastId);
        }
    }
}

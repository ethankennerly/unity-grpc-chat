using System.Threading.Tasks;
using Chat.Proto;
using Grpc.Net.Client;
using Xunit;

namespace Chat.Tests
{
    public sealed class RetentionTests : IClassFixture<TestChatFactory>
    {
        private readonly TestChatFactory _factory;
        public RetentionTests(TestChatFactory factory) { _factory = factory; }

        [Fact]
        public async Task Retains_Last_1024_Messages()
        {
            using var http = _factory.CreateDefaultClient();
            using var channel = GrpcChannel.ForAddress(http.BaseAddress!, new GrpcChannelOptions { HttpClient = http });
            var chat = new ChatService.ChatServiceClient(channel);

            long firstId = -1;
            long lastId = -1;

            // Insert 1200 messages
            for (int i = 0; i < 1200; i++)
            {
                var ack = await chat.SendMessageAsync(new SendMessageRequest
                {
                    Sender = "User",
                    Text = "Msg " + i
                });
                if (i == 0) firstId = ack.Id;
                lastId = ack.Id;
            }

            // Backlog since 0 should return the last 256 only (batch cap)
            using var call = chat.StreamMessages(new StreamRequest { SinceId = 0 });
            int count = 0;
            long firstBacklogId = -1, lastBacklogId = -1;

            // Read one batch (<=256) and stop
            while (await call.ResponseStream.MoveNext(default))
            {
                var msg = call.ResponseStream.Current;
                if (count == 0) firstBacklogId = msg.Id;
                lastBacklogId = msg.Id;
                count++;
                if (count >= 256) break;
            }

            Assert.True(count <= 256);
            // After 1200 inserts with retention 1024, the first retained id = lastId - 1023.
            var expectedFirstRetained = lastId - 1023;
            Assert.True(firstBacklogId >= expectedFirstRetained);
            Assert.True(lastBacklogId <= lastId);
        }
    }
}

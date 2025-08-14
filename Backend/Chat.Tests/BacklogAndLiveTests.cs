// Backend/Chat.Tests/BacklogAndLiveTests.cs
using System.Threading;
using System.Threading.Tasks;
using Chat.Proto;
using Grpc.Net.Client;
using Xunit;

namespace Chat.Tests
{
    public sealed class BacklogAndLiveTests : IClassFixture<TestChatFactory>
    {
        private readonly TestChatFactory _factory;
        public BacklogAndLiveTests(TestChatFactory factory) { _factory = factory; }

        [Fact]
        public async Task Stream_Backlog_Then_Live_Ordered()
        {
            using var http = _factory.CreateDefaultClient();
            using var channel = GrpcChannel.ForAddress(http.BaseAddress!, new GrpcChannelOptions { HttpClient = http });
            var chat = new ChatService.ChatServiceClient(channel);

            // Seed one message so there is a backlog
            var seed = await chat.SendMessageAsync(new SendMessageRequest { Sender = "Seed", Text = "old" });

            using var cts = new CancellationTokenSource(2000);
            using var call = chat.StreamMessages(new StreamRequest { SinceId = seed.Id - 1 }, cancellationToken: cts.Token);
            var stream = call.ResponseStream;

            // Read first backlog message
            Assert.True(await stream.MoveNext(cts.Token));
            var first = stream.Current;
            Assert.Equal(seed.Id, first.Id);

            // Now send a live message and expect it next on the same stream
            var live = await chat.SendMessageAsync(new SendMessageRequest { Sender = "Live", Text = "new" });

            Assert.True(await stream.MoveNext(cts.Token));
            var second = stream.Current;
            Assert.Equal(live.Id, second.Id);
        }
    }
}

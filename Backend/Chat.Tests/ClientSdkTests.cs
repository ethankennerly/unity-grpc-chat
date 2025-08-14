using System.Threading;
using System.Threading.Tasks;
using Chat.Client;
using Chat.Proto;
using Grpc.Net.Client;
using Xunit;

namespace Chat.Tests
{
    public sealed class ClientSdkTests : IClassFixture<TestChatFactory>
    {
        private readonly TestChatFactory _factory;

        public ClientSdkTests(TestChatFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ClientSdk_Send_Then_Stream_Works_EndToEnd()
        {
            using var http = _factory.CreateDefaultClient();

            var client = GrpcChatClient.Create(http, http.BaseAddress!.ToString());

            var aId = await client.SendAsync("Alice", "Hello", default);
            var bId = await client.SendAsync("Bob",   "Hi",    default);

            Assert.True(aId > 0 && bId > aId);

            using var cts = new CancellationTokenSource(2000);

            int seen = 0;
            long lastId = 0;

            await foreach (var msg in client.StreamAsync(aId - 1, cts.Token))
            {
                lastId = msg.Id;
                seen++;
                if (seen >= 2)
                {
                    break;
                }
            }

            Assert.Equal(bId, lastId);
        }
    }
}

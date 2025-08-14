using System.Threading;
using System.Threading.Tasks;
using Chat.Proto;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
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
            using var httpClient = _factory.CreateDefaultClient();

            using var channel = GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions
            {
                HttpClient = httpClient
            });

            var chat = new ChatService.ChatServiceClient(channel);

            // Send two messages
            var a = await chat.SendMessageAsync(new SendMessageRequest { Sender = "Alice", Text = "Hello" });
            var b = await chat.SendMessageAsync(new SendMessageRequest { Sender = "Bob",   Text = "Hi"    });

            Assert.True(a.Id > 0 && b.Id > a.Id);

            // Start streaming just before 'a' to avoid pre-existing rows (per-test DB should be empty,
            // but this also documents recommended client usage).
            using var cts = new CancellationTokenSource(millisecondsDelay: 2000);
            using var call = chat.StreamMessages(new StreamRequest { SinceId = a.Id - 1 },
                                                 cancellationToken: cts.Token);

            int seen = 0;
            var stream = call.ResponseStream;

            while (await stream.MoveNext(cts.Token))
            {
                var msg = stream.Current;

                if (seen == 0)
                {
                    Assert.Equal(a.Id, msg.Id);
                    Assert.Equal("Alice", msg.Sender);
                    Assert.Equal("Hello", msg.Text);
                }
                else if (seen == 1)
                {
                    Assert.Equal(b.Id, msg.Id);
                    Assert.Equal("Bob", msg.Sender);
                    Assert.Equal("Hi", msg.Text);
                    break; // done
                }

                seen++;
            }
        }
    }
}

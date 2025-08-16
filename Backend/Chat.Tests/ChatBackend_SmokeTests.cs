using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Chat.Proto;
using Xunit;

namespace Chat.Tests
{
    /// <summary>
    /// Minimal, stable backend smoke tests:
    /// - Send returns an id.
    /// - Streaming backlog returns the sent items in order.
    /// Keep counts small and timeouts generous for CI.
    /// </summary>
    public sealed class ChatBackend_SmokeTests : IClassFixture<TestChatFactory>
    {
        private readonly TestChatFactory _fx;

        public ChatBackend_SmokeTests(TestChatFactory fx)
        {
            _fx = fx;
        }

        [Fact]
        public async Task Send_Returns_Id_And_Persists()
        {
            var client = _fx.CreateChatClient();

            var ack = await client.SendMessageAsync(new SendMessageRequest
            {
                Sender = "A",
                Text = "one"
            });

            Assert.True(ack.Id > 0);
            Assert.True(ack.CreatedAt > 0);
        }

        [Fact]
        public async Task Stream_Backlog_Returns_Sent_In_Order()
        {
            var client = _fx.CreateChatClient();

            var a = await client.SendMessageAsync(new SendMessageRequest
            {
                Sender = "A",
                Text = "one"
            });

            var b = await client.SendMessageAsync(new SendMessageRequest
            {
                Sender = "B",
                Text = "two"
            });

            using var cts = new CancellationTokenSource(5000);

            using var call = client.StreamMessages(
                new StreamRequest { SinceId = 0 },
                cancellationToken: cts.Token);

            var items = new List<ChatMessage>(4);

            while (await call.ResponseStream.MoveNext(cts.Token))
            {
                items.Add(call.ResponseStream.Current);
                if (items.Count >= 2)
                {
                    break;
                }
            }

            Assert.Equal(2, items.Count);
            Assert.True(items[0].Id < items[1].Id);

            Assert.Contains(items, m => m.Id == a.Id &&
                                        m.Sender == "A" &&
                                        m.Text == "one");

            Assert.Contains(items, m => m.Id == b.Id &&
                                        m.Sender == "B" &&
                                        m.Text == "two");
        }
    }
}

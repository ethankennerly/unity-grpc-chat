using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MinimalChat.Tests
{
    /// <summary>
    /// Ensures presenter keeps streaming after a simulated drop (reconnect + dedupe).
    /// Deterministic: wait for backlog to render before enqueuing live.
    /// </summary>
    public sealed class ChatClientPresenter_ReconnectTests
    {
        [Test]
        public async Task Presenter_Reconnects_And_Dedupes_After_Drop()
        {
            var view = new FakeView();
            var loop = new FaultyService(failOnce: true);
            var remote = new FaultyService(failOnce: false);
            var fac = new FakeFactory(loop, remote);

            view.SetDisplayName("Alice");
            view.SetLoopbackForTest(true);

            // Seed backlog BEFORE starting stream so they're guaranteed as backlog
            await loop.SendMessageAsync(new SendMessageRequest { Sender = "A", Text = "one" }, default);
            await loop.SendMessageAsync(new SendMessageRequest { Sender = "B", Text = "two" }, default);

            var p = new ChatClientPresenter(view, fac);
            p.Start();

            // Wait until backlog is rendered
            Assert.IsTrue(await WaitForContains(view, "B: two", 300),
                "Backlog did not render in time.");

            // Now enqueue live; FaultyService will throw once after two live emits
            await loop.SendMessageAsync(new SendMessageRequest { Sender = "C", Text = "three" }, default);
            await loop.SendMessageAsync(new SendMessageRequest { Sender = "D", Text = "four" }, default);
            await loop.SendMessageAsync(new SendMessageRequest { Sender = "E", Text = "five" }, default);

            // Wait for live messages post-reconnect
            Assert.IsTrue(await WaitForContains(view, "C: three", 400),
                "Live 'C: three' not received.");
            Assert.IsTrue(await WaitForContains(view, "D: four", 400),
                "Live 'D: four' not received.");
            Assert.IsTrue(await WaitForContains(view, "E: five", 400),
                "Live 'E: five' not received.");

            var text = view.GetMessagesForTest();
            Assert.AreEqual(1, Count(text, "A: one"));
            Assert.AreEqual(1, Count(text, "B: two"));
            Assert.AreEqual(1, Count(text, "C: three"));
            Assert.AreEqual(1, Count(text, "D: four"));
            Assert.AreEqual(1, Count(text, "E: five"));

            await p.StopAsync();
        }

        private static async Task<bool> WaitForContains(FakeView view, string needle, int timeoutMs)
        {
            var start = Environment.TickCount;
            while (Environment.TickCount - start < timeoutMs)
            {
                var text = view.GetMessagesForTest();
                if (!string.IsNullOrEmpty(text))
                {
                    if (text.IndexOf(needle, StringComparison.Ordinal) >= 0)
                    {
                        return true;
                    }
                }

                await Task.Delay(10);
            }

            return false;
        }

        private static int Count(string haystack, string needle)
        {
            if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle))
            {
                return 0;
            }

            var n = 0;
            var i = 0;

            while (true)
            {
                i = haystack.IndexOf(needle, i, StringComparison.Ordinal);
                if (i < 0)
                {
                    break;
                }

                n += 1;
                i += needle.Length;
            }

            return n;
        }
    }
}
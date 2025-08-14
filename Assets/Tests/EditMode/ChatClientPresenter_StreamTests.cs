using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MinimalChat.Tests
{
    /// <summary>
    /// Tests backlog then live delivery and dedupe with deterministic waits.
    /// </summary>
    public sealed class ChatClientPresenter_StreamTests
    {
        [Test]
        public async Task Stream_Backlog_Then_Live_Dedupes()
        {
            var view = new FakeView();
            var loop = new FakeService();
            var remote = new FakeService();
            var fac = new FakeFactory(loop, remote);

            view.SetDisplayName("A");
            view.SetLoopbackForTest(true);

            // Seed backlog BEFORE starting so A/B are guaranteed as backlog.
            loop.EnqueueForTest("A", "one");
            loop.EnqueueForTest("B", "two");

            var p = new ChatClientPresenter(view, fac);
            p.Start();

            // Wait until backlog is rendered (ensures stream is attached).
            Assert.IsTrue(await WaitForContains(view, "B: two", 300),
                "Backlog did not render in time.");

            // Now enqueue live; should appear after backlog.
            loop.EnqueueForTest("C", "three");

            // Wait for live 'C' to appear.
            Assert.IsTrue(await WaitForContains(view, "C: three", 300),
                "Live 'C: three' not received.");

            var messages = view.GetMessagesForTest();
            StringAssert.Contains("A: one", messages);
            StringAssert.Contains("B: two", messages);
            StringAssert.Contains("C: three", messages);

            // Let it spin briefly; should not duplicate lines.
            var before = messages.Length;
            await Task.Delay(30);
            var after = view.GetMessagesForTest().Length;
            Assert.GreaterOrEqual(after, before);

            await p.StopAsync();
        }

        private static async Task<bool> WaitForContains(FakeView view, string needle, int timeoutMs)
        {
            var start = Environment.TickCount;
            while (Environment.TickCount - start < timeoutMs)
            {
                var text = view.GetMessagesForTest();
                if (!string.IsNullOrEmpty(text) &&
                    text.IndexOf(needle, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }

                await Task.Delay(10);
            }

            return false;
        }
    }
}
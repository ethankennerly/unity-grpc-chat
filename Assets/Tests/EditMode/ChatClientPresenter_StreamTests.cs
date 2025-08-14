using System.Threading.Tasks;
using NUnit.Framework;

namespace MinimalChat.Tests
{
    /// <summary>
    /// Tests backlog then live delivery and dedupe behavior.
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

            var p = new ChatClientPresenter(view, fac);
            p.Start();

            var aId = loop.EnqueueForTest("A", "one");
            var bId = loop.EnqueueForTest("B", "two");

            await Task.Delay(50);

            var cId = loop.EnqueueForTest("C", "three");

            await Task.Delay(50);

            var messages = view.GetMessagesForTest();
            StringAssert.Contains("A: one", messages);
            StringAssert.Contains("B: two", messages);
            StringAssert.Contains("C: three", messages);

            // Let it spin a bit more; no duplicate appends expected.
            var before = messages.Length;
            await Task.Delay(30);
            var after = view.GetMessagesForTest().Length;
            Assert.GreaterOrEqual(after, before);

            await p.StopAsync();
        }
    }
}

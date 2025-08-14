using System.Threading.Tasks;
using NUnit.Framework;

namespace MinimalChat.Tests
{
    /// <summary>
    /// Tests sending flow: validation, append, and input clearing.
    /// </summary>
    public sealed class ChatClientPresenter_SendTests
    {
        [Test]
        public async Task Send_Click_Appends_And_Clears()
        {
            var view = new FakeView();
            var loop = new FakeService();
            var remote = new FakeService();
            var fac = new FakeFactory(loop, remote);

            view.SetDisplayName("Alice");
            view.SetMessageInputForTest("Hello");
            view.SetLoopbackForTest(true);

            var p = new ChatClientPresenter(view, fac);
            p.Start();

            view.ClickSendForTest();

            await Task.Delay(10);

            Assert.AreEqual(1, loop.SendCount);
            var messages = view.GetMessagesForTest();
            StringAssert.Contains("Alice: Hello", messages);
            Assert.AreEqual(string.Empty, view.GetMessageInput());
        }

        [Test]
        public async Task Send_Invalid_Is_Rejected()
        {
            var view = new FakeView();
            var loop = new FakeService();
            var remote = new FakeService();
            var fac = new FakeFactory(loop, remote);

            view.SetDisplayName("Bob");
            view.SetMessageInputForTest(""); // invalid
            view.SetLoopbackForTest(true);

            var p = new ChatClientPresenter(view, fac);
            p.Start();

            view.ClickSendForTest();

            await Task.Delay(10);

            Assert.AreEqual(0, loop.SendCount);
            var messages = view.GetMessagesForTest();
            Assert.AreEqual(string.Empty, messages);
        }
    }
}

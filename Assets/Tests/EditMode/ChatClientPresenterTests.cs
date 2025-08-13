using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MinimalChat.Tests
{
    public sealed class StubView : IChatView
    {
        public string Name = "Tester";
        public string Input = "hello world";
        public string Output = "";
        public string Warn = "";
        public bool Loopback = true;

        public event Action SendClicked;
        public event Action LoopbackChanged;

        public string GetDisplayName() { return Name; }
        public string GetMessageInput() { return Input; }
        public void SetDisplayName(string value) { Name = value; }
        public void ClearMessageInput() { Input = ""; }
        public bool IsLoopbackEnabled() { return Loopback; }
        public void SetMessages(string text) { Output = text; }
        public void ShowWarning(string text) { Warn = text; }

        public void ClickSend()
        {
            if (SendClicked != null)
            {
                SendClicked();
            }
        }

        public void ToggleLoopback()
        {
            if (LoopbackChanged != null)
            {
                LoopbackChanged();
            }
        }
    }

    public sealed class TestFactory : IChatServiceFactory
    {
        public IChatService Create(bool useLoopback)
        {
            if (useLoopback)
            {
                return new LoopbackChatService();
            }

            throw new NotImplementedException("Remote unused in this test.");
        }
    }

    public sealed class ChatClientPresenterTests
    {
        [Test]
        public async Task Presenter_Loopback_Send_Renders_Line()
        {
            var view = new StubView();
            var factory = new TestFactory();
            var presenter = new ChatClientPresenter(view, factory);

            presenter.Start();

            view.ClickSend();

            var ok = await WaitUntilAsync(
                () => view.Output.IndexOf("Tester: hello world", StringComparison.Ordinal) >= 0,
                1000
            );

            Assert.IsTrue(ok, "Expected output to contain the sent message.");
            Assert.AreEqual("", view.Input, "Expected input cleared.");

            await presenter.StopAsync();
        }

        private static async Task<bool> WaitUntilAsync(Func<bool> cond, int timeoutMs)
        {
            var start = Environment.TickCount;

            while (Environment.TickCount - start < timeoutMs)
            {
                if (cond())
                {
                    return true;
                }

                await Task.Delay(10);
            }

            return cond();
        }
    }
}

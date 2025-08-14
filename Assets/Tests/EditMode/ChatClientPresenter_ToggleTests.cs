using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MinimalChat.Tests
{
    /// <summary>
    /// Tests loopback toggle switches service instances (with small waits to avoid races).
    /// </summary>
    public sealed class ChatClientPresenter_ToggleTests
    {
        [Test]
        public async Task Toggle_Loopback_Switches_Service()
        {
            var view = new FakeView();
            var loop = new FakeService();
            var remote = new FakeService();
            var fac = new FakeFactory(loop, remote);

            view.SetLoopbackForTest(true);

            var p = new ChatClientPresenter(view, fac);
            p.Start();

            // Wait until initial Create(true) has happened.
            Assert.IsTrue(await WaitFor(() => fac.CreateLoopCount >= 1, 200),
                "Presenter did not create loopback service in time.");

            // Now toggle to remote and wait for Create(false).
            view.ToggleLoopbackForTest(false);

            Assert.IsTrue(await WaitFor(() => fac.CreateRemoteCount >= 1, 300),
                "Presenter did not create remote service after toggle.");

            await p.StopAsync();
        }

        private static async Task<bool> WaitFor(Func<bool> cond, int timeoutMs)
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

            return false;
        }
    }
}
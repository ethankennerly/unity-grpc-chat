using System.Threading.Tasks;
using NUnit.Framework;

namespace MinimalChat.Tests
{
    /// <summary>
    /// Tests loopback toggle switches service instances.
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

            view.ToggleLoopbackForTest(false);

            await Task.Delay(10);

            Assert.GreaterOrEqual(fac.CreateLoopCount, 1);
            Assert.GreaterOrEqual(fac.CreateRemoteCount, 1);

            await p.StopAsync();
        }
    }
}

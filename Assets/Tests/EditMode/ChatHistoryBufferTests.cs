using NUnit.Framework;

namespace MinimalChat.Tests
{
    /// <summary>
    /// Tests ChatHistoryBuffer capacity and snapshot order.
    /// </summary>
    public sealed class ChatHistoryBufferTests
    {
        [Test]
        public void Append_RespectsCapacity_DropsOldest()
        {
            var h = new ChatHistoryBuffer();
            h.Capacity = 3;

            h.Append("a");
            h.Append("b");
            h.Append("c");
            h.Append("d");

            var snap = h.BuildSnapshot();
            StringAssert.DoesNotContain("a\n", snap);
            StringAssert.Contains("b\n", snap);
            StringAssert.Contains("c\n", snap);
            StringAssert.Contains("d\n", snap);
        }

        [Test]
        public void BuildSnapshot_OrdersLines()
        {
            var h = new ChatHistoryBuffer();
            h.Capacity = 4;

            h.Append("one");
            h.Append("two");
            h.Append("three");

            var snap = h.BuildSnapshot();
            var idxOne = snap.IndexOf("one\n");
            var idxTwo = snap.IndexOf("two\n");
            var idxThree = snap.IndexOf("three\n");

            Assert.GreaterOrEqual(idxTwo, 0);
            Assert.GreaterOrEqual(idxThree, 0);
            Assert.Greater(idxTwo, idxOne);
            Assert.Greater(idxThree, idxTwo);
        }
    }
}

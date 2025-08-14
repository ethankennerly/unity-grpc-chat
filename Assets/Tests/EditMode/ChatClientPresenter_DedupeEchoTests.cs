using System.Threading.Tasks;
using NUnit.Framework;

namespace MinimalChat.Tests
{
    /// <summary>
    /// Verifies presenter does not duplicate the user's own message:
    /// local echo + later stream delivery should produce ONE line.
    /// </summary>
    public sealed class ChatClientPresenter_DedupeEchoTests
    {
        [Test]
        public async Task LocalEcho_Is_Not_Duplicated_When_Stream_Delivers_Same_Message()
        {
            var view = new FakeView();
            var loop = new FakeService();   // streams every sent message
            var remote = new FakeService();
            var fac = new FakeFactory(loop, remote);

            view.SetDisplayName("Alice");
            view.SetLoopbackForTest(true);

            var p = new ChatClientPresenter(view, fac);
            p.Start();

            // Type and send one message
            view.SetMessageInputForTest("Hello");
            view.ClickSendForTest();

            // Give a moment for: Send → local echo, then stream to deliver same msg
            await Task.Delay(80);

            var messages = view.GetMessagesForTest();

            // Expect exactly one "Alice: Hello" line (no duplicate)
            var count = CountOccurrences(messages, "Alice: Hello");
            Assert.AreEqual(1, count, "Own message was duplicated (echo + stream).");

            await p.StopAsync();
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle))
            {
                return 0;
            }

            var count = 0;
            var idx = 0;

            while (true)
            {
                idx = haystack.IndexOf(needle, idx, System.StringComparison.Ordinal);
                if (idx < 0)
                {
                    break;
                }

                count = count + 1;
                idx = idx + needle.Length;
            }

            return count;
        }
    }
}

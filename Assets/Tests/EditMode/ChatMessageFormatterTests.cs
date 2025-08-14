using System;
using NUnit.Framework;

namespace MinimalChat.Tests
{
    /// <summary>
    /// Tests ChatMessageFormatter output.
    /// </summary>
    public sealed class ChatMessageFormatterTests
    {
        [Test]
        public void FormatOutgoing_UsesLocalTimeAndLayout()
        {
            var f = new ChatMessageFormatter();
            var when = new DateTime(2025, 1, 2, 3, 4, 0, DateTimeKind.Local);
            var s = f.FormatOutgoing("Alice", "Hello", when);
            // Expected like: [03:04] Alice: Hello
            StringAssert.StartsWith("[03:04] Alice: Hello", s);
        }

        [Test]
        public void FormatIncoming_ParsesIsoAndLayouts()
        {
            var f = new ChatMessageFormatter();
            var iso = "2025-01-02T03:04:05.0000000Z";
            var s = f.FormatIncoming("Bob", "Hi", iso);
            // Localize hour; we only assert prefix structure is correct.
            StringAssert.StartsWith("[", s);
            StringAssert.Contains("] Bob: Hi", s);
        }
    }
}

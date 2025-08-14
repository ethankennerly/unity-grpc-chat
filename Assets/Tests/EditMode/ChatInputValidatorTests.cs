using NUnit.Framework;

namespace MinimalChat.Tests
{
    /// <summary>
    /// Tests ChatInputValidator rules.
    /// </summary>
    public sealed class ChatInputValidatorTests
    {
        [Test]
        public void CanSend_ValidAsciiUnderLimit_ReturnsTrue()
        {
            var v = new ChatInputValidator();
            var ok = v.CanSend("Alice", "Hello");
            Assert.IsTrue(ok);
        }

        [Test]
        public void CanSend_EmptyNameOrText_ReturnsFalse()
        {
            var v = new ChatInputValidator();
            Assert.IsFalse(v.CanSend("", "Hi"));
            Assert.IsFalse(v.CanSend("Bob", ""));
        }

        [Test]
        public void CanSend_TooLong_ReturnsFalse()
        {
            var v = new ChatInputValidator();
            var buf = new char[1025];
            for (var i = 0; i < buf.Length; i++)
            {
                buf[i] = 'x';
            }

            var text = new string(buf);
            var ok = v.CanSend("Bob", text);
            Assert.IsFalse(ok);
        }

        [Test]
        public void CanSend_NonAscii_ReturnsFalse()
        {
            var v = new ChatInputValidator();
            var ok1 = v.CanSend("Alíce", "Hello");
            var ok2 = v.CanSend("Alice", "caf\u00E9");
            Assert.IsFalse(ok1);
            Assert.IsFalse(ok2);
        }
    }
}

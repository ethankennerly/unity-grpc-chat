using System;

namespace MinimalChat.Tests
{
    /// <summary>
    /// Test-only view fake. No Unity types. Minimal surface to drive presenter.
    /// </summary>
    internal sealed class FakeView : IChatView
    {
        private string _displayName = "Player0000";
        private string _input = string.Empty;
        private string _messages = string.Empty;
        private bool _loopback;

        public event Action SendClicked;
        public event Action LoopbackChanged;

        public string GetDisplayName()
        {
            return _displayName;
        }

        public void SetDisplayName(string value)
        {
            _displayName = value ?? string.Empty;
        }

        public string GetMessageInput()
        {
            return _input;
        }

        public void SetMessageInputForTest(string value)
        {
            _input = value ?? string.Empty;
        }

        public void ClearMessageInput()
        {
            _input = string.Empty;
        }

        public void SetMessages(string text)
        {
            _messages = text ?? string.Empty;
        }

        public string GetMessagesForTest()
        {
            return _messages;
        }

        public bool IsLoopbackEnabled()
        {
            return _loopback;
        }

        public void SetLoopbackForTest(bool value)
        {
            _loopback = value;
        }

        public void ClickSendForTest()
        {
            var h = SendClicked;
            if (h != null)
            {
                h();
            }
        }

        public void ToggleLoopbackForTest(bool value)
        {
            _loopback = value;
            var h = LoopbackChanged;
            if (h != null)
            {
                h();
            }
        }
    }
}

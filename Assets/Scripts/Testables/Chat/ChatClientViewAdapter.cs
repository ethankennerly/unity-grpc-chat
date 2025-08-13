using UnityEngine;
using UnityEngine.UI;

namespace MinimalChat
{
    /// <summary>
    /// Minimal view adapter. Forwards UI to presenter; no business logic here.
    /// </summary>
    public sealed class ChatClientViewAdapter : MonoBehaviour, IChatView
    {
        [SerializeField] private InputField _displayNameInput;
        [SerializeField] private InputField _messageInput;
        [SerializeField] private Button _sendButton;
        [SerializeField] private Toggle _loopbackToggle;
        [SerializeField] private Text _messagesText;

        private ChatClientPresenter _presenter;
        private readonly IChatServiceFactory _factory = new LoopbackOrRemoteFactory();

        public event System.Action SendClicked;
        public event System.Action LoopbackChanged;

        private void Awake()
        {
            if (_sendButton != null)
            {
                _sendButton.onClick.AddListener(() =>
                {
                    if (SendClicked != null)
                    {
                        SendClicked();
                    }
                });
            }

            if (_loopbackToggle != null)
            {
                _loopbackToggle.onValueChanged.AddListener(_ =>
                {
                    if (LoopbackChanged != null)
                    {
                        LoopbackChanged();
                    }
                });
            }
        }

        private void OnEnable()
        {
            _presenter = new ChatClientPresenter(this, _factory);
            _presenter.Start();
        }

        private async void OnDisable()
        {
            if (_presenter == null)
            {
                return;
            }

            await _presenter.StopAsync();
            _presenter = null;
        }

        public string GetDisplayName()
        {
            if (_displayNameInput == null)
            {
                return "";
            }

            return _displayNameInput.text;
        }

        public string GetMessageInput()
        {
            if (_messageInput == null)
            {
                return "";
            }

            return _messageInput.text;
        }

        public void SetDisplayName(string value)
        {
            if (_displayNameInput == null)
            {
                return;
            }

            _displayNameInput.text = value;
        }

        public void ClearMessageInput()
        {
            if (_messageInput == null)
            {
                return;
            }

            _messageInput.text = "";
        }

        public bool IsLoopbackEnabled()
        {
            if (_loopbackToggle == null)
            {
                return true;
            }

            return _loopbackToggle.isOn;
        }

        public void SetMessages(string text)
        {
            if (_messagesText == null)
            {
                return;
            }

            _messagesText.text = text;
        }

        public void ShowWarning(string text)
        {
            if (_messagesText == null)
            {
                return;
            }

            var cur = _messagesText.text;
            var next = cur + "\n[warn] " + text;
            _messagesText.text = next;
        }
    }
}

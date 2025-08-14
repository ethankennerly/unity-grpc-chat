using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinimalChat
{
    /// <summary>
    /// Minimal view adapter. Buffers UI updates from any thread and applies on main thread.
    /// Delegates auto-scroll to ChatAutoScroller.
    /// Includes a runtime UI toggle to force a large font for messages.
    /// </summary>
    public sealed class ChatClientViewAdapter : MonoBehaviour, IChatView
    {
        [SerializeField] private TMP_InputField _displayNameInput;
        [SerializeField] private TMP_InputField _messageInput;
        [SerializeField] private Button _sendButton;
        [SerializeField] private Toggle _loopbackToggle;
        [SerializeField] private TMP_Text _messagesText;
        [SerializeField] private ChatAutoScroller _autoScroller;

        // Debug: when true, force messages font size to 80; when false, restore original.
        [SerializeField] private bool _debugLargeFont = false;
        [SerializeField] private Toggle _debugLargeFontToggle;

        private float _originalFontSize;

        private ChatClientPresenter _presenter;
        private readonly IChatServiceFactory _factory = new LoopbackOrRemoteFactory();

        public event System.Action SendClicked;
        public event System.Action LoopbackChanged;

        private readonly object _uiGate = new object();

        private string _pendingMessages;
        private bool _hasPendingMessages;

        private string _pendingDisplayName;
        private bool _hasPendingDisplayName;

        private bool _pendingClearInput;

        private int _lastAppliedLength;

        private void Awake()
        {
            // Cache author-time font size once, before any runtime changes.
            if (_messagesText != null)
            {
                _originalFontSize = _messagesText.fontSize;
            }

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
            lock (_uiGate)
            {
                _pendingMessages = null;
                _hasPendingMessages = false;
                _pendingDisplayName = null;
                _hasPendingDisplayName = false;
                _pendingClearInput = false;
            }

            // Apply initial debug font state and wire the runtime toggle.
            ApplyDebugFontSize();

            if (_debugLargeFontToggle != null)
            {
                _debugLargeFontToggle.isOn = _debugLargeFont;
                _debugLargeFontToggle.onValueChanged.AddListener(OnDebugLargeFontChanged);
            }

            _lastAppliedLength = _messagesText != null ? _messagesText.text.Length : 0;

            _presenter = new ChatClientPresenter(this, _factory);
            _presenter.Start();
        }

        private async void OnDisable()
        {
            if (_presenter != null)
            {
                await _presenter.StopAsync();
                _presenter = null;
            }

            if (_debugLargeFontToggle != null)
            {
                _debugLargeFontToggle.onValueChanged.RemoveListener(OnDebugLargeFontChanged);
            }

            lock (_uiGate)
            {
                _pendingMessages = null;
                _hasPendingMessages = false;
                _pendingDisplayName = null;
                _hasPendingDisplayName = false;
                _pendingClearInput = false;
            }
        }

        private void Update()
        {
            ApplyPendingDisplayName();
            ApplyPendingMessages();
            ApplyPendingClearInput();
        }

        private void OnDebugLargeFontChanged(bool isOn)
        {
            _debugLargeFont = isOn;
            ApplyDebugFontSize();
        }

        private void ApplyDebugFontSize()
        {
            if (_messagesText == null)
            {
                return;
            }

            if (_debugLargeFont)
            {
                if (!Mathf.Approximately(_messagesText.fontSize, 80f))
                {
                    _messagesText.fontSize = 80f;
                }
            }
            else
            {
                if (!Mathf.Approximately(_messagesText.fontSize, _originalFontSize))
                {
                    _messagesText.fontSize = _originalFontSize;
                }
            }
        }

        private void ApplyPendingDisplayName()
        {
            if (_displayNameInput == null)
            {
                return;
            }

            string name = null;
            bool apply = false;

            lock (_uiGate)
            {
                if (_hasPendingDisplayName)
                {
                    name = _pendingDisplayName ?? string.Empty;
                    _pendingDisplayName = null;
                    _hasPendingDisplayName = false;
                    apply = true;
                }
            }

            if (!apply)
            {
                return;
            }

            _displayNameInput.text = name;
        }

        private void ApplyPendingMessages()
        {
            if (_messagesText == null)
            {
                return;
            }

            string snapshot = null;
            bool apply = false;

            lock (_uiGate)
            {
                if (_hasPendingMessages)
                {
                    snapshot = _pendingMessages ?? string.Empty;
                    _pendingMessages = null;
                    _hasPendingMessages = false;
                    apply = true;
                }
            }

            if (!apply)
            {
                return;
            }

            var prevLen = _lastAppliedLength;
            _messagesText.text = snapshot;
            _lastAppliedLength = snapshot.Length;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_messagesText.rectTransform);

            // if your Content is a parent that should grow, also:
            var contentRect = _messagesText.rectTransform.parent as RectTransform;
            if (contentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect); }

            if (_autoScroller != null && _lastAppliedLength > prevLen)
            {
                _autoScroller.RequestScrollToBottom();
            }
        }

        private void ApplyPendingClearInput()
        {
            if (_messageInput == null)
            {
                return;
            }

            if (!_pendingClearInput)
            {
                return;
            }

            _pendingClearInput = false;
            _messageInput.text = string.Empty;
        }

        public string GetDisplayName()
        {
            if (_displayNameInput == null)
            {
                return string.Empty;
            }

            return _displayNameInput.text;
        }

        public string GetMessageInput()
        {
            if (_messageInput == null)
            {
                return string.Empty;
            }

            return _messageInput.text;
        }

        public void SetDisplayName(string value)
        {
            lock (_uiGate)
            {
                _pendingDisplayName = value ?? string.Empty;
                _hasPendingDisplayName = true;
            }
        }

        public void ClearMessageInput()
        {
            _pendingClearInput = true;
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
            lock (_uiGate)
            {
                _pendingMessages = text ?? string.Empty;
                _hasPendingMessages = true;
            }
        }
    }
}
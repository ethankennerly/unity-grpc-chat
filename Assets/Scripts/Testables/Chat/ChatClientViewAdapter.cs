using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinimalChat
{
    /// <summary>
    /// Minimal view adapter. Buffers all updates and applies them on the main thread in Update().
    /// This prevents any background thread from touching Unity/TMP APIs.
    /// </summary>
    public sealed class ChatClientViewAdapter : MonoBehaviour, IChatView
    {
        [SerializeField] private TMP_InputField _displayNameInput;
        [SerializeField] private TMP_InputField _messageInput;
        [SerializeField] private Button _sendButton;
        [SerializeField] private Toggle _loopbackToggle;
        [SerializeField] private TMP_Text _messagesText;

        private ChatClientPresenter _presenter;
        private readonly IChatServiceFactory _factory = new LoopbackOrRemoteFactory();

        public event System.Action SendClicked;
        public event System.Action LoopbackChanged;

        // Gate for cross-thread buffers.
        private readonly object _uiGate = new object();

        // Pending full messages snapshot.
        private string _pendingMessages;
        private bool _hasPendingMessages;
        private bool _pendingClearInput;

        // Pending warning lines (raw, without "[warn] ").
        private readonly List<string> _pendingWarnings = new List<string>(8);

        // Pending display name set.
        private string _pendingDisplayName;
        private bool _hasPendingDisplayName;

        private void Awake()
        {
            // Wire UI events -> presenter events (main thread).
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
            // Reset buffers to avoid replaying stale warnings/snapshots.
            lock (_uiGate)
            {
                _pendingMessages = null;
                _hasPendingMessages = false;
                _pendingWarnings.Clear();
                _pendingDisplayName = null;
                _hasPendingDisplayName = false;
            }

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

            // Drop any pending UI work on disable.
            lock (_uiGate)
            {
                _pendingMessages = null;
                _hasPendingMessages = false;
                _pendingWarnings.Clear();
                _pendingDisplayName = null;
                _hasPendingDisplayName = false;
            }
        }

        private void Update()
        {
            // Apply pending display name.
            if (_displayNameInput != null)
            {
                string name = null;
                bool applyName = false;

                lock (_uiGate)
                {
                    if (_hasPendingDisplayName)
                    {
                        name = _pendingDisplayName ?? string.Empty;
                        _pendingDisplayName = null;
                        _hasPendingDisplayName = false;
                        applyName = true;
                    }
                }

                if (applyName)
                {
                    _displayNameInput.text = name;
                }
            }

            // Apply pending full messages snapshot.
            if (_messagesText != null)
            {
                string snapshot = null;
                bool applySnapshot = false;

                lock (_uiGate)
                {
                    if (_hasPendingMessages)
                    {
                        snapshot = _pendingMessages ?? string.Empty;
                        _pendingMessages = null;
                        _hasPendingMessages = false;
                        applySnapshot = true;
                    }
                }

                if (applySnapshot)
                {
                    _messagesText.text = snapshot;
                }

                // Apply queued warnings.
                System.Collections.Generic.List<string> warnings = null;

                lock (_uiGate)
                {
                    if (_pendingWarnings.Count > 0)
                    {
                        warnings = new System.Collections.Generic.List<string>(_pendingWarnings);
                        _pendingWarnings.Clear();
                    }
                }

                if (warnings != null)
                {
                    var baseText = _messagesText.text;

                    for (var i = 0; i < warnings.Count; i = i + 1)
                    {
                        var raw = warnings[i] ?? string.Empty;

                        // Prevent double "[warn]" if caller already prefixed.
                        var needsPrefix = !raw.StartsWith("[warn]");
                        var line = needsPrefix ? "[warn] " + raw : raw;

                        baseText = baseText + "\n" + line;
                    }

                    _messagesText.text = baseText;
                }
            }
        }

        /// <inheritdoc/>
        public string GetDisplayName()
        {
            if (_displayNameInput == null)
            {
                return string.Empty;
            }

            return _displayNameInput.text;
        }

        /// <inheritdoc/>
        public string GetMessageInput()
        {
            if (_messageInput == null)
            {
                return string.Empty;
            }

            return _messageInput.text;
        }

        /// <inheritdoc/>
        public void SetDisplayName(string value)
        {
            // Buffer any display name change to the main thread.
            lock (_uiGate)
            {
                _pendingDisplayName = value ?? string.Empty;
                _hasPendingDisplayName = true;
            }
        }

        /// <inheritdoc/>
        public void ClearMessageInput()
        {
            _pendingClearInput = true;
        }

        private void ClearMessageInputOnMainThread()
        {
            if (_pendingClearInput)
            {
                _pendingClearInput = false;
                _messageInput.text = string.Empty;
            }
            
            if (_messageInput == null)
            {
                return;
            }

            _messageInput.text = string.Empty;
        }

        /// <inheritdoc/>
        public bool IsLoopbackEnabled()
        {
            if (_loopbackToggle == null)
            {
                return true;
            }

            return _loopbackToggle.isOn;
        }

        /// <inheritdoc/>
        public void SetMessages(string text)
        {
            // Called from background stream; buffer for main-thread apply in Update().
            lock (_uiGate)
            {
                _pendingMessages = text ?? string.Empty;
                _hasPendingMessages = true;
            }
        }

        /// <inheritdoc/>
        public void ShowWarning(string text)
        {
            Debug.LogWarning("Chat warning: " + (text ?? "<null>"), this);
        }
    }
}
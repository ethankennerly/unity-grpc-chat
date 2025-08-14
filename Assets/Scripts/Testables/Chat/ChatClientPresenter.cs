using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace MinimalChat
{
    /// <summary>
    /// Presenter owns all logic: service switching, validation, streaming, formatting, history.
    /// </summary>
    public sealed class ChatClientPresenter
    {
        private readonly IChatView _view;
        private readonly IChatServiceFactory _factory;

        private IChatService _service;
        private CancellationTokenSource _streamCts;
        private Task _streamTask;
        private long _lastReceivedId;

        private readonly LinkedList<string> _lines = new LinkedList<string>();
        private readonly object _gate = new object();

        public int MaxMessagesToShow { get; set; } = 200;

        public ChatClientPresenter(IChatView view, IChatServiceFactory factory)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _view = view;
            _factory = factory;

            _view.SendClicked += OnSendClicked;
            _view.LoopbackChanged += OnLoopbackChanged;
        }

        /// <summary>
        /// Starts presentation. Ensures a display name and opens the initial stream.
        /// </summary>
        public void Start()
        {
            var name = _view.GetDisplayName();
            var fixedName = name == null ? "" : name.Trim();

            if (fixedName.Length == 0)
            {
                var random = new Random();
                var suffix = random.Next(1000, 9999);
                var gen = "Player" + suffix.ToString();
                _view.SetDisplayName(gen);
            }

            SwitchService(force: true);
        }

        /// <summary>
        /// Stops streaming. Presenter remains reusable after Stop().
        /// </summary>
        public async Task StopAsync()
        {
            if (_streamCts == null)
            {
                return;
            }

            _streamCts.Cancel();
            _streamCts.Dispose();
            _streamCts = null;

            if (_streamTask != null)
            {
                try
                {
                    await _streamTask.ConfigureAwait(false);
                }
                catch
                {
                }

                _streamTask = null;
            }
        }

        private async void OnLoopbackChanged()
        {
            await StopAsync();
            SwitchService(force: true);
        }

        private async void OnSendClicked()
        {
            await SendFromViewAsync();
        }

        private void SwitchService(bool force)
        {
            var useLoopback = _view.IsLoopbackEnabled();

            if (!force && _service != null)
            {
                var isLoop = _service is LoopbackChatService;

                if ((useLoopback && isLoop) || (!useLoopback && !isLoop))
                {
                    return;
                }
            }

            _service = _factory.Create(useLoopback);

            _streamCts = new CancellationTokenSource();
            _streamTask = RunStreamAsync(_streamCts.Token);
        }

        private async Task RunStreamAsync(CancellationToken ct)
        {
            if (_service == null)
            {
                return;
            }

            try
            {
                var stream = _service.SubscribeMessagesAsync(_lastReceivedId, ct);

                await foreach (var msg in stream.ConfigureAwait(false))
                {
                    if (msg.Id <= _lastReceivedId)
                    {
                        continue;
                    }

                    _lastReceivedId = msg.Id;

                    var time = TryFormatTime(msg.CreatedAt);
                    var line = "[" + time + "] " + msg.Sender + ": " + msg.Text;

                    AppendAndRender(line);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                var err = "[error] " + ex.GetType().Name + ": " + ex.Message;
                AppendAndRender(err);
            }
        }

        private async Task SendFromViewAsync()
        {
            if (_service == null)
            {
                Debug.LogWarning("Service not started.");
                return;
            }

            var nameRaw = _view.GetDisplayName();
            var textRaw = _view.GetMessageInput();

            var name = nameRaw == null ? "" : nameRaw.Trim();
            var text = textRaw == null ? "" : textRaw.Trim();

            if (name.Length == 0)
            {
                Debug.LogWarning("Enter a display name.");
                return;
            }

            if (text.Length == 0)
            {
                Debug.LogWarning("Enter a message.");
                return;
            }

            if (text.Length > 1024)
            {
                Debug.LogWarning("Message exceeds 1024 characters.");
                return;
            }

            if (!IsAscii(name) || !IsAscii(text))
            {
                Debug.LogWarning("Only ASCII characters are allowed.");
                return;
            }

            var req = new SendMessageRequest
            {
                Sender = name,
                Text = text
            };

            await _service.SendMessageAsync(req, CancellationToken.None).ConfigureAwait(false);

            _view.ClearMessageInput();
        }

        private void AppendAndRender(string line)
        {
            string snapshot;

            lock (_gate)
            {
                _lines.AddLast(line);

                while (_lines.Count > MaxMessagesToShow)
                {
                    _lines.RemoveFirst();
                }

                var cap = MaxMessagesToShow * 64;

                if (cap > 4096)
                {
                    cap = 4096;
                }

                var sb = new StringBuilder(cap);
                var node = _lines.First;

                while (node != null)
                {
                    var value = node.Value;
                    sb.AppendLine(value);
                    node = node.Next;
                }

                snapshot = sb.ToString();
            }

            _view.SetMessages(snapshot);
        }

        private static string TryFormatTime(string isoUtc)
        {
            if (DateTimeOffset.TryParse(isoUtc, out var dto))
            {
                return dto.ToLocalTime().ToString("HH:mm");
            }

            if (isoUtc == null)
            {
                return "";
            }

            return isoUtc;
        }

        private static bool IsAscii(string s)
        {
            if (s == null)
            {
                return false;
            }

            var i = 0;

            while (i < s.Length)
            {
                var c = s[i];

                if (c == '\n' || c == '\t')
                {
                    i = i + 1;
                    continue;
                }

                if (c < 0x20 || c > 0x7E)
                {
                    return false;
                }

                i = i + 1;
            }

            return true;
        }
    }
}

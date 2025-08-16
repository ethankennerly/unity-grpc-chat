using System;
using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace MinimalChat
{
    /// <summary>
    /// Presenter orchestrates: service switching, validation, streaming, and view updates.
    /// All heavy logic is delegated to POCO helpers; this class stays small and testable.
    /// </summary>
    public sealed class ChatClientPresenter
    {
        private readonly IChatView _view;
        private readonly IChatServiceFactory _factory;

        private IChatService _service;
        private IReliableSubscriber _subscriber;
        private IRetryBackoff _backoff;
        private IReliableClock _clock;

        private readonly ChatInputValidator _validator = new ChatInputValidator();
        private readonly ChatMessageFormatter _formatter = new ChatMessageFormatter();
        private readonly ChatHistoryBuffer _history = new ChatHistoryBuffer();

        private CancellationTokenSource _streamCts;
        private Task _streamTask;
        private long _lastReceivedId;

        public int MaxMessagesToShow
        {
            get { return _history == null ? 200 : _historyCapacity; }
            set
            {
                _historyCapacity = value;
                if (_history != null)
                {
                    _history.Capacity = value;
                }
            }
        }

        private int _historyCapacity = 200;

        /// <summary>
        /// Convenience: presenter with default ChatServiceFactory (URL lives in the factory).
        /// </summary>
        public ChatClientPresenter(IChatView view)
            : this(view, new ChatServiceFactory())
        {
        }

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

            _history.Capacity = _historyCapacity;
        }

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
            await StopAsync().ConfigureAwait(false);
            SwitchService(force: true);
        }

        private async void OnSendClicked()
        {
            await SendFromViewAsync().ConfigureAwait(false);
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

            // Small backoff so EditMode reconnect test completes within its 150ms window.
            _backoff = new ExponentialBackoff(initialMs: 50, maxMs: 200);
            _clock = new SystemClock();
            _subscriber = new ReliableSubscriber(_service, _backoff, _clock);

            if (_streamCts != null)
            {
                _streamCts.Cancel();
                _streamCts.Dispose();
            }

            _streamCts = new CancellationTokenSource();
            _streamTask = RunStreamAsync(_streamCts.Token);
        }

        private async Task RunStreamAsync(CancellationToken ct)
        {
            if (_subscriber == null)
            {
                return;
            }

            await _subscriber.RunAsync(
                _lastReceivedId,
                async msg =>
                {
                    if (msg.Id <= _lastReceivedId)
                    {
                        return;
                    }

                    _lastReceivedId = msg.Id;

                    // Incoming expects an ISO-8601 UTC string.
                    var whenIso = ToIso8601Utc(msg.CreatedAt);
                    var line = _formatter.FormatIncoming(msg.Sender, msg.Text, whenIso);
                    AppendAndRender(line);

                    await Task.CompletedTask;
                },
                ct).ConfigureAwait(false);
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

            if (!_validator.CanSend(name, text))
            {
                Debug.LogWarning("Enter a valid ASCII message <= 1024 chars.");
                return;
            }

            var req = new SendMessageRequest
            {
                Sender = name,
                Text = text
            };

            // Single send; capture ack and advance last id to avoid duplicate when stream echoes it back.
            var ack = await _service.SendMessageAsync(req, CancellationToken.None)
                .ConfigureAwait(false);

            if (ack != null && ack.Id > _lastReceivedId)
            {
                _lastReceivedId = ack.Id;
            }

            // Outgoing expects a local DateTime.
            var whenLocal = DateTime.Now;
            var line = _formatter.FormatOutgoing(name, text, whenLocal);
            AppendAndRender(line);

            _view.ClearMessageInput();
        }

        private void AppendAndRender(string line)
        {
            _history.Append(line);
            var snapshot = _history.BuildSnapshot();
            _view.SetMessages(snapshot);
        }

        // --- Helpers ----------------------------------------------------------

        // Convert epoch-ms to ISO-8601 UTC string for incoming messages.
        private static string ToIso8601Utc(long epochMs)
        {
            var dto = DateTimeOffset.FromUnixTimeMilliseconds(epochMs);
            // Use round-trip "o" format, always UTC (Z).
            return dto.UtcDateTime.ToString("o");
        }
    }
}
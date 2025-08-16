using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalChat.Tests
{
    /// <summary>
    /// Shared in-memory core for test services. Thread-safe; no LINQ; no Channels.
    /// Manages messages list, subscriber queues, and id/time helpers.
    /// </summary>
    internal sealed class InMemoryChatCore
    {
        private readonly object _gate = new object();
        private readonly List<MinimalChat.ChatMessage> _messages = new List<MinimalChat.ChatMessage>(256);
        private readonly List<AsyncQueue<MinimalChat.ChatMessage>> _subs = new List<AsyncQueue<MinimalChat.ChatMessage>>(8);
        private long _nextId = 1;

        public MinimalChat.ChatMessage AppendAndBroadcast(string sender, string text)
        {
            MinimalChat.ChatMessage msg;
            lock (_gate)
            {
                long id = _nextId;
                _nextId++;

                long nowMs = NowMs();

                msg = new MinimalChat.ChatMessage
                {
                    Id = id,
                    Sender = sender,
                    Text = text,
                    CreatedAt = nowMs
                };

                _messages.Add(msg);

                foreach (AsyncQueue<MinimalChat.ChatMessage> sub in _subs)
                {
                    sub.Enqueue(msg);
                }
            }

            return msg;
        }

        public void EnqueueExternal(MinimalChat.ChatMessage msg)
        {
            if (msg == null)
            {
                throw new ArgumentNullException(nameof(msg));
            }

            lock (_gate)
            {
                if (msg.Id <= 0)
                {
                    msg.Id = _nextId;
                    _nextId++;
                }

                if (msg.CreatedAt <= 0)
                {
                    msg.CreatedAt = NowMs();
                }

                _messages.Add(msg);

                foreach (AsyncQueue<MinimalChat.ChatMessage> sub in _subs)
                {
                    sub.Enqueue(msg);
                }
            }
        }

        public void SnapshotBacklog(long sinceId, List<MinimalChat.ChatMessage> dest)
        {
            if (dest == null)
            {
                throw new ArgumentNullException(nameof(dest));
            }

            lock (_gate)
            {
                foreach (MinimalChat.ChatMessage m in _messages)
                {
                    if (m.Id > sinceId)
                    {
                        dest.Add(m);
                    }
                }
            }
        }

        public AsyncQueue<MinimalChat.ChatMessage> AddSubscriber()
        {
            var q = new AsyncQueue<MinimalChat.ChatMessage>();
            lock (_gate)
            {
                _subs.Add(q);
            }
            return q;
        }

        public void RemoveSubscriber(AsyncQueue<MinimalChat.ChatMessage> q)
        {
            lock (_gate)
            {
                for (int i = 0; i < _subs.Count; i++)
                {
                    if (ReferenceEquals(_subs[i], q))
                    {
                        _subs.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static void ValidateAscii(string value)
        {
            if (value == null)
            {
                throw new ArgumentException("Value cannot be null.");
            }

            foreach (char c in value)
            {
                if (c == '\n' || c == '\t')
                {
                    continue;
                }

                if (c < 0x20 || c > 0x7E)
                {
                    throw new ArgumentException("Only ASCII characters are allowed.");
                }
            }
        }
    }
}
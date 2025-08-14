using System.Text;
using System.Collections.Generic;

namespace MinimalChat
{
    /// <summary>
    /// Keeps last N lines and renders a snapshot string efficiently.
    /// </summary>
    public sealed class ChatHistoryBuffer
    {
        private readonly LinkedList<string> _lines = new LinkedList<string>();
        private readonly object _gate = new object();

        private int _capacity = 200;

        public int Capacity
        {
            get { return _capacity; }
            set
            {
                if (value < 1)
                {
                    value = 1;
                }

                _capacity = value;
            }
        }

        public void Append(string line)
        {
            lock (_gate)
            {
                _lines.AddLast(line);

                while (_lines.Count > _capacity)
                {
                    _lines.RemoveFirst();
                }
            }
        }

        public string BuildSnapshot()
        {
            lock (_gate)
            {
                var cap = _capacity * 64;

                if (cap > 4096)
                {
                    cap = 4096;
                }

                var sb = new StringBuilder(cap);
                var node = _lines.First;

                while (node != null)
                {
                    sb.AppendLine(node.Value);
                    node = node.Next;
                }

                return sb.ToString();
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _lines.Clear();
            }
        }
    }
}

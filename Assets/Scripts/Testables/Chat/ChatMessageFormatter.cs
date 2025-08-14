using System;

namespace MinimalChat
{
    /// <summary>
    /// Formats outgoing and incoming chat lines consistently.
    /// </summary>
    public sealed class ChatMessageFormatter
    {
        public string FormatOutgoing(string sender, string text, DateTime nowLocal)
        {
            // Build the timestamp first; then format the full line.
            var ts = "[" + nowLocal.ToString("HH:mm") + "]";
            var line = ts + " " + sender + ": " + text;
            return line;
        }

        public string FormatIncoming(string sender, string text, string createdAtIso)
        {
            var time = TryFormatTime(createdAtIso);
            var ts = "[" + time + "]";
            var line = ts + " " + sender + ": " + text;
            return line;
        }

        private static string TryFormatTime(string isoUtc)
        {
            if (isoUtc == null)
            {
                return "";
            }

            if (DateTimeOffset.TryParse(isoUtc, out var dto))
            {
                return dto.ToLocalTime().ToString("HH:mm");
            }

            return isoUtc;
        }
    }
}

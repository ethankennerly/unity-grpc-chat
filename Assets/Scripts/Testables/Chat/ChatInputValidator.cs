using System;

namespace MinimalChat
{
    /// <summary>
    /// Validates display name and message text per product rules.
    /// ASCII only (except \n, \t); length limit; early returns; no LINQ.
    /// </summary>
    public sealed class ChatInputValidator
    {
        private const int MaxLen = 1024;

        public bool CanSend(string name, string text)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (text.Length > MaxLen)
            {
                return false;
            }

            if (!IsAscii(name))
            {
                return false;
            }

            if (!IsAscii(text))
            {
                return false;
            }

            return true;
        }

        public static bool IsAscii(string s)
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

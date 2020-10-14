using System;
using System.Text;

namespace GrouperLib.Database
{
    static class Helpers
    {
        public static bool IEquals(this string value, string other)
        {
            // This deviates from standard string.Equals which will throw if value is null
            if (value == null)
            {
                return false;
            }
            return value.Equals(other, StringComparison.OrdinalIgnoreCase);
        }

        public static string NullIfEmpty(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            return str;
        }

        public static string TranslateWildcard(string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                return null;
            }
            StringBuilder sb = new StringBuilder(inputString.Length * 2);
            bool escapeMode = false;
            char escapeChar = '\\';
            foreach (char c in inputString)
            {
                if (c == escapeChar)
                {
                    if (escapeMode)
                    {
                        sb.Append(c);
                    }
                }
                else if (c == '*')
                {
                    if (escapeMode)
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        sb.Append('%');
                    }
                }
                else if (c == '?')
                {
                    if (escapeMode)
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        sb.Append('_');
                    }
                }
                else if (c == '%' || c == '_' || c == '[' || c == ']')
                {
                    sb.Append('[');
                    sb.Append(c);
                    sb.Append(']');
                }
                else
                {
                    sb.Append(c);
                }
                escapeMode = !escapeMode && c == escapeChar;
            }
            return sb.ToString();
        }
    }
}

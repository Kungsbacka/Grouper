using System.Text;

namespace GrouperLib.Database;

static class Helpers
{
    public static bool IEquals(this string? value, string other)
    {
        // This deviates from standard string.Equals which will throw if the input string is null
        return value != null && value.Equals(other, StringComparison.OrdinalIgnoreCase);
    }

    public static string? NullIfEmpty(this string? str)
    {
        return string.IsNullOrEmpty(str) ? null : str;
    }

    public static string? TranslateWildcard(string? inputString)
    {
        if (string.IsNullOrEmpty(inputString))
        {
            return null;
        }
        StringBuilder sb = new(inputString.Length * 2);
        bool escapeMode = false;
        const char escapeChar = '\\';
        foreach (char c in inputString)
        {
            switch (c)
            {
                case escapeChar:
                {
                    if (escapeMode)
                    {
                        sb.Append(c);
                    }

                    break;
                }
                case '*' when escapeMode:
                    sb.Append(c);
                    break;
                case '*':
                    sb.Append('%');
                    break;
                case '?' when escapeMode:
                    sb.Append(c);
                    break;
                case '?':
                    sb.Append('_');
                    break;
                case '%':
                case '_':
                case '[':
                case ']':
                    sb.Append('[');
                    sb.Append(c);
                    sb.Append(']');
                    break;
                default:
                    sb.Append(c);
                    break;
            }

            escapeMode = !escapeMode && c == escapeChar;
        }
        return sb.ToString();
    }
}
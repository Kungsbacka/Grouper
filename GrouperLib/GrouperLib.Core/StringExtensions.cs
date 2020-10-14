using System;

namespace GrouperLib.Core
{
    static class StringExtensions
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
    }
}

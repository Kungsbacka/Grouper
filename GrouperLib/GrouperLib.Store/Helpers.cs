using System;

namespace GrouperLib.Store
{
    internal static class Helpers
    {
        public static bool IEquals(this string left, string right)
        {
            if (left is null)
            {
                return false;
            }
            return left.Equals(right, StringComparison.OrdinalIgnoreCase);
        }
    }
}

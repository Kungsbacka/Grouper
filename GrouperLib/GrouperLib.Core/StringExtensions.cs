namespace GrouperLib.Core;

static class StringExtensions
{
    public static bool IEquals(this string? value, string? other)
    {
        // This deviates from standard string.Equals which will throw if value is null
        return value != null && value.Equals(other, StringComparison.OrdinalIgnoreCase);
    }
}
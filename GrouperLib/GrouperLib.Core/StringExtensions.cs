namespace GrouperLib.Core;

static class StringExtensions
{
    public static bool IEquals(this string? value, string? other) =>
        string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
}

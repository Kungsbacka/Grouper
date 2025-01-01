using System.DirectoryServices.Protocols;

namespace GrouperLib.Store;

internal static class SearchResultEntryExtensions
{
    public static string? GetAsString(this SearchResultEntry? entry, string attributeName)
    {
        DirectoryAttribute? attribute = entry?.Attributes[attributeName];
        if (attribute is not { Count: 1 })
        {
            return null;
        }
        return attribute.GetValues(typeof(string))[0] as string;
    }

    public static Guid? GetAsGuid(this SearchResultEntry entry, string attributeName)
    {
        DirectoryAttribute? attribute = entry?.Attributes[attributeName];
        if (attribute is not { Count: 1 })
        {
            return null;
        }
        object[] value = attribute.GetValues(typeof(byte[]));
        if (value.Length == 1 && value[0] is byte[] bytes)
        {
            return new Guid(bytes);
        }
        return null;
    }
}
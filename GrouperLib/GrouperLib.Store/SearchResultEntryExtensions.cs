using System.DirectoryServices.Protocols;
using System.Security.Cryptography;
using System.Security.Principal;

namespace GrouperLib.Store
{
    internal static class SearchResultEntryExtensions
    {
        public static string? GetAsString(this SearchResultEntry entry, string attributeName)
        {
            if (entry == null)
            {
                return null;
            }
            DirectoryAttribute attribute = entry.Attributes[attributeName];
            if (entry.Attributes[attributeName] == null || entry.Attributes[attributeName].Count != 1)
            {
                return null;
            }
            return (string)attribute.GetValues(typeof(string))[0];
        }

        public static Guid? GetAsGuid(this SearchResultEntry entry, string attributeName)
        {
            if (entry == null)
            {
                return null;
            }
            DirectoryAttribute attribute = entry.Attributes[attributeName];
            if (entry.Attributes[attributeName] == null || entry.Attributes[attributeName].Count != 1)
            {
                return null;
            }
            object[]? value = attribute.GetValues(typeof(byte[]));
            if (value.Length == 1 && value[0] is byte[] bytes)
            {
                return new Guid(bytes);
            }
            return null;
        }
    }
}

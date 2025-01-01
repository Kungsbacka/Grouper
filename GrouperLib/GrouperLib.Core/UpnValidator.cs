using GrouperLib.Language;
using System.Text.RegularExpressions;

namespace GrouperLib.Core;

internal partial class UpnValidator : ICustomValidator
{
    private static readonly System.Buffers.SearchValues<char> invalidCharsInUserName = System.Buffers.SearchValues.Create("!@#$%^&*()+=[]{}\\/|;:\"<>?,");
    private static readonly Regex upnRegex = UpnRegex();
    
    public void Validate(GrouperDocument document, GrouperDocumentMember documentMember, List<ValidationError> validationErrors)
    {
        foreach (GrouperDocumentRule rule in documentMember.Rules)
        {
            if (rule.Name.IEquals("Upn") && !IsUpnValid(rule.Value))
            {
                validationErrors.Add(new ValidationError(nameof(rule.Value), ResourceString.ValidationErrorInvalidUpn, rule.Value));
            }
        }
    }

    // Reference: https://social.technet.microsoft.com/wiki/contents/articles/52250.active-directory-user-principal-name.aspx
    private static bool IsUpnValid(string? upn)
    {
        ArgumentNullException.ThrowIfNull(upn);
        string[] parts = upn.Split('@');
        if (parts.Length != 2)
        {
            return false;
        }
        string userName = parts[0];
        string domain = parts[1];
        if (userName.Length == 0 || domain.Length == 0)
        {
            return false;
        }
        if (userName.StartsWith('.') || userName.StartsWith('-') || userName.EndsWith('.') || userName.EndsWith('-'))
        {
            return false;
        }
        if (userName.Contains(".."))
        {
            return false;
        }
        if (userName.AsSpan().IndexOfAny(invalidCharsInUserName) > -1)
        {
            return false;
        }
        return upnRegex.Matches(domain).Count == 1;
    }

    [GeneratedRegex(@"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-zA-Z][-0-9a-zA-Z]*[0-9a-zA-Z]*\.)+[0-9a-zA-Z][-0-9a-zA-Z]{0,22}[0-9a-zA-Z]))$", RegexOptions.Compiled)]
    private static partial Regex UpnRegex();
}
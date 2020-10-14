using GrouperLib.Language;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GrouperLib.Core
{
    class UpnValidator : ICustomValidator
    {
        private readonly Regex domainRegex;
        private readonly char[] invalidCharsInUserName;

        public UpnValidator()
        {
            invalidCharsInUserName = "!@#$%^&*()+=[]{}\\/|;:\"<>?,".ToCharArray();
            domainRegex = new Regex(@"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-zA-Z][-0-9a-zA-Z]*[0-9a-zA-Z]*\.)+[0-9a-zA-Z][-0-9a-zA-Z]{0,22}[0-9a-zA-Z]))$",
                RegexOptions.Compiled);
        }

        public void Validate(DeserializedDocument deserializedDocument, DeserializedMember deserializedMember, List<ValidationError> validationErrors)
        {
            foreach (DeserializedRule rule in deserializedMember.Rules)
            {
                if (rule.Name.IEquals("Upn"))
                {
                    if (!IsUpnValid(rule.Value))
                    {
                        validationErrors.Add(new ValidationError(nameof(rule.Value), ResourceString.ValidationErrorInvalidUpn, rule.Value));
                    }
                }
            }
        }

        // Reference: https://social.technet.microsoft.com/wiki/contents/articles/52250.active-directory-user-principal-name.aspx
        private bool IsUpnValid(string upn)
        {
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
            if (userName.StartsWith(".") || userName.StartsWith("-") || userName.EndsWith(".") || userName.EndsWith("-"))
            {
                return false;
            }
            if (userName.Contains(".."))
            {
                return false;
            }
            if (userName.IndexOfAny(invalidCharsInUserName) > -1)
            {
                return false;
            }
            return domainRegex.Matches(domain).Count == 1;
        }
    }
}

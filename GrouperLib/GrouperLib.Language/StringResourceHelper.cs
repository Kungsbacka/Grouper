using System;
using System.Linq;

namespace GrouperLib.Language
{
    public class StringResourceHelper : IStringResourceHelper
    {
        public void SetLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang))
            {
                return;
            }
            System.Globalization.CultureInfo ci = new(lang);
            System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
        }

        public string GetString(string resourceId, params object[]? args)
        {
            _ = resourceId ?? throw new ArgumentNullException(nameof(resourceId));
            string? text = Resources.ResourceManager.GetString(resourceId)
                ?? throw new ArgumentException($"No string resource found with id {resourceId}.", nameof(resourceId));
            if (args != null)
            {
                text = string.Format(text, args.Select(o => { return o ?? "<NULL>"; }).ToArray());
            }
            return text;
        }

        public string GetString(string resourceId)
        {
            return GetString(resourceId, null);
        }
    }
}

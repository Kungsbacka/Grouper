using System;
using System.Linq;
using System.Xml;

namespace GrouperLib.Language
{
    public static class LanguageHelper
    {
        static LanguageHelper()
        {
            Resources.ResourceManager.IgnoreCase = true;
        }

        public static void SetLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang))
            {
                return;
            }
            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo(lang);
            System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
        }

        public static string GetErrorText(string errorId, params object[] args)
        {
            if (errorId == null)
            {
                throw new ArgumentNullException(nameof(errorId));
            }
            string text = Resources.ResourceManager.GetString(errorId);
            if (text == null)
            {
                throw new ArgumentException(nameof(errorId), "No error text found for error id");
            }
            if (args != null)
            {
                text = string.Format(text, args.Select(o => {return o ?? "<NULL>"; }).ToArray());
            }
            return text;
        }
    }
}

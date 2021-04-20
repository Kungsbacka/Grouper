using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace GrouperLib.Config
{
    public class GrouperConfiguration
    {
        public enum Role { GroupStore, MemberSource, GroupOwnerSource }

        public Role[] AzureAdRole { get; set; }
        public string AzureAdClientSecret { get; set; }
        public bool AzureAdClientSecretIsDpapiProtected { get; set; }
        public string AzureAdClientId { get; set; }
        public string AzureAdTenantId { get; set; }

        public Role[] ExchangeRole { get; set; }
        public string ExchangeUserName { get; set; }
        public string ExchangePassword { get; set; }
        public bool ExchangePasswordIsDpapiProtected { get; set; }

        public Role[] OnPremAdRole { get; set; }
        public string OnPremAdUserName { get; set; }
        public string OnPremAdPassword { get; set; }
        public bool OnPremAdPasswordIsDpapiProtected { get; set; }

        public string MemberDatabaseConnectionString { get; set; }
        public string DocumentDatabaseConnectionString { get; set; }
        public string LogDatabaseConnectionString { get; set; }
        public string OpenEDatabaseConnectionString { get; set; }

        public double ChangeRatioLowerLimit { get; set; }

        public bool AzureAdHasRole(Role role) => AzureAdRole.Any(r => r.Equals(role));
        public bool ExchangeHasRole(Role role) => ExchangeRole.Any(r => r.Equals(role));
        public bool OnPremAdHasRole(Role role) => OnPremAdRole.Any(r => r.Equals(role));

        public static GrouperConfiguration CreateFromHashtable(Hashtable hashtable)
        {
            GrouperConfiguration config = new GrouperConfiguration();
            foreach (PropertyInfo propertyInfo in config.GetType().GetProperties())
            {
                if (hashtable.ContainsKey(propertyInfo.Name))
                {
                    object value = hashtable[propertyInfo.Name];
                    if (propertyInfo.PropertyType == typeof(Role[]))
                    {
                        value = ((string[])value).Select(str => (Role)Enum.Parse(typeof(Role), str)).ToArray();
                    }
                    propertyInfo.SetValue(config, value);
                }
            }
            return config;
        }

        public static GrouperConfiguration CreateFromAppSettings(NameValueCollection appSettings)
        {
            GrouperConfiguration config = new GrouperConfiguration();
            foreach (PropertyInfo propertyInfo in config.GetType().GetProperties())
            {
                string value = appSettings[propertyInfo.Name];
                if (value != null)
                {
                    if (propertyInfo.PropertyType == typeof(bool))
                    {
                        propertyInfo.SetValue(config, bool.Parse(value));
                    }
                    else if (propertyInfo.PropertyType == typeof(double))
                    {
                        propertyInfo.SetValue(config, double.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else if (propertyInfo.PropertyType == typeof(Role[]))
                    {
                        propertyInfo.SetValue(config,
                            value.Split(',').Select(t => (Role)Enum.Parse(typeof(Role), t.Trim())).ToArray()
                        );
                    }
                    else
                    {
                        propertyInfo.SetValue(config, value);
                    }
                }
            }
            return config;
        }

        public static string UnprotectString(string protectedString)
        {
            byte[] secureBytes = Convert.FromBase64String(protectedString);
            byte[] unprotectedBytes = ProtectedData.Unprotect(secureBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.Unicode.GetString(unprotectedBytes);
        }

        public static string GetSensitiveString(string sensitive, bool isProtected)
        {
            if (isProtected)
            {
                return UnprotectString(sensitive);
            }
            return sensitive;
        }
    }
}

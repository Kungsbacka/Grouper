using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace GrouperLib.Config
{
    public class GrouperConfiguration
    {
        private readonly HashSet<string> _dpapiProtectedSettings = new HashSet<string>();
        private string azureAdClientSecret;
        private string azureAdClientId;
        private string azureAdTenantId;
        private string exchangeUserName;
        private string exchangePassword;
        private string onPremAdUserName;
        private string onPremAdPassword;
        private string memberDatabaseConnectionString;
        private string documentDatabaseConnectionString;
        private string logDatabaseConnectionString;
        private string openEDatabaseConnectionString;


        public string[] DpapiProtectedSettings
        {
            get
            {
                return _dpapiProtectedSettings.ToArray();
            }
            set
            {
                _dpapiProtectedSettings.Clear();
                _dpapiProtectedSettings.UnionWith(value);
            }
        }

        public enum Role { GroupStore, MemberSource, GroupOwnerSource }

        public Role[] AzureAdRole { get; set; }

        public string AzureAdClientSecret { get => Unprotect(nameof(AzureAdClientSecret), azureAdClientSecret); set => azureAdClientSecret = value; }
        public string AzureAdClientId { get => Unprotect(nameof(AzureAdClientId), azureAdClientId); set => azureAdClientId = value; }
        public string AzureAdTenantId { get => Unprotect(nameof(AzureAdTenantId), azureAdTenantId); set => azureAdTenantId = value; }

        public Role[] ExchangeRole { get; set; }
        public string ExchangeUserName { get => Unprotect(nameof(ExchangeUserName), exchangeUserName); set => exchangeUserName = value; }
        public string ExchangePassword { get => Unprotect(nameof(ExchangePassword), exchangePassword); set => exchangePassword = value; }

        public Role[] OnPremAdRole { get; set; }
        public string OnPremAdUserName { get => Unprotect(nameof(OnPremAdUserName), onPremAdUserName); set => onPremAdUserName = value; }
        public string OnPremAdPassword { get => Unprotect(nameof(OnPremAdPassword), onPremAdPassword); set => onPremAdPassword = value; }

        public string MemberDatabaseConnectionString { get => Unprotect(nameof(MemberDatabaseConnectionString), memberDatabaseConnectionString); set => memberDatabaseConnectionString = value; }
        public string DocumentDatabaseConnectionString { get => Unprotect(nameof(DocumentDatabaseConnectionString), documentDatabaseConnectionString); set => documentDatabaseConnectionString = value; }
        public string LogDatabaseConnectionString { get => Unprotect(nameof(LogDatabaseConnectionString), logDatabaseConnectionString); set => logDatabaseConnectionString = value; }
        public string OpenEDatabaseConnectionString { get => Unprotect(nameof(OpenEDatabaseConnectionString), openEDatabaseConnectionString); set => openEDatabaseConnectionString = value; }

        public double ChangeRatioLowerLimit { get; set; }

        public bool AzureAdHasRole(Role role) => AzureAdRole.Any(r => r.Equals(role));
        public bool ExchangeHasRole(Role role) => ExchangeRole.Any(r => r.Equals(role));
        public bool OnPremAdHasRole(Role role) => OnPremAdRole.Any(r => r.Equals(role));
        
        private string Unprotect(string setting, string value)
        {
            if (!string.IsNullOrEmpty(value) && _dpapiProtectedSettings.Contains(setting))
            {
                byte[] secureBytes = Convert.FromBase64String(value);
                byte[] unprotectedBytes = ProtectedData.Unprotect(secureBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.Unicode.GetString(unprotectedBytes);
            }
            return value;
        }

        public static GrouperConfiguration CreateFromHashtable(Hashtable hashtable)
        {
            GrouperConfiguration config = new GrouperConfiguration();
            foreach (PropertyInfo propertyInfo in config.GetType().GetProperties(BindingFlags.Public))
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
            if (hashtable.ContainsKey("DpapiProtectedSettings"))
            {
                config._dpapiProtectedSettings.UnionWith((string[])hashtable["DpapiProtectedSettings"]);
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
            string protectedSettings = appSettings["DpapiProtectedSettings"];
            if (protectedSettings != null)
            {
                config._dpapiProtectedSettings.UnionWith(protectedSettings.Split(','));
            }
            return config;
        }
    }
}

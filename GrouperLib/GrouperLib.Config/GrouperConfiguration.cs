using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace GrouperLib.Config
{
    public class GrouperConfiguration
    {
        private readonly HashSet<string> _dpapiProtectedSettings = new HashSet<string>();
        private string azureAdClientSecret;
        private string azureAdClientId;
        private string azureAdTenantId;
        private string azureAdCertificateFilePath;
        private string azureAdCertificatePassword;
        private string azureAdCertificateThumbprint;
        private string azureAdCertificateAsBase64;
        private string exchangeOrganization;
        private string exchangeAppId;
        private string exchangeCertificateFilePath;
        private string exchangeCertificatePassword;
        private string exchangeCertificateThumbprint;
        private string exchangeCertificateAsBase64;
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

        public string AzureAdCertificatePassword { get => Unprotect(nameof(AzureAdCertificatePassword), azureAdCertificatePassword); set => azureAdCertificatePassword = value; }
        public string AzureAdCertificateFilePath { get => Unprotect(nameof(AzureAdCertificateFilePath), azureAdCertificateFilePath); set => azureAdCertificateFilePath = value; }
        public string AzureAdCertificateThumbprint { get => Unprotect(nameof(AzureAdCertificateThumbprint), azureAdCertificateThumbprint); set => azureAdCertificateThumbprint = value; }
        public StoreLocation? AzureAdCertificateStoreLocation { get; set; }
        public string AzureAdCertificateAsBase64 { get => Unprotect(nameof(AzureAdCertificateAsBase64), azureAdCertificateAsBase64); set => azureAdCertificateAsBase64 = value; }

        public Role[] ExchangeRole { get; set; }
        public string ExchangeOrganization { get => Unprotect(nameof(ExchangeOrganization), exchangeOrganization); set => exchangeOrganization = value; }
        public string ExchangeAppId { get => Unprotect(nameof(ExchangeAppId), exchangeAppId); set => exchangeAppId = value; }
        public string ExchangeCertificatePassword { get => Unprotect(nameof(ExchangeCertificatePassword), exchangeCertificatePassword); set => exchangeCertificatePassword = value; }
        public string ExchangeCertificateFilePath { get => Unprotect(nameof(ExchangeCertificateFilePath), exchangeCertificateFilePath); set => exchangeCertificateFilePath = value; }
        public string ExchangeCertificateThumbprint { get => Unprotect(nameof(ExchangeCertificateThumbprint), exchangeCertificateThumbprint); set => exchangeCertificateThumbprint = value; }
        public StoreLocation? ExchangeCertificateStoreLocation { get; set; }
        public string ExchangeCertificateAsBase64 { get => Unprotect(nameof(ExchangeCertificateAsBase64), exchangeCertificateAsBase64); set => exchangeCertificateAsBase64 = value; }


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

        private static bool IsNullableEnum(Type t)
        {
            Type u = Nullable.GetUnderlyingType(t);
            return (u != null) && u.IsEnum;
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
                    else if (IsNullableEnum(propertyInfo.PropertyType))
                    {
                        propertyInfo.SetValue(config, Enum.Parse(Nullable.GetUnderlyingType(propertyInfo.PropertyType), value));
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

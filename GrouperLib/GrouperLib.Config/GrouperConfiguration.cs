using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace GrouperLib.Config;

[SupportedOSPlatform("windows")]
public class GrouperConfiguration
{
    private readonly HashSet<string> _dpapiProtectedSettings = new();
    private string? azureAdClientSecret;
    private string? azureAdClientId;
    private string? azureAdTenantId;
    private string? azureAdCertificateFilePath;
    private string? azureAdCertificatePassword;
    private string? azureAdCertificateThumbprint;
    private string? azureAdCertificateAsBase64;

    private string? exoClientSecret;
    private string? exoClientId;
    private string? exoTenantId;
    private string? exoCertificateFilePath;
    private string? exoCertificatePassword;
    private string? exoCertificateThumbprint;
    private string? exoCertificateAsBase64;

    private string? onPremAdUserName;
    private string? onPremAdPassword;
    private string? memberDatabaseConnectionString;
    private string? documentDatabaseConnectionString;
    private string? logDatabaseConnectionString;
    private string? openEDatabaseConnectionString;

    public string[] DpapiProtectedSettings
    {
        get => _dpapiProtectedSettings.ToArray();
        set
        {
            if (value is null) return;
            _dpapiProtectedSettings.Clear();
            _dpapiProtectedSettings.UnionWith(value);
        }
    }

    public enum Role { GroupStore, MemberSource, GroupOwnerSource }

    public Role[]? AzureAdRole { get; set; }

    public string? AzureAdClientSecret { get => Unprotect(nameof(AzureAdClientSecret), azureAdClientSecret); set => azureAdClientSecret = value; }
    public string? AzureAdClientId { get => Unprotect(nameof(AzureAdClientId), azureAdClientId); set => azureAdClientId = value; }
    public string? AzureAdTenantId { get => Unprotect(nameof(AzureAdTenantId), azureAdTenantId); set => azureAdTenantId = value; }

    public string? AzureAdCertificatePassword { get => Unprotect(nameof(AzureAdCertificatePassword), azureAdCertificatePassword); set => azureAdCertificatePassword = value; }
    public string? AzureAdCertificateFilePath { get => Unprotect(nameof(AzureAdCertificateFilePath), azureAdCertificateFilePath); set => azureAdCertificateFilePath = value; }
    public string? AzureAdCertificateThumbprint { get => Unprotect(nameof(AzureAdCertificateThumbprint), azureAdCertificateThumbprint); set => azureAdCertificateThumbprint = value; }
    public StoreLocation? AzureAdCertificateStoreLocation { get; set; }
    public string? AzureAdCertificateAsBase64 { get => Unprotect(nameof(AzureAdCertificateAsBase64), azureAdCertificateAsBase64); set => azureAdCertificateAsBase64 = value; }

    public Role[]? ExoRole { get; set; }
    public string? ExoClientId { get => Unprotect(nameof(ExoClientId), exoClientId); set => exoClientId = value; }
    public string? ExoTenantId { get => Unprotect(nameof(ExoTenantId), exoTenantId); set => exoTenantId = value; }
    public string? ExoClientSecret { get => Unprotect(nameof(ExoClientSecret), exoClientSecret); set => exoClientSecret = value; }
    public string? ExoCertificatePassword { get => Unprotect(nameof(ExoCertificatePassword), exoCertificatePassword); set => exoCertificatePassword = value; }
    public string? ExoCertificateFilePath { get => Unprotect(nameof(ExoCertificateFilePath), exoCertificateFilePath); set => exoCertificateFilePath = value; }
    public string? ExoCertificateThumbprint { get => Unprotect(nameof(ExoCertificateThumbprint), exoCertificateThumbprint); set => exoCertificateThumbprint = value; }
    public StoreLocation? ExoCertificateStoreLocation { get; set; }
    public string? ExoCertificateAsBase64 { get => Unprotect(nameof(ExoCertificateAsBase64), exoCertificateAsBase64); set => exoCertificateAsBase64 = value; }

    public Role[]? OnPremAdRole { get; set; }
    public string? OnPremAdUserName { get => Unprotect(nameof(OnPremAdUserName), onPremAdUserName); set => onPremAdUserName = value; }
    public string? OnPremAdPassword { get => Unprotect(nameof(OnPremAdPassword), onPremAdPassword); set => onPremAdPassword = value; }

    public string? MemberDatabaseConnectionString { get => Unprotect(nameof(MemberDatabaseConnectionString), memberDatabaseConnectionString); set => memberDatabaseConnectionString = value; }
    public string? DocumentDatabaseConnectionString { get => Unprotect(nameof(DocumentDatabaseConnectionString), documentDatabaseConnectionString); set => documentDatabaseConnectionString = value; }
    public string? LogDatabaseConnectionString { get => Unprotect(nameof(LogDatabaseConnectionString), logDatabaseConnectionString); set => logDatabaseConnectionString = value; }
    public string? OpenEDatabaseConnectionString { get => Unprotect(nameof(OpenEDatabaseConnectionString), openEDatabaseConnectionString); set => openEDatabaseConnectionString = value; }

    public double ChangeRatioLowerLimit { get; set; }

    public bool AzureAdHasRole(Role role) => AzureAdRole != null && AzureAdRole.Any(r => r.Equals(role));
    public bool ExoHasRole(Role role) => ExoRole != null && ExoRole.Any(r => r.Equals(role));
    public bool OnPremAdHasRole(Role role) => OnPremAdRole != null && OnPremAdRole.Any(r => r.Equals(role));

    private string? Unprotect(string setting, string? value)
    {
        if (string.IsNullOrEmpty(value) || !_dpapiProtectedSettings.Contains(setting))
        {
            return value;
        }

        byte[] secureBytes = Convert.FromBase64String(value);
        byte[] unprotectedBytes = ProtectedData.Unprotect(secureBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.Unicode.GetString(unprotectedBytes);
    }

    private static bool IsNullableEnum(Type type) => Nullable.GetUnderlyingType(type) is { IsEnum: true };

    public static GrouperConfiguration CreateFromHashtable(Hashtable hashtable)
    {
        GrouperConfiguration config = new();
        foreach (PropertyInfo propertyInfo in config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            object? value = hashtable[propertyInfo.Name];
            if (value != null && propertyInfo.PropertyType == typeof(Role[]))
            {
                value = ((string[])value).Select(Enum.Parse<Role>).ToArray();
            }
            propertyInfo.SetValue(config, value);
        }
        object? setting = hashtable["DpapiProtectedSettings"];
        if (setting is string[] dpapiProtected)
        {
            config._dpapiProtectedSettings.UnionWith(dpapiProtected);
        }
        return config;
    }

    public static GrouperConfiguration CreateFromAppSettings(NameValueCollection appSettings)
    {
        GrouperConfiguration config = new();
        foreach (PropertyInfo propertyInfo in config.GetType().GetProperties())
        {
            string? value = appSettings[propertyInfo.Name];
            if (value == null)
            {
                continue;
            }

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
                    value.Split(',').Where(x => !string.IsNullOrEmpty(x)).Select(x => Enum.Parse<Role>(x.Trim())).ToArray()
                );
            }
            else if (propertyInfo.PropertyType == typeof(string[]))
            {
                propertyInfo.SetValue(config, value.Split(','));
            }
            else if (IsNullableEnum(propertyInfo.PropertyType))
            {
                Type? type = Nullable.GetUnderlyingType(propertyInfo.PropertyType);
                if (type != null)
                {
                    propertyInfo.SetValue(config, Enum.Parse(type, value));
                }
            }
            else
            {
                propertyInfo.SetValue(config, value);
            }
        }
        string? protectedSettings = appSettings["DpapiProtectedSettings"];
        if (protectedSettings != null)
        {
            config._dpapiProtectedSettings.UnionWith(protectedSettings.Split(','));
        }
        return config;
    }
}
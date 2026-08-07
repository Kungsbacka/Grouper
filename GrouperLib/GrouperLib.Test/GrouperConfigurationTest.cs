using GrouperLib.Config;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace GrouperLib.Test;

/// <summary>
/// Covers <see cref="GrouperConfiguration"/>: the reflection-based binding from App.config and
/// from a PowerShell hashtable, and the per-setting DPAPI unprotection.
/// </summary>
[SupportedOSPlatform("windows")]
public class GrouperConfigurationTest
{
    private static NameValueCollection Settings(params (string Key, string Value)[] pairs)
    {
        NameValueCollection collection = [];
        foreach ((string key, string value) in pairs)
        {
            collection.Add(key, value);
        }
        return collection;
    }

    [Fact]
    public void TestStringSettingIsBound()
    {
        GrouperConfiguration config = GrouperConfiguration.CreateFromAppSettings(
            Settings(("AzureAdClientId", "not-a-guid-but-not-validated-here")));

        Assert.Equal("not-a-guid-but-not-validated-here", config.AzureAdClientId);
    }

    [Fact]
    public void TestAbsentSettingsLeaveDefaults()
    {
        GrouperConfiguration config = GrouperConfiguration.CreateFromAppSettings(Settings());

        Assert.Null(config.AzureAdClientId);
        Assert.Null(config.AzureAdRole);
        Assert.Null(config.AzureAdCertificateStoreLocation);
        Assert.Equal(0.0, config.ChangeRatioLowerLimit);
    }

    [Fact]
    public void TestUnknownSettingsAreIgnored()
    {
        Exception? exception = Record.Exception(() => GrouperConfiguration.CreateFromAppSettings(
            Settings(("NoSuchSetting", "whatever"), ("AzureAdClientId", "id"))));

        Assert.Null(exception);
    }

    [Fact]
    public void TestDoubleIsBound()
    {
        GrouperConfiguration config = GrouperConfiguration.CreateFromAppSettings(
            Settings(("ChangeRatioLowerLimit", "0.5")));

        Assert.Equal(0.5, config.ChangeRatioLowerLimit);
    }

    /// <summary>
    /// The double is parsed with the invariant culture, so a dot-decimal config value keeps its
    /// meaning on a machine whose locale uses a decimal comma. Without this, "0.5" would parse as
    /// 5 under sv-SE and the change-ratio guard would block every update.
    /// </summary>
    [Fact]
    public void TestDoubleIsParsedWithInvariantCultureUnderACommaDecimalLocale()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
            GrouperConfiguration config = GrouperConfiguration.CreateFromAppSettings(
                Settings(("ChangeRatioLowerLimit", "0.5")));

            Assert.Equal(0.5, config.ChangeRatioLowerLimit);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void TestRoleArrayIsSplit()
    {
        GrouperConfiguration config = GrouperConfiguration.CreateFromAppSettings(
            Settings(("AzureAdRole", "GroupStore,MemberSource,GroupOwnerSource")));

        Assert.Equal(
            [GrouperConfiguration.Role.GroupStore, GrouperConfiguration.Role.MemberSource, GrouperConfiguration.Role.GroupOwnerSource],
            config.AzureAdRole);
    }

    [Fact]
    public void TestRoleArrayToleratesWhitespaceAndEmptyEntries()
    {
        GrouperConfiguration config = GrouperConfiguration.CreateFromAppSettings(
            Settings(("ExoRole", "GroupStore, MemberSource,")));

        Assert.Equal(
            [GrouperConfiguration.Role.GroupStore, GrouperConfiguration.Role.MemberSource],
            config.ExoRole);
    }

    [Fact]
    public void TestUnknownRoleNameThrows()
    {
        Assert.Throws<ArgumentException>(() => GrouperConfiguration.CreateFromAppSettings(
            Settings(("AzureAdRole", "NotARole"))));
    }

    [Fact]
    public void TestNullableEnumIsBound()
    {
        GrouperConfiguration config = GrouperConfiguration.CreateFromAppSettings(
            Settings(("AzureAdCertificateStoreLocation", "LocalMachine")));

        Assert.Equal(StoreLocation.LocalMachine, config.AzureAdCertificateStoreLocation);
    }

    [Fact]
    public void TestStringArrayIsSplit()
    {
        GrouperConfiguration config = GrouperConfiguration.CreateFromAppSettings(
            Settings(("DpapiProtectedSettings", "AzureAdClientSecret,OnPremAdPassword")));

        Assert.Equal(["AzureAdClientSecret", "OnPremAdPassword"], config.DpapiProtectedSettings.Order().ToArray());
    }

    [Theory]
    [InlineData("GroupStore", GrouperConfiguration.Role.GroupStore, true)]
    [InlineData("GroupStore", GrouperConfiguration.Role.MemberSource, false)]
    public void TestHasRoleReflectsTheConfiguredRoles(string configured, GrouperConfiguration.Role probe, bool expected)
    {
        GrouperConfiguration config = GrouperConfiguration.CreateFromAppSettings(
            Settings(("AzureAdRole", configured), ("ExoRole", configured), ("OnPremAdRole", configured)));

        Assert.Equal(expected, config.AzureAdHasRole(probe));
        Assert.Equal(expected, config.ExoHasRole(probe));
        Assert.Equal(expected, config.OnPremAdHasRole(probe));
    }

    [Fact]
    public void TestHasRoleIsFalseWhenNoRolesAreConfigured()
    {
        GrouperConfiguration config = new();

        Assert.False(config.AzureAdHasRole(GrouperConfiguration.Role.GroupStore));
        Assert.False(config.ExoHasRole(GrouperConfiguration.Role.GroupStore));
        Assert.False(config.OnPremAdHasRole(GrouperConfiguration.Role.GroupStore));
    }

    /// <summary>A setting not listed in DpapiProtectedSettings is returned exactly as supplied.</summary>
    [Fact]
    public void TestUnprotectedSettingIsReturnedVerbatim()
    {
        GrouperConfiguration config = new() { AzureAdClientSecret = "plain-text-secret" };

        Assert.Equal("plain-text-secret", config.AzureAdClientSecret);
    }

    /// <summary>
    /// A listed setting is DPAPI-decrypted on read. Round-trips through the real
    /// <see cref="ProtectedData"/> API in CurrentUser scope, matching how tools/ProtectString.ps1
    /// produces the value: UTF-16 bytes, protected, then base64.
    /// </summary>
    [Fact]
    public void TestProtectedSettingIsDecryptedOnRead()
    {
        const string secret = "correct horse battery staple";
        string encrypted = Convert.ToBase64String(
            ProtectedData.Protect(Encoding.Unicode.GetBytes(secret), null, DataProtectionScope.CurrentUser));

        GrouperConfiguration config = new()
        {
            DpapiProtectedSettings = ["AzureAdClientSecret"],
            AzureAdClientSecret = encrypted
        };

        Assert.Equal(secret, config.AzureAdClientSecret);
    }

    [Fact]
    public void TestProtectedSettingThatIsEmptyIsLeftAlone()
    {
        GrouperConfiguration config = new()
        {
            DpapiProtectedSettings = ["AzureAdClientSecret"],
            AzureAdClientSecret = ""
        };

        Assert.Equal("", config.AzureAdClientSecret);
    }

    [Fact]
    public void TestDpapiProtectedSettingsSetterReplacesRatherThanAccumulates()
    {
        GrouperConfiguration config = new() { DpapiProtectedSettings = ["First"] };
        config.DpapiProtectedSettings = ["Second"];

        Assert.Equal(["Second"], config.DpapiProtectedSettings);
    }

    [Fact]
    public void TestCreateFromHashtableBindsStringsAndRoles()
    {
        Hashtable hashtable = new()
        {
            { "AzureAdClientId", "client-id" },
            { "AzureAdRole", new[] { "GroupStore", "MemberSource" } },
            { "ChangeRatioLowerLimit", 0.75 },
            { "DpapiProtectedSettings", new[] { "AzureAdClientSecret" } }
        };

        GrouperConfiguration config = GrouperConfiguration.CreateFromHashtable(hashtable);

        Assert.Equal("client-id", config.AzureAdClientId);
        Assert.Equal(
            [GrouperConfiguration.Role.GroupStore, GrouperConfiguration.Role.MemberSource],
            config.AzureAdRole);
        Assert.Equal(0.75, config.ChangeRatioLowerLimit);
        Assert.Equal(["AzureAdClientSecret"], config.DpapiProtectedSettings);
    }

    [Fact]
    public void TestCreateFromHashtableWithSparseHashtable()
    {
        Hashtable hashtable = new() { { "AzureAdClientId", "client-id" } };

        GrouperConfiguration config = GrouperConfiguration.CreateFromHashtable(hashtable);

        Assert.Equal("client-id", config.AzureAdClientId);
        Assert.Equal(0.0, config.ChangeRatioLowerLimit);
        Assert.Empty(config.DpapiProtectedSettings);
    }
}

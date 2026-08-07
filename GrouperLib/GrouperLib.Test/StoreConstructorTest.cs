using GrouperLib.Config;
using GrouperLib.Core;
using GrouperLib.Store;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;

namespace GrouperLib.Test;

/// <summary>
/// Covers the configuration validation each store adapter performs in its constructor. All of it
/// runs before any network, directory or database access, so a misconfiguration fails at startup
/// with a clear message instead of at the first group update.
///
/// This is the practical value: <c>Grouper.CreateFromConfig</c> builds these from App.config or
/// appsettings.json, so these are the errors an operator sees after a bad deployment.
/// </summary>
[SupportedOSPlatform("windows")]
public class StoreConstructorTest
{
    private const string ValidGuid = "11111111-2222-3333-4444-555555555555";
    private const string OtherGuid = "66666666-7777-8888-9999-aaaaaaaaaaaa";

    // ---- Exo -----------------------------------------------------------------------------

    [Theory]
    [InlineData("not-a-guid", ValidGuid)]
    [InlineData(ValidGuid, "not-a-guid")]
    [InlineData("", ValidGuid)]
    public void TestExoRejectsNonGuidTenantOrClientId(string tenantId, string clientId)
    {
        Assert.Throws<ArgumentException>(() => new Exo(tenantId, clientId, "secret"));
    }

    [Fact]
    public void TestExoAcceptsClientSecret()
    {
        using Exo exo = new(ValidGuid, OtherGuid, "secret");

        Assert.Equal([GroupStore.Exo], exo.GetSupportedGroupStores());
        Assert.Equal([GroupMemberSource.ExoGroup], exo.GetSupportedGrouperMemberSources());
    }

    /// <summary>
    /// Exactly one credential source must be configured. Zero means nothing to authenticate with;
    /// more than one is ambiguous, and silently preferring one would hide a config mistake.
    /// </summary>
    [Fact]
    public void TestExoRejectsNoCredentialSource()
    {
        GrouperConfiguration config = new() { ExoTenantId = ValidGuid, ExoClientId = OtherGuid };

        Assert.Throws<InvalidOperationException>(() => new Exo(config));
    }

    [Fact]
    public void TestExoRejectsMoreThanOneCredentialSource()
    {
        GrouperConfiguration config = new()
        {
            ExoTenantId = ValidGuid,
            ExoClientId = OtherGuid,
            ExoClientSecret = "secret",
            ExoCertificateThumbprint = "ABCDEF"
        };

        Assert.Throws<InvalidOperationException>(() => new Exo(config));
    }

    [Fact]
    public void TestExoRejectsCertificateFileWithoutPassword()
    {
        GrouperConfiguration config = new()
        {
            ExoTenantId = ValidGuid,
            ExoClientId = OtherGuid,
            ExoCertificateFilePath = @"C:\nonexistent\cert.pfx"
        };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => new Exo(config));
        Assert.Contains(nameof(config.ExoCertificatePassword), ex.Message);
    }

    /// <summary>A thumbprint identifies a certificate in a store, so the store location is required.</summary>
    [Fact]
    public void TestExoRejectsThumbprintWithoutStoreLocation()
    {
        GrouperConfiguration config = new()
        {
            ExoTenantId = ValidGuid,
            ExoClientId = OtherGuid,
            ExoCertificateThumbprint = "ABCDEF0123456789"
        };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => new Exo(config));
        Assert.Contains(nameof(config.ExoCertificateStoreLocation), ex.Message);
    }

    [Theory]
    [InlineData(null, OtherGuid)]
    [InlineData(ValidGuid, null)]
    public void TestExoRejectsMissingTenantOrClientIdFromConfig(string? tenantId, string? clientId)
    {
        GrouperConfiguration config = new()
        {
            ExoTenantId = tenantId,
            ExoClientId = clientId,
            ExoClientSecret = "secret"
        };

        Assert.Throws<ArgumentException>(() => new Exo(config));
    }

    // ---- AzureAd -------------------------------------------------------------------------

    [Theory]
    [InlineData("not-a-guid", ValidGuid)]
    [InlineData(ValidGuid, "not-a-guid")]
    public void TestAzureAdRejectsNonGuidTenantOrClientId(string tenantId, string clientId)
    {
        Assert.Throws<ArgumentException>(() => new AzureAd(tenantId, clientId, "secret"));
    }

    [Fact]
    public void TestAzureAdAcceptsClientSecretAndReportsItsRoles()
    {
        AzureAd azureAd = new(ValidGuid, OtherGuid, "secret");

        Assert.Equal([GroupStore.AzureAd], azureAd.GetSupportedGroupStores());
        Assert.Equal([GroupMemberSource.AzureAdGroup], azureAd.GetSupportedGrouperMemberSources());
    }

    [Fact]
    public void TestAzureAdRejectsNoCredentialSource()
    {
        GrouperConfiguration config = new() { AzureAdTenantId = ValidGuid, AzureAdClientId = OtherGuid };

        Assert.Throws<InvalidOperationException>(() => new AzureAd(config));
    }

    [Fact]
    public void TestAzureAdRejectsMoreThanOneCredentialSource()
    {
        GrouperConfiguration config = new()
        {
            AzureAdTenantId = ValidGuid,
            AzureAdClientId = OtherGuid,
            AzureAdClientSecret = "secret",
            AzureAdCertificateAsBase64 = "AAAA"
        };

        Assert.Throws<InvalidOperationException>(() => new AzureAd(config));
    }

    [Fact]
    public void TestAzureAdRejectsThumbprintWithoutStoreLocation()
    {
        GrouperConfiguration config = new()
        {
            AzureAdTenantId = ValidGuid,
            AzureAdClientId = OtherGuid,
            AzureAdCertificateThumbprint = "ABCDEF0123456789"
        };

        Assert.Throws<InvalidOperationException>(() => new AzureAd(config));
    }

    /// <summary>
    /// A thumbprint that matches nothing in the named store is an ArgumentException from the
    /// certificate lookup, not a silent null credential.
    /// </summary>
    [Fact]
    public void TestAzureAdRejectsThumbprintThatMatchesNoCertificate()
    {
        GrouperConfiguration config = new()
        {
            AzureAdTenantId = ValidGuid,
            AzureAdClientId = OtherGuid,
            AzureAdCertificateThumbprint = "0000000000000000000000000000000000000000",
            AzureAdCertificateStoreLocation = StoreLocation.CurrentUser
        };

        Assert.Throws<ArgumentException>(() => new AzureAd(config));
    }

    // ---- OpenE ---------------------------------------------------------------------------

    [Fact]
    public void TestOpenERejectsNullConnectionString()
    {
        Assert.Throws<ArgumentNullException>(() => new OpenE((string)null!));
    }

    [Fact]
    public void TestOpenERejectsMissingConnectionStringInConfig()
    {
        Assert.Throws<InvalidOperationException>(() => new OpenE(new GrouperConfiguration()));
    }

    [Fact]
    public void TestOpenEIsAGroupStoreOnly()
    {
        OpenE openE = new("Server=nowhere;Database=none;");

        Assert.Equal([GroupStore.OpenE], openE.GetSupportedGroupStores());
    }

    // ---- OnPremAd ------------------------------------------------------------------------

    /// <summary>An explicit user name must carry its domain, since it becomes a NetworkCredential.</summary>
    [Theory]
    [InlineData("nodomain")]
    [InlineData(@"too\many\parts")]
    public void TestOnPremAdRejectsUserNameWithoutASingleDomainSeparator(string userName)
    {
        Assert.Throws<ArgumentException>(() => new OnPremAd(userName, "password"));
    }

    [Fact]
    public void TestOnPremAdAcceptsDomainQualifiedUserName()
    {
        OnPremAd onPremAd = new(@"DOMAIN\serviceaccount", "password");

        Assert.Equal([GroupStore.OnPremAd], onPremAd.GetSupportedGroupStores());
    }

    /// <summary>
    /// No credentials means integrated authentication as the process identity, which is the gMSA
    /// deployment case -- so it must construct rather than reject.
    /// </summary>
    [Fact]
    public void TestOnPremAdWithoutCredentialsUsesIntegratedAuthentication()
    {
        OnPremAd onPremAd = new();

        Assert.Equal(
            [GroupMemberSource.OnPremAdGroup, GroupMemberSource.OnPremAdQuery],
            onPremAd.GetSupportedGrouperMemberSources());
    }

    [Fact]
    public void TestOnPremAdIgnoresPartialCredentials()
    {
        // Only one half supplied, so it falls back to integrated auth rather than throwing.
        Exception? exception = Record.Exception(() => new OnPremAd("DOMAIN\\user", null));

        Assert.Null(exception);
    }
}

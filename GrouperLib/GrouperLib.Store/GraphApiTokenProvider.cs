using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace GrouperLib.Store
{
    internal class GraphApiTokenProvider : IAccessTokenProvider
    {
        private readonly IConfidentialClientApplication _confidentialClientApp;
        private static readonly string[] scopes = new[] { "https://graph.microsoft.com/.default" };

        public DateTimeOffset TokenExpiresOn { get; private set; }

        private static Uri GetAuthorityUri(string tenantId)
        {
            return new Uri($"https://login.microsoftonline.com/{tenantId}");
        }

        public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = default,
            CancellationToken cancellationToken = default)
        {
            var authenticationResult = await _confidentialClientApp.AcquireTokenForClient(scopes).ExecuteAsync(cancellationToken);
            TokenExpiresOn = authenticationResult.ExpiresOn;
            return authenticationResult.AccessToken;
        }

        public AllowedHostsValidator AllowedHostsValidator { get; }

        public GraphApiTokenProvider(string tenantId, string clientId, string clientSecret)
        {
            _ = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
            _confidentialClientApp = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                .WithClientSecret(clientSecret)
                .Build();
            AllowedHostsValidator = CreateAllowedHostValidator();
        }

        public GraphApiTokenProvider(string tenantId, string clientId, X509Certificate2 certificate)
        {
            _ = certificate ?? throw new ArgumentNullException(nameof(certificate));
            _confidentialClientApp = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(GetAuthorityUri(tenantId))
                .WithCertificate(certificate)
                .Build();
            AllowedHostsValidator = CreateAllowedHostValidator();
        }

        private static AllowedHostsValidator CreateAllowedHostValidator()
        {
            return new AllowedHostsValidator();
        }
    }
}

using GrouperLib.Config;
using GrouperLib.Core;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GrouperLib.Store
{
    public class AzureAd : IMemberSource, IGroupStore, IGroupOwnerSource
    {
        readonly string _clientId;
        readonly string _clientSecret;
        X509Certificate2 _certificate;
        readonly Uri _authorityUri;
        DateTimeOffset _tokenExpiresOn;
        GraphServiceClient _graphClient;

        private static readonly Regex guidRegex = new Regex(
            "'(?<guid>[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12})'",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        // public AzureAd(string tenantId, string clientId, string clientSecret)
        // public AzureAd(string tenantId, string clientId, string clientSecret)


        public AzureAd(string tenantId, string clientId, string clientSecret)
        {
            ValidateCommonParameters(tenantId, clientId);
            _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
            _clientId = clientId;
            _authorityUri = new Uri($"https://login.microsoftonline.com/{tenantId}");
        }

        public AzureAd(string tenantId, string clientId, X509Certificate2 certificate)
        {
            ValidateCommonParameters(tenantId, clientId);
            _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
            _clientId = clientId;
            _authorityUri = new Uri($"https://login.microsoftonline.com/{tenantId}");
        }

        private void ValidateCommonParameters(string tenantId, string clientId)
        {
            if (!Guid.TryParse(tenantId, out _))
            {
                throw new ArgumentException(nameof(tenantId), "Argument is not a valid GUID.");
            }
            if (!Guid.TryParse(clientId, out _))
            {
                throw new ArgumentException(nameof(clientId), "Argument is not a valid GUID.");
            }
        }

        public AzureAd(GrouperConfiguration config)
        {
            string tenantId = config.AzureAdTenantId;
            string clientId = config.AzureAdClientId;
            ValidateCommonParameters(tenantId, clientId);


            string clientSecret = config.AzureAdClientSecret;
            string certificateFilePath = config.AzureAdCertificateFilePath;
            string certificateThumbprint = config.AzureAdCertificateThumbprint;
            string certificateAsBase64 = config.AzureAdCertificateAsBase64;
            string certificatePassword = "";

            int num = (clientSecret is null ? 0 : 1)
                    + (certificateFilePath is null ? 0 : 1)
                    + (certificateThumbprint is null ? 0 : 1)
                    + (certificateAsBase64 is null ? 0 : 1);

            if (num != 1)
            {
                throw new ArgumentException(
                    $"You must specify one of {nameof(config.AzureAdClientSecret)}, {nameof(config.AzureAdCertificateFilePath)}, {nameof(config.AzureAdCertificateThumbprint)} or {nameof(config.AzureAdCertificateAsBase64)} in the configuration"
                );
            }

            if (clientSecret is not null)
            {
                _clientSecret = clientSecret;
                return;
            }

            if (certificateFilePath is not null || certificateAsBase64 is not null)
            {
                certificatePassword = config.AzureAdCertificatePassword;
                if (certificatePassword == null)
                {
                    throw new ArgumentNullException(nameof(config.AzureAdCertificatePassword));
                }
            }

            if (certificateFilePath is not null)
            {
                _certificate = Helpers.GetCertificateFromFile(certificateFilePath, certificatePassword);
                return;
            }

            if (certificateThumbprint is not null)
            {
                if (config.AzureAdCertificateStoreLocation is null)
                {
                    throw new ArgumentNullException(
                        $"If certificate is loaded from store {nameof(config.AzureAdCertificateStoreLocation)} must be specified in the configuration"
                    );
                }
                _certificate = Helpers.GetCertificateFromStore(certificateThumbprint, config.AzureAdCertificateStoreLocation.Value);
                return;
            }

            _certificate = Helpers.GetCertificateFromBase64String(certificateAsBase64, certificatePassword);
        }

        private async Task CreateGraphClient()
        {
            var confidentialClientAppBuilder = ConfidentialClientApplicationBuilder
                .Create(_clientId)
                .WithAuthority(_authorityUri);
            if (_clientSecret is not null)
            {
                confidentialClientAppBuilder = confidentialClientAppBuilder.WithClientSecret(_clientSecret);
            }
            else
            {
                confidentialClientAppBuilder = confidentialClientAppBuilder.WithCertificate(_certificate);
            }
            var confidentialClientApp = confidentialClientAppBuilder.Build();
            string[] scopes = new[] { "https://graph.microsoft.com/.default" };
            var authenticationResult = await confidentialClientApp.AcquireTokenForClient(scopes).ExecuteAsync();
            _tokenExpiresOn = authenticationResult.ExpiresOn;
            _graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(requestMessage =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", authenticationResult.AccessToken);
                    return Task.FromResult(0);
                })
            );
        }

        private async Task AssureGraphClient()
        {
            if (_graphClient == null || _tokenExpiresOn <= DateTimeOffset.Now)
            {
                await CreateGraphClient();
            }
        }

        public async Task GetGroupMembersAsync(GroupMemberCollection memberCollection, Guid groupId)
        {
            await AssureGraphClient();
            try
            {
                var members = await _graphClient.Groups[groupId.ToString()].Members.Request().GetAsync();
                do
                {
                    foreach (var member in members)
                    {
                        if (member is User u)
                        {
                            memberCollection.Add(new GroupMember(Guid.Parse(u.Id), u.UserPrincipalName, GroupMemberTypes.AzureAd));
                        }
                        else
                        {
                            memberCollection.Add(new GroupMember(Guid.Parse(member.Id), member.Id, GroupMemberTypes.AzureAd));
                        }
                    } 
                }
                while (members.NextPageRequest != null && (members = await members.NextPageRequest.GetAsync()).Count > 0);
            }
            catch (ServiceException ex)
            {
                if (IsNotFoundError(ex))
                {
                    throw GroupNotFoundException.Create(groupId, ex);
                }
                throw;
            }
        }

        public async Task AddGroupMemberAsync(GroupMember member, Guid groupId)
        {
            if (member.MemberType != GroupMemberTypes.AzureAd)
            {
                throw new ArgumentException(nameof(member), "Can only add members of type 'AzureAd'");
            }
            await AssureGraphClient();
            try
            {
                await _graphClient.Groups[groupId.ToString()].Members.References.Request().AddAsync(
                    new DirectoryObject() { Id = member.Id.ToString() }
                );
            }
            catch (ServiceException ex)
            {
                if (IsNotFoundError(ex))
                {
                    Exception notFoundException = CreateNotFoundException(groupId, member.Id, ex);
                    if (notFoundException != null)
                    {
                        throw notFoundException;
                    }
                }
                throw;
            }
        }

        public async Task RemoveGroupMemberAsync(GroupMember member, Guid groupId)
        {
            if (member.MemberType != GroupMemberTypes.AzureAd)
            {
                throw new ArgumentException(nameof(member), "Can only remove members of type 'AzureAd'");
            }
            await AssureGraphClient();
            try
            {
                await _graphClient.Groups[groupId.ToString()].Members[member.Id.ToString()].Reference.Request().DeleteAsync();
            }
            catch (ServiceException ex)
            {
                if (IsNotFoundError(ex))
                {
                    Exception notFoundException = CreateNotFoundException(groupId, member.Id, ex);
                    if (notFoundException != null)
                    {
                        throw notFoundException;
                    }
                }
                throw;
            }
        }

        public async Task GetGroupOwnersAsync(GroupMemberCollection memberCollection, Guid groupId)
        {
            await AssureGraphClient();
            try
            {
                var owners = await _graphClient.Groups[groupId.ToString()].Owners.Request().GetAsync();
                do
                {
                    foreach (var owner in owners)
                    {
                        if (owner is User u)
                        {
                            memberCollection.Add(new GroupMember(Guid.Parse(u.Id), u.UserPrincipalName, GroupMemberTypes.AzureAd));
                        }
                        else
                        {
                            memberCollection.Add(new GroupMember(Guid.Parse(owner.Id), owner.Id, GroupMemberTypes.AzureAd));
                        }
                    }
                }
                while (owners.NextPageRequest != null && (owners = await owners.NextPageRequest.GetAsync()).Count > 0);
            }
            catch (ServiceException ex)
            {
                if (IsNotFoundError(ex))
                {
                    throw GroupNotFoundException.Create(groupId, ex);
                }
                throw;
            }
        }

        private bool IsNotFoundError(ServiceException ex)
        {
            switch (ex.Error.Code)
            {
                case "Request_ResourceNotFound":
                case "ResourceNotFound":
                case "ErrorItemNotFound":
                case "itemNotFound":
                    return true;
            }
            return false;
        }

        private Exception CreateNotFoundException(Guid groupId, Guid? memberId, ServiceException ex)
        {
            Exception exception = null;
            foreach (Match match in guidRegex.Matches(ex.Error.Message))
            {
                Guid? guid = Guid.Parse(match.Groups["guid"].Value);
                if (guid == groupId)
                {
                    exception = GroupNotFoundException.Create(groupId, ex);
                    break;
                }
                if (guid == memberId)
                {
                    exception = MemberNotFoundException.Create(groupId, ex);
                    break;
                }
            }
            return exception;
        }

        public async Task GetMembersFromSourceAsync(GroupMemberCollection memberCollection, GrouperDocumentMember grouperMember, GroupMemberTypes memberType)
        {
            if (memberType != GroupMemberTypes.AzureAd)
            {
                throw new ArgumentException("Invalid member type", nameof(memberType));
            }
            await GetGroupMembersAsync(
                memberCollection,
                Guid.Parse(grouperMember.Rules.Where(r => r.Name.IEquals("Group")).First().Value)
            );
        }

        public async Task<GroupInfo> GetGroupInfoAsync(Guid groupId)
        {
            await AssureGraphClient();
            try
            {
                var group = await _graphClient.Groups[groupId.ToString()].Request().GetAsync();
                return new GroupInfo(groupId, group.DisplayName, GroupStores.AzureAd);
            }
            catch (ServiceException ex)
            {
                if (IsNotFoundError(ex))
                {
                    throw GroupNotFoundException.Create(groupId, ex);
                }
                throw;
            }
        }

        public IEnumerable<GroupMemberSources> GetSupportedGrouperMemberSources()
        {
            return new GroupMemberSources[] { GroupMemberSources.AzureAdGroup };
        }

        public IEnumerable<GroupStores> GetSupportedGroupStores()
        {
            return new GroupStores[] { GroupStores.AzureAd };
        }
    }
}

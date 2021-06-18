using GrouperLib.Config;
using GrouperLib.Core;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GrouperLib.Store
{
    public class AzureAd : IMemberSource, IGroupStore, IGroupOwnerSource
    {
        readonly string _clientId;
        readonly string _clientSecret;
        readonly Uri _authorityUri;
        DateTimeOffset _tokenExpiresOn;
        GraphServiceClient _graphClient;

        private static readonly Regex guidRegex = new Regex(
            "'(?<guid>[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12})'",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        public AzureAd(string tenantId, string clientId, string clientSecret)
        {
            if (!Guid.TryParse(tenantId, out _))
            {
                throw new ArgumentException(nameof(tenantId), "Argument is not a valid GUID.");
            }
            if (!Guid.TryParse(clientId, out _))
            {
                throw new ArgumentException(nameof(clientId), "Argument is not a valid GUID.");
            }
            if (string.IsNullOrEmpty(clientSecret))
            {
                throw new ArgumentNullException(nameof(clientSecret));
            }
            _clientId = clientId;
            _clientSecret = clientSecret;
            _authorityUri = new Uri($"https://login.microsoftonline.com/{tenantId}");
        }

        public AzureAd(GrouperConfiguration config) : this(
            tenantId: config.AzureAdTenantId,
            clientId: config.AzureAdClientId,
            clientSecret: config.AzureAdClientSecret)
        {
        }

        private async Task CreateGraphClient()
        {
            var confidentialClientApp = ConfidentialClientApplicationBuilder
                .Create(_clientId)
                .WithClientSecret(_clientSecret)
                .WithAuthority(_authorityUri)
                .Build();
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

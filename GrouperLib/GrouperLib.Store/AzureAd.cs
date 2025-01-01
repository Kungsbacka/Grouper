using Azure.Core;
using Azure.Identity;
using GrouperLib.Config;
using GrouperLib.Core;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace GrouperLib.Store;

[SupportedOSPlatform("windows")]
public sealed class AzureAd : IMemberSource, IGroupStore, IGroupOwnerSource
{
    readonly TokenCredential _tokenCredential;
    GraphServiceClient? _graphClient;

    private static readonly Regex guidRegex = new(
        "'(?<guid>[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12})'",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static void ValidateCommonParameters(string? tenantId, string? clientId)
    {
        if (!Guid.TryParse(tenantId, out _))
        {
            throw new ArgumentException("Argument is not a valid GUID.", nameof(tenantId));
        }
        if (!Guid.TryParse(clientId, out _))
        {
            throw new ArgumentException("Argument is not a valid GUID.", nameof(clientId));
        }
    }

    public AzureAd(string tenantId, string clientId, string clientSecret)
    {
        ValidateCommonParameters(tenantId, clientId);
        _tokenCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
    }

    public AzureAd(string tenantId, string clientId, X509Certificate2 certificate)
    {
        ValidateCommonParameters(tenantId, clientId);
        _tokenCredential = new ClientCertificateCredential(tenantId, clientId, certificate);
    }

    public AzureAd(GrouperConfiguration config)
    {
        string? tenantId = config.AzureAdTenantId;
        string? clientId = config.AzureAdClientId;
        ValidateCommonParameters(tenantId, clientId);

        int num = (config.AzureAdClientSecret is null ? 0 : 1)
                  + (config.AzureAdCertificateFilePath is null ? 0 : 1)
                  + (config.AzureAdCertificateThumbprint is null ? 0 : 1)
                  + (config.AzureAdCertificateAsBase64 is null ? 0 : 1);
        if (num != 1)
        {
            throw new InvalidOperationException(
                $"You must specify exactly one of {nameof(config.AzureAdClientSecret)}, {nameof(config.AzureAdCertificateFilePath)}, {nameof(config.AzureAdCertificateThumbprint)} or {nameof(config.AzureAdCertificateAsBase64)} in the configuration."
            );
        }

        if (config.AzureAdClientSecret is not null)
        {
            _tokenCredential = new ClientSecretCredential(tenantId, clientId, config.AzureAdClientSecret);
            return;
        }

        if (config.AzureAdCertificateFilePath is not null || config.AzureAdCertificateAsBase64 is not null)
        {
            if (config.AzureAdCertificatePassword is null)
            {
                throw new InvalidOperationException($"{nameof(config.AzureAdCertificatePassword)} is not set in the configuration.");
            }
            if (config.AzureAdCertificateFilePath is not null)
            {
                _tokenCredential = new ClientCertificateCredential(
                    tenantId,
                    clientId,
                    Helpers.GetCertificateFromFile(config.AzureAdCertificateFilePath, config.AzureAdCertificatePassword)
                );
                return;
            }
            if (config.AzureAdCertificateAsBase64 is not null)
            {
                _tokenCredential = new ClientCertificateCredential(
                    tenantId,
                    clientId,
                    Helpers.GetCertificateFromBase64String(config.AzureAdCertificateAsBase64, config.AzureAdCertificatePassword)
                );
                return;
            }
        }

        if (config.AzureAdCertificateThumbprint is not null)
        {
            if (config.AzureAdCertificateStoreLocation is null)
            {
                throw new InvalidOperationException(
                    $"If certificate is loaded from store {nameof(config.AzureAdCertificateStoreLocation)} must be specified in the configuration."
                );
            }
            _tokenCredential = new ClientCertificateCredential(
                tenantId,
                clientId,
                Helpers.GetCertificateFromStore(config.AzureAdCertificateThumbprint, config.AzureAdCertificateStoreLocation.Value)
            );
        }

        if (_tokenCredential is null)
        {
            throw new InvalidOperationException("No TokenCredential was created.");
        }
    }

    private GraphServiceClient GraphClient
    {
        get
        {
            _graphClient ??= new GraphServiceClient(_tokenCredential);
            return _graphClient;
        }
    }

    public async Task GetGroupMembersAsync(GroupMemberCollection memberCollection, Guid groupId)
    {
        try
        {
            var members = await GraphClient.Groups[groupId.ToString()].Members.GetAsync(config =>
            {
                config.QueryParameters.Count = true;
            });
            if (members is null)
            {
                return;
            }
            var pageIterator = PageIterator<DirectoryObject, DirectoryObjectCollectionResponse>.CreatePageIterator(
                GraphClient,
                members,
                (member) =>
                {
                    if (member is User u)
                    {
                        if (u.Id is null)
                        {
                            throw new InvalidOperationException("User id is null.");
                        }
                        if (u.UserPrincipalName is null)
                        {
                            throw new InvalidOperationException("UserPrincipalName is null.");
                        }
                        memberCollection.Add(new GroupMember(Guid.Parse(u.Id), u.UserPrincipalName, GroupMemberType.AzureAd));
                    }
                    else
                    {
                        if (member.Id is null)
                        {
                            throw new InvalidOperationException("Member id is null.");
                        }
                        memberCollection.Add(new GroupMember(Guid.Parse(member.Id), member.Id, GroupMemberType.AzureAd));
                    }

                    return true;
                },
                (req) => req
            );
            await pageIterator.IterateAsync();
        }
        catch (ODataError ex)
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
        if (member.MemberType != GroupMemberType.AzureAd)
        {
            throw new InvalidOperationException($"Can only add members of type {nameof(GroupMemberType.AzureAd)}");
        }
        var referenceCreate = new ReferenceCreate()
        {
            OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{member.Id}"
        };
        try
        {
            await GraphClient.Groups[groupId.ToString()].Members.Ref.PostAsync(referenceCreate);
        }
        catch (ODataError ex)
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
        if (member.MemberType != GroupMemberType.AzureAd)
        {
            throw new InvalidOperationException($"Can only remove members of type {nameof(GroupMemberType.AzureAd)}.");
        }
        try
        {
            await GraphClient.Groups[groupId.ToString()].Members[member.Id.ToString()].Ref.DeleteAsync();
        }
        catch (ODataError ex)
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
        try
        {
            var owners = await GraphClient.Groups[groupId.ToString()].Owners.GetAsync();
            if (owners is null)
            {
                return;
            }
            var pageIterator = PageIterator<DirectoryObject, DirectoryObjectCollectionResponse>.CreatePageIterator(
                _graphClient,
                owners,
                (owner) =>
                {
                    if (owner is User u)
                    {
                        if (u.Id is null)
                        {
                            throw new InvalidOperationException("Owner id is null.");
                        }
                        if (u.UserPrincipalName is null)
                        {
                            throw new InvalidOperationException("UserPrincipalName is null.");
                        }
                        memberCollection.Add(new GroupMember(Guid.Parse(u.Id), u.UserPrincipalName, GroupMemberType.AzureAd));
                    }
                    else
                    {
                        if (owner.Id is null)
                        {
                            throw new InvalidOperationException("Owner id is null.");
                        }
                        memberCollection.Add(new GroupMember(Guid.Parse(owner.Id), owner.Id, GroupMemberType.AzureAd));
                    }

                    return true;
                },
                (req) => req
            );
            await pageIterator.IterateAsync();
        }
        catch (ODataError ex)
        {
            if (IsNotFoundError(ex))
            {
                throw GroupNotFoundException.Create(groupId, ex);
            }
            throw;
        }
    }

    private static bool IsNotFoundError(ODataError ex)
    {
        return ex.Error?.Code switch
        {
            "Request_ResourceNotFound" or "ResourceNotFound" or "ErrorItemNotFound" or "itemNotFound" => true,
            _ => false
        };
    }

    private static Exception CreateNotFoundException(Guid groupId, Guid? memberId, ODataError ex)
    {
        if (ex.Error?.Message is not null)
        {
            foreach (Match match in guidRegex.Matches(ex.Error.Message).Cast<Match>())
            {
                Guid? guid = Guid.Parse(match.Groups["guid"].Value);
                if (guid == groupId)
                {
                    return GroupNotFoundException.Create(groupId, ex);
                }
                if (guid == memberId)
                {
                    return MemberNotFoundException.Create(groupId, ex);
                }
            }
        }
        return ex;
    }

    public async Task GetMembersFromSourceAsync(GroupMemberCollection memberCollection, GrouperDocumentMember grouperMember, GroupMemberType memberType)
    {
        if (memberType != GroupMemberType.AzureAd)
        {
            throw new InvalidOperationException($"Can only get members of type {nameof(GroupMemberType.AzureAd)}");
        }

        var value = grouperMember.Rules.First(r => r.Name.IEquals("Group")).Value;
        if (value != null)
        {
            await GetGroupMembersAsync(
                memberCollection,
                Guid.Parse(value)
            );
        }
    }

    public async Task<GroupInfo> GetGroupInfoAsync(Guid groupId)
    {
        try
        {
            var group = await GraphClient.Groups[groupId.ToString()].GetAsync();
            return new GroupInfo(groupId, group?.DisplayName ?? groupId.ToString(), GroupStore.AzureAd);
        }
        catch (ODataError ex)
        {
            if (IsNotFoundError(ex))
            {
                throw GroupNotFoundException.Create(groupId, ex);
            }
            throw;
        }
    }

    public IEnumerable<GroupMemberSource> GetSupportedGrouperMemberSources()
    {
        return [GroupMemberSource.AzureAdGroup];
    }

    public IEnumerable<GroupStore> GetSupportedGroupStores()
    {
        return [GroupStore.AzureAd];
    }
}
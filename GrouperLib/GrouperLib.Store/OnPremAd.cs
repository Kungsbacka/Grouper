using GrouperLib.Config;
using GrouperLib.Core;
using Microsoft.Extensions.Caching.Memory;
using System.DirectoryServices.Protocols;
using System.Runtime.Versioning;

namespace GrouperLib.Store;

[SupportedOSPlatform("windows")]
public class OnPremAd : IMemberSource, IGroupStore
{
    private readonly IMemoryCache _dnCache;
    private readonly Ldap _ldap;
    private readonly GroupMemberSource[] _supportedGroupMemberSources =
    [
        GroupMemberSource.OnPremAdGroup,
        GroupMemberSource.OnPremAdQuery
    ];
    private readonly GroupStore[] _supportedGroupStores = [GroupStore.OnPremAd];
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);
    private readonly string[] _memberAttributes = [ObjectGuidAttribute, UserPrincipalNameAttribute, CommonNameAttribute];
    private readonly string[] _groupInfoAttributes = [DisplayNameAttribute, CommonNameAttribute];
    private const string DistinguishedNameAttribute = "distinguishedName";
    private const string ObjectGuidAttribute = "objectGUID";
    private const string UserPrincipalNameAttribute = "userPrincipalName";
    private const string CommonNameAttribute = "cn";
    private const string DisplayNameAttribute = "displayName";
    private const string MemberAttribute = "member";

    public OnPremAd(string? userName, string? password)
    {
        // userName and password are optional
        if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
        {
            _ldap = new Ldap(userName, password);
        }
        _ldap ??= new Ldap();
        _dnCache = new MemoryCache(new MemoryCacheOptions());
    }

    public OnPremAd(GrouperConfiguration config) : this(
        userName: config.OnPremAdUserName,
        password: config.OnPremAdPassword)
    {
    }

    public OnPremAd() : this(null, null)
    {
    }

    public async Task GetGroupMembersAsync(GroupMemberCollection memberCollection, Guid groupId)
    {
        string groupDn = await GetDistinguishedNameAsync(groupId) ?? throw GroupNotFoundException.Create(groupId);
        await QueryGroupMembersAsync(memberCollection, Ldap.GetMemberOfFilter(groupDn), null);
    }

    public async Task GetMembersFromSourceAsync(GroupMemberCollection memberCollection, GrouperDocumentMember grouperMember, GroupMemberType memberType)
    {
        if (memberType != GroupMemberType.OnPremAd)
        {
            throw new InvalidOperationException($"Can only get members of type {nameof(GroupMemberType.OnPremAd)}.");
        }
        switch (grouperMember.Source)
        {
            case GroupMemberSource.OnPremAdGroup:
                var value = grouperMember.Rules.First(r => r.Name.IEquals("Group")).Value;
                if (value != null)
                {
                    await GetGroupMembersAsync(
                        memberCollection,
                        Guid.Parse(value)
                    );
                }

                break;
            case GroupMemberSource.OnPremAdQuery:
                string? filter = grouperMember.Rules.First(r => r.Name.IEquals("LdapFilter")).Value;
                string? searchBase = grouperMember.Rules.FirstOrDefault(r => r.Name.IEquals("SearchBase"))?.Value;
                await QueryGroupMembersAsync(memberCollection, filter, searchBase);
                break;
            case GroupMemberSource.Personalsystem:
            case GroupMemberSource.Elevregister:
            case GroupMemberSource.AzureAdGroup:
            case GroupMemberSource.ExoGroup:
            case GroupMemberSource.CustomView:
            case GroupMemberSource.Static:
            default:
                throw new ArgumentException("Unknown group member source.", nameof(grouperMember));
        }
    }

    public async Task AddGroupMemberAsync(GroupMember member, Guid groupId)
    {
        try
        {
            await ModifyGroupMembersAsync(member, groupId, DirectoryAttributeOperation.Add);
        }
        catch (DirectoryOperationException ex)
        {
            if (ex.Response.ResultCode == ResultCode.EntryAlreadyExists)
            {
                throw ObjectAlreadyMemberException.Create(member.Id, groupId, ex);
            }

            throw;
        }
    }

    public async Task RemoveGroupMemberAsync(GroupMember member, Guid groupId)
    {
        try
        {
            await ModifyGroupMembersAsync(member, groupId, DirectoryAttributeOperation.Delete);
        }
        catch (DirectoryOperationException ex)
        {
            if (ex.Response.ResultCode == ResultCode.UnwillingToPerform)
            {
                throw ObjectNotMemberException.Create(member.Id, groupId, ex);
            }

            throw;
        }
    }

    public async Task<GroupInfo> GetGroupInfoAsync(Guid groupId)
    {
        await foreach (SearchResultEntry entry in _ldap.SearchObjectsAsync(Ldap.GetObjectGuidFilter(groupId), _groupInfoAttributes))
        {
            string? displayName = entry.GetAsString(DisplayNameAttribute);
            displayName ??= entry.GetAsString(CommonNameAttribute);
            displayName ??= groupId.ToString();
            return new GroupInfo(groupId, displayName, GroupStore.OnPremAd);
        }
        throw GroupNotFoundException.Create(groupId);
    }

    public IEnumerable<GroupMemberSource> GetSupportedGrouperMemberSources()
    {
        return _supportedGroupMemberSources;
    }

    public IEnumerable<GroupStore> GetSupportedGroupStores()
    {
        return _supportedGroupStores;
    }

    private async Task ModifyGroupMembersAsync(GroupMember member, Guid groupId, DirectoryAttributeOperation operation)
    {
        if (operation != DirectoryAttributeOperation.Add && operation != DirectoryAttributeOperation.Delete)
        {
            throw new InvalidOperationException("Only 'add' and 'delete' are valid operations.");
        }

        if (member.MemberType != GroupMemberType.OnPremAd)
        {
            throw new InvalidOperationException($"Can only remove members of type '{nameof(GroupMemberType.OnPremAd)}'.");
        }
        string groupDn = await GetDistinguishedNameAsync(groupId) ?? throw GroupNotFoundException.Create(groupId);
        string memberDn = await GetDistinguishedNameAsync(member.Id) ?? throw MemberNotFoundException.Create(member.Id);
        ModifyRequest modifyRequest = new(
            groupDn,
            operation,
            MemberAttribute,
            memberDn
        );
        await _ldap.SendModifyRequestAsync(modifyRequest);
    }

    private async Task<string?> GetDistinguishedNameAsync(Guid objectId)
    {
        if (_dnCache.TryGetValue(objectId, out object? value))
        {
            return (string?)value!;
        }

        await foreach (var obj in _ldap.SearchObjectsAsync(Ldap.GetObjectGuidFilter(objectId), DistinguishedNameAttribute))
        {
            string dn = obj.DistinguishedName;
            _dnCache.Set(objectId, dn, _cacheExpiration);
            return dn;
        }
        return null;
    }

    private async Task QueryGroupMembersAsync(GroupMemberCollection memberCollection, string? ldapFilter, string? searchBase)
    {
        _ = ldapFilter ?? throw new ArgumentNullException(nameof(ldapFilter));
        if (searchBase == null)
        {
            (_, searchBase) = _ldap.GetServerAndDefaultNamingContext();
        }
        await foreach (SearchResultEntry entry in _ldap.GetObjectsAsync(searchBase, ldapFilter, SearchScope.Subtree, _memberAttributes))
        {               
            Guid identity = entry.GetAsGuid(ObjectGuidAttribute) ?? throw new InvalidOperationException($"Could not read the objectGUID attribute on '{entry.DistinguishedName}'");
            string? displayName = entry.GetAsString(UserPrincipalNameAttribute);
            displayName ??= entry.GetAsString(CommonNameAttribute);
            displayName ??= identity.ToString();
            memberCollection.Add(new GroupMember(identity, displayName, GroupMemberType.OnPremAd));
        }
    }
}
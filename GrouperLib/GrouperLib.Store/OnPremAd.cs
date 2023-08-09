using GrouperLib.Config;
using GrouperLib.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Logging;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace GrouperLib.Store
{
    [SupportedOSPlatform("windows")]
    public class OnPremAd : IMemberSource, IGroupStore
    {
        private readonly IMemoryCache _dnCache;
        private readonly string? _userName;
        private readonly string? _password;
        private const int CacheExpirationMinutes = 30;
        private const uint LDAP_NO_SUCH_OBJECT = 0x80072030;

        public OnPremAd(string? userName, string? password)
        {
            // userName and password are optional
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                _userName = userName;
                _password = password;
            }
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

        private DirectoryEntry GetDirectoryEntry(string? path)
        {
            DirectoryEntry directoryEntry;
            if (string.IsNullOrEmpty(path))
            {
                directoryEntry = new DirectoryEntry();
            }
            else
            {
                if (!path.StartsWith("LDAP://", StringComparison.OrdinalIgnoreCase))
                {
                    path = "LDAP://" + path;
                }
                directoryEntry = new DirectoryEntry(path);
            }
            if (_userName != null && _password != null)
            {
                directoryEntry.Username = _userName;
                directoryEntry.Password = _password;
                directoryEntry.AuthenticationType = AuthenticationTypes.Secure;
            }
            return directoryEntry;
        }

        private string? GetDistinguishedName(Guid objectId)
        {
            if (_dnCache.TryGetValue(objectId, out object? value))
            {
                return (string?)value!;
            }
            string? distinguishedName = null;
            using DirectoryEntry directoryEntry = GetDirectoryEntry($"LDAP://<GUID={objectId}>");
            try
            {
                distinguishedName = (string?)directoryEntry.Properties["distinguishedName"].Value;
                if (distinguishedName is not null)
                {
                    _dnCache.Set(objectId, distinguishedName, TimeSpan.FromMinutes(CacheExpirationMinutes));
                }
            }
            catch (DirectoryServicesCOMException ex)
            {
                if ((uint)ex.HResult != LDAP_NO_SUCH_OBJECT)
                {
                    throw;
                }
            }
            return distinguishedName;
        }

        public async Task GetGroupMembersAsync(GroupMemberCollection memberCollection, Guid groupId)
        {
            string groupDn = GetDistinguishedName(groupId) ?? throw GroupNotFoundException.Create(groupId);
            string filter = $"(memberOf={groupDn})";
            QueryGroupMembers(memberCollection, filter, null);
            await Task.FromResult(0);
        }

        private void QueryGroupMembers(GroupMemberCollection memberCollection, string ldapFilter, string? searchBase)
        {
            if (ldapFilter == null)
            {
                throw new ArgumentNullException(nameof(ldapFilter));
            }
            using DirectoryEntry searchRoot = GetDirectoryEntry(searchBase);
            using DirectorySearcher directorySearcher = new(searchRoot, ldapFilter, new string[] { "objectGUID", "userPrincipalName" });
            directorySearcher.PageSize = 1000;
            using SearchResultCollection result = directorySearcher.FindAll();
            foreach (SearchResult item in result)
            {
                Guid identity = new((byte[])item.Properties["objectGuid"][0]);
                string displayName;
                if (item.Properties["userPrincipalName"].Count == 1)
                {
                    displayName = (string)item.Properties["userPrincipalName"][0];
                }
                else
                {
                    displayName = identity.ToString();
                }
                memberCollection.Add(new GroupMember(identity, displayName, GroupMemberType.OnPremAd));
            }
        }

        public async Task AddGroupMemberAsync(GroupMember member, Guid groupId)
        {
            if (member.MemberType != GroupMemberType.OnPremAd)
            {
                throw new InvalidOperationException($"Can only add members of type {nameof(GroupMemberType.OnPremAd)}.");
            }
            string groupDn = GetDistinguishedName(groupId) ?? throw GroupNotFoundException.Create(groupId);
            string memberDn = GetDistinguishedName(member.Id) ?? throw MemberNotFoundException.Create(member.Id);
            using (DirectoryEntry directoryEntry = GetDirectoryEntry(groupDn))
            {
                try
                {
                    directoryEntry.Properties["member"].Add(memberDn);
                    directoryEntry.CommitChanges();
                }
                catch (DirectoryServicesCOMException ex)
                {
                    if ((uint)ex.HResult == LDAP_NO_SUCH_OBJECT)
                    {
                        throw GroupNotFoundException.Create(groupId, ex);
                    }
                    throw;
                }
            }
            await Task.FromResult(0);
        }

        public async Task RemoveGroupMemberAsync(GroupMember member, Guid groupId)
        {
            if (member.MemberType != GroupMemberType.OnPremAd)
            {
                throw new InvalidOperationException($"Can only remove members of type {nameof(GroupMemberType.OnPremAd)}.");
            }
            string groupDn = GetDistinguishedName(groupId) ?? throw GroupNotFoundException.Create(groupId);
            string memberDn = GetDistinguishedName(member.Id) ?? throw MemberNotFoundException.Create(member.Id);
            using (DirectoryEntry directoryEntry = GetDirectoryEntry(groupDn))
            {
                try
                {
                    directoryEntry.Properties["member"].Remove(memberDn);
                    directoryEntry.CommitChanges();
                }
                catch (DirectoryServicesCOMException ex)
                {
                    if ((uint)ex.HResult == LDAP_NO_SUCH_OBJECT)
                    {
                        throw GroupNotFoundException.Create(groupId, ex);
                    }
                    throw;
                }
            }
            await Task.FromResult(0);
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
                    await GetGroupMembersAsync(
                        memberCollection,
                        Guid.Parse(grouperMember.Rules.Where(r => r.Name.IEquals("Group")).First().Value)
                    );
                    break;
                case GroupMemberSource.OnPremAdQuery:
                    string filter = grouperMember.Rules.Where(r => r.Name.IEquals("LdapFilter")).First().Value;
                    string? searchBase = grouperMember.Rules.Where(r => r.Name.IEquals("SearchBase")).FirstOrDefault()?.Value;
                    QueryGroupMembers(memberCollection, filter, searchBase);
                    break;
                default:
                    throw new ArgumentException("Unknown group member source.", nameof(grouperMember));
            }
        }

        public async Task<GroupInfo> GetGroupInfoAsync(Guid groupId)
        {
            try
            {
                using DirectoryEntry directoryEntry = new($"LDAP://<GUID={groupId}>");
                directoryEntry.RefreshCache(new string[] { "displayName", "cn" });
                string? displayName = null;
                if (directoryEntry.Properties["displayName"].Count == 1)
                {
                    displayName = (string?)directoryEntry.Properties["displayName"][0];
                }
                displayName ??= (string?)directoryEntry.Properties["cn"][0];
                displayName ??= groupId.ToString();
                return await Task.FromResult(new GroupInfo(groupId, displayName, GroupStore.OnPremAd));
            }
            catch (DirectoryServicesCOMException ex)
            {
                if ((uint)ex.HResult == LDAP_NO_SUCH_OBJECT)
                {
                    throw GroupNotFoundException.Create(groupId, ex);
                }
                throw;
            }
        }

        public IEnumerable<GroupMemberSource> GetSupportedGrouperMemberSources()
        {
            return new GroupMemberSource[] {
                GroupMemberSource.OnPremAdGroup,
                GroupMemberSource.OnPremAdQuery
            };
        }

        public IEnumerable<GroupStore> GetSupportedGroupStores()
        {
            return new GroupStore[] { GroupStore.OnPremAd };
        }
    }
}

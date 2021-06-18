using GrouperLib.Config;
using GrouperLib.Core;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace GrouperLib.Store
{
    public class OnPremAd : IMemberSource, IGroupStore
    {
        private readonly ObjectCache _dnCache;
        private readonly string _userName;
        private readonly string _password;
        private readonly CacheItemPolicy _cachePolicy;
        private const uint LDAP_NO_SUCH_OBJECT = 0x80072030;

        public OnPremAd(string userName, string password)
        {
            // userName and password are optional
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                _userName = userName;
                _password = password;
            }
            _dnCache = MemoryCache.Default;
            _cachePolicy = new CacheItemPolicy()
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
            };
        }

        public OnPremAd(GrouperConfiguration config) : this(
            userName: config.OnPremAdUserName,
            password: config.OnPremAdPassword)
        {
        }

        public OnPremAd() : this(null, null)
        {
        }

        private DirectoryEntry GetDirectoryEntry(string path)
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

        private string GetDistinguishedName(Guid objectId)
        {
            string cacheKey = objectId.ToString();
            string distinguishedName = null;
            if (_dnCache.Contains(cacheKey))
            {
                distinguishedName = (string)_dnCache.Get(cacheKey);
            }
            else
            {
                using (DirectoryEntry directoryEntry = GetDirectoryEntry($"LDAP://<GUID={objectId}>"))
                {
                    try
                    {
                        distinguishedName = (string)directoryEntry.Properties["distinguishedName"].Value;
                        _dnCache.Add(cacheKey, distinguishedName, _cachePolicy);
                    }
                    catch (DirectoryServicesCOMException ex)
                    {
                        if ((uint)ex.HResult != LDAP_NO_SUCH_OBJECT)
                        {
                            throw;
                        }
                    }
                }
            }
            return distinguishedName;
        }

        public async Task GetGroupMembersAsync(GroupMemberCollection memberCollection, Guid groupId)
        {
            string groupDn = GetDistinguishedName(groupId);
            if (groupDn == null)
            {
                throw GroupNotFoundException.Create(groupId);
            }
            string filter = $"(memberOf={groupDn})";
            QueryGroupMembers(memberCollection, filter, null);
            await Task.FromResult(0);
        }

        private void QueryGroupMembers(GroupMemberCollection memberCollection, string ldapFilter, string searchBase)
        {
            if (ldapFilter == null)
            {
                throw new ArgumentNullException(nameof(ldapFilter));
            }
            using (DirectoryEntry searchRoot = GetDirectoryEntry(searchBase))
            using (DirectorySearcher directorySearcher = new DirectorySearcher(searchRoot, ldapFilter, new string[] { "objectGUID", "userPrincipalName" }))
            {
                directorySearcher.PageSize = 1000;
                using (SearchResultCollection result = directorySearcher.FindAll())
                {
                    foreach (SearchResult item in result)
                    {
                        Guid identity = new Guid((byte[])item.Properties["objectGuid"][0]);
                        string displayName;
                        if (item.Properties["userPrincipalName"].Count == 1)
                        {
                            displayName = (string)item.Properties["userPrincipalName"][0];
                        }
                        else
                        {
                            displayName = identity.ToString();
                        }
                        memberCollection.Add(new GroupMember(identity, displayName, GroupMemberTypes.OnPremAd));
                    }
                }
            }
        }

        public async Task AddGroupMemberAsync(GroupMember member, Guid groupId)
        {
            if (member.MemberType != GroupMemberTypes.OnPremAd)
            {
                throw new ArgumentException(nameof(member), "Can only add members of type 'OnPremAd'");
            }
            string groupDn = GetDistinguishedName(groupId);
            if (groupDn == null)
            {
                throw GroupNotFoundException.Create(groupId);
            }
            string memberDn = GetDistinguishedName(member.Id);
            if (memberDn == null)
            {
                throw MemberNotFoundException.Create(member.Id);
            }
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
            if (member.MemberType != GroupMemberTypes.OnPremAd)
            {
                throw new ArgumentException(nameof(member), "Can only remove members of type 'OnPremAd'");
            }
            string groupDn = GetDistinguishedName(groupId);
            if (groupDn == null)
            {
                throw GroupNotFoundException.Create(groupId);
            }
            string memberDn = GetDistinguishedName(member.Id);
            if (memberDn == null)
            {
                throw MemberNotFoundException.Create(member.Id);
            }
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

        public async Task GetMembersFromSourceAsync(GroupMemberCollection memberCollection, GrouperDocumentMember grouperMember, GroupMemberTypes memberType)
        {
            if (memberType != GroupMemberTypes.OnPremAd)
            {
                throw new ArgumentException("Invalid member type", nameof(memberType));
            }
            switch (grouperMember.Source)
            {
                case GroupMemberSources.OnPremAdGroup:
                    await GetGroupMembersAsync(
                        memberCollection,
                        Guid.Parse(grouperMember.Rules.Where(r => r.Name.IEquals("Group")).First().Value)
                    );
                    break;
                case GroupMemberSources.OnPremAdQuery:
                    string filter = grouperMember.Rules.Where(r => r.Name.IEquals("LdapFilter")).FirstOrDefault()?.Value;
                    string searchBase = grouperMember.Rules.Where(r => r.Name.IEquals("SearchBase")).FirstOrDefault()?.Value;
                    QueryGroupMembers(memberCollection, filter, searchBase);
                    break;
                default:
                    throw new ArgumentException(nameof(grouperMember));
            }
        }

        public async Task<GroupInfo> GetGroupInfoAsync(Guid groupId)
        {
            GroupInfo groupInfo = null;
            try
            {
                using (DirectoryEntry directoryEntry = new DirectoryEntry($"LDAP://<GUID={groupId}>"))
                {
                    directoryEntry.RefreshCache(new string[] { "displayName", "cn" });
                    string displayName = null;
                    if (directoryEntry.Properties["displayName"].Count == 1)
                    {
                        displayName = (string)directoryEntry.Properties["displayName"][0];
                    }
                    if (displayName == null)
                    {
                        displayName = (string)directoryEntry.Properties["cn"][0];
                    }
                    groupInfo = new GroupInfo(groupId, displayName, GroupStores.OnPremAd);
                }
            }
            catch (DirectoryServicesCOMException ex)
            {
                if ((uint)ex.HResult == LDAP_NO_SUCH_OBJECT)
                {
                    throw GroupNotFoundException.Create(groupId, ex);
                }
                throw;
            }
            return await Task.FromResult(groupInfo);
        }

        public IEnumerable<GroupMemberSources> GetSupportedGrouperMemberSources()
        {
            return new GroupMemberSources[] {
                GroupMemberSources.OnPremAdGroup,
                GroupMemberSources.OnPremAdQuery
            };
        }

        public IEnumerable<GroupStores> GetSupportedGroupStores()
        {
            return new GroupStores[] { GroupStores.OnPremAd };
        }
    }
}

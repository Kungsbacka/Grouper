using GrouperLib.Config;
using GrouperLib.Database;
using GrouperLib.Core;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GrouperLib.Store;
using System.Linq;

namespace GrouperLib.Backend
{
    public class Grouper : IDisposable
    {
        private ILogger _logger;
        private bool _disposed;
        private readonly double _changeRatioLowerLimit;
        private readonly Dictionary<GroupMemberSources, IMemberSource> _memberSources;
        private readonly Dictionary<GroupStores, IGroupOwnerSource> _ownerSources;
        private readonly Dictionary<GroupStores, IGroupStore> _groupStores;

        public Grouper(double changeRatioLowerLimit)
        {
            _changeRatioLowerLimit = changeRatioLowerLimit;
            _memberSources = new Dictionary<GroupMemberSources, IMemberSource>();
            _ownerSources = new Dictionary<GroupStores, IGroupOwnerSource>();
            _groupStores = new Dictionary<GroupStores, IGroupStore>();
        }

        public static Grouper CreateFromConfig(GrouperConfiguration config)
        {
            Grouper grouper = new Grouper(config.ChangeRatioLowerLimit);
            if (config.AzureAdRole != null && config.AzureAdRole.Length > 0)
            {
                AzureAd az = new AzureAd(config);
                if (config.AzureAdHasRole(GrouperConfiguration.Role.GroupStore))
                {
                    grouper.AddGroupStore(az);
                }
                if (config.ExchangeHasRole(GrouperConfiguration.Role.MemberSource))
                {
                    grouper.AddMemberSource(az);
                }
                if (config.AzureAdHasRole(GrouperConfiguration.Role.GroupOwnerSource))
                {
                    grouper.AddGroupOwnerSource(az);
                }
            }
            if (config.ExchangeRole != null && config.ExchangeRole.Length > 0)
            {
                Exo exo = new Exo(config);
                if (config.ExchangeHasRole(GrouperConfiguration.Role.GroupStore))
                {
                    grouper.AddGroupStore(exo);
                }
                if (config.ExchangeHasRole(GrouperConfiguration.Role.MemberSource))
                {
                    grouper.AddMemberSource(exo);
                }
            }
            if (config.OnPremAdHasRole(GrouperConfiguration.Role.GroupStore))
            {
                OnPremAd onPremAd = new OnPremAd(config);
                if (config.OnPremAdHasRole(GrouperConfiguration.Role.GroupStore))
                {
                    grouper.AddGroupStore(onPremAd);
                }
                if (config.OnPremAdHasRole(GrouperConfiguration.Role.MemberSource))
                {
                    grouper.AddMemberSource(onPremAd);
                }
            }
            if (!string.IsNullOrEmpty(config.MemberDatabaseConnectionString))
            {
                grouper.AddMemberSource(new MemberDb(config));
            }
            if (!string.IsNullOrEmpty(config.LogDatabaseConnectionString))
            {
                grouper.AddLogger(new LogDb(config));
            }
            return grouper;
        }

        public Grouper AddMemberSource(IMemberSource memberSource)
        {
            foreach (GroupMemberSources source in memberSource.GetSupportedGrouperMemberSources())
            {
                if (_memberSources.ContainsKey(source))
                {
                    throw new ArgumentException($"A member source for {source} is already added.", nameof(memberSource));
                }
                _memberSources.Add(source, memberSource);
            }
            return this;
        }

        public Grouper AddGroupStore(IGroupStore groupStore)
        {
            foreach (GroupStores store in groupStore.GetSupportedGroupStores())
            {
                if (_groupStores.ContainsKey(store))
                {
                    throw new ArgumentException($"A group store for {store} is already added.", nameof(groupStore));
                }
                _groupStores.Add(store, groupStore);
            }
            return this;
        }

        public Grouper AddGroupOwnerSource(IGroupOwnerSource ownerSource)
        {
            foreach (GroupStores store in ownerSource.GetSupportedGroupStores())
            {
                if (_ownerSources.ContainsKey(store))
                {
                    throw new ArgumentException($"An owner source for {store} is already added.", nameof(ownerSource));
                }
                _ownerSources.Add(store, ownerSource);
            }
            return this;
        }

        public Grouper AddLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            return this;
        }

        public async Task<GroupMemberDiff> GetMemberDiffAsync(GrouperDocument document, bool includeUnchanged = false)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }
            var currentMembers = await GetCurrentMembersAsync(document);
            var targetMembers = await GetTargetMembersAsync(document, currentMembers);
            IGroupOwnerSource ownerSource = GetOwnerSource(document);
            if (ownerSource != null)
            {
                if (document.Owner != GroupOwnerActions.MatchSource)
                {
                    var owners = new GroupMemberCollection();
                    await ownerSource.GetGroupOwnersAsync(owners, document.GroupId);
                    if (document.Owner == GroupOwnerActions.KeepExisting)
                    {
                        owners.IntersectWith(currentMembers);
                    }
                    targetMembers.Add(owners);
                }
            }
            if (!currentMembers.ContainsMatchingMemberType(targetMembers))
            {
                throw new InvalidOperationException("Member types does not match");
            }
            GroupMemberCollection unchangedMembers = new GroupMemberCollection();
            if (includeUnchanged)
            {
                unchangedMembers = currentMembers.Clone();
                unchangedMembers.IntersectWith(targetMembers);
            }
            int currentCount = currentMembers.Count;
            currentMembers.FilterUniqueMember(targetMembers);
            double changeRatio;
            if (currentCount == 0)
            {
                changeRatio = targetMembers.Count == 0 ? 1.0 : targetMembers.Count;
            }
            else
            {
                changeRatio = (currentCount - currentMembers.Count + targetMembers.Count) / (double)currentCount;
            }
            return new GroupMemberDiff(document, targetMembers, currentMembers, unchangedMembers, changeRatio);
        }

        public async Task UpdateGroupAsync(GroupMemberDiff memberDiff, bool ignoreChangeLimit = false)
        {
            if (memberDiff is null)
            {
                throw new ArgumentNullException(nameof(memberDiff));
            }
            if (!ignoreChangeLimit && memberDiff.ChangeRatio < _changeRatioLowerLimit)
            {
                throw new ChangeRatioException();
            }
            IGroupStore store = GetGroupStore(memberDiff.Document);
            foreach (GroupMember member in memberDiff.Remove)
            {
                await store.RemoveGroupMemberAsync(member, memberDiff.Document.GroupId);
                await _logger?.StoreOperationalLogItemAsync(new OperationalLogItem(memberDiff.Document, GroupMemberOperations.Remove, member));
            }
            foreach (GroupMember member in memberDiff.Add)
            {
                await store.AddGroupMemberAsync(member, memberDiff.Document.GroupId);
                await _logger?.StoreOperationalLogItemAsync(new OperationalLogItem(memberDiff.Document, GroupMemberOperations.Add, member));
            }
        }

        public async Task<GroupInfo> GetGroupInfoAsync(GrouperDocument document)
        {
            IGroupStore store = GetGroupStore(document);
            return await store.GetGroupInfoAsync(document.GroupId);
        }

        private async Task<GroupMemberCollection> GetCurrentMembersAsync(GrouperDocument document)
        {
            var memberCollection = new GroupMemberCollection();
            IGroupStore store = GetGroupStore(document);
            await store.GetGroupMembersAsync(memberCollection, document.GroupId);
            return memberCollection;
        }

        private async Task<GroupMemberCollection> GetTargetMembersAsync(GrouperDocument document, GroupMemberCollection currentMembers)
        {
            var include = new GroupMemberCollection();
            var exclude = new GroupMemberCollection();
            if (!(document.Members.Any(m => m.Action == GroupMemberActions.Include)))
            {
                include.Add(currentMembers);
            }
            foreach (GrouperDocumentMember member in document.Members)
            {
                var memberCollection = member.Action == GroupMemberActions.Exclude ? exclude : include;
                IMemberSource source = GetMemberSource(member);
                await source.GetMembersFromSourceAsync(memberCollection, member);
            }
            include.ExceptWith(exclude);
            return include;
        }

        private IMemberSource GetMemberSource(GrouperDocumentMember member)
        {
            if (_memberSources.TryGetValue(member.Source, out IMemberSource source))
            {
                return source;
            }
            throw new InvalidOperationException($"There is no member source added for {member.Source}");
        }

        private IGroupOwnerSource GetOwnerSource(GrouperDocument document)
        {
            if (_ownerSources.TryGetValue(document.Store, out IGroupOwnerSource source))
            {
                return source;
            }
            return null;
        }

        private IGroupStore GetGroupStore(GrouperDocument document)
        {
            if (_groupStores.TryGetValue(document.Store, out IGroupStore store))
            {
                return store;
            }
            throw new InvalidOperationException($"There is no group store added for {document.Store}");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_memberSources.TryGetValue(GroupMemberSources.ExoGroup, out IMemberSource source))
                    {
                        ((Exo)source).Dispose();
                    }
                    if (_groupStores.TryGetValue(GroupStores.Exo, out IGroupStore store))
                    {
                        ((Exo)store).Dispose();
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

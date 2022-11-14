using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrouperLib.Core
{
    public interface IGroupStore
    {
        Task GetGroupMembersAsync(GroupMemberCollection memberCollection, Guid groupId);
        Task AddGroupMemberAsync(GroupMember member, Guid groupId);
        Task RemoveGroupMemberAsync(GroupMember member, Guid groupId);
        Task<GroupInfo> GetGroupInfoAsync(Guid groupId);

        IEnumerable<GroupStore> GetSupportedGroupStores();
    }
}

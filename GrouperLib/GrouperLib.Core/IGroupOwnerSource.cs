namespace GrouperLib.Core;

public interface IGroupOwnerSource
{
    Task GetGroupOwnersAsync(GroupMemberCollection memberCollection, Guid groupId);

    IEnumerable<GroupStore> GetSupportedGroupStores();
}
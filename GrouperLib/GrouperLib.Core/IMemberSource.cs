﻿namespace GrouperLib.Core;

public interface IMemberSource
{
    Task GetMembersFromSourceAsync(GroupMemberCollection memberCollection, GrouperDocumentMember grouperMember, GroupMemberType memberType);

    IEnumerable<GroupMemberSource> GetSupportedGrouperMemberSources();
}
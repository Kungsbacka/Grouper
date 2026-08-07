using GrouperLib.Core;
using Moq;
using System.Runtime.Versioning;

namespace GrouperLib.Test;

/// <summary>
/// Covers how <see cref="GroupOwnerAction"/> affects the target membership.
///
/// Note that owners are folded in *after* the include/exclude pipeline has run, so an
/// owner is never subject to the document's exclude rules. That is pinned below because
/// it is not obvious from the document alone.
/// </summary>
[SupportedOSPlatform("windows")]
public class GrouperOwnerActionTest
{
    private static readonly GroupMember memberA = BackendTestHarness.AzureMember("A");
    private static readonly GroupMember ownerO = BackendTestHarness.AzureMember("O");
    private static readonly GroupMember ownerP = BackendTestHarness.AzureMember("P");

    [Fact]
    public async Task TestAddAllAddsOwnersMissingFromTheSource()
    {
        BackendTestHarness harness = new BackendTestHarness { OwnerAction = GroupOwnerAction.AddAll }
            .WithCurrentMembers(memberA)
            .WithOwners(ownerO)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(["O"], BackendTestHarness.Names(diff.Add));
        Assert.Empty(diff.Remove);
    }

    /// <summary>
    /// KeepExisting intersects the owners with the group's current members: an owner who is
    /// already a member is retained, an owner who is not is not added.
    /// </summary>
    [Fact]
    public async Task TestKeepExistingRetainsOwnersAlreadyInTheGroup()
    {
        BackendTestHarness harness = new BackendTestHarness { OwnerAction = GroupOwnerAction.KeepExisting }
            .WithCurrentMembers(memberA, ownerO)
            .WithOwners(ownerO, ownerP)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA);

        GroupMemberDiff diff = await harness.DiffAsync();

        // O retained (not removed even though no rule selects it), P never added.
        Assert.Empty(diff.Add);
        Assert.Empty(diff.Remove);
    }

    [Fact]
    public async Task TestMatchSourceLeavesOwnerHandlingToTheRules()
    {
        BackendTestHarness harness = new BackendTestHarness { OwnerAction = GroupOwnerAction.MatchSource }
            .WithCurrentMembers(memberA, ownerO)
            .WithOwners(ownerO)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA);

        GroupMemberDiff diff = await harness.DiffAsync();

        // No rule selects O, so it is removed like any other unmatched member.
        Assert.Empty(diff.Add);
        Assert.Equal(["O"], BackendTestHarness.Names(diff.Remove));
    }

    [Fact]
    public async Task TestMatchSourceNeverQueriesTheOwnerSource()
    {
        BackendTestHarness harness = new BackendTestHarness { OwnerAction = GroupOwnerAction.MatchSource }
            .WithOwners(ownerO)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA);

        await harness.DiffAsync();

        harness.OwnerSourceMock.Verify(
            s => s.GetGroupOwnersAsync(It.IsAny<GroupMemberCollection>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task TestOwnersAreIgnoredWhenNoOwnerSourceIsRegistered()
    {
        BackendTestHarness harness = new BackendTestHarness { OwnerAction = GroupOwnerAction.AddAll }
            .WithCurrentMembers(memberA)
            .WithOwners(ownerO)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA);
        harness.RegisterOwnerSource = false;

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Empty(diff.Add);
        Assert.Empty(diff.Remove);
    }

    /// <summary>
    /// Owners are added after the include/exclude pipeline, so an exclude rule naming an
    /// owner does not keep that owner out of the group.
    /// </summary>
    [Fact]
    public async Task TestOwnersBypassExcludeRules()
    {
        BackendTestHarness harness = new BackendTestHarness { OwnerAction = GroupOwnerAction.AddAll }
            .WithOwners(ownerO)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, ownerO)
            .WithRule(GroupMemberSource.Static, GroupMemberAction.Exclude, ownerO);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(["A", "O"], BackendTestHarness.Names(diff.Add));
    }
}

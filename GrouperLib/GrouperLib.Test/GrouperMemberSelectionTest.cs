using GrouperLib.Core;
using System.Runtime.Versioning;

namespace GrouperLib.Test;

/// <summary>
/// Covers how <c>Grouper.GetTargetMembersAsync</c> combines member objects into a target
/// membership: the four-step include/exclude ordering, static precedence, and the
/// exclude-only special case. A mistake in any of these adds or removes the wrong people
/// from a real group without throwing, so each rung is pinned separately.
/// </summary>
[SupportedOSPlatform("windows")]
public class GrouperMemberSelectionTest
{
    private static readonly GroupMember memberA = BackendTestHarness.AzureMember("A");
    private static readonly GroupMember memberB = BackendTestHarness.AzureMember("B");
    private static readonly GroupMember memberC = BackendTestHarness.AzureMember("C");

    [Fact]
    public async Task TestSingleIncludeRuleYieldsSourceMembers()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, memberB);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(["A", "B"], BackendTestHarness.Names(diff.Add));
        Assert.Empty(diff.Remove);
    }

    [Fact]
    public async Task TestExcludeRuleRemovesFromInclude()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, memberB)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Exclude, memberB);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(["A"], BackendTestHarness.Names(diff.Add));
    }

    [Fact]
    public async Task TestMultipleIncludeRulesAreUnioned()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA)
            .WithRule(GroupMemberSource.CustomView, GroupMemberAction.Include, memberB, memberC);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(["A", "B", "C"], BackendTestHarness.Names(diff.Add));
    }

    [Fact]
    public async Task TestMultipleExcludeRulesAreUnioned()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, memberB, memberC)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Exclude, memberB)
            .WithRule(GroupMemberSource.CustomView, GroupMemberAction.Exclude, memberC);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(["A"], BackendTestHarness.Names(diff.Add));
    }

    /// <summary>
    /// Step 3 runs after step 2, so a static include re-adds a member that a non-static
    /// exclude removed. This is the ordering that matters most: static membership wins.
    /// </summary>
    [Fact]
    public async Task TestStaticIncludeOverridesNonStaticExclude()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, memberB)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Exclude, memberB)
            .WithRule(GroupMemberSource.Static, GroupMemberAction.Include, memberB);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(["A", "B"], BackendTestHarness.Names(diff.Add));
    }

    /// <summary>Step 4 is last, so a static exclude beats a static include.</summary>
    [Fact]
    public async Task TestStaticExcludeOverridesStaticInclude()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.Static, GroupMemberAction.Include, memberA, memberB)
            .WithRule(GroupMemberSource.Static, GroupMemberAction.Exclude, memberB);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(["A"], BackendTestHarness.Names(diff.Add));
    }

    /// <summary>A static exclude also beats a non-static include.</summary>
    [Fact]
    public async Task TestStaticExcludeOverridesNonStaticInclude()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, memberB)
            .WithRule(GroupMemberSource.Static, GroupMemberAction.Exclude, memberB);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(["A"], BackendTestHarness.Names(diff.Add));
    }

    /// <summary>
    /// A document with no include rules at all seeds the target from the group's *current*
    /// members rather than from nothing, so it trims the group instead of emptying it.
    /// This is what makes paired groups with inverse rules work.
    /// </summary>
    [Fact]
    public async Task TestExcludeOnlyDocumentSeedsFromCurrentMembers()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithCurrentMembers(memberA, memberB, memberC)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Exclude, memberB);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Empty(diff.Add);
        Assert.Equal(["B"], BackendTestHarness.Names(diff.Remove));
    }

    [Fact]
    public async Task TestExcludeOnlyDocumentWithStaticRuleSeedsFromCurrentMembers()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithCurrentMembers(memberA, memberB, memberC)
            .WithRule(GroupMemberSource.Static, GroupMemberAction.Exclude, memberC);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Empty(diff.Add);
        Assert.Equal(["C"], BackendTestHarness.Names(diff.Remove));
    }

    [Fact]
    public async Task TestExcludeOnlyDocumentOnEmptyGroupIsNoOp()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Exclude, memberB);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Empty(diff.Add);
        Assert.Empty(diff.Remove);
    }

    /// <summary>
    /// With at least one include rule present the exclude-only path must not trigger, so
    /// current members that match nothing get removed.
    /// </summary>
    [Fact]
    public async Task TestIncludeRulePresentDoesNotSeedFromCurrentMembers()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithCurrentMembers(memberA, memberB, memberC)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Empty(diff.Add);
        Assert.Equal(["B", "C"], BackendTestHarness.Names(diff.Remove));
    }

    [Fact]
    public async Task TestAddAndRemoveAreDisjointAndExcludeUnchanged()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithCurrentMembers(memberA, memberB)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, memberC);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(["C"], BackendTestHarness.Names(diff.Add));
        Assert.Equal(["B"], BackendTestHarness.Names(diff.Remove));
    }

    [Fact]
    public async Task TestUnchangedIsEmptyUnlessRequested()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithCurrentMembers(memberA, memberB)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, memberB);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Empty(diff.Unchanged);
    }

    [Fact]
    public async Task TestUnchangedContainsMembersOnBothSidesWhenRequested()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithCurrentMembers(memberA, memberB)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, memberC);

        GroupMemberDiff diff = await harness.DiffAsync(includeUnchanged: true);

        Assert.Equal(["A"], BackendTestHarness.Names(diff.Unchanged));
        Assert.Equal(["C"], BackendTestHarness.Names(diff.Add));
        Assert.Equal(["B"], BackendTestHarness.Names(diff.Remove));
    }

    [Fact]
    public async Task TestDocumentIsCarriedOnTheDiff()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(BackendTestHarness.DocumentId, diff.Document.Id);
    }
}

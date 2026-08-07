using GrouperLib.Backend;
using GrouperLib.Core;
using Moq;
using System.Runtime.Versioning;

namespace GrouperLib.Test;

/// <summary>
/// Covers the change-ratio calculation and the guard in <c>Grouper.UpdateGroupAsync</c>.
///
/// The formula is <c>(currentCount - removals + additions) / currentCount</c>, which is
/// the group's *resulting size divided by its current size*. So the limit is a floor on how
/// far a group may shrink, not a cap on how much it may churn.
///
/// That is deliberate. The failure mode being guarded against is a member source returning
/// empty or partial data, which always shows up as the group collapsing in size. Full identity
/// turnover at stable size is normal here: many education groups replace their entire
/// membership at each new school year. A retention-based measure was tried and rejected because
/// it flagged too many groups every August for manual review.
/// </summary>
[SupportedOSPlatform("windows")]
public class GrouperChangeRatioTest
{
    private static readonly GroupMember memberA = BackendTestHarness.AzureMember("A");
    private static readonly GroupMember memberB = BackendTestHarness.AzureMember("B");
    private static readonly GroupMember memberC = BackendTestHarness.AzureMember("C");
    private static readonly GroupMember memberD = BackendTestHarness.AzureMember("D");

    private static BackendTestHarness Harness(double limit = 0.0) => new() { ChangeRatioLowerLimit = limit };

    [Fact]
    public async Task TestRatioForShrinkingGroup()
    {
        // current 4, keep 1 -> (4 - 3 + 0) / 4
        BackendTestHarness harness = Harness()
            .WithCurrentMembers(memberA, memberB, memberC, memberD)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(0.25, diff.ChangeRatio);
    }

    [Fact]
    public async Task TestRatioForGrowingGroup()
    {
        // current 1, add 2 -> (1 - 0 + 2) / 1
        BackendTestHarness harness = Harness()
            .WithCurrentMembers(memberA)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, memberB, memberC);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(3.0, diff.ChangeRatio);
    }

    [Fact]
    public async Task TestRatioForUnchangedGroupIsOne()
    {
        BackendTestHarness harness = Harness()
            .WithCurrentMembers(memberA, memberB)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, memberB);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(1.0, diff.ChangeRatio);
    }

    /// <summary>
    /// Replacing every member yields a ratio of 1.0 and passes any limit at or below one,
    /// because the resulting size equals the current size.
    ///
    /// This is required behaviour, not a gap: education groups turn over their whole membership
    /// at each new school year and must roll over unattended. If this test ever fails because
    /// the ratio became retention-based, the school-year rollover has been broken -- see the
    /// class summary before changing the formula.
    /// </summary>
    [Fact]
    public async Task TestRatioForCompleteReplacementIsOne()
    {
        BackendTestHarness harness = Harness()
            .WithCurrentMembers(memberA, memberB)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberC, memberD);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(1.0, diff.ChangeRatio);
        Assert.Equal(["C", "D"], BackendTestHarness.Names(diff.Add));
        Assert.Equal(["A", "B"], BackendTestHarness.Names(diff.Remove));
    }

    [Fact]
    public async Task TestRatioForEmptyGroupAndEmptyTargetIsOne()
    {
        BackendTestHarness harness = Harness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(1.0, diff.ChangeRatio);
    }

    /// <summary>
    /// Characterizes <c>Grouper.cs:169</c>: with no current members the ratio is the raw
    /// target *count* rather than a ratio. It is always &gt;= 1 for a non-empty target, so
    /// populating an empty group is never blocked -- but the value is a count, not a ratio.
    /// </summary>
    [Fact]
    public async Task TestRatioForEmptyGroupIsTargetCountNotARatio()
    {
        BackendTestHarness harness = Harness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, memberB, memberC);

        GroupMemberDiff diff = await harness.DiffAsync();

        Assert.Equal(3.0, diff.ChangeRatio);
    }

    [Fact]
    public async Task TestUpdateThrowsWhenRatioIsBelowLimit()
    {
        BackendTestHarness harness = Harness(0.5)
            .WithCurrentMembers(memberA, memberB, memberC, memberD)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA);

        Grouper grouper = harness.BuildGrouper();
        GroupMemberDiff diff = await grouper.GetMemberDiffAsync(harness.BuildDocument());

        await Assert.ThrowsAsync<ChangeRatioException>(() => grouper.UpdateGroupAsync(diff));
    }

    [Fact]
    public async Task TestUpdateMakesNoStoreCallsWhenRatioIsBelowLimit()
    {
        BackendTestHarness harness = Harness(0.5)
            .WithCurrentMembers(memberA, memberB, memberC, memberD)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA);

        Grouper grouper = harness.BuildGrouper();
        GroupMemberDiff diff = await grouper.GetMemberDiffAsync(harness.BuildDocument());
        await Assert.ThrowsAsync<ChangeRatioException>(() => grouper.UpdateGroupAsync(diff));

        harness.GroupStoreMock.Verify(
            s => s.RemoveGroupMemberAsync(It.IsAny<GroupMember>(), It.IsAny<Guid>()), Times.Never);
        harness.GroupStoreMock.Verify(
            s => s.AddGroupMemberAsync(It.IsAny<GroupMember>(), It.IsAny<Guid>()), Times.Never);
    }

    /// <summary>A ratio exactly at the limit is allowed: the check is strictly less-than.</summary>
    [Fact]
    public async Task TestUpdateProceedsWhenRatioEqualsLimit()
    {
        BackendTestHarness harness = Harness(0.5)
            .WithCurrentMembers(memberA, memberB, memberC, memberD)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, memberB);

        Grouper grouper = harness.BuildGrouper();
        GroupMemberDiff diff = await grouper.GetMemberDiffAsync(harness.BuildDocument());

        Assert.Equal(0.5, diff.ChangeRatio);
        Exception? exception = await Record.ExceptionAsync(() => grouper.UpdateGroupAsync(diff));
        Assert.Null(exception);
        harness.GroupStoreMock.Verify(
            s => s.RemoveGroupMemberAsync(It.IsAny<GroupMember>(), It.IsAny<Guid>()), Times.Exactly(2));
    }

    [Fact]
    public async Task TestUpdateProceedsBelowLimitWhenIgnoreChangeLimitIsSet()
    {
        BackendTestHarness harness = Harness(0.5)
            .WithCurrentMembers(memberA, memberB, memberC, memberD)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA);

        Grouper grouper = harness.BuildGrouper();
        GroupMemberDiff diff = await grouper.GetMemberDiffAsync(harness.BuildDocument());
        await grouper.UpdateGroupAsync(diff, ignoreChangeLimit: true);

        harness.GroupStoreMock.Verify(
            s => s.RemoveGroupMemberAsync(It.IsAny<GroupMember>(), It.IsAny<Guid>()), Times.Exactly(3));
    }

    [Fact]
    public async Task TestUpdateGrowingGroupIsNeverBlocked()
    {
        BackendTestHarness harness = Harness(0.9)
            .WithCurrentMembers(memberA)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, memberB);

        Grouper grouper = harness.BuildGrouper();
        GroupMemberDiff diff = await grouper.GetMemberDiffAsync(harness.BuildDocument());

        Exception? exception = await Record.ExceptionAsync(() => grouper.UpdateGroupAsync(diff));
        Assert.Null(exception);
    }
}

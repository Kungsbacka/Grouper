using GrouperLib.Backend;
using GrouperLib.Core;
using GrouperLib.Database;
using Moq;
using System.Runtime.Versioning;

namespace GrouperLib.Test;

/// <summary>
/// Covers how <see cref="Grouper"/> resolves stores and sources, and the errors it raises
/// when a document names something that was never registered or mixes member types.
/// </summary>
[SupportedOSPlatform("windows")]
public class GrouperRegistryTest
{
    private static readonly GroupMember azureMember = BackendTestHarness.AzureMember("A");
    private static readonly GroupMember onPremMember = BackendTestHarness.OnPremMember("X");

    [Fact]
    public async Task TestUnregisteredGroupStoreThrows()
    {
        BackendTestHarness harness = new BackendTestHarness { RegisterGroupStore = false }
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, azureMember);

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.DiffAsync());
    }

    [Fact]
    public async Task TestUnregisteredMemberSourceThrows()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithUnregisteredRule(GroupMemberSource.Personalsystem, GroupMemberAction.Include);

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.DiffAsync());
    }

    /// <summary>
    /// Current members and target members must be the same member type. The store is on premises so
    /// that the document itself is a legitimate one -- an on-premises source in an Azure store is
    /// rejected by validation long before the member types are compared.
    /// </summary>
    [Fact]
    public async Task TestMismatchedMemberTypesThrow()
    {
        BackendTestHarness harness = new BackendTestHarness { Store = GroupStore.OnPremAd }
            .WithCurrentMembers(azureMember)
            .WithRule(GroupMemberSource.OnPremAdGroup, GroupMemberAction.Include, onPremMember);

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.DiffAsync());
    }

    /// <summary>A target mixing both member types is rejected even when the group is empty.</summary>
    [Fact]
    public async Task TestTargetMixingMemberTypesThrows()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, azureMember, onPremMember);

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.DiffAsync());
    }

    [Fact]
    public void TestRegisteringTwoStoresForTheSameStoreThrows()
    {
        Mock<IGroupStore> first = new();
        Mock<IGroupStore> second = new();
        first.Setup(s => s.GetSupportedGroupStores()).Returns([GroupStore.AzureAd]);
        second.Setup(s => s.GetSupportedGroupStores()).Returns([GroupStore.AzureAd]);

        Grouper grouper = new(0.0);
        grouper.AddGroupStore(first.Object);

        Assert.Throws<ArgumentException>(() => grouper.AddGroupStore(second.Object));
    }

    [Fact]
    public void TestRegisteringTwoSourcesForTheSameSourceThrows()
    {
        Mock<IMemberSource> first = new();
        Mock<IMemberSource> second = new();
        first.Setup(s => s.GetSupportedGrouperMemberSources()).Returns([GroupMemberSource.Static]);
        second.Setup(s => s.GetSupportedGrouperMemberSources()).Returns([GroupMemberSource.Static]);

        Grouper grouper = new(0.0);
        grouper.AddMemberSource(first.Object);

        Assert.Throws<ArgumentException>(() => grouper.AddMemberSource(second.Object));
    }

    [Fact]
    public void TestRegisteringTwoOwnerSourcesForTheSameStoreThrows()
    {
        Mock<IGroupOwnerSource> first = new();
        Mock<IGroupOwnerSource> second = new();
        first.Setup(s => s.GetSupportedGroupStores()).Returns([GroupStore.AzureAd]);
        second.Setup(s => s.GetSupportedGroupStores()).Returns([GroupStore.AzureAd]);

        Grouper grouper = new(0.0);
        grouper.AddGroupOwnerSource(first.Object);

        Assert.Throws<ArgumentException>(() => grouper.AddGroupOwnerSource(second.Object));
    }

    /// <summary>One implementation may serve several sources, registered under each.</summary>
    [Fact]
    public void TestOneSourceCanServeSeveralMemberSources()
    {
        Mock<IMemberSource> source = new();
        source.Setup(s => s.GetSupportedGrouperMemberSources())
            .Returns([GroupMemberSource.Static, GroupMemberSource.CustomView]);

        Grouper grouper = new(0.0);
        Exception? exception = Record.Exception(() => grouper.AddMemberSource(source.Object));

        Assert.Null(exception);
    }

    [Fact]
    public void TestAddLoggerRejectsNull()
    {
        Grouper grouper = new(0.0);
        Assert.Throws<ArgumentNullException>(() => grouper.AddLogger(null!));
    }

    [Fact]
    public async Task TestGetMemberDiffRejectsNullDocument()
    {
        Grouper grouper = new(0.0);
        await Assert.ThrowsAsync<ArgumentNullException>(() => grouper.GetMemberDiffAsync(null!));
    }

    /// <summary>
    /// Documents are fetched from the database without being gated on validation, so one that no
    /// longer satisfies the rules can reach this far. It describes a membership nobody can vouch
    /// for, and is refused before the group store is asked anything.
    /// </summary>
    [Fact]
    public async Task TestGetMemberDiffRejectsAnInvalidDocument()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, azureMember);
        GrouperDocument invalid = harness.BuildDocument().CloneWithNewGroupName("");

        InvalidGrouperDocumentException exception = await Assert.ThrowsAsync<InvalidGrouperDocumentException>(
            () => harness.BuildGrouper().GetMemberDiffAsync(invalid));

        Assert.NotEmpty(exception.ValidationErrors);
        harness.GroupStoreMock.Verify(
            s => s.GetGroupMembersAsync(It.IsAny<GroupMemberCollection>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task TestUpdateGroupRejectsNullDiff()
    {
        Grouper grouper = new(0.0);
        await Assert.ThrowsAsync<ArgumentNullException>(() => grouper.UpdateGroupAsync(null!));
    }

    [Fact]
    public async Task TestGetGroupInfoDelegatesToTheStore()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, azureMember);
        harness.GroupStoreMock
            .Setup(s => s.GetGroupInfoAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new GroupInfo(BackendTestHarness.GroupId, "Resolved Name", GroupStore.AzureAd));

        GroupInfo info = await harness.BuildGrouper().GetGroupInfoAsync(harness.BuildDocument());

        Assert.Equal("Resolved Name", info.DisplayName);
        harness.GroupStoreMock.Verify(s => s.GetGroupInfoAsync(BackendTestHarness.GroupId), Times.Once);
    }

    /// <summary>
    /// Pins the guard arm in <c>GetTargetMembersAsync</c>'s collection-selection switch
    /// (<c>Grouper.cs:260</c>). It is unreachable with today's two <see cref="GroupMemberAction"/>
    /// values and exists on purpose: if a third action is ever added and the switch is not
    /// updated, this arm turns a silent misclassification into a loud failure.
    ///
    /// The out-of-range cast below stands in for that future enum member. Keep this test if the
    /// arm ever looks like dead code worth deleting -- the arm is the safety net, and this is
    /// the proof it fires. The source is registered so the failure can only come from the
    /// switch and not from source resolution, which throws the same exception type.
    /// </summary>
    [Fact]
    public async Task TestUnknownMemberActionIsRejectedByTheGuardArm()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.AzureAdGroup, (GroupMemberAction)99, azureMember);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => harness.DiffAsync());

        Assert.Contains("Cannot choose collection", exception.Message);
    }

    /// <summary>
    /// Dispose only has work to do when Exchange Online is registered; with anything else it
    /// must be a no-op rather than tripping over the unguarded cast to Exo.
    /// </summary>
    [Fact]
    public void TestDisposeWithoutExoDoesNotThrow()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, azureMember);
        Grouper grouper = harness.BuildGrouper();

        Assert.Null(Record.Exception(grouper.Dispose));
    }

    [Fact]
    public void TestDisposeIsIdempotent()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, azureMember);
        Grouper grouper = harness.BuildGrouper();
        grouper.Dispose();

        Assert.Null(Record.Exception(grouper.Dispose));
    }

    /// <summary>Fluent registration returns the same instance so calls can be chained.</summary>
    [Fact]
    public void TestRegistrationIsChainable()
    {
        Mock<IGroupStore> store = new();
        Mock<ILogger> logger = new();
        store.Setup(s => s.GetSupportedGroupStores()).Returns([GroupStore.AzureAd]);

        Grouper grouper = new(0.0);
        Grouper returned = grouper.AddGroupStore(store.Object).AddLogger(logger.Object);

        Assert.Same(grouper, returned);
    }
}

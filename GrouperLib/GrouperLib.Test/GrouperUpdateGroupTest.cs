using GrouperLib.Backend;
using GrouperLib.Core;
using Moq;
using System.Runtime.Versioning;

namespace GrouperLib.Test;

/// <summary>
/// Covers what <c>Grouper.UpdateGroupAsync</c> writes: which store calls it makes, in what
/// order, and what it records in the operational log.
/// </summary>
[SupportedOSPlatform("windows")]
public class GrouperUpdateGroupTest
{
    private static readonly GroupMember memberA = BackendTestHarness.AzureMember("A");
    private static readonly GroupMember memberB = BackendTestHarness.AzureMember("B");
    private static readonly GroupMember memberC = BackendTestHarness.AzureMember("C");

    /// <summary>current {A,B} -> target {A,C}: remove B, add C.</summary>
    private static BackendTestHarness AddOneRemoveOne() =>
        new BackendTestHarness()
            .WithCurrentMembers(memberA, memberB)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA, memberC);

    [Fact]
    public async Task TestAddsAndRemovesExactlyTheDiffedMembers()
    {
        BackendTestHarness harness = AddOneRemoveOne();
        Grouper grouper = harness.BuildGrouper();

        await grouper.UpdateGroupAsync(await grouper.GetMemberDiffAsync(harness.BuildDocument()));

        harness.GroupStoreMock.Verify(s => s.RemoveGroupMemberAsync(memberB, BackendTestHarness.GroupId), Times.Once);
        harness.GroupStoreMock.Verify(s => s.AddGroupMemberAsync(memberC, BackendTestHarness.GroupId), Times.Once);
        harness.GroupStoreMock.Verify(
            s => s.RemoveGroupMemberAsync(It.IsAny<GroupMember>(), It.IsAny<Guid>()), Times.Once);
        harness.GroupStoreMock.Verify(
            s => s.AddGroupMemberAsync(It.IsAny<GroupMember>(), It.IsAny<Guid>()), Times.Once);
    }

    /// <summary>
    /// Removals are issued before additions. That ordering matters for stores with a
    /// membership ceiling, where adding first could hit the limit.
    /// </summary>
    [Fact]
    public async Task TestRemovalsHappenBeforeAdditions()
    {
        BackendTestHarness harness = AddOneRemoveOne();
        Grouper grouper = harness.BuildGrouper();
        List<string> calls = [];
        harness.GroupStoreMock
            .Setup(s => s.RemoveGroupMemberAsync(It.IsAny<GroupMember>(), It.IsAny<Guid>()))
            .Returns((GroupMember m, Guid _) => { calls.Add($"remove:{m.DisplayName}"); return Task.CompletedTask; });
        harness.GroupStoreMock
            .Setup(s => s.AddGroupMemberAsync(It.IsAny<GroupMember>(), It.IsAny<Guid>()))
            .Returns((GroupMember m, Guid _) => { calls.Add($"add:{m.DisplayName}"); return Task.CompletedTask; });

        await grouper.UpdateGroupAsync(await grouper.GetMemberDiffAsync(harness.BuildDocument()));

        Assert.Equal(["remove:B", "add:C"], calls);
    }

    [Fact]
    public async Task TestLogsOneEntryPerOperation()
    {
        BackendTestHarness harness = AddOneRemoveOne();
        Grouper grouper = harness.BuildGrouper();
        List<OperationalLogItem> logged = [];
        harness.LoggerMock
            .Setup(l => l.StoreOperationalLogItemAsync(It.IsAny<OperationalLogItem>()))
            .Returns((OperationalLogItem item) => { logged.Add(item); return Task.CompletedTask; });

        await grouper.UpdateGroupAsync(await grouper.GetMemberDiffAsync(harness.BuildDocument()));

        Assert.Equal(2, logged.Count);
        OperationalLogItem removal = Assert.Single(logged, i => i.Operation == GroupMemberOperation.Remove);
        OperationalLogItem addition = Assert.Single(logged, i => i.Operation == GroupMemberOperation.Add);
        Assert.Equal("B", removal.TargetDisplayName);
        Assert.Equal("C", addition.TargetDisplayName);
    }

    [Fact]
    public async Task TestLogEntriesCarryDocumentAndGroupIdentity()
    {
        BackendTestHarness harness = AddOneRemoveOne();
        Grouper grouper = harness.BuildGrouper();
        List<OperationalLogItem> logged = [];
        harness.LoggerMock
            .Setup(l => l.StoreOperationalLogItemAsync(It.IsAny<OperationalLogItem>()))
            .Returns((OperationalLogItem item) => { logged.Add(item); return Task.CompletedTask; });

        await grouper.UpdateGroupAsync(await grouper.GetMemberDiffAsync(harness.BuildDocument()));

        Assert.All(logged, item =>
        {
            Assert.Equal(BackendTestHarness.DocumentId, item.DocumentId);
            Assert.Equal(BackendTestHarness.GroupId, item.GroupId);
            Assert.Equal(GroupStore.AzureAd, item.GroupStore);
        });
    }

    [Fact]
    public async Task TestUpdateWithoutLoggerDoesNotThrow()
    {
        BackendTestHarness harness = AddOneRemoveOne();
        harness.RegisterLogger = false;
        Grouper grouper = harness.BuildGrouper();

        Exception? exception = await Record.ExceptionAsync(async () =>
            await grouper.UpdateGroupAsync(await grouper.GetMemberDiffAsync(harness.BuildDocument())));

        Assert.Null(exception);
        harness.GroupStoreMock.Verify(s => s.AddGroupMemberAsync(memberC, BackendTestHarness.GroupId), Times.Once);
    }

    [Fact]
    public async Task TestUpdateWithNothingToDoMakesNoStoreCalls()
    {
        BackendTestHarness harness = new BackendTestHarness()
            .WithCurrentMembers(memberA)
            .WithRule(GroupMemberSource.AzureAdGroup, GroupMemberAction.Include, memberA);
        Grouper grouper = harness.BuildGrouper();

        await grouper.UpdateGroupAsync(await grouper.GetMemberDiffAsync(harness.BuildDocument()));

        harness.GroupStoreMock.Verify(
            s => s.RemoveGroupMemberAsync(It.IsAny<GroupMember>(), It.IsAny<Guid>()), Times.Never);
        harness.GroupStoreMock.Verify(
            s => s.AddGroupMemberAsync(It.IsAny<GroupMember>(), It.IsAny<Guid>()), Times.Never);
        harness.LoggerMock.Verify(
            l => l.StoreOperationalLogItemAsync(It.IsAny<OperationalLogItem>()), Times.Never);
    }
}

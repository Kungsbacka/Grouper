using GrouperLib.Core;

namespace GrouperLib.Test;

/// <summary>
/// Covers <see cref="GroupMemberTask"/>, the "one member, one operation, one group" record.
/// </summary>
public class GroupMemberTaskTest
{
    private static readonly GroupMember member =
        new("9e9abe09-5e43-4f61-a09a-f1aa370be6c1", "Member 1", GroupMemberType.AzureAd);

    [Fact]
    public void TestConstructionFromExplicitValues()
    {
        Guid groupId = Guid.Parse("baefe5f4-d404-491d-89d0-fb192afa3c1d");
        GroupMemberTask task = new(groupId, "My Group", member, GroupMemberOperation.Add);

        Assert.Equal(groupId, task.GroupId);
        Assert.Equal("My Group", task.GroupName);
        Assert.Same(member, task.Member);
        Assert.Equal(GroupMemberOperation.Add, task.Operation);
    }

    /// <summary>The document overload copies the group identity off the document.</summary>
    [Fact]
    public void TestConstructionFromDocument()
    {
        GrouperDocument document = TestHelpers.MakeDocument();
        GroupMemberTask task = new(document, member, GroupMemberOperation.Remove);

        Assert.Equal(document.GroupId, task.GroupId);
        Assert.Equal(document.GroupName, task.GroupName);
        Assert.Equal(GroupMemberOperation.Remove, task.Operation);
    }

    [Fact]
    public void TestNullMemberThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GroupMemberTask(Guid.NewGuid(), "My Group", null!, GroupMemberOperation.Add));
    }

    [Fact]
    public void TestNullGroupNameIsAllowed()
    {
        GroupMemberTask task = new(Guid.NewGuid(), null, member, GroupMemberOperation.None);

        Assert.Null(task.GroupName);
    }

    [Theory]
    [InlineData(GroupMemberOperation.Add)]
    [InlineData(GroupMemberOperation.Remove)]
    [InlineData(GroupMemberOperation.None)]
    public void TestEveryOperationRoundTrips(GroupMemberOperation operation)
    {
        Assert.Equal(operation, new GroupMemberTask(Guid.NewGuid(), "G", member, operation).Operation);
    }
}

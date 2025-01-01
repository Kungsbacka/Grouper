using GrouperLib.Core;
using System.Text.Json;

namespace GrouperLib.Test;

public class GroupMemberOperationTest
{
    [Fact]
    public void TestConstruction()
    {
        GroupMember member = new(Guid.Empty, "Member", GroupMemberType.OnPremAd);
        GroupMemberOperation operation = GroupMemberOperation.Add;
        GroupMemberTask memberOperation = new(TestHelpers.DefaultDocumentId, TestHelpers.DefaultGroupName, member, operation);
        Assert.Equal(TestHelpers.DefaultDocumentId, memberOperation.GroupId);
        Assert.Equal(TestHelpers.DefaultGroupName, memberOperation.GroupName);
        Assert.Equal(member, memberOperation.Member);
        Assert.Equal(operation, memberOperation.Operation);
    }

    [Fact]
    public void TestConstructionWithDocument()
    {
        GrouperDocument document = TestHelpers.MakeDocument();
        GroupMember member = new(Guid.Empty, "Member", GroupMemberType.OnPremAd);
        GroupMemberOperation operation = GroupMemberOperation.Add;
        GroupMemberTask memberOperation = new(document, member, operation);
        Assert.Equal(document.GroupId, memberOperation.GroupId);
        Assert.Equal(document.GroupName, memberOperation.GroupName);
        Assert.Equal(member, memberOperation.Member);
        Assert.Equal(operation, memberOperation.Operation);
    }

    [Fact]
    public void TestSerializedNames()
    {
        GroupMember member = new(Guid.Empty, "Member", GroupMemberType.OnPremAd);
        GroupMemberOperation operation = GroupMemberOperation.Add;
        GroupMemberTask memberOperation = new(TestHelpers.DefaultDocumentId, TestHelpers.DefaultGroupName, member, operation);
        string json = JsonSerializer.Serialize(memberOperation);
        Dictionary<string,object>? obj = JsonSerializer.Deserialize<Dictionary<string,object>>(json);
        Assert.True(obj?.ContainsKey("groupId"));
        Assert.True(obj?.ContainsKey("groupName"));
        Assert.True(obj?.ContainsKey("member"));
        Assert.True(obj?.ContainsKey("operation"));
    }
}
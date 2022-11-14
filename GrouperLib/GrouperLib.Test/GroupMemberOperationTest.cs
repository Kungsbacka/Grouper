using System;
using Xunit;
using GrouperLib.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GrouperLib.Test
{
    public class GroupMemberOperationTest
    {
        [Fact]
        public void TestConstruction()
        {
            GroupMember member = new GroupMember(Guid.Empty, "Member", GroupMemberType.OnPremAd);
            GroupMemberOperation operation = GroupMemberOperation.Add;
            GroupMemberTask memberOperation = new GroupMemberTask(TestHelpers.DefaultDocumentId, TestHelpers.DefaultGroupName, member, operation);
            Assert.Equal(TestHelpers.DefaultDocumentId, memberOperation.GroupId);
            Assert.Equal(TestHelpers.DefaultGroupName, memberOperation.GroupName);
            Assert.Equal(member, memberOperation.Member);
            Assert.Equal(operation, memberOperation.Operation);
        }

        [Fact]
        public void TestConstructionWithDocument()
        {
            GrouperDocument document = TestHelpers.MakeDocument();
            GroupMember member = new GroupMember(Guid.Empty, "Member", GroupMemberType.OnPremAd);
            GroupMemberOperation operation = GroupMemberOperation.Add;
            GroupMemberTask memberOperation = new GroupMemberTask(document, member, operation);
            Assert.Equal(document.GroupId, memberOperation.GroupId);
            Assert.Equal(document.GroupName, memberOperation.GroupName);
            Assert.Equal(member, memberOperation.Member);
            Assert.Equal(operation, memberOperation.Operation);
        }

        [Fact]
        public void TestSerialzedNames()
        {
            GroupMember member = new GroupMember(Guid.Empty, "Member", GroupMemberType.OnPremAd);
            GroupMemberOperation operation = GroupMemberOperation.Add;
            GroupMemberTask memberOperation = new GroupMemberTask(TestHelpers.DefaultDocumentId, TestHelpers.DefaultGroupName, member, operation);
            string json = JsonConvert.SerializeObject(memberOperation);
            JObject obj = JObject.Parse(json);
            Assert.True(obj.ContainsKey("groupId"));
            Assert.True(obj.ContainsKey("groupName"));
            Assert.True(obj.ContainsKey("member"));
            Assert.True(obj.ContainsKey("operation"));
        }
    }
}

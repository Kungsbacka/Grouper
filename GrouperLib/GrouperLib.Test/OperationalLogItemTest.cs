using System;
using Xunit;
using GrouperLib.Core;
using Newtonsoft.Json;

namespace GrouperLib.Test
{
    public class OperationalLogItemTest
    {
        private static readonly Guid documentId = Guid.Parse("3cbc0481-23b0-4860-a58a-7a723ee250c5");
        private static readonly Guid groupId = Guid.Parse("baefe5f4-d404-491d-89d0-fb192afa3c1d");
        private static readonly Guid targetId = Guid.Parse("1905ae2d-ec75-4362-943e-2f8f9d58f3f9");
        private static readonly string groupName = "Test Group";
        private static readonly GroupStores store = GroupStores.OnPremAd;
        private static readonly string targetName = "target@example.com";
        private static readonly GroupMemberOperations operation = GroupMemberOperations.Add;

        [Fact]
        public void TestOperationalLogItemConstruction()
        {
            DateTime now = DateTime.Now;
            OperationalLogItem logItem = new OperationalLogItem(
                logTime: now,
                documentId: documentId,
                groupId: groupId,
                groupDisplayName: groupName,
                groupStore: store.ToString(),
                operation: operation.ToString(),
                targetId: targetId,
                targetDisplayName: targetName
            );
            Assert.Equal(now, logItem.LogTime);
            Assert.Equal(documentId, logItem.DocumentId);
            Assert.Equal(groupId, logItem.GroupId);
            Assert.Equal(groupName, logItem.GroupDisplayName);
            Assert.Equal(store, logItem.GroupStore);
            Assert.Equal(operation, logItem.Operation);
            Assert.Equal(targetId, logItem.TargetId);
            Assert.Equal(targetName, logItem.TargetDisplayName);
        }

        [Fact]
        public void TestOperationalLogItemConstructionWithDocument()
        {
            DateTime now = DateTime.Now;
            GrouperDocument document = TestHelpers.MakeDocument();
            OperationalLogItem logItem = new OperationalLogItem(document, operation,
                new GroupMember(targetId, targetName, GroupMemberTypes.OnPremAd));
            Assert.True(logItem.LogTime >= now);
            Assert.Equal(TestHelpers.DefaultDocumentId, logItem.DocumentId);
            Assert.Equal(TestHelpers.DefaultGroupId, logItem.GroupId);
            Assert.Equal(TestHelpers.DefaultGroupName, logItem.GroupDisplayName);
            Assert.Equal(TestHelpers.DefaultGroupStore, logItem.GroupStore);
            Assert.Equal(operation, logItem.Operation);
            Assert.Equal(targetId, logItem.TargetId);
            Assert.Equal(targetName, logItem.TargetDisplayName);
        }

        [Fact]
        public void TestOperationalLogItemSerialization()
        {
            string validJson = @"{
  ""logTime"": ""2020-11-19T21:28:18.3926113+01:00"",
  ""documentId"": ""3cbc0481-23b0-4860-a58a-7a723ee250c5"",
  ""groupId"": ""baefe5f4-d404-491d-89d0-fb192afa3c1d"",
  ""groupDisplayName"": ""Test Group"",
  ""groupStore"": ""OnPremAd"",
  ""operation"": ""Add"",
  ""targetId"": ""1905ae2d-ec75-4362-943e-2f8f9d58f3f9"",
  ""targetDisplayName"": ""target@example.com""
}";
            DateTime time = DateTime.Parse("2020-11-19T21:28:18.3926113+01:00");
            OperationalLogItem logItem = new OperationalLogItem(
                logTime: time,
                documentId: documentId,
                groupId: groupId,
                groupDisplayName: groupName,
                groupStore: store.ToString(),
                operation: operation.ToString(),
                targetId: targetId,
                targetDisplayName: targetName
            ); string json = JsonConvert.SerializeObject(logItem, Formatting.Indented);
            Assert.Equal(validJson, json);
        }
    }
}

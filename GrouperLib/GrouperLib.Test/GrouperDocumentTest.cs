using System;
using Xunit;
using GrouperLib.Core;
using System.Collections.Generic;

namespace GrouperLib.Test
{
    public class GrouperDocumentTest
    {
        private static readonly Guid documentId = Guid.Parse("3cbc0481-23b0-4860-a58a-7a723ee250c5");
        private static readonly Guid groupId = Guid.Parse("baefe5f4-d404-491d-89d0-fb192afa3c1d");
        private static readonly string groupName = "Test Group";
        private static readonly GroupStores store = GroupStores.OnPremAd;
        private static readonly string targetName = "target@example.com";
        private static readonly GroupOwnerActions ownerAction = GroupOwnerActions.KeepExisting;
        private static readonly int processingInterval = 3;

        [Fact]
        public void GrouperDocumentCloneWithNewNameTest()
        {
            GrouperDocument document = GetDocument(store);
            string json = document.ToJson();
            string newGroupName = "New Group Namn";
            GrouperDocument newDocument = document.CloneWithNewGroupName(newGroupName);
            Assert.Equal(documentId, newDocument.Id);
            Assert.Equal(newGroupName, newDocument.GroupName);
        }

        [Fact]
        public void TestGrouperDocumentShouldSerializeOwnerWithAzureAd()
        {
            Assert.True(GetDocument(GroupStores.AzureAd).ShouldSerializeOwner());
        }

        [Fact]
        public void TestGrouperDocumentShouldSerializeOwnerWithOnPremAd()
        {
            Assert.False(GetDocument(GroupStores.OnPremAd).ShouldSerializeOwner());
        }

        [Fact]
        public void TestGrouperDocumentShouldSerializeOwnerWithExo()
        {
            Assert.False(GetDocument(GroupStores.Exo).ShouldSerializeOwner());
        }

        [Fact]
        public void TestGrouperDocumentShouldSerializeProcessingIntervalZero()
        {
            Assert.False(GetDocument(0).ShouldSerializeProcessingInterval());
        }

        [Fact]
        public void TestGrouperDocumentShouldSerializeProcessingIntervalNotZero()
        {
            Assert.True(GetDocument(5).ShouldSerializeProcessingInterval());
        }

        private GrouperDocument GetDocument(int processingInterval)
        {
            return GetDocument(store, processingInterval);
        }

        private GrouperDocument GetDocument(GroupStores store)
        {
            return GetDocument(store, processingInterval);
        }

        private GrouperDocument GetDocument(GroupStores store, int processingInterval)
        {
            return TestHelpers.MakeDocument(new
            {
                Id = documentId,
                Interval = processingInterval,
                GroupName = groupName,
                GroupId = groupId,
                Store = store,
                Owner = ownerAction,
                Members = new []
                {
                    new
                    {
                        Action = GroupMemberActions.Include,
                        Source = GroupMemberSources.Static,
                        Rules = new []
                        {
                            new
                            {
                                Name = "Upn",
                                Value = targetName
                            }
                        }
                    }
                }
            });
        }

        private string GetDocumentJson()
        {
            return @"{
  ""id"": ""3cbc0481-23b0-4860-a58a-7a723ee250c5"",
  ""interval"": 3,
  ""groupId"": ""baefe5f4-d404-491d-89d0-fb192afa3c1d"",
  ""groupName"": ""Test Group"",
  ""store"": ""OnPremAd"",
  ""members"": [
    {
      ""source"": ""Static"",
      ""action"": ""Include"",
      ""rules"": [
        {
          ""name"": ""Upn"",
          ""value"": ""target@example.com""
        }
      ]
    }
  ]
}";
        }
    }
}

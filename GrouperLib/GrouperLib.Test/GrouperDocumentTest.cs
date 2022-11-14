using System;
using Xunit;
using GrouperLib.Core;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.Graph;
using System.Collections.Generic;
using Microsoft.Identity.Client;

namespace GrouperLib.Test
{
    public class GrouperDocumentTest
    {
        [Fact]
        public void TestCloneWithNewName()
        {
            GrouperDocument document = TestHelpers.MakeDocument();
            string newGroupName = "New Group Name";
            GrouperDocument newDocument = document.CloneWithNewGroupName(newGroupName);
            Assert.Equal(newDocument.Id, TestHelpers.DefaultDocumentId);
            Assert.Equal(newGroupName, newDocument.GroupName);
        }

        [Fact]
        public void TestShouldSerializeOwnerWithAzureAd()
        {
            bool shoudSerializeOwner = TestHelpers.MakeDocument(new { Store = GroupStore.AzureAd }).ShouldSerializeOwner();
            Assert.True(shoudSerializeOwner);
        }

        [Fact]
        public void TestShouldSerializeOwnerWithOnPremAd()
        {
            bool shoudSerializeOwner = TestHelpers.MakeDocument(new { Store = GroupStore.OnPremAd }).ShouldSerializeOwner();
            Assert.False(shoudSerializeOwner);
        }

        [Fact]
        public void TestShouldSerializeOwnerWithExo()
        {
            bool shoudSerializeOwner = TestHelpers.MakeDocument(new { Store = GroupStore.Exo }).ShouldSerializeOwner();
            Assert.False(shoudSerializeOwner);
        }

        [Fact]
        public void TestShouldSerializeProcessingIntervalZero()
        {
            bool shouldSerializeProcessingInterval = TestHelpers.MakeDocument(new { Interval = 0 }).ShouldSerializeProcessingInterval();
            Assert.False(shouldSerializeProcessingInterval);
        }

        [Fact]
        public void TestShouldSerializeProcessingIntervalNonZero()
        {
            bool shouldSerializeProcessingInterval = TestHelpers.MakeDocument(new { Interval = 5 }).ShouldSerializeProcessingInterval();
            Assert.True(shouldSerializeProcessingInterval);
        }

        [Fact]
        public void TestSerializationOfGroupStore()
        {
            GrouperDocument document = TestHelpers.MakeDocument();
            string serializedDocument = document.ToJson();
            dynamic obj = JObject.Parse(serializedDocument);
            Assert.Equal(TestHelpers.DefaultGroupStore.ToString(), (string)obj.store);
        }

        [Fact]
        public void TestSerializationOfGroupOwnerAction()
        {
            GrouperDocument document = TestHelpers.MakeDocument(new { Store = GroupStore.AzureAd, Owner = TestHelpers.DefaultOwnerAction });
            string serializedDocument = document.ToJson();
            dynamic obj = JObject.Parse(serializedDocument);
            Assert.Equal(TestHelpers.DefaultOwnerAction.ToString(), (string)obj.owner);
        }

        [Fact]
        public void TestSerializedNames()
        {
            GrouperDocument document = TestHelpers.MakeDocument(
                new
                {
                    Store = GroupStore.AzureAd,
                    Interval = 10
                }
            );
            string json = JsonConvert.SerializeObject(document, Formatting.Indented);
            JObject obj = JObject.Parse(json);
            Assert.True(obj.ContainsKey("id"));
            Assert.True(obj.ContainsKey("interval"));
            Assert.True(obj.ContainsKey("groupId"));
            Assert.True(obj.ContainsKey("groupName"));
            Assert.True(obj.ContainsKey("store"));
            Assert.True(obj.ContainsKey("owner"));
            Assert.True(obj.ContainsKey("members"));
        }

        [Fact]
        public void TestEquals()
        {
            GrouperDocument document1 = TestHelpers.MakeDocument();
            GrouperDocument document2 = TestHelpers.MakeDocument();
            Assert.True(document1.Equals(document2));
        }

        [Fact]
        public void TestNotEquals()
        {
            Guid guid = Guid.Parse("191de1de-df8c-4f1d-b07c-aec81fff52c1");
            GrouperDocument document1 = TestHelpers.MakeDocument();
            GrouperDocument document2 = TestHelpers.MakeDocument(new { Id = guid });
            Assert.False(document1.Equals(document2));

        }

        [Fact]
        public void TestGetHashCode()
        {
            GrouperDocument document = TestHelpers.MakeDocument();
            Assert.Equal(TestHelpers.DefaultDocumentId.GetHashCode(), document.GetHashCode());
        }

        [Fact]
        public void TestCreateDocument()
        {
            List<ValidationError> validationErrors = new List<ValidationError>();
            Guid id = Guid.Parse("efe73af2-db9d-4b82-a21e-c93c5a067d80");
            int interval = 1;
            Guid groupId = Guid.Parse("6d00d587-cd31-4ac7-9a54-ffe4543dfd43");
            string groupName = "Test group";
            GroupOwnerAction owner = GroupOwnerAction.KeepExisting;
            GroupStore store = GroupStore.OnPremAd;

            List<GrouperDocumentMember> members = new List<GrouperDocumentMember>()
            {
                new GrouperDocumentMember(GroupMemberSource.Static, GroupMemberAction.Include, new List<GrouperDocumentRule>()
                {
                    new GrouperDocumentRule("Upn", "user@example.com")
                })
            };
            GrouperDocument document = GrouperDocument.Create(id, interval, groupId, groupName, store, owner, members, validationErrors);
            Assert.NotNull(document);
            Assert.Empty(validationErrors);
            Assert.Equal(id, document.Id);
            Assert.Equal(interval, document.ProcessingInterval);
            Assert.Equal(groupName, document.GroupName);
            Assert.Equal(store, document.Store);
            Assert.Equal(owner, document.Owner);
            Assert.Equal(members, document.Members);
        }
    }
}

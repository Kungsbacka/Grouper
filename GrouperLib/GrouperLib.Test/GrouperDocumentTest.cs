using System;
using Xunit;
using GrouperLib.Core;
using Newtonsoft.Json.Linq;

namespace GrouperLib.Test
{
    public class GrouperDocumentTest
    {
        [Fact]
        public void TestGrouperDocumentCloneWithNewName()
        {
            GrouperDocument document = TestHelpers.MakeDocument();
            string newGroupName = "New Group Name";
            GrouperDocument newDocument = document.CloneWithNewGroupName(newGroupName);
            Assert.Equal(TestHelpers.DefaultDocumentId, newDocument.Id);
            Assert.Equal(newGroupName, newDocument.GroupName);
        }

        [Fact]
        public void TestGrouperDocumentShouldSerializeOwnerWithAzureAd()
        {
            bool shoudSerializeOwner = TestHelpers.MakeDocument(new { Store = GroupStores.AzureAd }).ShouldSerializeOwner();
            Assert.True(shoudSerializeOwner);
        }

        [Fact]
        public void TestGrouperDocumentShouldSerializeOwnerWithOnPremAd()
        {
            bool shoudSerializeOwner = TestHelpers.MakeDocument(new { Store = GroupStores.OnPremAd }).ShouldSerializeOwner();
            Assert.False(shoudSerializeOwner);
        }

        [Fact]
        public void TestGrouperDocumentShouldSerializeOwnerWithExo()
        {
            bool shoudSerializeOwner = TestHelpers.MakeDocument(new { Store = GroupStores.Exo }).ShouldSerializeOwner();
            Assert.False(shoudSerializeOwner);
        }

        [Fact]
        public void TestGrouperDocumentShouldSerializeProcessingIntervalZero()
        {
            bool shouldSerializeProcessingInterval = TestHelpers.MakeDocument(new { Interval = 0 }).ShouldSerializeProcessingInterval();
            Assert.False(shouldSerializeProcessingInterval);
        }

        [Fact]
        public void TestGrouperDocumentShouldSerializeProcessingIntervalNonZero()
        {
            bool shouldSerializeProcessingInterval = TestHelpers.MakeDocument(new { Interval = 5 }).ShouldSerializeProcessingInterval();
            Assert.True(shouldSerializeProcessingInterval);
        }

        [Fact]
        public void TestGrouperDocumentSerializationOfGroupStore()
        {
            GrouperDocument document = TestHelpers.MakeDocument();
            string serializedDocument = document.ToJson();
            dynamic obj = JObject.Parse(serializedDocument);
            Assert.Equal(TestHelpers.DefaultGroupStore.ToString(), (string)obj.store);
        }

        [Fact]
        public void TestGrouperDocumentSerializationOfGroupOwnerAction()
        {
            GrouperDocument document = TestHelpers.MakeDocument(new { Store = GroupStores.AzureAd, Owner = TestHelpers.DefaultOwnerAction });
            string serializedDocument = document.ToJson();
            dynamic obj = JObject.Parse(serializedDocument);
            Assert.Equal(TestHelpers.DefaultOwnerAction.ToString(), (string)obj.owner);
        }

        [Fact]
        public void TestGrouperDocumentEquals()
        {
            GrouperDocument document1 = TestHelpers.MakeDocument();
            GrouperDocument document2 = TestHelpers.MakeDocument();
            Assert.True(document1.Equals(document2));
        }

        [Fact]
        public void TestGrouperDocumentNotEquals()
        {
            Guid guid = Guid.Parse("191de1de-df8c-4f1d-b07c-aec81fff52c1");
            GrouperDocument document1 = TestHelpers.MakeDocument();
            GrouperDocument document2 = TestHelpers.MakeDocument(new { Id = guid });
            Assert.False(document1.Equals(document2));

        }

        [Fact]
        public void TestGrouperDocumentGetHashCode()
        {
            GrouperDocument document = TestHelpers.MakeDocument();
            Assert.Equal(TestHelpers.DefaultDocumentId.GetHashCode(), document.GetHashCode());
        }

        [Fact]
        public void TestGrouperDocumentMembersListShouldBeImmutable()
        {
            GrouperDocument document = TestHelpers.MakeDocument();
            Assert.Throws<NotSupportedException>(() => { document.Members.RemoveAt(0); });
        }
    }
}

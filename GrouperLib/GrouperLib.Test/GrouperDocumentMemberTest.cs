using System;
using Xunit;
using GrouperLib.Core;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace GrouperLib.Test
{
    public class GrouperDocumentMemberTest
    {
        [Fact]
        public void TestEquals()
        {
            GrouperDocumentMember member1 = TestHelpers.MakeMember();
            GrouperDocumentMember member2 = TestHelpers.MakeMember();
            Assert.True(member1.Equals(member2));
        }

        [Fact]
        public void TestNotEqualsDifferentSource()
        {
            GrouperDocumentMember member1 = TestHelpers.MakeMember(new { Source = GroupMemberSources.Static });
            GrouperDocumentMember member2 = TestHelpers.MakeMember(new { Source = GroupMemberSources.OnPremAdGroup });
            Assert.False(member1.Equals(member2));
        }

        [Fact]
        public void TestNotEqualsDifferentAction()
        {
            GrouperDocumentMember member1 = TestHelpers.MakeMember(new { Action = GroupMemberActions.Include });
            GrouperDocumentMember member2 = TestHelpers.MakeMember(new { Action = GroupMemberActions.Exclude });
            Assert.False(member1.Equals(member2));
        }

        [Fact]
        public void TestNotEqualsDifferentRule()
        {
            GrouperDocumentMember member1 = TestHelpers.MakeMember(new { Rules = new[] { new { Name = "Upn", Value = "Test" } } });
            GrouperDocumentMember member2 = TestHelpers.MakeMember(new { Rules = new[] { new { Name = "Group", Value = "Test" } } });
            Assert.False(member1.Equals(member2));
        }

        [Fact]
        public void TestShouldSerializeMemberType()
        {
            GrouperDocumentMember member = TestHelpers.MakeMember();
            Assert.False(member.ShouldSerializeMemberType());
        }

        [Fact]
        public void TestSerializeSource()
        {
            GrouperDocumentMember member = TestHelpers.MakeMember();
            string json = JsonConvert.SerializeObject(member);
            dynamic obj = JObject.Parse(json);
            Assert.Equal(TestHelpers.DefaultGroupMemberSource.ToString(), (string)obj.source);
        }

        [Fact]
        public void TestSerializeAction()
        {
            GrouperDocumentMember member = TestHelpers.MakeMember();
            string json = JsonConvert.SerializeObject(member);
            dynamic obj = JObject.Parse(json);
            Assert.Equal(TestHelpers.DefaultGroupMemberAction.ToString(), (string)obj.action);
        }

        [Fact]
        public void TestSerializedNames()
        {
            GrouperDocumentMember member = TestHelpers.MakeMember();
            string json = JsonConvert.SerializeObject(member);
            JObject obj = JObject.Parse(json);
            Assert.True(obj.ContainsKey("source"));
            Assert.True(obj.ContainsKey("action"));
            Assert.True(obj.ContainsKey("rules"));
        }

        [Fact]
        public void TestRulesListShouldBeImmutable()
        {
            GrouperDocumentMember member = TestHelpers.MakeMember();
            Assert.Throws<NotSupportedException>(() => { member.Rules.RemoveAt(0); });
        }
    }
}



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
        public void TestGrouperDocumentMemberEquals()
        {
            GrouperDocumentMember member1 = TestHelpers.MakeMember();
            GrouperDocumentMember member2 = TestHelpers.MakeMember();
            Assert.True(member1.Equals(member2));
        }

        [Fact]
        public void TestGrouperDocumentMemberNotEqualsDifferentSource()
        {
            GrouperDocumentMember member1 = TestHelpers.MakeMember(new { Source = GroupMemberSources.Static });
            GrouperDocumentMember member2 = TestHelpers.MakeMember(new { Source = GroupMemberSources.OnPremAdGroup });
            Assert.False(member1.Equals(member2));
        }

        [Fact]
        public void TestGrouperDocumentMemberNotEqualsDifferentAction()
        {
            GrouperDocumentMember member1 = TestHelpers.MakeMember(new { Action = GroupMemberActions.Include });
            GrouperDocumentMember member2 = TestHelpers.MakeMember(new { Action = GroupMemberActions.Exclude });
            Assert.False(member1.Equals(member2));
        }

        [Fact]
        public void TestGrouperDocumentMemberNotEqualsDifferentRule()
        {
            GrouperDocumentMember member1 = TestHelpers.MakeMember(new { Rules = new[] { new { Name = "Upn", Value = "Test" } } });
            GrouperDocumentMember member2 = TestHelpers.MakeMember(new { Rules = new[] { new { Name = "Group", Value = "Test" } } });
            Assert.False(member1.Equals(member2));
        }

        [Fact]
        public void TestGrouperDocumentMemberShouldSerializeMemberType()
        {
            GrouperDocumentMember member = TestHelpers.MakeMember();
            Assert.False(member.ShouldSerializeMemberType());
        }

        [Fact]
        public void TestGrouperDocumentMemberSerializeSource()
        {
            GrouperDocumentMember member = TestHelpers.MakeMember();
            string serializedMember = JsonConvert.SerializeObject(member);
            dynamic obj = JObject.Parse(serializedMember);
            Assert.Equal(TestHelpers.DefaultGroupMemberSource.ToString(), (string)obj.source);
        }

        [Fact]
        public void TestGrouperDocumentMemberSerializeAction()
        {
            GrouperDocumentMember member = TestHelpers.MakeMember();
            string serializedMember = JsonConvert.SerializeObject(member);
            dynamic obj = JObject.Parse(serializedMember);
            Assert.Equal(TestHelpers.DefaultGroupMemberAction.ToString(), (string)obj.action);
        }

        [Fact]
        public void TestGrouperDocumentMemberRulesListShouldBeImmutable()
        {
            GrouperDocumentMember member = TestHelpers.MakeMember();
            Assert.Throws<NotSupportedException>(() => { member.Rules.RemoveAt(0); });
        }
    }
}



﻿using System;
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
            GrouperDocumentMember member1 = TestHelpers.MakeMember(new { Source = GroupMemberSource.Static });
            GrouperDocumentMember member2 = TestHelpers.MakeMember(new { Source = GroupMemberSource.OnPremAdGroup });
            Assert.False(member1.Equals(member2));
        }

        [Fact]
        public void TestNotEqualsDifferentAction()
        {
            GrouperDocumentMember member1 = TestHelpers.MakeMember(new { Action = GroupMemberAction.Include });
            GrouperDocumentMember member2 = TestHelpers.MakeMember(new { Action = GroupMemberAction.Exclude });
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
            Assert.False(GrouperDocumentMember.ShouldSerializeMemberType());
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
    }
}



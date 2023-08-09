using System;
using System.Linq;
using Xunit;
using GrouperLib.Core;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace GrouperLib.Test
{
    public class GroupMemberDiffTest
    {
        [Fact]
        public void TestConstruction()
        {
            double ratio = 0.5;
            GrouperDocument doc = TestHelpers.MakeDocument();
            GroupMemberCollection add = new()
            {
                new GroupMember(Guid.Empty, "M1", GroupMemberType.AzureAd)
            };
            GroupMemberCollection remove = new()
            {
                new GroupMember(Guid.Empty, "M2", GroupMemberType.AzureAd)
            };
            GroupMemberCollection unchanged = new()
            {
                new GroupMember(Guid.Empty, "M3", GroupMemberType.AzureAd)
            };
            GroupMemberDiff diff = new(doc, add, remove, unchanged, ratio);
            Assert.NotNull(diff.Add.FirstOrDefault(m => m.DisplayName == "M1"));
            Assert.NotNull(diff.Remove.FirstOrDefault(m => m.DisplayName == "M2"));
            Assert.NotNull(diff.Unchanged.FirstOrDefault(m => m.DisplayName == "M3"));
            Assert.Equal(ratio, diff.ChangeRatio);
            Assert.Equal(doc, diff.Document);
        }

        [Fact]
        public void TestConstructionWithSameCollection()
        {
            GroupMemberCollection col = new();
            Assert.Throws<ArgumentException>(() => { new GroupMemberDiff(TestHelpers.MakeDocument(), col, col, col, 0.5); });
        }

        [Fact]
        public void TestSerializedPropertyNames()
        {
            GroupMemberDiff diff = new(
                document: TestHelpers.MakeDocument(),
                addMemberCollection: new GroupMemberCollection(),
                removeMemberCollection: new GroupMemberCollection(),
                unchangedMemberCollection: new GroupMemberCollection(),
                changeRatio: 0
            );
            string serializedDiff = JsonConvert.SerializeObject(diff);
            JObject obj = JObject.Parse(serializedDiff);
            Assert.True(obj.ContainsKey("document"));
            Assert.True(obj.ContainsKey("add"));
            Assert.True(obj.ContainsKey("remove"));
            Assert.True(obj.ContainsKey("unchanged"));
            Assert.True(obj.ContainsKey("ratio"));
        }
    }
}

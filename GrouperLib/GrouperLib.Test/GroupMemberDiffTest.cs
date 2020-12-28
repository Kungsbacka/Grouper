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
            GroupMemberCollection add = new GroupMemberCollection();
            add.Add(new GroupMember(Guid.Empty, "M1", GroupMemberTypes.AzureAd));
            GroupMemberCollection remove = new GroupMemberCollection();
            remove.Add(new GroupMember(Guid.Empty, "M2", GroupMemberTypes.AzureAd));
            GroupMemberCollection unchanged = new GroupMemberCollection();
            unchanged.Add(new GroupMember(Guid.Empty, "M3", GroupMemberTypes.AzureAd));
            GroupMemberDiff diff = new GroupMemberDiff(doc, add, remove, unchanged, ratio);
            Assert.NotNull(diff.Add.FirstOrDefault(m => m.DisplayName == "M1"));
            Assert.NotNull(diff.Remove.FirstOrDefault(m => m.DisplayName == "M2"));
            Assert.NotNull(diff.Unchanged.FirstOrDefault(m => m.DisplayName == "M3"));
            Assert.Equal(ratio, diff.ChangeRatio);
            Assert.Equal(doc, diff.Document);
        }

        [Fact]
        public void TestConstructionWithSameCollection()
        {
            GroupMemberCollection col = new GroupMemberCollection();
            Assert.Throws<ArgumentException>(() => { new GroupMemberDiff(TestHelpers.MakeDocument(), col, col, col, 0.5); });
        }

        [Fact]
        public void TestConstructionWithAddCollectionNull()
        {
            GroupMemberCollection col1 = new GroupMemberCollection();
            GroupMemberCollection col2 = new GroupMemberCollection();
            Assert.Throws<ArgumentNullException>(() => { new GroupMemberDiff(TestHelpers.MakeDocument(), addMemberCollection: null, col1, col2, 0.5); });
        }

        [Fact]
        public void TestConstructionWithRemoveCollectionNull()
        {
            GroupMemberCollection col1 = new GroupMemberCollection();
            GroupMemberCollection col2 = new GroupMemberCollection();
            Assert.Throws<ArgumentNullException>(() => { new GroupMemberDiff(TestHelpers.MakeDocument(), col1, removeMemberCollection: null, col2, 0.5); });
        }

        [Fact]
        public void TestConstructionWithUnchangedCollectionNull()
        {
            GroupMemberCollection col1 = new GroupMemberCollection();
            GroupMemberCollection col2 = new GroupMemberCollection();
            Assert.Throws<ArgumentNullException>(() => { new GroupMemberDiff(TestHelpers.MakeDocument(), col1, col2, unchangedMemberCollection: null, 0.5); });
        }

        [Fact]
        public void TestConstructionWithDocumentNull()
        {
            GroupMemberCollection col1 = new GroupMemberCollection();
            GroupMemberCollection col2 = new GroupMemberCollection();
            GroupMemberCollection col3 = new GroupMemberCollection();
            Assert.Throws<ArgumentNullException>(() => { new GroupMemberDiff(document: null, col1, col2, col3, 0.5); });
        }

        [Fact]
        public void TestSerializedPropertyNames()
        {
            GroupMemberDiff diff = new GroupMemberDiff(
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

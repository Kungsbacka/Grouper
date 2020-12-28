using Xunit;
using GrouperLib.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GrouperLib.Test
{
    public class GrouperDocumentRuleTest
    {
        [Fact]
        public void TestEquals()
        {
            GrouperDocumentRule rule1 = TestHelpers.MakeRule();
            GrouperDocumentRule rule2 = TestHelpers.MakeRule();
            Assert.True(rule1.Equals(rule2));
        }

        [Fact]
        public void TestNotEqualsDifferentName()
        {
            GrouperDocumentRule rule1 = TestHelpers.MakeRule(new { Name = "Upn", Value = "Same" });
            GrouperDocumentRule rule2 = TestHelpers.MakeRule(new { Name = "Group", Value = "Same" });
            Assert.False(rule1.Equals(rule2));
        }

        [Fact]
        public void TestNotEqualsDifferentValue()
        {
            GrouperDocumentRule rule1 = TestHelpers.MakeRule(new { Name = "Upn", Value = "One" });
            GrouperDocumentRule rule2 = TestHelpers.MakeRule(new { Name = "Upn", Value = "Two" });
            Assert.False(rule1.Equals(rule2));
        }

        [Fact]
        public void TestSerializedNames()
        {
            GrouperDocumentRule rule = TestHelpers.MakeRule();
            string json = JsonConvert.SerializeObject(rule);
            JObject obj = JObject.Parse(json);
            Assert.True(obj.ContainsKey("name"));
            Assert.True(obj.ContainsKey("value"));
        }
    }
}



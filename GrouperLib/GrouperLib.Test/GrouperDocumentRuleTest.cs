using Xunit;
using GrouperLib.Core;

namespace GrouperLib.Test
{
    public class GrouperDocumentRuleTest
    {
        [Fact]
        public void TestGrouperDocumentRuleEquals()
        {
            GrouperDocumentRule rule1 = TestHelpers.MakeRule();
            GrouperDocumentRule rule2 = TestHelpers.MakeRule();
            Assert.True(rule1.Equals(rule2));
        }

        [Fact]
        public void TestGrouperDocumentMemberNotEqualsDifferentName()
        {
            GrouperDocumentRule rule1 = TestHelpers.MakeRule(new { Name = "Upn", Value = "Same" });
            GrouperDocumentRule rule2 = TestHelpers.MakeRule(new { Name = "Group", Value = "Same" });
            Assert.False(rule1.Equals(rule2));
        }

        [Fact]
        public void TestGrouperDocumentMemberNotEqualsDifferentValue()
        {
            GrouperDocumentRule rule1 = TestHelpers.MakeRule(new { Name = "Upn", Value = "One" });
            GrouperDocumentRule rule2 = TestHelpers.MakeRule(new { Name = "Upn", Value = "Two" });
            Assert.False(rule1.Equals(rule2));
        }
    }
}



using GrouperLib.Core;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrouperLib.Test;

public class GrouperDocumentRuleTest
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

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
        string json = JsonSerializer.Serialize(rule, _serializerOptions);
        Dictionary<string,object>? obj = JsonSerializer.Deserialize<Dictionary<string,object>>(json);
        Assert.True(obj?.ContainsKey("name"));
        Assert.True(obj?.ContainsKey("value"));
    }
}
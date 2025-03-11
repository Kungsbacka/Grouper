using GrouperLib.Core;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrouperLib.Test;

public class GrouperDocumentMemberTest
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
    public void TestSerializeSource()
    {
        GrouperDocumentMember member = TestHelpers.MakeMember();
        string json = JsonSerializer.Serialize(member, _serializerOptions);
        Dictionary<string,object>? obj = JsonSerializer.Deserialize<Dictionary<string,object>>(json);
        Assert.Equal(TestHelpers.DefaultGroupMemberSource.ToString(), obj?["source"]?.ToString());
    }

    [Fact]
    public void TestSerializeAction()
    {
        GrouperDocumentMember member = TestHelpers.MakeMember();
        string json = JsonSerializer.Serialize(member, _serializerOptions);
        Dictionary<string,object>? obj = JsonSerializer.Deserialize<Dictionary<string,object>>(json);
        Assert.Equal(TestHelpers.DefaultGroupMemberAction.ToString(), obj?["action"]?.ToString());
    }

    [Fact]
    public void TestSerializedNames()
    {
        GrouperDocumentMember member = TestHelpers.MakeMember();
        string json = JsonSerializer.Serialize(member, _serializerOptions);
        Dictionary<string,object>? obj = JsonSerializer.Deserialize<Dictionary<string,object>>(json);
        Assert.True(obj?.ContainsKey("source"));
        Assert.True(obj?.ContainsKey("action"));
        Assert.True(obj?.ContainsKey("rules"));
    }
}
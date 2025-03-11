using GrouperLib.Core;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrouperLib.Test;

public class GrouperDocumentTest
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };
    
    [Fact]
    public void TestCloneWithNewName()
    {
        GrouperDocument document = TestHelpers.MakeDocument();
        const string newGroupName = "New Group Name";
        GrouperDocument newDocument = document.CloneWithNewGroupName(newGroupName);
        Assert.Equal(newDocument.Id, TestHelpers.DefaultDocumentId);
        Assert.Equal(newGroupName, newDocument.GroupName);
    }

    [Fact]
    public void TestSerializationOfGroupStore()
    {
        GrouperDocument document = TestHelpers.MakeDocument();
        string? serializedDocument = document.ToJson();
        Dictionary<string,object>? obj = JsonSerializer.Deserialize<Dictionary<string,object>>(serializedDocument, _serializerOptions);
        Assert.Equal(TestHelpers.DefaultGroupStore.ToString(), obj?["store"].ToString());
    }

    [Fact]
    public void TestSerializationOfGroupOwnerAction()
    {
        GrouperDocument document = TestHelpers.MakeDocument(new { Store = GroupStore.AzureAd, Owner = TestHelpers.DefaultOwnerAction });
        string? serializedDocument = document.ToJson();
        Dictionary<string,object>? obj = JsonSerializer.Deserialize<Dictionary<string,object>>(serializedDocument);
        Assert.Equal(TestHelpers.DefaultOwnerAction.ToString(), obj?["owner"].ToString());
    }

    [Fact]
    public void TestSerializedNames()
    {
        GrouperDocument document = TestHelpers.MakeDocument(
            new
            {
                Store = GroupStore.AzureAd,
                Interval = 10
            }
        );
        string json = JsonSerializer.Serialize(document, _serializerOptions);
        Dictionary<string,object>? obj = JsonSerializer.Deserialize<Dictionary<string,object>>(json);
        Assert.True(obj?.ContainsKey("id"));
        Assert.True(obj?.ContainsKey("interval"));
        Assert.True(obj?.ContainsKey("groupId"));
        Assert.True(obj?.ContainsKey("groupName"));
        Assert.True(obj?.ContainsKey("store"));
        Assert.True(obj?.ContainsKey("owner"));
        Assert.True(obj?.ContainsKey("members"));
    }

    [Fact]
    public void TestEquals()
    {
        GrouperDocument document1 = TestHelpers.MakeDocument();
        GrouperDocument document2 = TestHelpers.MakeDocument();
        Assert.True(document1.Equals(document2));
    }

    [Fact]
    public void TestNotEquals()
    {
        Guid guid = Guid.Parse("191de1de-df8c-4f1d-b07c-aec81fff52c1");
        GrouperDocument document1 = TestHelpers.MakeDocument();
        GrouperDocument document2 = TestHelpers.MakeDocument(new { Id = guid });
        Assert.False(document1.Equals(document2));

    }

    [Fact]
    public void TestGetHashCode()
    {
        GrouperDocument document = TestHelpers.MakeDocument();
        Assert.Equal(TestHelpers.DefaultDocumentId.GetHashCode(), document.GetHashCode());
    }

    [Fact]
    public void TestCreateDocument()
    {
        List<ValidationError> validationErrors = new();
        Guid id = Guid.Parse("efe73af2-db9d-4b82-a21e-c93c5a067d80");
        int interval = 1;
        Guid groupId = Guid.Parse("6d00d587-cd31-4ac7-9a54-ffe4543dfd43");
        string groupName = "Test group";
        GroupOwnerAction owner = GroupOwnerAction.KeepExisting;
        GroupStore store = GroupStore.OnPremAd;

        List<GrouperDocumentMember> members = new()
        {
            new GrouperDocumentMember(GroupMemberSource.Static, GroupMemberAction.Include, new List<GrouperDocumentRule>()
            {
                new GrouperDocumentRule("Upn", "user@example.com")
            })
        };
        GrouperDocument? document = GrouperDocument.Create(id, interval, groupId, groupName, store, owner, members, validationErrors);
        Assert.NotNull(document);
        Assert.Empty(validationErrors);
        Assert.Equal(id, document.Id);
        Assert.Equal(interval, document.Interval);
        Assert.Equal(groupName, document.GroupName);
        Assert.Equal(store, document.Store);
        Assert.Equal(owner, document.Owner);
        Assert.Equal(members, document.Members);
    }
}
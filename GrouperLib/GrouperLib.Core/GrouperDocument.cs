using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrouperLib.Core;

public sealed class GrouperDocument
{
    [JsonPropertyName("id")]
    public Guid Id { get; }

    [JsonPropertyName("interval")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Interval { get; }

    [JsonPropertyName("groupId")] 
    public Guid GroupId { get; }

    [JsonPropertyName("groupName")]
    public string GroupName { get; }

    [JsonPropertyName("store")]
    [JsonConverter(typeof(JsonStringEnumConverter<GroupStore>))]
    public GroupStore Store { get; }

    [JsonPropertyName("owner")]
    [JsonConverter(typeof(JsonStringEnumConverter<GroupOwnerAction>))] 
    public GroupOwnerAction Owner { get; }

    [JsonPropertyName("members")]
    public IReadOnlyCollection<GrouperDocumentMember> Members { get; }
    
    [JsonIgnore]
    public GroupMemberType MemberType
    {
        get
        {
            return Store switch
            {
                GroupStore.OnPremAd or GroupStore.OpenE => GroupMemberType.OnPremAd,
                _ => GroupMemberType.AzureAd,
            };
        }
    }

    private static readonly JsonSerializerOptions serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions serializerOptionsCompact = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly GrouperDocumentJsonContext defaultContext = new(serializerOptions);
    private static readonly GrouperDocumentJsonContext compactContext = new(serializerOptionsCompact);

    [JsonConstructor]
    public GrouperDocument(Guid id, Guid groupId, string groupName, GroupStore store,
        IReadOnlyCollection<GrouperDocumentMember> members, GroupOwnerAction owner = GroupOwnerAction.KeepExisting, int interval = 0)
    {
        Id = id;
        Interval = interval;
        GroupId = groupId;
        GroupName = groupName;
        Store = store;
        Owner = owner;
        Members = members;
    }
    
    public static GrouperDocument? Create(Guid id, int interval, Guid groupId, string groupName, GroupStore store,
        GroupOwnerAction owner, IReadOnlyCollection<GrouperDocumentMember> members, List<ValidationError> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);
        GrouperDocument document = new(id, groupId, groupName, store, members, owner, interval);
        DocumentValidator.Validate(document, validationErrors);
        return validationErrors.Count > 0 ? null : document;
    }

    public static GrouperDocument Create(Guid id, int interval, Guid groupId, string groupName, GroupStore store,
        GroupOwnerAction owner, IReadOnlyCollection<GrouperDocumentMember> members)
    {
        List<ValidationError> validationErrors = [];
        GrouperDocument? document = Create(id, interval, groupId, groupName, store, owner, members, validationErrors);
        return document ?? throw new InvalidGrouperDocumentException();
    }
    
    public GrouperDocument CloneWithNewGroupName(string groupName)
    {
        return new GrouperDocument(Id, GroupId, groupName, Store, Members, Owner, Interval);
    }

    public string ToJson(bool indented = false)
    {
        var context = indented ? defaultContext : compactContext;
        return JsonSerializer.Serialize(this, context.GrouperDocument);
    }

    public static GrouperDocument? FromJson(string json, List<ValidationError> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);
        GrouperDocument? document = DocumentValidator.DeserializeAndValidate(json, validationErrors);
        return validationErrors.Count > 0 ? null : document;
    }

    public static GrouperDocument FromJson(string json)
    {
        List<ValidationError> validationErrors = [];
        GrouperDocument? document = FromJson(json, validationErrors);
        return document ?? throw new InvalidGrouperDocumentException();
    }

    public string ToString(bool logFormat)
    {
        if (!logFormat)
        {
            return Id.ToString();
        }

        StringBuilder sb = new();
        sb.Append("         Group Name: ");
        sb.AppendLine(GroupName);
        sb.Append("        Group Store: ");
        sb.AppendLine(Store.ToString());
        sb.Append("       Owner Action: ");
        sb.AppendLine(Owner.ToString());
        sb.Append("           Group ID: ");
        sb.AppendLine(GroupId.ToString());
        sb.Append("        Document ID: ");
        if (Interval > 0)
        {
            sb.Append("Processing Interval: ");
            sb.AppendLine(Interval.ToString());
        }
        sb.AppendLine(Id.ToString());
        sb.Append("       Member Rules: ");
        sb.AppendLine(Members.Count.ToString());
        int maxIndent = (int)Math.Floor(Math.Log10(Members.Count));
        int level = 0;
        foreach (var member in Members)
        {
            int indent = maxIndent + 4;
            if (level >= 9)
            {
                indent -= (int)Math.Floor(Math.Log10(level + 1));
            }
            sb.Append(new string(' ', indent));
            sb.Append('(');
            sb.Append(level + 1);
            sb.Append(") ");
            sb.Append(member.Action);
            sb.Append(": ");
            sb.AppendLine(member.Source.ToString());
            level++;
        }
        return sb.ToString();
    }

    public override string ToString()
    {
        return ToString(false);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not GrouperDocument document)
        {
            return false;
        }
        return Id == document.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

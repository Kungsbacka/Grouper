using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrouperLib.Core;

public sealed class GrouperDocument
{
    [JsonPropertyName("id")]
    [JsonPropertyOrder(1)]
    public Guid Id { get; }

    [JsonPropertyName("interval")]
    [JsonPropertyOrder(2)]
    public int Interval { get; }

    [JsonPropertyName("groupId")]
    [JsonPropertyOrder(3)]
    public Guid GroupId { get; }

    [JsonPropertyName("groupName")]
    [JsonPropertyOrder(4)]
    public string GroupName { get; }

    [JsonPropertyName("store")]
    [JsonPropertyOrder(5)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GroupStore Store { get; }

    [JsonPropertyName("owner")]
    [JsonPropertyOrder(6)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GroupOwnerAction Owner { get; }

    [JsonPropertyName("members")]
    [JsonPropertyOrder(7)]
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

    private static readonly JsonSerializerOptions serializerOptionsWriteIndented = new() { WriteIndented = true };

    [JsonConstructor]
    private GrouperDocument(Guid id, Guid groupId, string groupName, GroupStore store, IReadOnlyCollection<GrouperDocumentMember> members, GroupOwnerAction owner = GroupOwnerAction.KeepExisting, int interval = 0)
    {
        Id = id;
        Interval = interval;
        GroupId = groupId;
        GroupName = groupName;
        Store = store;
        Owner = owner;
        Members = members;
    }
    
    public bool ShouldSerializeOwner() => Store == GroupStore.AzureAd;

    public bool ShouldSerializeProcessingInterval() => Interval > 0;

    public static bool ShouldSerializeMemberType() => false;
    
    public static GrouperDocument? Create(Guid id, int interval, Guid groupId, string groupName, GroupStore store, GroupOwnerAction owner, IReadOnlyCollection<GrouperDocumentMember> members, List<ValidationError> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);
        GrouperDocument document = new(id, groupId, groupName, store, members, owner, interval);
        DocumentValidator.Validate(document, validationErrors);
        return validationErrors.Count > 0 ? null : document;
    }

    public static GrouperDocument Create(Guid id, int interval, Guid groupId, string groupName, GroupStore store, GroupOwnerAction owner, IReadOnlyCollection<GrouperDocumentMember> members)
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
        return indented ? JsonSerializer.Serialize(this, serializerOptionsWriteIndented) : JsonSerializer.Serialize(this);
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

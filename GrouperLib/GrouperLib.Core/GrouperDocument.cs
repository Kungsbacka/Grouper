using System.Text.Json.Serialization;
using System.Text.Json;
 // using Newtonsoft.Json;
 // using Newtonsoft.Json.Converters;
using System.Text;

namespace GrouperLib.Core;

public sealed class GrouperDocument
{
    [JsonPropertyName("id")]
    [JsonPropertyOrder(1)]
    public Guid Id { get; }

    [JsonPropertyName("interval")]
    [JsonPropertyOrder(2)]
    public int ProcessingInterval { get; } = 0;

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
    public GroupOwnerAction Owner { get; } = GroupOwnerAction.KeepExisting;

    [JsonPropertyName("members")]
    [JsonPropertyOrder(7)]
    public IReadOnlyCollection<GrouperDocumentMember> Members => _members.AsReadOnly();
    private readonly List<GrouperDocumentMember> _members;

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

    [JsonConstructor]
    private GrouperDocument(Guid id, int interval, Guid groupId, string groupName, GroupStore store, GroupOwnerAction owner, IReadOnlyCollection<GrouperDocumentMember> members)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            throw new ArgumentException("Parameter cannot be null or empty", nameof(groupName));
        }
        if (members == null)
        {
            throw new ArgumentNullException(nameof(members));
        }
        Id = id;
        ProcessingInterval = interval;
        GroupId = groupId;
        GroupName = groupName;
        Store = store;
        Owner = owner;
        _members = members.Select(m => new GrouperDocumentMember(m)).ToList();
    }

    public bool ShouldSerializeOwner() => Store == GroupStore.AzureAd;

    public bool ShouldSerializeProcessingInterval() => ProcessingInterval > 0;

    public static bool ShouldSerializeMemberType() => false;


    public static GrouperDocument? Create(Guid id, int interval, Guid groupId, string groupName, GroupStore store, GroupOwnerAction owner, IReadOnlyCollection<GrouperDocumentMember> members, List<ValidationError> validationErrors)
    {
        if (validationErrors == null)
        {
            throw new ArgumentNullException(nameof(validationErrors));
        }
        GrouperDocument document = new(id, interval, groupId, groupName, store, owner, members);
        DocumentValidator.Validate(document, validationErrors);
        if (validationErrors.Count > 0)
        {
            return null;
        }
        return document;
    }

    public static GrouperDocument Create(Guid id, int interval, Guid groupId, string groupName, GroupStore store, GroupOwnerAction owner, IReadOnlyCollection<GrouperDocumentMember> members)
    {
        List<ValidationError> validationErrors = new();
        GrouperDocument? document = Create(id, interval, groupId, groupName, store, owner, members, validationErrors);
        return document ?? throw new InvalidGrouperDocumentException();
    }


    public GrouperDocument CloneWithNewGroupName(string groupName)
    {
        return new GrouperDocument(Id, ProcessingInterval, GroupId, groupName, Store, Owner, Members);
    }

    public string? ToJson(bool indented = false)
    {   
        return JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = indented });
    }

    public static GrouperDocument? FromJson(string json, List<ValidationError> validationErrors)
    {
        if (validationErrors == null)
        {
            throw new ArgumentNullException(nameof(validationErrors));
        }
        GrouperDocument? document = DocumentValidator.DeserializeAndValidate(json, validationErrors);
        if (validationErrors.Count > 0)
        {
            return null;
        }
        return document;
    }

    public static GrouperDocument FromJson(string json)
    {
        List<ValidationError> validationErrors = new();
        GrouperDocument? document = FromJson(json, validationErrors);
        return document ?? throw new InvalidGrouperDocumentException();
    }

    public string ToString(bool logFormat)
    {
        if (logFormat)
        {
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
            if (ProcessingInterval > 0)
            {
                sb.Append("Processing Interval: ");
                sb.AppendLine(ProcessingInterval.ToString());
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
        return Id.ToString();
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
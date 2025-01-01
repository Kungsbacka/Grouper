using System.Text;
using System.Text.Json.Serialization;

namespace GrouperLib.Core;

public sealed class OperationalLogItem
{
    [JsonPropertyName("logTime")]
    [JsonPropertyOrder(1)]
    public DateTime LogTime { get; }

    [JsonPropertyName("documentId")]
    [JsonPropertyOrder(2)]
    public Guid DocumentId { get; }

    [JsonPropertyName("groupId")]
    [JsonPropertyOrder(3)]
    public Guid GroupId { get; }

    [JsonPropertyName("groupDisplayName")]
    [JsonPropertyOrder(4)]
    public string? GroupDisplayName { get; }

    [JsonPropertyName("groupStore")]
    [JsonPropertyOrder(5)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GroupStore GroupStore { get; }

    [JsonPropertyName("operation")]
    [JsonPropertyOrder(6)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GroupMemberOperation Operation { get; }

    [JsonPropertyName("targetId")]
    [JsonPropertyOrder(7)]
    public Guid TargetId { get; }

    [JsonPropertyName("targetDisplayName")]
    [JsonPropertyOrder(8)]
    public string? TargetDisplayName { get; }

    public OperationalLogItem(GrouperDocument document, GroupMemberOperation operation, GroupMember member)
    {
        LogTime = DateTime.Now;
        DocumentId = document.Id;
        GroupId = document.GroupId;
        GroupDisplayName = document.GroupName;
        GroupStore = document.Store;
        Operation = operation;
        TargetId = member.Id;
        TargetDisplayName = member.DisplayName;
    }

    public OperationalLogItem(DateTime logTime, Guid documentId, Guid groupId, string? groupDisplayName, string groupStore, string operation, Guid targetId, string? targetDisplayName)
    {
        LogTime = logTime;
        DocumentId = documentId;
        GroupId = groupId;
        GroupDisplayName = groupDisplayName;
        GroupStore = Enum.Parse<GroupStore>(groupStore, true);
        Operation = Enum.Parse<GroupMemberOperation>(operation, true);
        TargetId = targetId;
        TargetDisplayName = targetDisplayName;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append(LogTime.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.Append(": ");
        if (Operation != GroupMemberOperation.None)
        {
            sb.Append(Operation.ToString());
            sb.Append("ed ");
        }
        else
        {
            sb.Append("Did nothing to ");
        }
        sb.Append(TargetDisplayName ?? TargetId.ToString());
        switch (Operation)
        {
            case GroupMemberOperation.Add:
                sb.Append(" to ");
                break;
            case GroupMemberOperation.Remove:
                sb.Append(" from ");
                break;
            case GroupMemberOperation.None:
            default:
                sb.Append(" for ");
                break;
        }
        sb.Append(GroupDisplayName ?? GroupId.ToString());
        return sb.ToString();
    }
}
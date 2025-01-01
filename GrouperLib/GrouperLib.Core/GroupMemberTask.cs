using System.Text.Json.Serialization;

namespace GrouperLib.Core;

public sealed class GroupMemberTask
{
    [JsonPropertyName("groupId")]
    public Guid GroupId { get; }

    [JsonPropertyName("groupName")]
    public string? GroupName { get; }

    [JsonPropertyName("member")]
    public GroupMember Member { get; }

    [JsonPropertyName("operation")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GroupMemberOperation Operation { get; }

    public GroupMemberTask(Guid groupId, string? groupName, GroupMember member, GroupMemberOperation operation)
    {
        GroupId = groupId;
        GroupName = groupName;
        Member = member ?? throw new ArgumentNullException(nameof(member));
        Operation = operation;
    }

    public GroupMemberTask(GrouperDocument document, GroupMember member, GroupMemberOperation operation)
        : this(document.GroupId, document.GroupName, member, operation)
    {
    }
}
using System.Text;
using System.Text.Json.Serialization;

namespace GrouperLib.Core;

public sealed class EventLogItem
{
    [JsonPropertyName("logTime")]
    [JsonPropertyOrder(1)]
    public DateTime LogTime { get; }

    [JsonPropertyName("documentId")]
    [JsonPropertyOrder(2)]
    public Guid? DocumentId { get; }

    [JsonPropertyName("groupId")]
    [JsonPropertyOrder(3)]
    public Guid? GroupId { get; }

    [JsonPropertyName("groupDisplayName")]
    [JsonPropertyOrder(4)]
    public string? GroupDisplayName { get; }

    [JsonPropertyName("groupStore")]
    [JsonPropertyOrder(5)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GroupStore? GroupStore { get; }

    [JsonPropertyName("message")]
    [JsonPropertyOrder(6)]
    public string? Message { get; }

    [JsonPropertyName("logLevel")]
    [JsonPropertyOrder(7)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogLevel LogLevel { get; }
    
    public EventLogItem(GrouperDocument document, string? message, LogLevel logLevel)
    {
        LogTime = DateTime.Now;
        DocumentId = document.Id;
        GroupId = document.GroupId;
        GroupDisplayName = document.GroupName == "" ? null : document.GroupName;
        GroupStore = document.Store;
        Message = message;
        LogLevel = logLevel;
    }

    public EventLogItem(DateTime logTime, Guid? documentId, Guid? groupId, string? groupDisplayName, string? groupStore, string? message, LogLevel logLevel)
    {
        LogTime = logTime;
        DocumentId = documentId;
        GroupId = groupId;
        GroupDisplayName = groupDisplayName == "" ? null : groupDisplayName;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        LogLevel = logLevel;
        if (!string.IsNullOrEmpty(groupStore))
        {
            GroupStore = Enum.Parse<GroupStore>(groupStore, true);
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(LogTime.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.Append(' ');
        sb.Append(LogLevel);
        sb.Append(": ");
        int pos = Message?.IndexOf('\n') ?? -1;
        if (pos > -1)
        {
            sb.Append(Message.AsSpan(0, pos - 1));
            sb.Append("...");
        }
        else
        {
            sb.Append(Message);
        }

        if (GroupDisplayName == null)
        {
            return sb.ToString();
        }

        sb.Append(" (Group: ");
        sb.Append(GroupDisplayName);
        sb.Append(')');

        return sb.ToString();
    }
}
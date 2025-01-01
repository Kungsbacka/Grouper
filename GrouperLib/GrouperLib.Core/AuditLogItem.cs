using System.Text;
using System.Text.Json.Serialization;

namespace GrouperLib.Core;

public sealed class AuditLogItem
{
    [JsonPropertyName("logTime")]
    [JsonPropertyOrder(1)]
    public DateTime LogTime { get; }

    [JsonPropertyName("documentId")]
    [JsonPropertyOrder(2)]
    public Guid DocumentId { get; }

    [JsonPropertyName("actor")]
    [JsonPropertyOrder(3)]
    public string Actor { get; }

    [JsonPropertyName("action")]
    [JsonPropertyOrder(4)]
    public string Action { get; }

    [JsonPropertyName("additionalInformation")]
    [JsonPropertyOrder(5)]
    public string? AdditionalInformation { get; }

    public AuditLogItem(Guid documentId, string actor, string action, string additionalInformation) :
        this(DateTime.Now, documentId, actor, action, additionalInformation)
    { }

    public AuditLogItem(DateTime logTime, Guid documentId, string actor, string action, string? additionalInformation)
    {
        LogTime = logTime;
        DocumentId = documentId;
        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
        Action = action ?? throw new ArgumentNullException(nameof(action));
        AdditionalInformation = additionalInformation;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append(LogTime.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.Append(": ");
        sb.Append("Document: ");
        sb.Append(DocumentId);
        sb.Append(", Actor: ");
        sb.Append(Actor);
        sb.Append(", Action: ");
        sb.Append(Action);
        sb.Append('.');
        return sb.ToString();
    }
}
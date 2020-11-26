using System;
using System.Text;
using Newtonsoft.Json;

namespace GrouperLib.Core
{
    public sealed class AuditLogItem
    {
        [JsonProperty(PropertyName = "logTime", Order = 1)]
        public DateTime LogTime { get; }

        [JsonProperty(PropertyName = "documentId", Order = 2)]
        public Guid DocumentId { get; }

        [JsonProperty(PropertyName = "actor", Order = 3)]
        public string Actor { get; }

        [JsonProperty(PropertyName = "action", Order = 4)]
        public string Action { get; }

        [JsonProperty(PropertyName = "additionalInformation", Order = 5)]
        public string AdditionalInformation { get; }

        public AuditLogItem(Guid documentId, string actor, string action, string additionaInformation) :
            this(DateTime.Now, documentId, actor, action, additionaInformation)
        { }

        public AuditLogItem(DateTime logTime, Guid documentId, string actor, string action, string additionalInformation)
        {
            LogTime = logTime;
            DocumentId = documentId;
            Actor = actor ?? throw new ArgumentNullException(nameof(actor));
            Action = action ?? throw new ArgumentNullException(nameof(action));
            AdditionalInformation = additionalInformation;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
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
}

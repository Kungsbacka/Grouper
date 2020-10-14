using System;
using System.Text;

namespace GrouperLib.Core
{
    public sealed class AuditLogItem
    {
        public DateTime LogTime { get; }
        public Guid DocumentId { get; }
        public string Actor { get; }
        public string Action { get; }
        public string AdditionalInformation { get; }

        public AuditLogItem(Guid documentId, string actor, string action, string additionaInformation)
        {
            LogTime = DateTime.Now;
            DocumentId = documentId;
            Actor = actor;
            Action = action;
            AdditionalInformation = additionaInformation;
        }

        public AuditLogItem(DateTime logTime, Guid documentId, string actor, string action, string additionalInformation)
        {
            LogTime = logTime;
            DocumentId = documentId;
            Actor = actor;
            Action = action;
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

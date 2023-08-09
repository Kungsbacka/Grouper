using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Text;

namespace GrouperLib.Core
{
    public sealed class EventLogItem
    {
        [JsonProperty(PropertyName = "logTime", Order = 1)]
        public DateTime LogTime { get; }

        [JsonProperty(PropertyName = "documentId", Order = 2)]
        public Guid? DocumentId { get; }

        [JsonProperty(PropertyName = "groupId", Order = 3)]
        public Guid? GroupId { get; }

        [JsonProperty(PropertyName = "groupDisplayName", Order = 4)]
        public string? GroupDisplayName { get; }

        [JsonProperty(PropertyName = "groupStore", Order = 5)]
        [JsonConverter(typeof(StringEnumConverter))]
        public GroupStore? GroupStore { get; }

        [JsonProperty(PropertyName = "message", Order = 6)]
        public string Message { get; }

        [JsonProperty(PropertyName = "logLevel", Order = 7)]
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel LogLevel { get; }


        public EventLogItem(GrouperDocument document, string message, LogLevel logLevel)
        {
            LogTime = DateTime.Now;
            DocumentId = document.Id;
            GroupId = document.GroupId;
            GroupDisplayName = document.GroupName == "" ? null : document.GroupName;
            GroupStore = document.Store;
            Message = message;
            LogLevel = logLevel;
        }

        public EventLogItem(DateTime logTime, Guid? documentId, Guid? groupId, string? groupDisplayName, string? groupStore, string message, LogLevel logLevel)
        {
            LogTime = logTime;
            DocumentId = documentId;
            GroupId = groupId;
            GroupDisplayName = groupDisplayName == "" ? null : groupDisplayName;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            LogLevel = logLevel;
            if (!string.IsNullOrEmpty(groupStore))
            {
                GroupStore = (GroupStore)Enum.Parse(typeof(GroupStore), groupStore, true);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(LogTime.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.Append(' ');
            sb.Append(LogLevel);
            sb.Append(": ");
            int pos = Message.IndexOf('\n');
            if (pos > -1)
            {
                sb.Append(Message.Substring(0, pos - 1));
                sb.Append("...");
            }
            else
            {
                sb.Append(Message);
            }
            if (GroupDisplayName != null)
            {
                sb.Append(" (Group: ");
                sb.Append(GroupDisplayName);
                sb.Append(")");
            }
            return sb.ToString();
        }
    }
}

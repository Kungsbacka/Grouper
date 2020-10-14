using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Text;

namespace GrouperLib.Core
{
    public class EventLogItem
    {
        public DateTime LogTime { get; }
        public Guid? DocumentId { get; }
        public Guid? GroupId { get; }
        public string GroupDisplayName { get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public GroupStores? GroupStore { get; }
        public string Message { get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevels LogLevel { get; }


        public EventLogItem(GrouperDocument document, string message, LogLevels logLevel)
        {
            LogTime = DateTime.Now;
            DocumentId = document.Id;
            GroupId = document.GroupId;
            GroupDisplayName = document.GroupName;
            GroupStore = document.Store;
            Message = message;
            LogLevel = logLevel;
        }

        public EventLogItem(DateTime logTime, Guid? documentId, Guid? groupId, string groupDisplayName, string groupStore, string message, int logLevel)
        {
            LogTime = logTime;
            DocumentId = documentId;
            GroupId = groupId;
            if (!string.IsNullOrEmpty(groupDisplayName))
            {
                GroupDisplayName = groupDisplayName;
            }
            if (!string.IsNullOrEmpty(groupStore))
            {
                GroupStore = (GroupStores)Enum.Parse(typeof(GroupStores), groupStore, true);
            }
            Message = message;
            LogLevel = (LogLevels)logLevel;
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

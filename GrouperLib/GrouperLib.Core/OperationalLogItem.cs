using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Text;

namespace GrouperLib.Core
{
    public sealed class OperationalLogItem
    {
        [JsonProperty(PropertyName = "logTime", Order = 1)]
        public DateTime LogTime { get; }

        [JsonProperty(PropertyName = "documentId", Order = 2)]
        public Guid DocumentId { get; }

        [JsonProperty(PropertyName = "groupId", Order = 3)]
        public Guid GroupId { get; }

        [JsonProperty(PropertyName = "groupDisplayName", Order = 4)]
        public string GroupDisplayName { get; }

        [JsonProperty(PropertyName = "groupStore", Order = 5)]
        [JsonConverter(typeof(StringEnumConverter))]
        public GroupStore GroupStore { get; }

        [JsonProperty(PropertyName = "operation", Order = 6)]
        [JsonConverter(typeof(StringEnumConverter))]
        public GroupMemberOperation Operation { get; }

        [JsonProperty(PropertyName = "targetId", Order = 7)]
        public Guid TargetId { get; }

        [JsonProperty(PropertyName = "targetDisplayName", Order = 8)]
        public string TargetDisplayName { get; }

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

        public OperationalLogItem(DateTime logTime, Guid documentId, Guid groupId, string groupDisplayName, string groupStore, string operation, Guid targetId, string targetDisplayName)
        {
            LogTime = logTime;
            DocumentId = documentId;
            GroupId = groupId;
            GroupDisplayName = groupDisplayName;
            GroupStore = (GroupStore)Enum.Parse(typeof(GroupStore), groupStore, true);
            Operation = (GroupMemberOperation)Enum.Parse(typeof(GroupMemberOperation), operation, true);
            TargetId = targetId;
            TargetDisplayName = targetDisplayName;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
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
            if (Operation == GroupMemberOperation.Add)
            {
                sb.Append(" to ");
            }
            else if (Operation == GroupMemberOperation.Remove)
            {
                sb.Append(" from ");
            }
            else
            {
                sb.Append(" for ");
            }
            sb.Append(GroupDisplayName ?? GroupId.ToString());
            return sb.ToString();
        }
    }
}

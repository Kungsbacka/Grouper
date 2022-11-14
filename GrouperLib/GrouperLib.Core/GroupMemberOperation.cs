using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace GrouperLib.Core
{
    public sealed class GroupMemberTask
    {
        [JsonProperty(PropertyName = "groupId")]
        public Guid GroupId { get; }

        [JsonProperty(PropertyName = "groupName")]
        public string GroupName { get; }

        [JsonProperty(PropertyName = "member")]
        public GroupMember Member { get; }

        [JsonProperty(PropertyName = "operation")]
        [JsonConverter(typeof(StringEnumConverter))]
        public GroupMemberOperation Operation { get; }

        public GroupMemberTask(Guid groupId, string groupName, GroupMember member, GroupMemberOperation operation)
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
}

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GrouperLib.Core
{
    public class GroupMemberOperation
    {
        [JsonProperty(PropertyName = "groupId")]
        public Guid GroupId { get; }

        [JsonProperty(PropertyName = "groupName")]
        public string GroupName { get; }

        [JsonProperty(PropertyName = "member")]
        public GroupMember Member { get; private set; }

        [JsonProperty(PropertyName = "operation")]
        [JsonConverter(typeof(StringEnumConverter))]
        public GroupMemberOperations Operation { get; private set; }

        public GroupMemberOperation(Guid groupId, string groupName, GroupMember member, GroupMemberOperations operation)
        {
            GroupId = groupId;
            GroupName = groupName;
            Member = member ?? throw new ArgumentNullException(nameof(member));
            Operation = operation;
        }

        public GroupMemberOperation(GrouperDocument document, GroupMember member, GroupMemberOperations operation)
            : this(document.GroupId, document.GroupName, member, operation)
        {
        }
    }
}

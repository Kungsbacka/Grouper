using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace GrouperLib.Core
{
    public sealed class GroupMemberDiff
    {
        [JsonProperty(PropertyName = "document")]
        public GrouperDocument Document { get; }

        [JsonProperty(PropertyName = "add")]
        public IEnumerable<GroupMember> Add { get; }

        [JsonProperty(PropertyName = "remove")]
        public IEnumerable<GroupMember> Remove { get; }

        [JsonProperty(PropertyName = "unchanged")]
        public IEnumerable<GroupMember> Unchanged { get; }

        [JsonProperty(PropertyName = "ratio")]
        public double ChangeRatio { get; }

        public GroupMemberDiff(GrouperDocument document, GroupMemberCollection addMemberCollection,
            GroupMemberCollection removeMemberCollection, GroupMemberCollection unchangedMemberCollection,
            double changeRatio)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Add = addMemberCollection ?? throw new ArgumentNullException(nameof(addMemberCollection));
            Remove = removeMemberCollection ?? throw new ArgumentNullException(nameof(removeMemberCollection));
            Unchanged = unchangedMemberCollection ?? throw new ArgumentNullException(nameof(unchangedMemberCollection));
            ChangeRatio = changeRatio;
            if (addMemberCollection == removeMemberCollection || addMemberCollection == unchangedMemberCollection || removeMemberCollection == unchangedMemberCollection)
            {
                throw new ArgumentException("All collections must be separate objects");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

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
            if (addMemberCollection is null)
            {
                throw new ArgumentNullException(nameof(addMemberCollection));
            }
            if (removeMemberCollection is null)
            {
                throw new ArgumentNullException(nameof(removeMemberCollection));
            }
            if (unchangedMemberCollection is null)
            {
                throw new ArgumentNullException(nameof(unchangedMemberCollection));
            }
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Add = addMemberCollection.AsEnumerable();
            Remove = removeMemberCollection.AsEnumerable();
            Unchanged = unchangedMemberCollection.AsEnumerable();
            ChangeRatio = changeRatio;
        }
    }
}

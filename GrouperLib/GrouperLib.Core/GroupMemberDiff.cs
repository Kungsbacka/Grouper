using System.Text.Json.Serialization;

namespace GrouperLib.Core;

public sealed class GroupMemberDiff
{
    [JsonPropertyName("document")]
    public GrouperDocument Document { get; }

    [JsonPropertyName("add")]
    public IEnumerable<GroupMember> Add { get; }

    [JsonPropertyName("remove")]
    public IEnumerable<GroupMember> Remove { get; }

    [JsonPropertyName("unchanged")]
    public IEnumerable<GroupMember> Unchanged { get; }

    [JsonPropertyName("ratio")]
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
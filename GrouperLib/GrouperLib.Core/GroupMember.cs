using System.Text.Json.Serialization;

namespace GrouperLib.Core;

public sealed class GroupMember
{
    [JsonPropertyName("id")]
    public Guid Id { get; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; }

    [JsonPropertyName("memberType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GroupMemberType MemberType { get; }

    public GroupMember(Guid id, string displayName, GroupMemberType memberType)
    {
        Id = id;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        MemberType = memberType;
    }

    public GroupMember(string id, string displayName, GroupMemberType memberType)
    {
        if (Guid.TryParse(id, out Guid guid))
        {
            Id = guid;
        }
        else
        {
            throw new ArgumentException("Argument is not a valid GUID", nameof(id));
        }
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        MemberType = memberType;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not GroupMember member)
        {
            return false;
        }
        return MemberType.Equals(member.MemberType) && Id.Equals(member.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return DisplayName;
    }
}
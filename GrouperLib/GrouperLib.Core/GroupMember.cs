using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GrouperLib.Core
{
    public class GroupMember
    {

        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; }

        [JsonProperty(PropertyName = "displayName")]
        public string DisplayName { get; }
        
        [JsonProperty(PropertyName = "memberType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public GroupMemberTypes MemberType { get; }

        public GroupMember(Guid id, string displayName, GroupMemberTypes memberType)
        {
            Id = id;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            MemberType = memberType;
        }

        public GroupMember(string id, string displayName, GroupMemberTypes memberType)
        {
            if (Guid.TryParse(id, out Guid guid))
            {
                Id = guid;
            }
            else
            {
                throw new ArgumentException(nameof(id), "Argument is not a valid GUID");
            }
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            MemberType = memberType;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GroupMember member))
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
}

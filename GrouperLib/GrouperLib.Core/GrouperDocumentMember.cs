using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GrouperLib.Core
{
    public sealed class GrouperDocumentMember
    {
        [JsonProperty(PropertyName = "source", Order = 1)]
        [JsonConverter(typeof(StringEnumConverter))]
        public GroupMemberSources Source { get; }

        [JsonProperty(PropertyName = "action", Order = 2)]
        [JsonConverter(typeof(StringEnumConverter))]
        public GroupMemberActions Action { get; }

        [JsonProperty(PropertyName = "rules", Order = 3)]
        public IList<GrouperDocumentRule> Rules
        {
            get
            {
                return _rules.AsReadOnly();
            }
        }
        private readonly List<GrouperDocumentRule> _rules;

        public GroupMemberTypes MemberType { get; }

        public bool ShouldSerializeMemberType() => false;

        internal GrouperDocumentMember(DeserializedMember deserializedMember, GroupMemberTypes memberType)
        {
            Source = (GroupMemberSources)Enum.Parse(typeof(GroupMemberSources), deserializedMember.Source, true);
            Action = (GroupMemberActions)Enum.Parse(typeof(GroupMemberActions), deserializedMember.Action, true);
            MemberType = memberType;
            _rules = deserializedMember.Rules.Select(r => new GrouperDocumentRule(r)).ToList();
        }

        internal GrouperDocumentMember(GrouperDocumentMember documentMember)
        {
            Source = documentMember.Source;
            Action = documentMember.Action;
            MemberType = documentMember.MemberType;
            _rules = documentMember.Rules.Select(r => new GrouperDocumentRule(r)).ToList();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GrouperDocumentMember member))
            {
                return false;
            }
            if (Source != member.Source || Action != member.Action)
            {
                return false;
            }
            if (Rules == null || member.Rules == null)
            {
                return Rules == null && member.Rules == null;
            }
            if (Rules.Count == 0 && member.Rules.Count == 0)
            {
                return true;
            }
            if (Rules.Count != member.Rules.Count)
            {
                return false;
            }
            return Rules.Intersect(member.Rules).Count() == Rules.Count;
        }

        public override int GetHashCode()
        {
            // https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
            // unchecked to allow integer overflow
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(Source);
                hash = hash * 31 + Action.GetHashCode();
                if (Rules != null)
                {
                    foreach (GrouperDocumentRule rule in Rules)
                    {
                        hash = hash * 31 + rule.GetHashCode();
                    }
                }
                return hash;
            }
        }
    }

}
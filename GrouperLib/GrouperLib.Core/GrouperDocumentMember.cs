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
                return _rules?.AsReadOnly();
            }
        }
        private readonly List<GrouperDocumentRule> _rules;

        public bool ShouldSerializeMemberType() => false;

        [JsonConstructor]
#pragma warning disable IDE0051 // "Remove unused private members" - Used when deserializing from JSON
        private GrouperDocumentMember(GroupMemberSources source, GroupMemberActions action, List<GrouperDocumentRule> rules)
#pragma warning restore IDE0051 // "Remove unused private members"
        {
            Source = source;
            Action = action;
            _rules = rules;
        }

        internal GrouperDocumentMember(GrouperDocumentMember documentMember)
        {
            Source = documentMember.Source;
            Action = documentMember.Action;
            _rules = documentMember.Rules.Select(r => new GrouperDocumentRule(r)).ToList();
        }

        public override bool Equals(object obj)
        {
            if (obj is not GrouperDocumentMember member)
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
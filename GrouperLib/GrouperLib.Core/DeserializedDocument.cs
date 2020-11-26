using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GrouperLib.Core
{
    internal class DeserializedDocument
    {
        public string Id { get; set; }
        [DefaultValue(0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int Interval { get; set; }
        public string GroupName { get; set; }
        public string GroupId { get; set; }
        public string Store { get; set; }
        [DefaultValue("KeepExisting")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string Owner { get; set; }
        public List<DeserializedMember> Members { get; set; }
    }

    internal class DeserializedMember
    {
        public string Source { get; set; }
        public string Action { get; set; }
        public List<DeserializedRule> Rules { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is DeserializedMember member))
            {
                return false;
            }
            if (!Source.IEquals(member.Source) || Action != member.Action)
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
                    foreach (DeserializedRule rule in Rules)
                    {
                        hash = hash * 31 + rule.GetHashCode();
                    }
                }
                return hash;
            }
        }
    }

    internal class DeserializedRule
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is DeserializedRule rule))
            {
                return false;
            }
            return Name.IEquals(rule.Name) && Value.IEquals(rule.Value);
        }

        public override int GetHashCode()
        {          
            // https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
            // unchecked to allow integer overflow
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(Name ?? string.Empty);
                hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(Value ?? string.Empty);
                return hash;
            }
        }
    }
}

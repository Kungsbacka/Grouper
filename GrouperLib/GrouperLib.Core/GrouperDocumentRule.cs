using Newtonsoft.Json;
using System;

namespace GrouperLib.Core
{
    public sealed class GrouperDocumentRule
    {
        [JsonProperty(PropertyName = "name", Order = 1)]
        public string Name { get; }

        [JsonProperty(PropertyName = "value", Order = 2)]
        public string Value { get; }

        [JsonConstructor]
        public GrouperDocumentRule(string name, string value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        internal GrouperDocumentRule(GrouperDocumentRule documentRule)
        {
            Name = documentRule.Name;
            Value = documentRule.Value;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not GrouperDocumentRule rule)
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
                hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
                hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Name} => {Value}";
        }
    }

}
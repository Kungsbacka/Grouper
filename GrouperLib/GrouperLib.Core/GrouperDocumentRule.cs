﻿using System.Text.Json.Serialization;

namespace GrouperLib.Core;

public sealed class GrouperDocumentRule
{
    public string Name { get; }

    public string Value { get; }

    [JsonConstructor]
    public GrouperDocumentRule(string name, string value)
    {
        Name = name;
        Value = value;
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
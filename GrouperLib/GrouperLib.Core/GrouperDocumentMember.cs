﻿using System.Text.Json.Serialization;

namespace GrouperLib.Core;

public sealed class GrouperDocumentMember
{
    [JsonPropertyName("source")]
    [JsonPropertyOrder(1)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GroupMemberSource Source { get; }

    [JsonPropertyName("action")]
    [JsonPropertyOrder(2)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GroupMemberAction Action { get; }

    [JsonPropertyName("rules")]
    [JsonPropertyOrder(3)]
    public IReadOnlyCollection<GrouperDocumentRule> Rules => _rules.AsReadOnly();

    private readonly List<GrouperDocumentRule> _rules;
        
    [JsonConstructor]
    public GrouperDocumentMember(GroupMemberSource source, GroupMemberAction action, List<GrouperDocumentRule> rules)
    {
        Source = source;
        Action = action;
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    internal GrouperDocumentMember(GrouperDocumentMember documentMember)
    {
        Source = documentMember.Source;
        Action = documentMember.Action;
        _rules = documentMember.Rules.Select(r => new GrouperDocumentRule(r)).ToList();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not GrouperDocumentMember member)
        {
            return false;
        }
        if (Source != member.Source || Action != member.Action)
        {
            return false;
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
            foreach (GrouperDocumentRule rule in Rules)
            {
                hash = hash * 31 + rule.GetHashCode();
            }
            return hash;
        }
    }
}
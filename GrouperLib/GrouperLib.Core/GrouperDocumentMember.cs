using System.Text.Json.Serialization;

namespace GrouperLib.Core;

public sealed class GrouperDocumentMember
{
    public GroupMemberSource Source { get; }

    public GroupMemberAction Action { get; }

    public IReadOnlyCollection<GrouperDocumentRule> Rules { get; }
        
    [JsonConstructor]
    public GrouperDocumentMember(GroupMemberSource source, GroupMemberAction action, IReadOnlyCollection<GrouperDocumentRule> rules)
    {
        Source = source;
        Action = action;
        Rules = rules;
    }

    internal GrouperDocumentMember(GrouperDocumentMember documentMember)
    {
        Source = documentMember.Source;
        Action = documentMember.Action;
        Rules = documentMember.Rules;
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
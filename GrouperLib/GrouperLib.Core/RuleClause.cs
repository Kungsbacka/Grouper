namespace GrouperLib.Core;

internal abstract class RuleClause
{
    protected RuleClause(params string[] names)
    {
        if (names.Length == 0)
        {
            throw new ArgumentException("A rule clause must declare at least one rule name.", nameof(names));
        }
        Names = names;
    }
    
    public IReadOnlyList<string> Names { get; }
    public abstract bool IsSatisfiedBy(IReadOnlySet<string> ruleNames);
}

internal sealed class RequiredClause(string name) : RuleClause(name)
{
    public override bool IsSatisfiedBy(IReadOnlySet<string> ruleNames) =>
        ruleNames.Contains(name);
}

internal sealed class OptionalClause(params string[] names) : RuleClause(names)
{
    public override bool IsSatisfiedBy(IReadOnlySet<string> ruleNames) => true;
}

internal sealed class AtLeastOneOfClause(params string[] names) : RuleClause(names)
{
    public override bool IsSatisfiedBy(IReadOnlySet<string> ruleNames) =>
        Names.Any(ruleNames.Contains);
}

internal sealed class RequiresClause(string dependent, string prerequisite)
    : RuleClause(dependent, prerequisite)
{
    public override bool IsSatisfiedBy(IReadOnlySet<string> ruleNames)
        => !ruleNames.Contains(dependent) || ruleNames.Contains(prerequisite);
}

internal sealed class MutuallyExclusiveGroupsClause : RuleClause
{
    private readonly string[][] _groups;

    public MutuallyExclusiveGroupsClause(params string[][] groups)
        : base([.. groups.SelectMany(g => g)])
    {
        if (groups.Length < 2 || groups.Any(g => g.Length == 0))
        {
            throw new ArgumentException("Mutually exclusive groups require at least two non-empty groups.", nameof(groups));
        }

        _groups = groups;
    }

    public override bool IsSatisfiedBy(IReadOnlySet<string> ruleNames)
        => _groups.Count(g => g.Any(ruleNames.Contains)) <= 1;
}

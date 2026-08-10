using GrouperLib.Language;

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

    /// <summary>
    /// The error to report when this clause is the reason a combination was rejected. Only ever
    /// called for a clause that just failed, so an implementation may assume its own failure
    /// condition holds.
    ///
    /// Abstract rather than virtual on purpose: a clause that cannot say why it rejected a document
    /// is not worth adding, so there is no generic fallback to inherit.
    /// </summary>
    public abstract (string ErrorId, object?[] Args) DescribeFailure(
        IReadOnlySet<string> ruleNames, GroupMemberSource memberSource);
}

internal sealed class RequiredClause(string name) : RuleClause(name)
{
    public override bool IsSatisfiedBy(IReadOnlySet<string> ruleNames) =>
        ruleNames.Contains(name);

    public override (string ErrorId, object?[] Args) DescribeFailure(
        IReadOnlySet<string> ruleNames, GroupMemberSource memberSource) =>
        (ResourceString.ValidationErrorRequiredRuleMissing, [name, memberSource]);
}

internal sealed class AtLeastOneOfClause(params string[] names) : RuleClause(names)
{
    public override bool IsSatisfiedBy(IReadOnlySet<string> ruleNames) =>
        Names.Any(ruleNames.Contains);

    public override (string ErrorId, object?[] Args) DescribeFailure(
        IReadOnlySet<string> ruleNames, GroupMemberSource memberSource) =>
        (ResourceString.ValidationErrorAtLeastOneRuleRequired, [memberSource, string.Join(", ", Names)]);
}

internal sealed class RequiresClause(string dependent, string prerequisite)
    : RuleClause(dependent, prerequisite)
{
    public override bool IsSatisfiedBy(IReadOnlySet<string> ruleNames)
        => !ruleNames.Contains(dependent) || ruleNames.Contains(prerequisite);

    public override (string ErrorId, object?[] Args) DescribeFailure(
        IReadOnlySet<string> ruleNames, GroupMemberSource memberSource) =>
        (ResourceString.ValidationErrorRuleRequiresAnotherRule, [dependent, prerequisite, memberSource]);
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

    /// <summary>
    /// Names one rule per colliding group rather than every name present. A group may contribute
    /// several names -- Skolform and Årskurs together -- but the conflict is between the groups, so
    /// one representative each is what identifies the choice that has to be dropped.
    /// </summary>
    public override (string ErrorId, object?[] Args) DescribeFailure(
        IReadOnlySet<string> ruleNames, GroupMemberSource memberSource)
    {
        IEnumerable<string> colliding = _groups
            .Where(g => g.Any(ruleNames.Contains))
            .Select(g => g.First(ruleNames.Contains));
        return (ResourceString.ValidationErrorMutuallyExclusiveRules,
            [memberSource, string.Join(", ", colliding)]);
    }
}

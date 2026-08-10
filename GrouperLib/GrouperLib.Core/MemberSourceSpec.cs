using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GrouperLib.Core;

internal sealed class MemberSourceSpec
{
    // Ordinal (not OrdinalIgnoreCase) is the default, but we add it explicitly
    // to show that this is important.
    private readonly HashSet<string> _names = new(StringComparer.Ordinal);
    private readonly HashSet<string> _repeatable = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Regex> _patterns = new(StringComparer.Ordinal);
    private readonly List<RuleClause> _clauses = [];
    private readonly List<ICustomValidator> _custom = [];

    private MemberSourceSpec(ResourceLocation location)
    {
        Location = location;
    }

    public static MemberSourceSpec Independent() => new(ResourceLocation.Independent);
    public static MemberSourceSpec OnPrem()      => new(ResourceLocation.OnPrem);
    public static MemberSourceSpec Azure()       => new(ResourceLocation.Azure);

    public MemberSourceSpec Required(string name) => Add(new RequiredClause(name));
    public MemberSourceSpec AtLeastOneOf(params string[] names) => Add(new AtLeastOneOfClause(names));
    public MemberSourceSpec Requires(string dependent, string prerequisite) => Add(new RequiresClause(dependent, prerequisite));
    public MemberSourceSpec MutuallyExclusiveGroups(params string[][] groups) => Add(new MutuallyExclusiveGroupsClause(groups));
    public MemberSourceSpec Repeatable(string name) { _repeatable.Add(name); return this; }
    public MemberSourceSpec Matches(string name, Regex pattern) { _patterns.Add(name, pattern); return this; }
    public MemberSourceSpec Custom(ICustomValidator validator) { _custom.Add(validator); return this; }

    /// <summary>
    /// Declares rule names that no clause constrains -- recognised, but legal in any combination.
    /// The one path that adds to the vocabulary without going through a clause.
    /// </summary>
    public MemberSourceSpec Optional(params string[] names)
    {
        if (names.Length == 0)
        {
            throw new ArgumentException("Declare at least one rule name.", nameof(names));
        }
        foreach (string name in names)
        {
            _names.Add(name);
        }
        return this;
    }

    private MemberSourceSpec Add(RuleClause clause)
    {
        _clauses.Add(clause);
        foreach (string name in clause.Names)
        {
            _names.Add(name);
        }
        return this;
    }

    public ResourceLocation Location { get; }

    public IReadOnlyList<ICustomValidator> CustomValidators => _custom;

    public bool IsKnownName(string? name) => name is not null && _names.Contains(name);

    public bool IsRepeatable(string? name) => name is not null && _repeatable.Contains(name);

    public bool TryGetPattern(string name, [NotNullWhen(true)] out Regex? pattern)
        => _patterns.TryGetValue(name, out pattern);

    public RuleClause? FirstUnsatisfied(IReadOnlySet<string> ruleNames)
        => _clauses.FirstOrDefault(clause => !clause.IsSatisfiedBy(ruleNames));
}

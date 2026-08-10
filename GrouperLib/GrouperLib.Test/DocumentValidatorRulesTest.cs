using GrouperLib.Core;
using GrouperLib.Language;

namespace GrouperLib.Test;

/// <summary>
/// Characterization suite for the rules validation half of <c>DocumentValidator</c>.
///
/// Every member source declares the exact *combinations* of rule names it accepts, so this
/// enumerates **every subset** of each source's recognised names and asserts the accept/reject
/// verdict against the combinations transcribed from <c>DocumentValidator.memberSources</c>.
/// Values are always valid samples, so only the combination is under test.
/// </summary>
public class DocumentValidatorRulesTest
{
    private static readonly Guid documentId = Guid.Parse("aa11bb22-cc33-dd44-ee55-ff6677889900");
    private static readonly Guid groupId = Guid.Parse("bb22cc33-dd44-ee55-ff66-778899001122");

    /// <summary>A group id other than the document's own, so self-reference validators pass.</summary>
    private const string OtherGroupId = "cc33dd44-ee55-ff66-1122-334455667788";

    private sealed record SourceSpec(
        GroupMemberSource Source,
        GroupStore CompatibleStore,
        string[] RuleNames,
        string[][] AcceptedCombinations,
        Dictionary<string, string> ValidValues);

    private static readonly SourceSpec[] specs =
    [
        new(GroupMemberSource.Personalsystem, GroupStore.AzureAd,
            ["Organisation", "Befattning", "IncludeManager"],
            [
                ["Organisation"],
                ["Befattning"],
                ["Organisation", "Befattning"],
                ["Organisation", "IncludeManager"],
                ["Organisation", "Befattning", "IncludeManager"]
            ],
            new() { ["Organisation"] = "011JABCDEF12", ["Befattning"] = "Lärare", ["IncludeManager"] = "true" }),

        new(GroupMemberSource.Elevregister, GroupStore.AzureAd,
            ["Roll", "Enhet", "Klass", "Grupp", "Skolform", "Årskurs"],
            [
                ["Roll"],
                ["Enhet"], ["Roll", "Enhet"],
                ["Klass"], ["Roll", "Klass"], ["Enhet", "Klass"], ["Roll", "Enhet", "Klass"],
                ["Grupp"], ["Roll", "Grupp"], ["Enhet", "Grupp"], ["Roll", "Enhet", "Grupp"],
                ["Skolform"], ["Roll", "Skolform"], ["Enhet", "Skolform"], ["Roll", "Enhet", "Skolform"],
                ["Årskurs"], ["Roll", "Årskurs"], ["Enhet", "Årskurs"], ["Roll", "Enhet", "Årskurs"],
                ["Skolform", "Årskurs"], ["Roll", "Skolform", "Årskurs"],
                ["Enhet", "Skolform", "Årskurs"], ["Roll", "Enhet", "Skolform", "Årskurs"]
            ],
            new()
            {
                ["Roll"] = "Elev",
                ["Enhet"] = "ARA",
                ["Klass"] = "EG_41e60dc2-1300-471d-a3a9-674664320e25",
                ["Grupp"] = "FG_41e60dc2-1300-471d-a3a9-674664320e25",
                ["Skolform"] = "GR",
                ["Årskurs"] = "5"
            }),

        new(GroupMemberSource.OnPremAdGroup, GroupStore.OnPremAd,
            ["Group"], [["Group"]],
            new() { ["Group"] = OtherGroupId }),

        new(GroupMemberSource.OnPremAdQuery, GroupStore.OnPremAd,
            ["LdapFilter", "SearchBase"],
            [["LdapFilter"], ["LdapFilter", "SearchBase"]],
            new() { ["LdapFilter"] = "(objectClass=user)", ["SearchBase"] = "OU=Users,DC=example,DC=com" }),

        new(GroupMemberSource.AzureAdGroup, GroupStore.AzureAd,
            ["Group"], [["Group"]],
            new() { ["Group"] = OtherGroupId }),

        new(GroupMemberSource.ExoGroup, GroupStore.Exo,
            ["Group"], [["Group"]],
            new() { ["Group"] = OtherGroupId }),

        new(GroupMemberSource.CustomView, GroupStore.AzureAd,
            ["View"], [["View"]],
            new() { ["View"] = "vwCustomMembers" }),

        new(GroupMemberSource.Static, GroupStore.AzureAd,
            ["Upn"], [["Upn"]],
            new() { ["Upn"] = "member@example.com" })
    ];

    private static SourceSpec SpecFor(GroupMemberSource source) => specs.Single(s => s.Source == source);

    /// <summary>Builds an otherwise-valid document carrying one member object with the given rules.</summary>
    private static GrouperDocument Build(SourceSpec spec, IEnumerable<string> ruleNames, GroupStore? store = null)
    {
        List<GrouperDocumentRule> rules =
            [.. ruleNames.Select(n => new GrouperDocumentRule(n, spec.ValidValues[n]))];
        GrouperDocumentMember member = new(spec.Source, GroupMemberAction.Include, rules);
        return new GrouperDocument(documentId, groupId, "Test Group", store ?? spec.CompatibleStore, [member]);
    }

    private static List<ValidationError> Validate(GrouperDocument document)
    {
        List<ValidationError> errors = [];
        DocumentValidator.Validate(document, errors);
        return errors;
    }

    /// <summary>Every subset of every source's rule names, with the expected verdict.</summary>
    public static TheoryData<GroupMemberSource, string, bool> AllRuleSubsets()
    {
        TheoryData<GroupMemberSource, string, bool> data = [];
        foreach (SourceSpec spec in specs)
        {
            HashSet<string> accepted =
                [.. spec.AcceptedCombinations.Select(c => string.Join(",", c.Order(StringComparer.Ordinal)))];

            int total = 1 << spec.RuleNames.Length;
            for (int mask = 0; mask < total; mask++)
            {
                List<string> subset = [];
                for (int bit = 0; bit < spec.RuleNames.Length; bit++)
                {
                    if ((mask & (1 << bit)) != 0)
                    {
                        subset.Add(spec.RuleNames[bit]);
                    }
                }
                string key = string.Join(",", subset.Order(StringComparer.Ordinal));
                data.Add(spec.Source, string.Join(",", subset), accepted.Contains(key));
            }
        }
        return data;
    }

    /// <summary>
    /// 88 cases: 64 for Elevregister, 8 for Personalsystem, 4 for OnPremAdQuery, 2 for each of the
    /// five single-rule sources. A verdict that flips in either direction is a behaviour change.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllRuleSubsets))]
    public void TestRuleCombinationVerdictMatchesTheEnumeratedRuleSets(
        GroupMemberSource source, string ruleNames, bool expectedValid)
    {
        SourceSpec spec = SpecFor(source);
        string[] names = ruleNames.Length == 0 ? [] : ruleNames.Split(',');

        List<ValidationError> errors = Validate(Build(spec, names));

        Assert.Equal(expectedValid, errors.Count == 0);
    }

    [Fact]
    public void TestMemberObjectWithNoRulesIsRejected()
    {
        List<ValidationError> errors = Validate(Build(SpecFor(GroupMemberSource.Static), []));

        Assert.Contains(ResourceString.ValidationErrorMemberObjectHasNoRules, errors.Select(e => e.ErrorId));
    }

    [Fact]
    public void TestUnrecognisedRuleNameIsRejected()
    {
        SourceSpec spec = SpecFor(GroupMemberSource.Static);
        GrouperDocumentMember member = new(spec.Source, GroupMemberAction.Include,
            [new GrouperDocumentRule("NoSuchRule", "value")]);
        GrouperDocument document = new(documentId, groupId, "Test Group", GroupStore.AzureAd, [member]);

        List<ValidationError> errors = Validate(document);

        Assert.Contains(ResourceString.ValidationErrorInvalidRuleName, errors.Select(e => e.ErrorId));
    }

    /// <summary>
    /// A rule combination that is individually recognised but not a legal *set* reports the
    /// combination error specifically, not a per-name error.
    /// </summary>
    [Fact]
    public void TestRecognisedNamesInAnIllegalCombinationReportTheCombinationError()
    {
        SourceSpec spec = SpecFor(GroupMemberSource.Elevregister);

        List<ValidationError> errors = Validate(Build(spec, ["Klass", "Skolform"]));

        Assert.Contains(ResourceString.ValidationErrorInvalidCombinationOfRules, errors.Select(e => e.ErrorId));
    }

    // ---- repeatable vs single-occurrence rule names -------------------------------------

    [Theory]
    [InlineData(GroupMemberSource.Personalsystem, "Befattning", "Lärare", "Rektor")]
    [InlineData(GroupMemberSource.Static, "Upn", "a@example.com", "b@example.com")]
    public void TestRepeatableRuleNameMayAppearTwice(
        GroupMemberSource source, string ruleName, string first, string second)
    {
        SourceSpec spec = SpecFor(source);
        GrouperDocumentMember member = new(source, GroupMemberAction.Include,
            [new GrouperDocumentRule(ruleName, first), new GrouperDocumentRule(ruleName, second)]);
        GrouperDocument document = new(documentId, groupId, "Test Group", spec.CompatibleStore, [member]);

        Assert.Empty(Validate(document));
    }

    [Fact]
    public void TestRepeatableArskursMayAppearTwice()
    {
        SourceSpec spec = SpecFor(GroupMemberSource.Elevregister);
        GrouperDocumentMember member = new(GroupMemberSource.Elevregister, GroupMemberAction.Include,
            [new GrouperDocumentRule("Årskurs", "5"), new GrouperDocumentRule("Årskurs", "6")]);
        GrouperDocument document = new(documentId, groupId, "Test Group", spec.CompatibleStore, [member]);

        Assert.Empty(Validate(document));
    }

    [Fact]
    public void TestNonRepeatableRuleNameTwiceIsRejected()
    {
        SourceSpec spec = SpecFor(GroupMemberSource.Elevregister);
        GrouperDocumentMember member = new(GroupMemberSource.Elevregister, GroupMemberAction.Include,
            [new GrouperDocumentRule("Enhet", "ARA"), new GrouperDocumentRule("Enhet", "ELOF")]);
        GrouperDocument document = new(documentId, groupId, "Test Group", spec.CompatibleStore, [member]);

        Assert.Contains(ResourceString.ValidationErrorDuplicateRuleName, Validate(document).Select(e => e.ErrorId));
    }

    /// <summary>
    /// The same name *and* value twice is rejected even when the name is repeatable -- it is
    /// duplication rather than a second selector.
    /// </summary>
    [Fact]
    public void TestSameRuleNameAndValueTwiceIsRejectedEvenWhenRepeatable()
    {
        SourceSpec spec = SpecFor(GroupMemberSource.Static);
        GrouperDocumentMember member = new(GroupMemberSource.Static, GroupMemberAction.Include,
            [new GrouperDocumentRule("Upn", "a@example.com"), new GrouperDocumentRule("Upn", "a@example.com")]);
        GrouperDocument document = new(documentId, groupId, "Test Group", spec.CompatibleStore, [member]);

        Assert.Contains(ResourceString.ValidationErrorDuplicateRule, Validate(document).Select(e => e.ErrorId));
    }

    // ---- rule name casing ---------------------------------------------------------------

    /// <summary>
    /// Rule names match ordinally, so a case variant is an unrecognised name and is reported as
    /// one. The distinction matters: the rule-set check would also reject this document, but as an
    /// illegal *combination*, which reads as nonsense for a member object carrying a single rule.
    /// </summary>
    [Fact]
    public void TestWronglyCasedRuleNameIsRejectedAsAnInvalidName()
    {
        SourceSpec spec = SpecFor(GroupMemberSource.Elevregister);
        GrouperDocumentMember member = new(GroupMemberSource.Elevregister, GroupMemberAction.Include,
            [new GrouperDocumentRule("enhet", "ARA")]);
        GrouperDocument document = new(documentId, groupId, "Test Group", spec.CompatibleStore, [member]);

        List<ValidationError> errors = Validate(document);

        Assert.Contains(ResourceString.ValidationErrorInvalidRuleName, errors.Select(e => e.ErrorId));
        Assert.DoesNotContain(ResourceString.ValidationErrorInvalidCombinationOfRules, errors.Select(e => e.ErrorId));
    }

    /// <summary>
    /// A repeatable name is the case that escapes if the accumulator keying rule names is
    /// case-insensitive: the correctly-cased rule seeds the bucket, the case variant merges into
    /// it, and the rule-set check only ever sees the good casing -- so the document validates
    /// clean. The variant has to come second for that to happen.
    /// </summary>
    [Theory]
    [InlineData(GroupMemberSource.Personalsystem, "Befattning", "Lärare", "befattning", "Rektor")]
    [InlineData(GroupMemberSource.Elevregister, "Årskurs", "5", "årskurs", "6")]
    [InlineData(GroupMemberSource.Static, "Upn", "a@example.com", "upn", "b@example.com")]
    public void TestWronglyCasedRepeatableRuleNameIsRejected(
        GroupMemberSource source, string name, string value, string casedName, string casedValue)
    {
        SourceSpec spec = SpecFor(source);
        GrouperDocumentMember member = new(source, GroupMemberAction.Include,
            [new GrouperDocumentRule(name, value), new GrouperDocumentRule(casedName, casedValue)]);
        GrouperDocument document = new(documentId, groupId, "Test Group", spec.CompatibleStore, [member]);

        Assert.Contains(ResourceString.ValidationErrorInvalidRuleName, Validate(document).Select(e => e.ErrorId));
    }

    /// <summary>
    /// Value regexes are keyed by rule name ordinally, so before names were matched ordinally a
    /// case variant passed the name checks and then missed its own regex -- the value went
    /// unvalidated. Rejecting the name is what closes that.
    /// </summary>
    [Fact]
    public void TestWronglyCasedRuleNameDoesNotSkipItsValueRegex()
    {
        SourceSpec spec = SpecFor(GroupMemberSource.Elevregister);
        GrouperDocumentMember member = new(GroupMemberSource.Elevregister, GroupMemberAction.Include,
            [new GrouperDocumentRule("skolform", "NotASchoolForm")]);
        GrouperDocument document = new(documentId, groupId, "Test Group", spec.CompatibleStore, [member]);

        Assert.NotEmpty(Validate(document));
    }

    /// <summary>
    /// Only names went ordinal. Values still compare case-insensitively, so a repeated name whose
    /// value differs only in case is duplication rather than a second selector.
    /// </summary>
    [Fact]
    public void TestRepeatableRuleWithValuesDifferingOnlyInCaseIsRejected()
    {
        SourceSpec spec = SpecFor(GroupMemberSource.Static);
        GrouperDocumentMember member = new(GroupMemberSource.Static, GroupMemberAction.Include,
            [new GrouperDocumentRule("Upn", "a@example.com"), new GrouperDocumentRule("Upn", "A@Example.com")]);
        GrouperDocument document = new(documentId, groupId, "Test Group", spec.CompatibleStore, [member]);

        Assert.Contains(ResourceString.ValidationErrorDuplicateRule, Validate(document).Select(e => e.ErrorId));
    }

    // ---- rule name and value emptiness --------------------------------------------------

    [Fact]
    public void TestEmptyRuleNameIsRejected()
    {
        SourceSpec spec = SpecFor(GroupMemberSource.Static);
        GrouperDocumentMember member = new(GroupMemberSource.Static, GroupMemberAction.Include,
            [new GrouperDocumentRule("", "member@example.com")]);
        GrouperDocument document = new(documentId, groupId, "Test Group", spec.CompatibleStore, [member]);

        Assert.Contains(ResourceString.ValidationErrorInvalidRuleName, Validate(document).Select(e => e.ErrorId));
    }

    [Fact]
    public void TestEmptyRuleValueIsRejected()
    {
        SourceSpec spec = SpecFor(GroupMemberSource.CustomView);
        GrouperDocumentMember member = new(GroupMemberSource.CustomView, GroupMemberAction.Include,
            [new GrouperDocumentRule("View", "")]);
        GrouperDocument document = new(documentId, groupId, "Test Group", spec.CompatibleStore, [member]);

        Assert.Contains(ResourceString.ValidationErrorRuleValueIsNullOrEmpty, Validate(document).Select(e => e.ErrorId));
    }

    // ---- duplicate member objects -------------------------------------------------------

    [Fact]
    public void TestTwoIdenticalMemberObjectsAreRejected()
    {
        SourceSpec spec = SpecFor(GroupMemberSource.Static);
        GrouperDocumentMember first = new(GroupMemberSource.Static, GroupMemberAction.Include,
            [new GrouperDocumentRule("Upn", "a@example.com")]);
        GrouperDocumentMember second = new(GroupMemberSource.Static, GroupMemberAction.Include,
            [new GrouperDocumentRule("Upn", "a@example.com")]);
        GrouperDocument document = new(documentId, groupId, "Test Group", spec.CompatibleStore, [first, second]);

        Assert.Contains(ResourceString.ValidationErrorDuplicateMemberObject, Validate(document).Select(e => e.ErrorId));
    }

    /// <summary>Same source and rules but the opposite action is a different member object.</summary>
    [Fact]
    public void TestSameRulesWithOppositeActionIsAllowed()
    {
        SourceSpec spec = SpecFor(GroupMemberSource.Static);
        GrouperDocumentMember include = new(GroupMemberSource.Static, GroupMemberAction.Include,
            [new GrouperDocumentRule("Upn", "a@example.com")]);
        GrouperDocumentMember exclude = new(GroupMemberSource.Static, GroupMemberAction.Exclude,
            [new GrouperDocumentRule("Upn", "a@example.com")]);
        GrouperDocument document = new(documentId, groupId, "Test Group", spec.CompatibleStore, [include, exclude]);

        Assert.Empty(Validate(document));
    }

    [Fact]
    public void TestDocumentWithNoMemberObjectsIsRejected()
    {
        GrouperDocument document = new(documentId, groupId, "Test Group", GroupStore.AzureAd, []);

        Assert.Contains(ResourceString.ValidationErrorNoMemberObjects, Validate(document).Select(e => e.ErrorId));
    }
}

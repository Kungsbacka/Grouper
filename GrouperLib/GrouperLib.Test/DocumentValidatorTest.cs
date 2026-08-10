using GrouperLib.Core;
using GrouperLib.Language;

namespace GrouperLib.Test;

/// <summary>
/// Characterization suite for the document-level half of <c>DocumentValidator</c>: the checks on
/// the document's own fields, the store/source location cross-check, the per-rule value regexes,
/// and the custom validators.
///
/// Companion to <see cref="DocumentValidatorRulesTest"/>, which covers rule-name combinations.
/// Both exist to make the refactor in docs/plans/2-validation-refactor.md provably
/// behaviour-preserving.
/// </summary>
public class DocumentValidatorTest
{
    private static readonly Guid documentId = Guid.Parse("aa11bb22-cc33-dd44-ee55-ff6677889900");
    private static readonly Guid groupId = Guid.Parse("bb22cc33-dd44-ee55-ff66-778899001122");
    private const string OtherGroupId = "cc33dd44-ee55-ff66-1122-334455667788";

    private static List<ValidationError> Validate(GrouperDocument document)
    {
        List<ValidationError> errors = [];
        DocumentValidator.Validate(document, errors);
        return errors;
    }

    private static GrouperDocument Document(
        Guid? id = null, Guid? group = null, string name = "Test Group",
        GroupStore store = GroupStore.AzureAd, int interval = 0,
        GroupMemberSource source = GroupMemberSource.Static, string ruleName = "Upn",
        string ruleValue = "member@example.com")
    {
        GrouperDocumentMember member = new(source, GroupMemberAction.Include,
            [new GrouperDocumentRule(ruleName, ruleValue)]);
        return new GrouperDocument(id ?? documentId, group ?? groupId, name, store, [member],
            GroupOwnerAction.KeepExisting, interval);
    }

    // ---- document-level fields ----------------------------------------------------------

    [Fact]
    public void TestValidDocumentProducesNoErrors()
    {
        Assert.Empty(Validate(Document()));
    }

    [Fact]
    public void TestEmptyDocumentIdIsRejected()
    {
        Assert.Contains(ResourceString.ValidationErrorDocumentIdNotValid,
            Validate(Document(id: Guid.Empty)).Select(e => e.ErrorId));
    }

    [Fact]
    public void TestEmptyGroupIdIsRejected()
    {
        Assert.Contains(ResourceString.ValidationErrorGroupIdNotValid,
            Validate(Document(group: Guid.Empty)).Select(e => e.ErrorId));
    }

    [Fact]
    public void TestEmptyGroupNameIsRejected()
    {
        Assert.Contains(ResourceString.ValidationErrorGroupNameIsNullOrEmpty,
            Validate(Document(name: "")).Select(e => e.ErrorId));
    }

    [Fact]
    public void TestNegativeIntervalIsRejected()
    {
        Assert.Contains(ResourceString.ValidationErrorIllegalInterval,
            Validate(Document(interval: -1)).Select(e => e.ErrorId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public void TestNonNegativeIntervalIsAccepted(int interval)
    {
        Assert.Empty(Validate(Document(interval: interval)));
    }

    /// <summary>
    /// An out-of-range store value stops validation before member checks run. This is the enum
    /// guard pattern -- unreachable for the four declared stores, deliberately present so a new
    /// store that nobody wired up fails loudly rather than validating by accident.
    /// </summary>
    [Fact]
    public void TestUnrecognisedStoreIsRejectedAndStopsValidation()
    {
        List<ValidationError> errors = Validate(Document(store: (GroupStore)99));

        Assert.Contains(ResourceString.ValidationErrorStoreNotRecognized, errors.Select(e => e.ErrorId));
    }

    [Fact]
    public void TestUnrecognisedMemberSourceIsRejected()
    {
        Assert.Contains(ResourceString.ValidationErrorInvalidMemberSource,
            Validate(Document(source: (GroupMemberSource)99)).Select(e => e.ErrorId));
    }

    // ---- store / source location cross-check --------------------------------------------

    public static TheoryData<GroupStore, GroupMemberSource, bool> StoreSourceMatrix()
    {
        // Independent sources work with any store; the rest must match the store's location.
        GroupMemberSource[] independent =
            [GroupMemberSource.Personalsystem, GroupMemberSource.Elevregister,
             GroupMemberSource.CustomView, GroupMemberSource.Static];
        GroupMemberSource[] onPrem = [GroupMemberSource.OnPremAdGroup, GroupMemberSource.OnPremAdQuery];
        GroupMemberSource[] azure = [GroupMemberSource.AzureAdGroup, GroupMemberSource.ExoGroup];
        GroupStore[] onPremStores = [GroupStore.OnPremAd, GroupStore.OpenE];
        GroupStore[] azureStores = [GroupStore.AzureAd, GroupStore.Exo];

        TheoryData<GroupStore, GroupMemberSource, bool> data = [];
        foreach (GroupStore store in (GroupStore[])[.. onPremStores, .. azureStores])
        {
            foreach (GroupMemberSource source in independent)
            {
                data.Add(store, source, true);
            }
            foreach (GroupMemberSource source in onPrem)
            {
                data.Add(store, source, onPremStores.Contains(store));
            }
            foreach (GroupMemberSource source in azure)
            {
                data.Add(store, source, azureStores.Contains(store));
            }
        }
        return data;
    }

    /// <summary>
    /// 32 cases. Stops an on-premises source feeding a cloud group and vice versa, which would
    /// otherwise fail later with a member-type mismatch at processing time.
    /// </summary>
    [Theory]
    [MemberData(nameof(StoreSourceMatrix))]
    public void TestStoreAndSourceLocationsMustAgree(GroupStore store, GroupMemberSource source, bool expectedValid)
    {
        (string name, string value) = source switch
        {
            GroupMemberSource.Personalsystem => ("Organisation", "011JABCDEF12"),
            GroupMemberSource.Elevregister => ("Roll", "Elev"),
            GroupMemberSource.CustomView => ("View", "vwMembers"),
            GroupMemberSource.Static => ("Upn", "member@example.com"),
            GroupMemberSource.OnPremAdQuery => ("LdapFilter", "(objectClass=user)"),
            _ => ("Group", OtherGroupId)
        };

        List<ValidationError> errors = Validate(Document(store: store, source: source, ruleName: name, ruleValue: value));

        Assert.Equal(expectedValid, errors.Count == 0);
        if (!expectedValid)
        {
            Assert.Contains(ResourceString.ValidationErrorInvalidCombinationOfGroupStoreAndMemberSource,
                errors.Select(e => e.ErrorId));
        }
    }

    // ---- custom validators --------------------------------------------------------------

    /// <summary>
    /// All three group-member sources attach the same <c>SelfReferenceValidator</c>. ExoGroup was
    /// the last to get it: a distribution group could previously list itself as its own member
    /// source where the Entra ID and on-premises equivalents rejected it. That was an oversight
    /// rather than a policy, closed after confirming no stored document relied on it -- so this
    /// asserting three sources rather than two is a deliberate behaviour change.
    /// </summary>
    [Theory]
    [InlineData(GroupMemberSource.AzureAdGroup, GroupStore.AzureAd)]
    [InlineData(GroupMemberSource.OnPremAdGroup, GroupStore.OnPremAd)]
    [InlineData(GroupMemberSource.ExoGroup, GroupStore.Exo)]
    public void TestGroupCannotBeItsOwnMemberSource(GroupMemberSource source, GroupStore store)
    {
        List<ValidationError> errors = Validate(Document(
            store: store, source: source, ruleName: "Group", ruleValue: groupId.ToString()));

        Assert.Contains(ResourceString.ValidationErrorSourceGroupSameAsTarget, errors.Select(e => e.ErrorId));
    }

    // ---- rule value regexes -------------------------------------------------------------

    [Theory]
    [InlineData("011JABCDEF12", true)]
    [InlineData("011jabcdef12", true)]          // case-insensitive
    [InlineData("011J", false)]                  // too short
    [InlineData("011JABCDEF123", false)]         // too long
    [InlineData("012JABCDEF12", false)]          // wrong prefix
    public void TestOrganisationValueFormat(string value, bool expectedValid)
    {
        List<ValidationError> errors = Validate(Document(
            source: GroupMemberSource.Personalsystem, ruleName: "Organisation", ruleValue: value));

        Assert.Equal(expectedValid, errors.Count == 0);
    }

    [Theory]
    [InlineData("FSK", true)]
    [InlineData("GR", true)]
    [InlineData("GRSÄR", true)]
    [InlineData("GY", true)]
    [InlineData("GYSÄR", true)]
    [InlineData("gr", true)]
    [InlineData("XX", false)]
    [InlineData("GRSAR", false)]                 // requires the Swedish Ä
    public void TestSkolformValueFormat(string value, bool expectedValid)
    {
        List<ValidationError> errors = Validate(Document(
            source: GroupMemberSource.Elevregister, ruleName: "Skolform", ruleValue: value));

        Assert.Equal(expectedValid, errors.Count == 0);
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("9", true)]
    [InlineData("F", true)]                      // förskoleklass
    [InlineData("10", false)]                    // single character only
    [InlineData("A", false)]
    public void TestArskursValueFormat(string value, bool expectedValid)
    {
        List<ValidationError> errors = Validate(Document(
            source: GroupMemberSource.Elevregister, ruleName: "Årskurs", ruleValue: value));

        Assert.Equal(expectedValid, errors.Count == 0);
    }

    [Theory]
    [InlineData("cc33dd44-ee55-ff66-1122-334455667788", true)]
    [InlineData("not-a-guid", false)]
    [InlineData("cc33dd44ee55ff661122334455667788", false)]   // hyphens required
    public void TestGroupValueMustBeAGuid(string value, bool expectedValid)
    {
        List<ValidationError> errors = Validate(Document(
            source: GroupMemberSource.AzureAdGroup, ruleName: "Group", ruleValue: value));

        Assert.Equal(expectedValid, errors.Count == 0);
    }

    /// <summary>
    /// Roll is fully anchored as <c>^(Personal|Elev)$</c>, so only the two exact values pass.
    ///
    /// It previously read <c>^Personal|Elev$</c>, which C# parses as <c>(^Personal)|(Elev$)</c> --
    /// accepting anything that merely started with "Personal" or ended with "Elev". The
    /// parentheses were added after confirming no stored document depended on the loose form.
    /// Three sibling patterns still have the un-parenthesised shape; see
    /// <see cref="TestEnhetValueFormatIsStillLooselyAnchored"/>.
    /// </summary>
    [Theory]
    [InlineData("Elev", true)]
    [InlineData("Personal", true)]
    [InlineData("elev", true)]                   // case-insensitive
    [InlineData("Personalxyz", false)]           // no longer matches: anchors now bind the alternation
    [InlineData("xyzElev", false)]
    [InlineData("Larare", false)]
    public void TestRollValueFormat(string value, bool expectedValid)
    {
        List<ValidationError> errors = Validate(Document(
            source: GroupMemberSource.Elevregister, ruleName: "Roll", ruleValue: value));

        Assert.Equal(expectedValid, errors.Count == 0);
    }

    /// <summary>
    /// IncludeManager is fully anchored as <c>^(true|false)$</c>, tightened alongside Roll.
    /// </summary>
    [Theory]
    [InlineData("true", true)]
    [InlineData("false", true)]
    [InlineData("TRUE", true)]
    [InlineData("trueish", false)]               // no longer matches ^true
    [InlineData("maybefalse", false)]            // no longer matches false$
    [InlineData("truthy", false)]
    [InlineData("yes", false)]
    public void TestIncludeManagerValueFormat(string value, bool expectedValid)
    {
        GrouperDocumentMember member = new(GroupMemberSource.Personalsystem, GroupMemberAction.Include,
            [new GrouperDocumentRule("Organisation", "011JABCDEF12"),
             new GrouperDocumentRule("IncludeManager", value)]);
        GrouperDocument document = new(documentId, groupId, "Test Group", GroupStore.AzureAd, [member]);

        Assert.Equal(expectedValid, Validate(document).Count == 0);
    }

    /// <summary>
    /// Enhet is fully anchored as <c>^(ARA|ELOF|S_?guid|guid)$</c>: the two literals, or a GUID
    /// with an optional <c>S_</c> prefix, and nothing either side.
    ///
    /// It previously read <c>^ARA|ELOF|S_?guid|guid$</c>, which C# parses as
    /// <c>(^ARA)|(ELOF)|(S_?guid)|(guid$)</c>. With more than two alternatives that is worse than
    /// the two-alternative case, because a *middle* alternative is unanchored at both ends -- so
    /// "xyzELOFxyz" validated. The rejections below are the cases the parentheses fixed.
    /// </summary>
    [Theory]
    [InlineData("ARA", true)]
    [InlineData("ELOF", true)]
    [InlineData("41e60dc2-1300-471d-a3a9-674664320e25", true)]
    [InlineData("S_41e60dc2-1300-471d-a3a9-674664320e25", true)]
    [InlineData("S41e60dc2-1300-471d-a3a9-674664320e25", true)]          // underscore optional
    [InlineData("ARAxyz", false)]                                        // was: matched ^ARA
    [InlineData("xyzELOFxyz", false)]                                    // was: middle alternative, unanchored
    [InlineData("prefix41e60dc2-1300-471d-a3a9-674664320e25", false)]     // was: ended with a GUID
    [InlineData("41e60dc2-1300-471d-a3a9-674664320e25suffix", false)]
    [InlineData("xyzARA", false)]
    [InlineData("NOPE", false)]
    public void TestEnhetValueFormat(string value, bool expectedValid)
    {
        List<ValidationError> errors = Validate(Document(
            source: GroupMemberSource.Elevregister, ruleName: "Enhet", ruleValue: value));

        Assert.Equal(expectedValid, errors.Count == 0);
    }

    /// <summary>
    /// Klass and Grupp were tightened alongside Enhet. The trailing-junk cases are the ones that
    /// mattered most in practice: <c>EG_&lt;guid&gt;trailing</c> used to validate and then match
    /// nothing at processing time, so the document silently contributed no members.
    /// </summary>
    [Theory]
    [InlineData("Klass", "EG_41e60dc2-1300-471d-a3a9-674664320e25", true)]
    [InlineData("Klass", "EG41e60dc2-1300-471d-a3a9-674664320e25", true)]
    [InlineData("Klass", "41e60dc2-1300-471d-a3a9-674664320e25", true)]
    [InlineData("Klass", "EG_41e60dc2-1300-471d-a3a9-674664320e25trailing", false)]   // was accepted
    [InlineData("Klass", "prefix41e60dc2-1300-471d-a3a9-674664320e25", false)]        // was accepted
    [InlineData("Klass", "EG_notaguid", false)]
    [InlineData("Grupp", "FG_41e60dc2-1300-471d-a3a9-674664320e25", true)]
    [InlineData("Grupp", "41e60dc2-1300-471d-a3a9-674664320e25", true)]
    [InlineData("Grupp", "FG_41e60dc2-1300-471d-a3a9-674664320e25trailing", false)]   // was accepted
    [InlineData("Grupp", "FG_bad", false)]
    public void TestKlassAndGruppValueFormats(string ruleName, string value, bool expectedValid)
    {
        List<ValidationError> errors = Validate(Document(
            source: GroupMemberSource.Elevregister, ruleName: ruleName, ruleValue: value));

        Assert.Equal(expectedValid, errors.Count == 0);
    }

    /// <summary>
    /// Guards the anchoring fix as a class, not case by case: every rule value regex must bind its
    /// anchors to the whole alternation. Appending junk to a valid value must never still validate.
    /// A new pattern written <c>^a|b$</c> fails here without anyone remembering to add cases.
    /// </summary>
    [Theory]
    [InlineData(GroupMemberSource.Elevregister, "Roll", "Elev")]
    [InlineData(GroupMemberSource.Elevregister, "Enhet", "ARA")]
    [InlineData(GroupMemberSource.Elevregister, "Enhet", "41e60dc2-1300-471d-a3a9-674664320e25")]
    [InlineData(GroupMemberSource.Elevregister, "Klass", "EG_41e60dc2-1300-471d-a3a9-674664320e25")]
    [InlineData(GroupMemberSource.Elevregister, "Grupp", "FG_41e60dc2-1300-471d-a3a9-674664320e25")]
    [InlineData(GroupMemberSource.Elevregister, "Skolform", "GR")]
    [InlineData(GroupMemberSource.Elevregister, "Årskurs", "5")]
    [InlineData(GroupMemberSource.Personalsystem, "Organisation", "011JABCDEF12")]
    [InlineData(GroupMemberSource.AzureAdGroup, "Group", OtherGroupId)]
    public void TestNoRuleValueRegexAcceptsSurroundingJunk(
        GroupMemberSource source, string ruleName, string validValue)
    {
        GroupStore store = source == GroupMemberSource.AzureAdGroup ? GroupStore.AzureAd : GroupStore.AzureAd;

        Assert.Empty(Validate(Document(store: store, source: source, ruleName: ruleName, ruleValue: validValue)));
        Assert.NotEmpty(Validate(Document(store: store, source: source, ruleName: ruleName, ruleValue: validValue + "junk")));
        Assert.NotEmpty(Validate(Document(store: store, source: source, ruleName: ruleName, ruleValue: "junk" + validValue)));
    }

    /// <summary>Static members are UPN-validated by a custom validator, not a regex.</summary>
    [Theory]
    [InlineData("member@example.com", true)]
    [InlineData("no-at-sign", false)]
    [InlineData("two@at@signs.com", false)]
    [InlineData("@example.com", false)]
    [InlineData("member@", false)]
    public void TestStaticUpnIsValidated(string value, bool expectedValid)
    {
        List<ValidationError> errors = Validate(Document(ruleName: "Upn", ruleValue: value));

        Assert.Equal(expectedValid, errors.Count == 0);
        if (!expectedValid)
        {
            Assert.Contains(ResourceString.ValidationErrorInvalidUpn, errors.Select(e => e.ErrorId));
        }
    }

    /// <summary>
    /// The UPN domain must match in full. <c>UpnValidator.UpnRegex</c> is applied as
    /// <c>Matches(domain).Count == 1</c>, so before it was anchored at the start it accepted a
    /// domain whenever *any suffix* of it matched -- junk before a valid domain passed, including a
    /// space. Adding <c>^</c> closed that; these are the cases it fixed.
    ///
    /// This was the last of the anchoring gaps, and the only one outside DocumentValidator's
    /// rule-value patterns. It mattered for the same reason as <c>EG_&lt;guid&gt;trailing</c>: the
    /// document validated, then <c>spGrouperStaticMember</c> matched nobody, so the member object
    /// silently contributed no members.
    /// </summary>
    [Theory]
    [InlineData("member@!!!example.com")]
    [InlineData("member@bad domain.example.com")]
    [InlineData("member@..example.com")]
    [InlineData("member@-example.com")]
    public void TestUpnDomainRejectsJunkBeforeAValidSuffix(string upn)
    {
        Assert.Contains(ResourceString.ValidationErrorInvalidUpn,
            Validate(Document(ruleName: "Upn", ruleValue: upn)).Select(e => e.ErrorId));
    }

    /// <summary>
    /// A bracketed IP literal takes the conditional branch of the pattern, <c>(?(\[)…|…)</c>.
    /// Worth keeping: prepending <c>^</c> to a conditional construct is the kind of change that
    /// could plausibly have broken this branch, and this is what proves it did not.
    /// </summary>
    [Theory]
    [InlineData("member@[192.168.1.1]", true)]
    [InlineData("member@[999.999.999.999]", true)]      // shape only; octet range is not checked
    [InlineData("member@[192.168.1]", false)]           // needs four octets
    [InlineData("member@example.c", false)]             // final label needs at least two characters
    public void TestUpnDomainEdgeCases(string upn, bool expectedValid)
    {
        Assert.Equal(expectedValid, Validate(Document(ruleName: "Upn", ruleValue: upn)).Count == 0);
    }

    /// <summary>
    /// Guard for the domain half of the UPN pattern, matching
    /// <see cref="TestNoRuleValueRegexAcceptsSurroundingJunk"/>: trailing junk must be rejected, so
    /// a future edit that loosens the anchors fails here.
    ///
    /// Only trailing junk is asserted. Leading junk lands in the *user-name* half, which is
    /// validated by a character blocklist rather than by this regex -- see
    /// <see cref="TestUpnUserNameAcceptsWhitespace"/>.
    /// </summary>
    [Theory]
    [InlineData("member@example.com")]
    [InlineData("first.last@sub.example.com")]
    public void TestValidUpnWithTrailingJunkIsRejected(string validUpn)
    {
        Assert.Empty(Validate(Document(ruleName: "Upn", ruleValue: validUpn)));
        Assert.NotEmpty(Validate(Document(ruleName: "Upn", ruleValue: validUpn + " junk")));
        Assert.NotEmpty(Validate(Document(ruleName: "Upn", ruleValue: validUpn + ".c")));
    }

    /// <summary>
    /// Characterizes the user-name half of the UPN check, which is a character blocklist rather
    /// than a pattern. <c>UpnValidator.invalidCharsInUserName</c> lists
    /// <c>!@#$%^&amp;*()+=[]{}\/|;:"&lt;&gt;?,</c> -- so whitespace, apostrophes and other
    /// characters that cannot appear in a real UPN are accepted. "junk member@example.com"
    /// validates today.
    ///
    /// Milder than the anchoring gaps and the same practical outcome: the document validates and
    /// then matches nobody. Recorded, not fixed -- tightening this is a blocklist-versus-allowlist
    /// decision, not a one-character anchor. If these flip to rejecting, the check was tightened;
    /// update the expectations.
    /// </summary>
    [Theory]
    [InlineData("junk member@example.com")]
    [InlineData("first last@example.com")]
    [InlineData("o'brien@example.com")]
    public void TestUpnUserNameAcceptsWhitespace(string upn)
    {
        Assert.Empty(Validate(Document(ruleName: "Upn", ruleValue: upn)));
    }

    /// <summary>
    /// Custom validators run only after everything else passes, so a document with a structural
    /// error does not also report UPN errors.
    /// </summary>
    [Fact]
    public void TestCustomValidatorsDoNotRunWhenEarlierChecksFail()
    {
        List<ValidationError> errors = Validate(Document(name: "", ruleValue: "not-a-upn"));

        Assert.Contains(ResourceString.ValidationErrorGroupNameIsNullOrEmpty, errors.Select(e => e.ErrorId));
        Assert.DoesNotContain(ResourceString.ValidationErrorInvalidUpn, errors.Select(e => e.ErrorId));
    }

    /// <summary>Sources with no declared value validation accept anything non-empty.</summary>
    [Theory]
    [InlineData(GroupMemberSource.CustomView, GroupStore.AzureAd, "View", "anything at all")]
    [InlineData(GroupMemberSource.OnPremAdQuery, GroupStore.OnPremAd, "LdapFilter", "not(even(valid")]
    public void TestUnvalidatedRuleValuesAreAccepted(
        GroupMemberSource source, GroupStore store, string name, string value)
    {
        Assert.Empty(Validate(Document(store: store, source: source, ruleName: name, ruleValue: value)));
    }
}

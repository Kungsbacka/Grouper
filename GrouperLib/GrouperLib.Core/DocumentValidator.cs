using GrouperLib.Language;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GrouperLib.Core;

internal static class DocumentValidator
{
    private enum ResourceLocation { Independent, OnPrem, Azure }

    private class DocumentMemberValidationRules
    {
        public ResourceLocation Location { get; init; }
        public string[][] RuleSets { get; init; } = [];
        public string[] MultipleRulesAllowed { get; init; } = [];
        public Dictionary<string, Regex> ValidationRules { get; init; } = new();
        public ICustomValidator[] CustomValidators { get; init; } = [];

        public bool InAnyRuleSet(string? ruleName)
        {
            return RuleSets.Any(rs => rs.Any(r => r.IEquals(ruleName)));
        }

        public bool HasMatchingRuleSet(IEnumerable<string?> rules)
        {
            var rulesHashSet = new HashSet<string?>(rules, StringComparer.OrdinalIgnoreCase);
            return RuleSets.Any(ruleSet => ruleSet.All(rulesHashSet.Contains) && rulesHashSet.Count == ruleSet.Length);
        }

        public bool IsMultipleRulesAllowed(string? ruleName)
        {
            return MultipleRulesAllowed.Any(r => r.IEquals(ruleName));
        }
    }

    private static readonly Dictionary<GroupStore, ResourceLocation> storeLocations = new() {
        { GroupStore.OnPremAd, ResourceLocation.OnPrem },
        { GroupStore.AzureAd, ResourceLocation.Azure },
        { GroupStore.Exo, ResourceLocation.Azure },
        { GroupStore.OpenE, ResourceLocation.OnPrem }
    };

    private static readonly Regex guidRegex = new(
        "^[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Regex eregExtIdRegex = new(
        "^ARXX|LIXX|BEDA|S_[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}|[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    // Gymnasiet currently uses another system with other IDs for classes and groups
    //
    //private static readonly Regex eregKlassIdRegex = new Regex(
    //    "^EG_[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}$",
    //    RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
    //);

    //private static readonly Regex eregGruppIdRegex = new Regex(
    //    "^FG_[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}$",
    //    RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
    //);

    private static readonly Regex personecOrgIdRegex = new(
        "^011J[0-9A-Z]{8}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Regex trueFalseRegex = new(
        "^true|false$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Regex rollRegex = new(
        "^Personal|Elev$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Regex arskursRegex = new(
        "^[0-9F]$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Regex skolformRegex = new(
        "^(FSK|GR|GRSÄR|GY|GYSÄR)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Dictionary<GroupMemberSource, DocumentMemberValidationRules> memberSources = new()
    {
        { GroupMemberSource.Personalsystem, new DocumentMemberValidationRules()
            {
                Location = ResourceLocation.Independent,
                RuleSets =
                [
                    ["Organisation"],
                    ["Befattning"],
                    ["Organisation", "Befattning"],
                    ["Organisation", "IncludeManager"],
                    ["Organisation", "Befattning", "IncludeManager"]
                ],
                ValidationRules = new Dictionary<string, Regex>()
                {
                    {"Organisation", personecOrgIdRegex },
                    {"IncludeManager", trueFalseRegex}
                },
                MultipleRulesAllowed = ["Befattning"]
            }
        },
        { GroupMemberSource.Elevregister, new DocumentMemberValidationRules()
            {
                Location = ResourceLocation.Independent,
                RuleSets =
                [
                    ["Roll"],
                    ["Enhet"],
                    ["Roll", "Enhet"],
                    ["Klass"],
                    ["Roll", "Klass"],
                    ["Enhet", "Klass"],
                    ["Roll", "Enhet", "Klass"],
                    ["Grupp"],
                    ["Roll", "Grupp"],
                    ["Enhet", "Grupp"],
                    ["Roll", "Enhet", "Grupp"],
                    ["Skolform"],
                    ["Roll", "Skolform"],
                    ["Enhet", "Skolform"],
                    ["Roll", "Enhet", "Skolform"],
                    ["Årskurs"],
                    ["Roll", "Årskurs"],
                    ["Enhet", "Årskurs"],
                    ["Roll", "Enhet", "Årskurs"],
                    ["Skolform", "Årskurs"],
                    ["Roll", "Skolform", "Årskurs"],
                    ["Enhet", "Skolform", "Årskurs"],
                    ["Roll", "Enhet", "Skolform", "Årskurs"]
                ],
                ValidationRules = new Dictionary<string, Regex>()
                {
                    {"Skolform", skolformRegex},
                    {"Enhet", eregExtIdRegex},
                    //{"Klass", eregKlassIdRegex },
                    //{"Grupp", eregGruppIdRegex },
                    {"Årskurs", arskursRegex},
                    {"Roll", rollRegex }
                },
                MultipleRulesAllowed = ["Årskurs"]
            }
        },
        { GroupMemberSource.OnPremAdGroup, new DocumentMemberValidationRules()
            {
                Location = ResourceLocation.OnPrem,
                RuleSets =
                [
                    ["Group"]
                ],
                ValidationRules = new Dictionary<string, Regex>()
                {
                    {"Group", guidRegex}
                },
                CustomValidators =
                [
                    new OnPremAdValidator()
                ]
            }
        },
        { GroupMemberSource.OnPremAdQuery, new DocumentMemberValidationRules()
            {
                Location = ResourceLocation.OnPrem,
                RuleSets =
                [
                    ["LdapFilter"],
                    ["LdapFilter", "SearchBase"]
                ]
            }
        },
        { GroupMemberSource.AzureAdGroup, new DocumentMemberValidationRules()
            {
                Location = ResourceLocation.Azure,
                RuleSets = [["Group"]],
                ValidationRules = new Dictionary<string, Regex>()
                {
                    {"Group", guidRegex}
                },
                CustomValidators =
                [
                    new AzureAdValidator()
                ]
            }
        },
        { GroupMemberSource.ExoGroup, new DocumentMemberValidationRules()
            {
                Location = ResourceLocation.Azure,
                RuleSets = [["Group"]],
                ValidationRules = new Dictionary<string, Regex>()
                {
                    {"Group", guidRegex}
                }
            }
        },
        { GroupMemberSource.CustomView, new DocumentMemberValidationRules()
            {
                Location = ResourceLocation.Independent,
                RuleSets = [["View"]]
            }
        },
        { GroupMemberSource.Static, new DocumentMemberValidationRules()
            {
                Location = ResourceLocation.Independent,
                RuleSets = [["Upn"]],
                CustomValidators =
                [
                    new UpnValidator()
                ],
                MultipleRulesAllowed = ["Upn"]
            }
        }
    };

    private static void InternalValidateDocument(GrouperDocument document, List<ValidationError> validationErrors)
    {
        if (document.Id == Guid.Empty)
        {
            validationErrors.Add(new ValidationError(nameof(document.Id), ResourceString.ValidationErrorDocumentIdNotValid, document.Id));
        }
        if (document.Interval < 0)
        {
            validationErrors.Add(new ValidationError(nameof(document.Interval), ResourceString.ValidationErrorIllegalInterval));
        }
        if (string.IsNullOrEmpty(document.GroupName))
        {
            validationErrors.Add(new ValidationError(nameof(document.GroupName), ResourceString.ValidationErrorGroupNameIsNullOrEmpty));
        }
        if (document.GroupId == Guid.Empty)
        {
            validationErrors.Add(new ValidationError(nameof(document.GroupId), ResourceString.ValidationErrorGroupIdNotValid, document.GroupId));
        }
        if (!storeLocations.TryGetValue(document.Store, out ResourceLocation groupLocation))
        {
            validationErrors.Add(new ValidationError(nameof(document.Store), ResourceString.ValidationErrorStoreNotRecognized, document.Store.ToString()));
            return;
        }
        InternalValidateMembers(document.Members, document.Store, groupLocation, validationErrors);
        if (validationErrors.Count > 0)
        {
            return;
        }
        foreach (GrouperDocumentMember documentMember in document.Members)
        {
            if (memberSources.TryGetValue(documentMember.Source, out DocumentMemberValidationRules? memberSourceInfo))
            {
                foreach (ICustomValidator validator in memberSourceInfo.CustomValidators)
                {
                    validator.Validate(document, documentMember, validationErrors);
                }
            }
        }
    }

    private static void InternalValidateMembers(IReadOnlyCollection<GrouperDocumentMember> documentMembers, GroupStore groupStore, ResourceLocation groupLocation, List<ValidationError> validationErrors)
    {
        if (documentMembers.Count == 0)
        {
            validationErrors.Add(new ValidationError(nameof(GrouperDocument.Members), ResourceString.ValidationErrorNoMemberObjects));
            return;
        }
        foreach (GrouperDocumentMember member in documentMembers)
        {
            if (memberSources.TryGetValue(member.Source, out DocumentMemberValidationRules? memberSourceInfo))
            {
                if (memberSourceInfo.Location != ResourceLocation.Independent && memberSourceInfo.Location != groupLocation)
                {
                    validationErrors.Add(new ValidationError(nameof(GrouperDocumentMember.Source), ResourceString.ValidationErrorInvalidCombinationOfGroupStoreAndMemberSource, groupStore, member.Source));
                }
            }
            else
            {
                validationErrors.Add(new ValidationError(nameof(GrouperDocumentMember.Source), ResourceString.ValidationErrorInvalidMemberSource, member.Source));
            }
        }
        if (validationErrors.Count > 0)
        {
            return;
        }
        HashSet<GrouperDocumentMember> members = [];
        foreach (GrouperDocumentMember member in documentMembers)
        {
            if (members.Add(member))
            {
                InternalValidateRules(member.Rules, member.Source, validationErrors);
            }
            else
            {
                validationErrors.Add(new ValidationError(nameof(GrouperDocumentMember.Action), ResourceString.ValidationErrorDuplicateMemberObject, member.Source, member.Action, member.Rules.Count));
            }
        }
    }

    private static void InternalValidateRules(IReadOnlyCollection<GrouperDocumentRule> documentRules, GroupMemberSource memberSource, List<ValidationError> validationErrors)
    {
        if (documentRules.Count == 0)
        {
            validationErrors.Add(new ValidationError(nameof(GrouperDocumentMember.Rules), ResourceString.ValidationErrorMemberObjectHasNoRules));
            return;
        }
        var rules = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (!memberSources.TryGetValue(memberSource, out DocumentMemberValidationRules? memberSourceInfo))
        {
            validationErrors.Add(new ValidationError(nameof(GrouperDocumentMember.Source), ResourceString.ValidationErrorInvalidMemberSource, memberSource));
            return;
        }
        foreach (GrouperDocumentRule rule in documentRules)
        {
            if (string.IsNullOrEmpty(rule.Name))
            {
                validationErrors.Add(new ValidationError(nameof(rule.Name), ResourceString.ValidationErrorInvalidRuleName, rule.Name, memberSource));
            }
            else
            {
                if (rules.TryGetValue(rule.Name, out HashSet<string>? values))
                {
                    if (!memberSourceInfo.IsMultipleRulesAllowed(rule.Name))
                    {
                        validationErrors.Add(new ValidationError(nameof(rule.Name), ResourceString.ValidationErrorDuplicateRuleName, rule.Name));
                    }
                    if (!values.Add(rule.Value))
                    {
                        validationErrors.Add(new ValidationError("Rule", ResourceString.ValidationErrorDuplicateRule, rule.Name, rule.Value));
                    }
                }
                else
                {
                    rules.Add(rule.Name, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rule.Value });
                }
                if (!memberSourceInfo.InAnyRuleSet(rule.Name))
                {
                    validationErrors.Add(new ValidationError(nameof(rule.Name), ResourceString.ValidationErrorInvalidRuleName, rule.Name, memberSource));
                }
            }
        }
        if (validationErrors.Count > 0)
        {
            return;
        }
        if (!memberSourceInfo.HasMatchingRuleSet(rules.Keys))
        {
            validationErrors.Add(new ValidationError(nameof(GrouperDocumentMember.Rules), ResourceString.ValidationErrorInvalidCombinationOfRules, memberSource));
        }
        foreach (GrouperDocumentRule rule in documentRules)
        {
            if (string.IsNullOrEmpty(rule.Value))
            {
                validationErrors.Add(new ValidationError(nameof(rule.Value), ResourceString.ValidationErrorRuleValueIsNullOrEmpty, rule.Name));
            }
            else if (memberSourceInfo.ValidationRules.TryGetValue(rule.Name, out Regex? validationRegex))
            {
                if (!validationRegex.IsMatch(rule.Value))
                {
                    validationErrors.Add(new ValidationError(nameof(rule.Value), ResourceString.ValidationErrorRuleValueDoesNotValidate, rule.Name, rule.Value));
                }
            }
        }
    }

    internal static GrouperDocument? DeserializeAndValidate(string json, List<ValidationError> validationErrors)
    {
        if (string.IsNullOrEmpty(json))
        {
            validationErrors.Add(new ValidationError(nameof(json), ResourceString.ValidationJsonMissingError));
            return null;
        }
        GrouperDocument? document = null;
        try
        {
            document = JsonSerializer.Deserialize<GrouperDocument>(json);
        }
        catch (JsonException ex)
        {
            validationErrors.Add(new ValidationError(nameof(json), ResourceString.ValidationJsonParsingError, ex.LineNumber!, ex.BytePositionInLine!, ex.Message));
        }
        if (document == null)
        {
            validationErrors.Add(new ValidationError(nameof(json), ResourceString.DefaultValidationError));
            return null;
        }
        InternalValidateDocument(document, validationErrors);
        return document;
    }

    internal static void Validate(GrouperDocument document, List<ValidationError> validationErrors)
    {
        InternalValidateDocument(document, validationErrors);
    }
}
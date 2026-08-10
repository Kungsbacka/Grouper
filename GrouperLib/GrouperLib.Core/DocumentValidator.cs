using GrouperLib.Language;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GrouperLib.Core;

/// <summary>
/// The validation engine. Every check here is shared by all member sources; what each source
/// permits is declared in DocumentValidator.Sources.cs. If a check in this file needs to know
/// which source it is looking at, something belongs in a spec instead.
/// </summary>
internal static partial class DocumentValidator
{
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
            if (memberSources.TryGetValue(documentMember.Source, out MemberSourceSpec? spec))
            {
                foreach (ICustomValidator validator in spec.CustomValidators)
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
            if (memberSources.TryGetValue(member.Source, out MemberSourceSpec? spec))
            {
                if (spec.Location != ResourceLocation.Independent && spec.Location != groupLocation)
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
                // Every source resolved in the loop above, and the gate on it returned if any did
                // not -- so the indexer cannot miss here.
                InternalValidateRules(member.Rules, member.Source, memberSources[member.Source], validationErrors);
            }
            else
            {
                validationErrors.Add(new ValidationError(nameof(GrouperDocumentMember.Action), ResourceString.ValidationErrorDuplicateMemberObject, member.Source, member.Action, member.Rules.Count));
            }
        }
    }

    private static void InternalValidateRules(IReadOnlyCollection<GrouperDocumentRule> documentRules, GroupMemberSource memberSource, MemberSourceSpec spec, List<ValidationError> validationErrors)
    {
        if (documentRules.Count == 0)
        {
            validationErrors.Add(new ValidationError(nameof(GrouperDocumentMember.Rules), ResourceString.ValidationErrorMemberObjectHasNoRules));
            return;
        }
        // Rule names are matched ordinally; rule values are not. A name spelled with the wrong
        // casing is an unrecognised name, which is what stops it from missing its value regex.
        var rules = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
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
                    if (!spec.IsRepeatable(rule.Name))
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
                if (!spec.IsKnownName(rule.Name))
                {
                    validationErrors.Add(new ValidationError(nameof(rule.Name), ResourceString.ValidationErrorInvalidRuleName, rule.Name, memberSource));
                }
            }
        }
        if (validationErrors.Count > 0)
        {
            return;
        }
        // Every name is recognised by now, so the clauses only have to judge the combination. The
        // failing clause names its own reason -- only the first is reported, so an admin fixes one
        // problem at a time rather than reading a list that may partly resolve itself.
        var ruleNames = new HashSet<string>(rules.Keys, StringComparer.Ordinal);
        if (spec.FirstUnsatisfied(ruleNames) is RuleClause unsatisfied)
        {
            (string errorId, object?[] args) = unsatisfied.DescribeFailure(ruleNames, memberSource);
            validationErrors.Add(new ValidationError(nameof(GrouperDocumentMember.Rules), errorId, args));
        }
        foreach (GrouperDocumentRule rule in documentRules)
        {
            if (string.IsNullOrEmpty(rule.Value))
            {
                validationErrors.Add(new ValidationError(nameof(rule.Value), ResourceString.ValidationErrorRuleValueIsNullOrEmpty, rule.Name));
            }
            else if (spec.TryGetPattern(rule.Name, out Regex? validationRegex))
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
            document = JsonSerializer.Deserialize(json, GrouperDocumentJsonContext.Default.GrouperDocument);
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

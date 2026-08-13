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

        // Deliberately the whole list and not just what the call above added: the custom validators
        // run only on a document that is otherwise sound, so that a structural problem is not
        // reported together with the UPN or self-reference errors it may well be the cause of. The
        // gates inside InternalValidateMembers are scoped, because those suppress findings that have
        // nothing to do with each other; this one is a decision about what is worth reporting.
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

        int errorsBeforeSources = validationErrors.Count;
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

        // Only this loop's own findings may stop the rule checks below, because what they protect is
        // the source lookup on line 95 -- nothing else here makes the rules unreadable.
        if (validationErrors.Count > errorsBeforeSources)
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

        int errorsBeforeNames = validationErrors.Count;
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

        if (validationErrors.Count > errorsBeforeNames)
        {
            return;
        }

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

    /// <summary>
    /// Reports the properties whose absence the document model cannot represent. Store, source and
    /// action are enums with no value meaning "not stated", so leaving one out of the payload yields
    /// the first member of the enum -- OnPremAd, Personalsystem, Include -- and every later check
    /// accepts it. The payload is the last place the difference is still visible, which is why this
    /// reads it rather than the document. Everything else that can be left out either has a documented
    /// default (owner, interval) or lands on a value the validator already rejects.
    /// </summary>
    private static void ValidateRequiredProperties(string json, List<ValidationError> validationErrors)
    {
        using JsonDocument parsed = JsonDocument.Parse(json);
        if (parsed.RootElement.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        // Property names are matched exactly, because that is how the deserializer matched them.
        if (!parsed.RootElement.TryGetProperty("store", out _))
        {
            validationErrors.Add(new ValidationError(nameof(GrouperDocument.Store), ResourceString.ValidationErrorRequiredPropertyMissing, "store"));
        }

        if (!parsed.RootElement.TryGetProperty("members", out JsonElement members) || members.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement member in members.EnumerateArray())
        {
            if (member.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!member.TryGetProperty("source", out _))
            {
                validationErrors.Add(new ValidationError(nameof(GrouperDocumentMember.Source), ResourceString.ValidationErrorRequiredPropertyMissing, "source"));
            }

            if (!member.TryGetProperty("action", out _))
            {
                validationErrors.Add(new ValidationError(nameof(GrouperDocumentMember.Action), ResourceString.ValidationErrorRequiredPropertyMissing, "action"));
            }
        }
    }

    internal static GrouperDocument? Deserialize(string json, List<ValidationError> validationErrors)
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

        // Deliberately here rather than alongside the rule checks, so that both parse entry points
        // agree. A document with no store is not an old document that today's rules reject; it is one
        // the model cannot hold, which is exactly what this entry point promises to refuse.
        int errorsBefore = validationErrors.Count;
        ValidateRequiredProperties(json, validationErrors);
        if (validationErrors.Count > errorsBefore)
        {
            return null;
        }

        return document;
    }

    internal static GrouperDocument? DeserializeAndValidate(string json, List<ValidationError> validationErrors)
    {
        GrouperDocument? document = Deserialize(json, validationErrors);
        if (document == null)
        {
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

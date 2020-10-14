using GrouperLib.Language;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GrouperLib.Core
{
    internal static class DocumentValidator
    {
        internal enum ResourceLocation { Independent, OnPrem, Azure }

        private class MemberSourceInfo
        {
            public ResourceLocation Location { get; set; }
            public string[][] RuleSets { get; set; }
            public string[] MultipleRulesAllowed { get; set; }
            public Dictionary<string, Regex> ValidationRules { get; set; }
            public ICustomValidator[] CustomValidators { get; set; }

            public bool InAnyRuleSet(string ruleName)
            {
                return RuleSets.Any(rs => rs.Any(r => r.IEquals(ruleName)));
            }

            public bool HasMatchingRuleSet(IEnumerable<string> rules)
            {
                foreach (string[] ruleSet in RuleSets)
                {
                    if (ruleSet.Intersect(rules, StringComparer.OrdinalIgnoreCase).Count() == rules.Count())
                    {
                        return true;
                    }
                }
                return false;
            }

            public bool IsMultipleRulesAllowed(string ruleName)
            {
                return MultipleRulesAllowed != null && MultipleRulesAllowed.Any(r => r.IEquals(ruleName));
            }
        }

        private static readonly Dictionary<GroupStores, ResourceLocation> storeLocations = new Dictionary<GroupStores, ResourceLocation>() {
            { GroupStores.OnPremAd, ResourceLocation.OnPrem },
            { GroupStores.AzureAd, ResourceLocation.Azure },
            { GroupStores.Exo, ResourceLocation.Azure }
        };

        private static readonly Regex guidRegex = new Regex(
            "^[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        private static readonly Regex eregExtIdRegex = new Regex(
            "^ARXX|LIXX|BEDA|S_[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}$",
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

        private static readonly Regex personecOrgIdRegex = new Regex(
            "^011J[0-9A-Z]{8}$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        private static readonly Regex trueFalseRegex = new Regex(
            "^true|false$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        private static readonly Regex rollRegex = new Regex(
            "^Personal|Elev$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        private static readonly Regex arskursRegex = new Regex(
            "^[0-9F]$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        private static readonly Regex skolformRegex = new Regex(
            "^(FSK|GR|GRSÄR|GY|GYSÄR)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        private static readonly Dictionary<string, MemberSourceInfo> memberSources = new Dictionary<string, MemberSourceInfo>(StringComparer.OrdinalIgnoreCase)
        {
            { "Personalsystem", new MemberSourceInfo()
                {
                    Location = ResourceLocation.Independent,
                    RuleSets = new string[][] {
                        new string[] {"Organisation"},
                        new string[] {"Befattning"},
                        new string[] {"Organisation", "Befattning"},
                        new string[] {"Organisation", "IncludeManager"},
                        new string[] {"Organisation", "Befattning", "IncludeManager"}
                    },
                    ValidationRules = new Dictionary<string, Regex>()
                    {
                        {"Organisation", personecOrgIdRegex },
                        {"IncludeManager", trueFalseRegex}
                    },
                    MultipleRulesAllowed = new string[] {"Befattning"}
                }
            },
            { "Elevregister", new MemberSourceInfo()
                {
                    Location = ResourceLocation.Independent,
                    RuleSets = new string[][]
                    {
                        new string[] {"Roll"},
                        new string[] {"Skolform"},
                        new string[] {"Skolform", "Roll"},
                        new string[] {"Skolform", "Årskurs"},
                        new string[] {"Skolform", "Årskurs", "Roll" },
                        new string[] {"Enhet"},
                        new string[] {"Enhet", "Roll"},
                        new string[] {"Enhet", "Årskurs"},
                        new string[] {"Enhet", "Årskurs", "Roll" },
                        new string[] {"Klass"},
                        new string[] {"Klass", "Roll"},
                        new string[] {"Grupp"},
                        new string[] {"Grupp", "Roll"},
                    },
                    ValidationRules = new Dictionary<string, Regex>()
                    {
                        {"Skolform", skolformRegex},
                        {"Enhet", eregExtIdRegex},
                        //{"Klass", eregKlassIdRegex },
                        //{"Grupp", eregGruppIdRegex },
                        {"Årskurs", arskursRegex},
                        {"Roll", rollRegex }
                    },
                    MultipleRulesAllowed = new string[] {"Årskurs"}
                }
            },
            { "OnPremAdGroup", new MemberSourceInfo()
                {
                    Location = ResourceLocation.OnPrem,
                    RuleSets = new string[][]
                    {
                        new string[] {"Group"}
                    },
                    ValidationRules = new Dictionary<string, Regex>()
                    {
                        {"Group", guidRegex}
                    },
                    CustomValidators = new ICustomValidator[]
                    {
                        new OnPremAdValidator()
                    }
                }
            },
            { "OnPremAdQuery", new MemberSourceInfo()
                {
                    Location = ResourceLocation.OnPrem,
                    RuleSets = new string[][]
                    {
                        new string[] {"LdapFilter"},
                        new string[] {"LdapFilter", "SearchBase"}
                    }
                }
            },
            { "AzureAdGroup", new MemberSourceInfo()
                {
                    Location = ResourceLocation.Azure,
                    RuleSets = new string[][] { new string[] {"Group"} },
                    ValidationRules = new Dictionary<string, Regex>()
                    {
                        {"Group", guidRegex}
                    },
                    CustomValidators = new ICustomValidator[]
                    {
                        new AzureAdValidator()
                    }
                }
            },
            { "ExoGroup", new MemberSourceInfo()
                {
                    Location = ResourceLocation.Azure,
                    RuleSets = new string[][] { new string[] {"Group"} },
                    ValidationRules = new Dictionary<string, Regex>()
                    {
                        {"Group", guidRegex}
                    }
                }
            },
            { "CustomView", new MemberSourceInfo()
                {
                    Location = ResourceLocation.Independent,
                    RuleSets = new string[][] { new string[] {"View"} }
                }
            },
            { "Static", new MemberSourceInfo()
                {
                    Location = ResourceLocation.Independent,
                    RuleSets = new string[][] { new string[] {"Upn"} },
                    CustomValidators = new ICustomValidator[]
                    {
                        new UpnValidator()
                    },
                    MultipleRulesAllowed = new string[] {"Upn"}
                }
            }
        };

        private static void InternalValidateDocument(DeserializedDocument deserializedDocument, List<ValidationError> validationErrors)
        {
            if (string.IsNullOrEmpty(deserializedDocument.Id) || !guidRegex.IsMatch(deserializedDocument.Id))
            {
                validationErrors.Add(new ValidationError(nameof(deserializedDocument.Id), ResourceString.ValidationErrorDocumentIdNotValid, deserializedDocument.Id));
            }
            if (deserializedDocument.Interval < 0)
            {
                validationErrors.Add(new ValidationError(nameof(deserializedDocument.Interval), ResourceString.ValidationErrorIllegalInterval));
            }
            if (string.IsNullOrEmpty(deserializedDocument.GroupName))
            {
                validationErrors.Add(new ValidationError(nameof(deserializedDocument.GroupName), ResourceString.ValidationErrorGroupNameIsNullOrEmpty));
            }
            if (string.IsNullOrEmpty(deserializedDocument.GroupId) || !guidRegex.IsMatch(deserializedDocument.GroupId))
            {
                validationErrors.Add(new ValidationError(nameof(deserializedDocument.GroupId), ResourceString.ValidationErrorGroupIdNotValid, deserializedDocument.GroupId));
            }
            if (!Enum.TryParse(deserializedDocument.Owner, true, out GroupOwnerActions _))
            {
                validationErrors.Add(new ValidationError(nameof(deserializedDocument.Owner), ResourceString.ValidationErrorInvalidOwnerAction, deserializedDocument.Owner));
            }
            if (deserializedDocument.Store == null || !Enum.TryParse(deserializedDocument.Store, true, out GroupStores store))
            {
                validationErrors.Add(new ValidationError(nameof(deserializedDocument.Store), ResourceString.ValidationErrorInvalidGroupStore, deserializedDocument.Store));
                return;
            }
            if (!storeLocations.TryGetValue(store, out ResourceLocation groupLocation))
            {
                validationErrors.Add(new ValidationError(nameof(deserializedDocument.Store), ResourceString.ValidationErrorStoreNotRecognized, store.ToString()));
                return;
            }
            InternalValidateMembers(deserializedDocument.Members, deserializedDocument.Store, groupLocation, validationErrors);
            if (validationErrors.Count > 0)
            {
                return;
            }
            foreach (DeserializedMember deserializedMember in deserializedDocument.Members)
            {
                if (memberSources.TryGetValue(deserializedMember.Source, out MemberSourceInfo memberSourceInfo))
                {
                    if (memberSourceInfo.CustomValidators != null)
                    {
                        foreach (ICustomValidator validator in memberSourceInfo.CustomValidators)
                        {
                            validator.Validate(deserializedDocument, deserializedMember, validationErrors);
                        }
                    }
                }
            }
        }

        private static void InternalValidateMembers(List<DeserializedMember> deserializedMembers, string groupStore, ResourceLocation groupLocation, List<ValidationError> validationErrors)
        {
            if (deserializedMembers == null || deserializedMembers.Count == 0)
            {
                validationErrors.Add(new ValidationError(nameof(DeserializedDocument.Members), ResourceString.ValidationErrorNoMemberObjects));
            }
            foreach (DeserializedMember deserializedMember in deserializedMembers)
            {
                if (!Enum.TryParse(deserializedMember.Action, true, out GroupMemberActions _))
                {
                    validationErrors.Add(new ValidationError(nameof(deserializedMember.Action), ResourceString.ValidationErrorInvalidMemberAction, deserializedMember.Action));
                }
                if (memberSources.TryGetValue(deserializedMember.Source, out MemberSourceInfo memberSourceInfo))
                {
                    if (memberSourceInfo.Location != ResourceLocation.Independent && memberSourceInfo.Location != groupLocation)
                    {
                        validationErrors.Add(new ValidationError(nameof(deserializedMember.Source), ResourceString.ValidationErrorInvalidCombinationOfGroupStoreAndMemberSource, groupStore, deserializedMember.Source));
                    }
                }
                else
                {
                    validationErrors.Add(new ValidationError(nameof(deserializedMember.Source), ResourceString.ValidationErrorInvalidMemberSource, deserializedMember.Source));
                }
            }
            if (validationErrors.Count > 0)
            {
                return;
            }
            HashSet<DeserializedMember> members = new HashSet<DeserializedMember>();
            foreach (DeserializedMember deserializedMember in deserializedMembers)
            {
                if (members.Add(deserializedMember))
                {
                    InternalValidateRules(deserializedMember.Rules, deserializedMember.Source, validationErrors);
                }
                else
                {
                    validationErrors.Add(new ValidationError(nameof(deserializedMember.Action), ResourceString.ValidationErrorDuplicateMemberObject, deserializedMember.Source, deserializedMember.Action, deserializedMember.Rules?.Count));
                }
            }
        }

        private static void InternalValidateRules(List<DeserializedRule> deserializedRules, string memberSourceString, List<ValidationError> validationErrors)
        {
            if (deserializedRules == null || deserializedRules.Count == 0)
            {
                validationErrors.Add(new ValidationError(nameof(DeserializedMember.Rules), ResourceString.ValidationErrorMemberObjectHasNoRules));
                return;
            }
            var rules = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            if (!memberSources.TryGetValue(memberSourceString, out MemberSourceInfo memberSource))
            {
                validationErrors.Add(new ValidationError(nameof(DeserializedMember.Source), ResourceString.ValidationErrorInvalidMemberSource, memberSourceString));
                return;
            }
            foreach (DeserializedRule rule in deserializedRules)
            {
                if (string.IsNullOrEmpty(rule.Name))
                {
                    validationErrors.Add(new ValidationError(nameof(rule.Name), ResourceString.ValidationErrorInvalidRuleName, rule.Name, memberSourceString));
                }
                else
                {
                    if (rules.TryGetValue(rule.Name, out HashSet<string> values))
                    {
                        if (!memberSource.IsMultipleRulesAllowed(rule.Name))
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
                    if (!memberSource.InAnyRuleSet(rule.Name))
                    {
                        validationErrors.Add(new ValidationError(nameof(rule.Name), ResourceString.ValidationErrorInvalidRuleName, rule.Name, memberSourceString));
                    }
                }
            }
            if (validationErrors.Count > 0)
            {
                return;
            }
            if (!memberSource.HasMatchingRuleSet(rules.Keys))
            {
                validationErrors.Add(new ValidationError(nameof(DeserializedMember.Rules), ResourceString.ValidationErrorInvalidCombinationOfRules, memberSourceString));
            }
            foreach (DeserializedRule rule in deserializedRules)
            {
                if (string.IsNullOrEmpty(rule.Value))
                {
                    validationErrors.Add(new ValidationError(nameof(rule.Value), ResourceString.ValidationErrorRuleValueIsNullOrEmpty, rule.Name));
                }
                else if (memberSource.ValidationRules != null && memberSource.ValidationRules.TryGetValue(rule.Name, out Regex validationRegex))
                {
                    if (!validationRegex.IsMatch(rule.Value))
                    {
                        validationErrors.Add(new ValidationError(nameof(rule.Value), ResourceString.ValidationErrorRuleValueDoesNotValidate, rule.Name, rule.Value));
                    }
                }
            }
            return;
        }

        internal static DeserializedDocument DeserializeAndValidate(string json, List<ValidationError> validationErrors)
        {
            DeserializedDocument deserializedDocument = null;
            try
            {
                deserializedDocument = JsonConvert.DeserializeObject<DeserializedDocument>(json);
            }
            catch (JsonReaderException ex)
            {
                validationErrors.Add(new ValidationError(nameof(json), ResourceString.ValidationJsonParsingError, ex.LineNumber, ex.LinePosition, ex.Message));
            }
            catch (JsonSerializationException ex)
            {
                validationErrors.Add(new ValidationError(nameof(json), ResourceString.ValidationJsonParsingError, 0, 0, ex.Message));
            }
            if (deserializedDocument == null)
            {
                validationErrors.Add(new ValidationError(nameof(json), ResourceString.ValidationCouldNotDeserializeJson));
            }
            if (validationErrors.Count > 0)
            {
                return null;
            }
            InternalValidateDocument(deserializedDocument, validationErrors);
            return deserializedDocument;
        }
    }
}

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

        private class DocumentMemberValidationRules
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

        private static readonly Dictionary<GroupMemberSources, DocumentMemberValidationRules> memberSources = new Dictionary<GroupMemberSources, DocumentMemberValidationRules>()
        {
            { GroupMemberSources.Personalsystem, new DocumentMemberValidationRules()
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
            { GroupMemberSources.Elevregister, new DocumentMemberValidationRules()
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
            { GroupMemberSources.OnPremAdGroup, new DocumentMemberValidationRules()
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
            { GroupMemberSources.OnPremAdQuery, new DocumentMemberValidationRules()
                {
                    Location = ResourceLocation.OnPrem,
                    RuleSets = new string[][]
                    {
                        new string[] {"LdapFilter"},
                        new string[] {"LdapFilter", "SearchBase"}
                    }
                }
            },
            { GroupMemberSources.AzureAdGroup, new DocumentMemberValidationRules()
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
            { GroupMemberSources.ExoGroup, new DocumentMemberValidationRules()
                {
                    Location = ResourceLocation.Azure,
                    RuleSets = new string[][] { new string[] {"Group"} },
                    ValidationRules = new Dictionary<string, Regex>()
                    {
                        {"Group", guidRegex}
                    }
                }
            },
            { GroupMemberSources.CustomView, new DocumentMemberValidationRules()
                {
                    Location = ResourceLocation.Independent,
                    RuleSets = new string[][] { new string[] {"View"} }
                }
            },
            { GroupMemberSources.Static, new DocumentMemberValidationRules()
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

        private static void InternalValidateDocument(GrouperDocument document, List<ValidationError> validationErrors)
        {
            if (document.Id == Guid.Empty)
            {
                validationErrors.Add(new ValidationError(nameof(document.Id), ResourceString.ValidationErrorDocumentIdNotValid, document.Id));
            }
            if (document.ProcessingInterval < 0)
            {
                validationErrors.Add(new ValidationError(nameof(document.ProcessingInterval), ResourceString.ValidationErrorIllegalInterval));
            }
            if (string.IsNullOrEmpty(document.GroupName))
            {
                validationErrors.Add(new ValidationError(nameof(document.GroupName), ResourceString.ValidationErrorGroupNameIsNullOrEmpty));
            }
            if (document.Id == Guid.Empty)
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
                if (memberSources.TryGetValue(documentMember.Source, out DocumentMemberValidationRules memberSourceInfo))
                {
                    if (memberSourceInfo.CustomValidators != null)
                    {
                        foreach (ICustomValidator validator in memberSourceInfo.CustomValidators)
                        {
                            validator.Validate(document, documentMember, validationErrors);
                        }
                    }
                }
            }
        }

        private static void InternalValidateMembers(IList<GrouperDocumentMember> documentMembers, GroupStores groupStore, ResourceLocation groupLocation, List<ValidationError> validationErrors)
        {
            if (documentMembers == null || documentMembers.Count == 0)
            {
                validationErrors.Add(new ValidationError(nameof(GrouperDocument.Members), ResourceString.ValidationErrorNoMemberObjects));
            }
            foreach (GrouperDocumentMember member in documentMembers)
            {
                if (memberSources.TryGetValue(member.Source, out DocumentMemberValidationRules memberSourceInfo))
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
            HashSet<GrouperDocumentMember> members = new HashSet<GrouperDocumentMember>();
            foreach (GrouperDocumentMember member in documentMembers)
            {
                if (members.Add(member))
                {
                    InternalValidateRules(member.Rules, member.Source, validationErrors);
                }
                else
                {
                    validationErrors.Add(new ValidationError(nameof(GrouperDocumentMember.Action), ResourceString.ValidationErrorDuplicateMemberObject, member.Source, member.Action, member.Rules?.Count));
                }
            }
        }

        private static void InternalValidateRules(IList<GrouperDocumentRule> documentRules, GroupMemberSources memberSource, List<ValidationError> validationErrors)
        {
            if (documentRules == null || documentRules.Count == 0)
            {
                validationErrors.Add(new ValidationError(nameof(GrouperDocumentMember.Rules), ResourceString.ValidationErrorMemberObjectHasNoRules));
                return;
            }
            var rules = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            if (!memberSources.TryGetValue(memberSource, out DocumentMemberValidationRules memberSourceInfo))
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
                    if (rules.TryGetValue(rule.Name, out HashSet<string> values))
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
                else if (memberSourceInfo.ValidationRules != null && memberSourceInfo.ValidationRules.TryGetValue(rule.Name, out Regex validationRegex))
                {
                    if (!validationRegex.IsMatch(rule.Value))
                    {
                        validationErrors.Add(new ValidationError(nameof(rule.Value), ResourceString.ValidationErrorRuleValueDoesNotValidate, rule.Name, rule.Value));
                    }
                }
            }
            return;
        }

        internal static GrouperDocument DeserializeAndValidate(string json, List<ValidationError> validationErrors)
        {
            GrouperDocument document = null;
            try
            {
                document = JsonConvert.DeserializeObject<GrouperDocument>(json);
            }
            catch (JsonReaderException ex)
            {
                validationErrors.Add(new ValidationError(nameof(json), ResourceString.ValidationJsonParsingError, ex.LineNumber, ex.LinePosition, ex.Message));
            }
            catch (JsonSerializationException ex)
            {
                validationErrors.Add(new ValidationError(nameof(json), ResourceString.ValidationJsonParsingError, 0, 0, ex.Message));
            }
            if (validationErrors.Count > 0)
            {
                return null;
            }
            InternalValidateDocument(document, validationErrors);
            return document;
        }
    }
}

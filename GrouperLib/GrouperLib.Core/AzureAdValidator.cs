﻿using GrouperLib.Language;
using System.Collections.Generic;

namespace GrouperLib.Core
{
    class AzureAdValidator : ICustomValidator
    {
        public void Validate(GrouperDocument document, GrouperDocumentMember documentMember, List<ValidationError> validationErrors)
        {
            foreach (GrouperDocumentRule rule in documentMember.Rules)
            {
                if (rule.Name.IEquals("Group"))
                {
                    if (rule.Value.IEquals(document.GroupId.ToString()))
                    {
                        validationErrors.Add(new ValidationError(nameof(rule.Value), ResourceString.ValidationErrorSourceGroupSameAsTarget));
                    }
                }
            }
        }
    }
}

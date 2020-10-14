using GrouperLib.Language;
using System.Collections.Generic;

namespace GrouperLib.Core
{
    class OnPremAdValidator : ICustomValidator
    {
        public void Validate(DeserializedDocument deserializedDocument, DeserializedMember deserializedMember, List<ValidationError> validationErrors)
        {
            foreach (DeserializedRule rule in deserializedMember.Rules)
            {
                if (rule.Name.IEquals("Group"))
                {
                    if (rule.Value.IEquals(deserializedDocument.GroupId))
                    {
                        validationErrors.Add(new ValidationError(nameof(rule.Value), ResourceString.ValidationErrorSourceGroupSameAsTarget));
                    }
                }
            }
        }
    }
}

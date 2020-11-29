using System.Collections.Generic;

namespace GrouperLib.Core
{
    interface ICustomValidator
    {
        void Validate(GrouperDocument document, GrouperDocumentMember documentMember, List<ValidationError> validationErrors);
    }
}

using System.Collections.Generic;

namespace GrouperLib.Core
{
    interface ICustomValidator
    {
        void Validate(DeserializedDocument deserializedDocument, DeserializedMember deserializedMember, List<ValidationError> validationErrors);
    }
}

namespace GrouperLib.Core;

public interface ICustomValidator
{
    void Validate(GrouperDocument document, GrouperDocumentMember documentMember, List<ValidationError> validationErrors);
}
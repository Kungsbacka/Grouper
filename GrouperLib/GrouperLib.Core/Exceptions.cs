namespace GrouperLib.Core;

public sealed class InvalidGrouperDocumentException : Exception
{
    /// <summary>
    /// What made the document invalid. Empty when the JSON could not be turned into a document at
    /// all, because in that case there was never anything to validate.
    /// </summary>
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    public InvalidGrouperDocumentException() : this([])
    {
    }

    public InvalidGrouperDocumentException(IReadOnlyList<ValidationError> validationErrors)
        : base(BuildMessage(validationErrors))
    {
        ValidationErrors = validationErrors ?? [];
    }

    private static string BuildMessage(IReadOnlyList<ValidationError>? validationErrors)
    {
        if (validationErrors is null || validationErrors.Count == 0)
        {
            return "The document is not a valid Grouper document.";
        }
        
        return "The document is not a valid Grouper document: "
            + string.Join(" ", validationErrors.Select(error => error.ErrorMessage));
    }
}
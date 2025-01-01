using GrouperLib.Language;
using System.Text.Json.Serialization;

namespace GrouperLib.Core;

public sealed class ValidationError
{
    [JsonPropertyName("propertyName")]
    [JsonPropertyOrder(1)]
    public string PropertyName { get; }

    [JsonPropertyName("errorId")]
    [JsonPropertyOrder(2)]
    public string ErrorId { get; }

    [JsonPropertyName("errorMessage")]
    [JsonPropertyOrder(3)]
    public string ErrorMessage { get; }

    public ValidationError(string propertyName, string errorId)
        : this(propertyName, errorId, null) { }

    public ValidationError(string propertyName, string errorId, params object?[]? args)
        : this(new StringResourceHelper(), propertyName, errorId, args) { }

    public ValidationError(IStringResourceHelper stringResourceHelper, string propertyName, string errorId)
        : this(stringResourceHelper, propertyName, errorId, null) { }

    public ValidationError(IStringResourceHelper stringResourceHelper, string propertyName, string errorId, params object?[]? args)
    {
        _ = stringResourceHelper ?? throw new ArgumentNullException(nameof(stringResourceHelper));
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        ErrorId = errorId ?? throw new ArgumentNullException(nameof(errorId));
        ErrorMessage = stringResourceHelper.GetString(errorId, args);
    }

    public override string ToString()
    {
        return ErrorMessage;
    }
}
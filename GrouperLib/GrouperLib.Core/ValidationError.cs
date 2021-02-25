using Newtonsoft.Json;
using GrouperLib.Language;

namespace GrouperLib.Core
{
    public sealed class ValidationError
    {
        [JsonProperty(PropertyName = "propertyName", Order = 1)]
        public string PropertyName { get; }

        [JsonProperty(PropertyName = "errorId", Order = 2)]
        public string ErrorId { get; }

        [JsonProperty(PropertyName = "errorId", Order = 3)]
        public string ErrorMessage { get; }

        public ValidationError(string propertyName, string errorId)
            : this(propertyName, errorId, null) { }

        public ValidationError(string propertyName, string errorId, params object[] args)
            : this(new StringResourceHelper(), propertyName, errorId, args) { }

        public ValidationError(IStringResourceHelper stringResourceHelper, string propertyName, string errorId)
        : this(stringResourceHelper, propertyName, errorId, null) { }

        public ValidationError(IStringResourceHelper stringResourceHelper, string propertyName, string errorId, params object[] args)
        {
            PropertyName = propertyName;
            ErrorId = errorId;
            ErrorMessage = stringResourceHelper.GetString(errorId, args);
        }

        public override string ToString()
        {
            return ErrorMessage;
        }
    }
}

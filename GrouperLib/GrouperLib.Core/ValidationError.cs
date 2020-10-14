using GrouperLib.Language;

namespace GrouperLib.Core
{
    public sealed class ValidationError
    {
        public string PropertyName { get; }
        public string ErrorId { get; }
        public string ErrorText { get; }

        public ValidationError(string propertyName, string errorId) : this(propertyName, errorId, null) { }

        public ValidationError(string propertyName, string errorId, params object[] args)
        {
            PropertyName = propertyName;
            ErrorId = errorId;
            ErrorText = LanguageHelper.GetErrorText(ErrorId, args);
        }

        public override string ToString()
        {
            return ErrorText;
        }
    }
}

namespace GrouperLib.Language
{
    public interface IStringResourceHelper
    {
        string GetString(string resourceId, params object[] args);

        string GetString(string resourceId);

        void SetLanguage(string lang);
    }
}

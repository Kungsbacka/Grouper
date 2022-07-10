using GrouperLib.Core;

namespace GrouperApi
{
    internal static class DocumentHelper
    {
        public static async Task<GrouperDocument> MakeDocumentAsync(HttpRequest request)
        {
            using StreamReader stream = new(request.Body);
            string document = await stream.ReadToEndAsync();
            return GrouperDocument.FromJson(document);
        }
    }
}
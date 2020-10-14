using GrouperLib.Core;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace GrouperApi
{
    internal static class Helper
    {
        public static async Task<GrouperDocument> MakeDocumentAsync(HttpRequest request)
        {
            using StreamReader stream = new StreamReader(request.Body);
            string document = await stream.ReadToEndAsync();
            return GrouperDocument.FromJson(document);
        }
    }
}
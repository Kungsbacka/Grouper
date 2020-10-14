using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GrouperLib.Language;
using GrouperLib.Backend;
using GrouperLib.Core;

namespace GrouperApi.Controllers
{
    [ApiController]
    public class ErrorController : ControllerBase
    {
        [Route("/error")]
        public IActionResult Error()
        {
            if (HttpContext.Request.Query.TryGetValue("lang", out var values))
            {
                if (values.Count > 0)
                {
                    LanguageHelper.SetLanguage(values[0]);
                }
            }
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (feature?.Error is ChangeRatioException)
            {
                return Problem(LanguageHelper.GetErrorText(ResourceString.ErrorBelowChangeLimit));
            }
            if (feature?.Error is InvalidGrouperDocumentException)
            {
                return Problem(LanguageHelper.GetErrorText(ResourceString.ErrorGrouperDocumentNotValid));
            }
            return Problem();
        }
    }
}

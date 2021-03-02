using GrouperLib.Backend;
using GrouperLib.Core;
using GrouperLib.Language;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GrouperApi.Controllers
{
    [ApiController]
    public class ErrorController : ControllerBase
    {

        private readonly IStringResourceHelper _stringResourceHelper;

        public ErrorController(IStringResourceHelper stringResourceHelper)
        {
            _stringResourceHelper = stringResourceHelper;
        }

        [Route("/error")]
        public IActionResult Error()
        {
            if (HttpContext.Request.Query.TryGetValue("lang", out var values))
            {
                if (values.Count > 0)
                {
                    _stringResourceHelper.SetLanguage(values[0]);
                }
            }
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (feature?.Error is ChangeRatioException)
            {
                return Problem(_stringResourceHelper.GetString(ResourceString.ErrorBelowChangeLimit));
            }
            if (feature?.Error is InvalidGrouperDocumentException)
            {
                return Problem(_stringResourceHelper.GetString(ResourceString.ErrorGrouperDocumentNotValid));
            }
            return Problem();
        }
    }
}

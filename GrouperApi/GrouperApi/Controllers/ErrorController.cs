using GrouperLib.Backend;
using GrouperLib.Core;
using GrouperLib.Language;
using GrouperLib.Store;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GrouperApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ErrorController : ControllerBase
    {

        private readonly IStringResourceHelper _stringResourceHelper;

        public ErrorController(IStringResourceHelper stringResourceHelper)
        {
            _stringResourceHelper = stringResourceHelper;
        }

        [HttpGet]
        public IActionResult Error()
        {
            if (HttpContext.Request.Query.TryGetValue("lang", out var values))
            {
                if (values.Count > 0)
                {
                    var lang = values[0];
                    if (lang != null)
                    {
                        _stringResourceHelper.SetLanguage(lang);
                    }
                }
            }
            // Each of these carries a status code that says what happened, rather than the 500 they
            // all used to share. A caller cannot tell one 500 from another, and a missing group in
            // particular is an ordinary answer that a client needs to act on differently from a
            // failure. The detail is localized; the status code is what a client should branch on.
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (feature?.Error is GroupNotFoundException)
            {
                return Problem(_stringResourceHelper.GetString(ResourceString.ErrorGroupNotFound),
                    statusCode: StatusCodes.Status404NotFound);
            }
            if (feature?.Error is ChangeRatioException)
            {
                return Problem(_stringResourceHelper.GetString(ResourceString.ErrorBelowChangeLimit),
                    statusCode: StatusCodes.Status409Conflict);
            }
            if (feature?.Error is InvalidGrouperDocumentException)
            {
                return Problem(_stringResourceHelper.GetString(ResourceString.ErrorGrouperDocumentNotValid),
                    statusCode: StatusCodes.Status400BadRequest);
            }
            return Problem();
        }
    }
}

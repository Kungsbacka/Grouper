using GrouperLib.Backend;
using GrouperLib.Core;
using GrouperLib.Language;
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

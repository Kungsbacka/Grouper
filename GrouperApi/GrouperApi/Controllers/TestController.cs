using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Runtime.Versioning;

namespace GrouperApi.Controllers
{
    [SupportedOSPlatform("windows")]
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        public TestController()
        {
        }

        [Authorize(Policy = "All")]
        [HttpGet("echo")]
        public IActionResult Echo(string input)
        {
            return Ok(input);
        }

        [Authorize(Policy = "All")]
        [HttpGet("version")]
        public IActionResult Version()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            if (ver == null)
            {
                return Problem("Version not found");
            }
            return Ok($"{ver.Major}.{ver.Minor}");
        }
    }
}

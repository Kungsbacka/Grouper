using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace GrouperApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        public TestController()
        {
        }

        [HttpGet("echo")]
        public IActionResult Echo(string input)
        {
            return Ok(input);
        }

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

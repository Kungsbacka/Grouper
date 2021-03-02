using GrouperLib.Backend;
using GrouperLib.Config;
using GrouperLib.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace GrouperApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class GroupInfoController : ControllerBase
    {
        private readonly GrouperConfiguration _config;

        public GroupInfoController(IOptions<GrouperConfiguration> config)
        {
            _config = config.Value ?? throw new ArgumentNullException();
        }

        [HttpPost]
        public async Task<IActionResult> GetGroupInfo()
        {
            try
            {
                GroupInfo info = await GetGrouperBackend().GetGroupInfoAsync(await Helper.MakeDocumentAsync(Request));
                return Ok(info);
            }
            catch
            {
                return Ok();
            }
        }

        private Grouper GetGrouperBackend()
        {
            return Grouper.CreateFromConfig(_config);
        }
    }
}

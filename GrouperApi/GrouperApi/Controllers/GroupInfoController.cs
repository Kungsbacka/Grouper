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
        private readonly Grouper _grouperBackend;

        public GroupInfoController(Grouper grouper)
        {
            _grouperBackend = grouper ?? throw new ArgumentNullException(nameof(grouper));
        }

        [HttpPost]
        public async Task<IActionResult> GetGroupInfo()
        {
            try
            {
                GroupInfo info = await _grouperBackend.GetGroupInfoAsync(await Helper.MakeDocumentAsync(Request));
                return Ok(info);
            }
            catch
            {
                return Ok();
            }
        }
    }
}

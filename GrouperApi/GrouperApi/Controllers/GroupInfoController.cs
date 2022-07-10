using GrouperLib.Backend;
using GrouperLib.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
                GroupInfo info = await _grouperBackend.GetGroupInfoAsync(await DocumentHelper.MakeDocumentAsync(Request));
                return Ok(info);
        }
    }
}

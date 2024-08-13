using GrouperLib.Backend;
using GrouperLib.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Versioning;

namespace GrouperApi.Controllers
{
    [SupportedOSPlatform("windows")]
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class GrouperController : ControllerBase
    {
        private readonly Grouper _grouperBackend;

        public GrouperController(Grouper grouper)
        {
            _grouperBackend = grouper ?? throw new ArgumentNullException(nameof(grouper));
        }

        [Authorize(Policy = "All")]
        [HttpPost("diff")]
        public async Task<IActionResult> GetDiffAsync(bool? unchanged)
        {
            GroupMemberDiff diff = await _grouperBackend.GetMemberDiffAsync(await DocumentHelper.MakeDocumentAsync(Request), unchanged ?? false);
            return Ok(diff);
        }

        [Authorize(Policy = "Admin")]
        [HttpPost("invoke")]
        public async Task<IActionResult> InvokeGrouper(bool? ignoreChangelimit)
        {
            GrouperDocument document = await DocumentHelper.MakeDocumentAsync(Request);
            GroupMemberDiff diff = await _grouperBackend.GetMemberDiffAsync(document);
            await _grouperBackend.UpdateGroupAsync(diff, ignoreChangelimit ?? false);
            var changes = new List<OperationalLogItem>();
            foreach (GroupMember member in diff.Add)
            {
                changes.Add(new OperationalLogItem(document, GroupMemberOperation.Add, member));
            }
            foreach (GroupMember member in diff.Remove)
            {
                changes.Add(new OperationalLogItem(document, GroupMemberOperation.Remove, member));
            }
            return Ok(changes);
        }
    }
}

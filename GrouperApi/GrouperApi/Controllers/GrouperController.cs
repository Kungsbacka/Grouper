using GrouperLib.Backend;
using GrouperLib.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrouperApi.Controllers
{
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

        [HttpPost("diff")]
        public async Task<IActionResult> GetDiffAsync(bool unchanged)
        {
            GroupMemberDiff diff = await _grouperBackend.GetMemberDiffAsync(await DocumentHelper.MakeDocumentAsync(Request), unchanged);
            return Ok(diff);
        }

        [HttpPost("invoke")]
        public async Task<IActionResult> InvokeGrouper(bool ignoreChangelimit)
        {
            GrouperDocument document = await DocumentHelper.MakeDocumentAsync(Request);
            GroupMemberDiff diff = await _grouperBackend.GetMemberDiffAsync(document);
            await _grouperBackend.UpdateGroupAsync(diff, ignoreChangelimit);
            var changes = new List<OperationalLogItem>();
            foreach (GroupMember member in diff.Add)
            {
                changes.Add(new OperationalLogItem(document, GroupMemberOperations.Add, member));
            }
            foreach (GroupMember member in diff.Remove)
            {
                changes.Add(new OperationalLogItem(document, GroupMemberOperations.Remove, member));
            }
            return Ok(changes);
        }
    }
}

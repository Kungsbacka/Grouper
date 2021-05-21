using GrouperLib.Backend;
using GrouperLib.Config;
using GrouperLib.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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

        [HttpPost("invoke")]
        public async Task<IActionResult> InvokeGrouper(bool ignoreChangelimit)
        {
            GrouperDocument document = await Helper.MakeDocumentAsync(Request);
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

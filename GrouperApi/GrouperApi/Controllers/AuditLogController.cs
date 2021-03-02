using GrouperLib.Config;
using GrouperLib.Core;
using GrouperLib.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrouperApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AuditLogController : ControllerBase
    {
        private readonly GrouperConfiguration _config;

        public AuditLogController(IOptions<GrouperConfiguration> config)
        {
            _config = config.Value ?? throw new ArgumentNullException();
        }

        public async Task<IActionResult> GetLogEntries(Guid? documentId, string actionContains, string actorContains, DateTime? startDate, DateTime? endDate, int? count)
        {
            IEnumerable<AuditLogItem> items = await GetLogDb().GetAuditLogItemsAsync(new AuditLogQuery()
            {
                Count = count ?? int.MaxValue,
                DocumentId = documentId,
                StartDate = startDate,
                EndDate = endDate,
                ActionContains = actionContains,
                ActorContains = actorContains
            });
            return Ok(items);
        }

        private LogDb GetLogDb()
        {
            return new LogDb(_config.DocumentDatabaseConnectionString);
        }
    }
}

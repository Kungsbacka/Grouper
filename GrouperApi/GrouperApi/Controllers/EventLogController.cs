using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrouperLib.Config;
using GrouperLib.Core;
using GrouperLib.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrouperApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class EventLogController : ControllerBase
    {
        private readonly GrouperConfiguration _config;

        public EventLogController(Microsoft.Extensions.Options.IOptions<GrouperConfiguration> config)
        {
            _config = config.Value ?? throw new ArgumentNullException();
        }

        [HttpGet]
        public async Task<IActionResult> GetLogEntries(Guid? documentId, Guid? groupId, string groupDisplayNameContains, string messageContains, LogLevels? logLevel, DateTime? startDate, DateTime? endDate, int? count)
        {
            IEnumerable<EventLogItem> items = await GetLogDb().GetEventLogItemsAsync(new EventLogQuery()
            {
                Count = count ?? int.MaxValue,
                DocumentId = documentId,
                GroupId = groupId,
                GroupDisplayNameContains = groupDisplayNameContains,
                MessageContains = messageContains,
                StartDate = startDate,
                EndDate = endDate,
                LogLevel = logLevel
            });
            return Ok(items);
        }

        private LogDb GetLogDb()
        {
            return new LogDb(_config.DocumentDatabaseConnectionString);
        }
    }
}

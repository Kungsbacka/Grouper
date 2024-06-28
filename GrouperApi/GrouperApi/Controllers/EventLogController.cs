using GrouperLib.Config;
using GrouperLib.Core;
using GrouperLib.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Runtime.Versioning;

namespace GrouperApi.Controllers
{
    [SupportedOSPlatform("windows")]
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class EventLogController : ControllerBase
    {
        private readonly GrouperConfiguration _config;

        public EventLogController(IOptions<GrouperConfiguration> config)
        {
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        }

        [Authorize(Policy = "All")]
        [HttpGet]
        public async Task<IActionResult> GetLogEntries(Guid? documentId, Guid? groupId, string? groupDisplayNameContains, string? messageContains, GrouperLib.Core.LogLevel? logLevel, DateTime? startDate, DateTime? endDate, int? count)
        {
            IEnumerable<EventLogItem> items = await GetLogDb().GetEventLogItemsAsync(new EventLogQuery()
            {
                Count = count ?? 100,
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
            string connectionString = _config.LogDatabaseConnectionString ??
                throw new InvalidOperationException("Connection string missing in configuration");
            return new LogDb(connectionString);
        }
    }
}

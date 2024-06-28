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
    public class AuditLogController : ControllerBase
    {
        private readonly GrouperConfiguration _config;

        public AuditLogController(IOptions<GrouperConfiguration> config)
        {
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        }

        [Authorize(Policy = "All")]
        [HttpGet]
        public async Task<IActionResult> GetLogEntries(Guid? documentId, string? actionContains, string? actorContains, DateTime? startDate, DateTime? endDate, int? count)
        {
            IEnumerable<AuditLogItem> items = await GetLogDb().GetAuditLogItemsAsync(new AuditLogQuery()
            {
                Count = count ?? 100,
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
            string connectionString = _config.LogDatabaseConnectionString ??
                throw new InvalidOperationException("Connection string missing in configuration");
            return new LogDb(connectionString);
        }
    }
}

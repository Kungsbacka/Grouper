﻿using GrouperLib.Config;
using GrouperLib.Core;
using GrouperLib.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GrouperApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class OperationalLogController : ControllerBase
    {
        private readonly GrouperConfiguration _config;

        public OperationalLogController(IOptions<GrouperConfiguration> config)
        {
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        }

        [HttpGet]
        public async Task<IActionResult> GetLogEntries(Guid? documentId, Guid? groupId, string? groupDisplayNameContains, Guid? targetId, string? targetDisplayNameContains, GroupMemberOperation? operation, DateTime? startDate, DateTime? endDate, int? count)
        {
            IEnumerable<OperationalLogItem> items = await GetLogDb().GetOperationalLogItemsAsync(new OperationalLogQuery()
            {
                Count = count ?? 100,
                DocumentId = documentId,
                GroupId = groupId,
                GroupDisplayNameContains = groupDisplayNameContains,
                StartDate = startDate,
                EndDate = endDate,
                Operation = operation,
                TargetDisplayNameContains = targetDisplayNameContains,
                TargetId = targetId
            });
            return Ok(items);
        }

        private LogDb GetLogDb()
        {
            return new LogDb(_config.DocumentDatabaseConnectionString);
        }
    }
}

using GrouperLib.Config;
using GrouperLib.Core;
using GrouperLib.Database;
using GrouperLib.Language;
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
    public class DocumentController : ControllerBase
    {
        private readonly GrouperConfiguration _config;
        private readonly IStringResourceHelper _stringResourceHelper;

        public DocumentController(IOptions<GrouperConfiguration> config, IStringResourceHelper stringResourceHelper)
        {
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _stringResourceHelper = stringResourceHelper;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllDocumentsAsync(GroupStore? store, bool? unpublished, bool? deleted)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetAllEntriesAsync(store, unpublished ?? false, deleted ?? false);
            return Ok(entries);
        }

        [HttpGet("id/{id:guid}")]
        public async Task<IActionResult> GetByDocumentIdAsync(Guid id, bool? unpublished, bool? deleted)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetEntriesByDocumentIdAsync(id, unpublished ?? false, deleted ?? false);
            return Ok(entries);
        }

        [HttpGet("unpublished")]
        public async Task<IActionResult> GetUnpublishedAsync(GroupStore? store)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetUnpublishedEntriesAsync(store);
            return Ok(entries);
        }

        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeletedAsync(GroupStore? store)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetDeletedEntriesAsync(store);
            return Ok(entries);
        }

        [HttpGet("group/name/{name}")]
        public async Task<IActionResult> GetByGroupNameAsync(string name, GroupStore? store, bool? unpublished, bool? deleted)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetEntriesByGroupNameAsync(name, store, unpublished ?? false, deleted ?? false);
            return Ok(entries);
        }

        [HttpGet("group/id/{id:guid}")]
        public async Task<IActionResult> GetByGroupIdAsync(Guid id, GroupStore? store, bool? unpublished, bool? deleted)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetEntriesByGroupIdAsync(id, store, unpublished ?? false, deleted ?? false);
            return Ok(entries);
        }

        [HttpGet("source/{source}")]
        public async Task<IActionResult> GetByMemberSourceAsync(GroupMemberSource source, GroupStore? store, bool? unpublished, bool? deleted)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetEntriesByMemberSourceAsync(source, store, unpublished ?? false, deleted ?? false);
            return Ok(entries);
        }

        [HttpGet("rule/{rule}/{value?}")]
        public async Task<IActionResult> GetByMemberRuleAsync(string rule, string? value, GroupStore? store, bool? unpublished, bool? deleted)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetEntriesByMemberRuleAsync(ruleName: rule, ruleValue: value, store, unpublished ?? false, deleted ?? false);
            return Ok(entries);
        }

        [HttpPost("publish/{id:guid}")]
        public async Task<IActionResult> PublishDocumentAsync(Guid id)
        {
            await GetDocumentDb().PublishDocumentAsync(id);
            return Ok();
        }

        [HttpPost("unpublish/{id:guid}")]
        public async Task<IActionResult> UnpublishDocumentAsync(Guid id)
        {
            await GetDocumentDb().UnpublishDocumentAsync(id);
            return Ok();
        }

        [HttpDelete("id/{id:guid}")]
        public async Task<IActionResult> DeleteDocumentAsync(Guid id)
        {
            await GetDocumentDb().DeleteDocumentAsync(id);
            return Ok();
        }

        [HttpPost("restore/{id:guid}")]
        public async Task<IActionResult> RestoreDocumentAsync(Guid id, int? revision)
        {
            if (revision.HasValue)
            {
                await GetDocumentDb().RestoreRevisionAsync(id, revision.Value);
            }
            else
            {
                await GetDocumentDb().RestoreDeletedDocumentAsync(id);
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> StoreDocumentAsync()
        {
            await GetDocumentDb().StoreDocumentAsync(await DocumentHelper.MakeDocumentAsync(Request));
            return Ok();
        }

        [HttpPost("tag/{id:guid}")]
        public async Task<IActionResult> AddDocumentTagAsync(Guid id, string tag, bool? useExisting)
        {
            await GetDocumentDb().AddDocumentTagAsync(id, tag, useExisting ?? false);
            return Ok();
        }

        [HttpDelete("tag/{id:guid}")]
        public async Task<IActionResult> RemoveDocumentTagAsync(Guid id, string tag)
        {
            await GetDocumentDb().RemoveDocumentTagAsync(id, tag);
            return Ok();
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateDocument(string? lang)
        {
            if (string.IsNullOrEmpty(lang))
            {
                _stringResourceHelper.SetLanguage("en");
            }
            else
        {
            _stringResourceHelper.SetLanguage(lang);
            }
            using StreamReader stream = new(Request.Body);
            string document = await stream.ReadToEndAsync();
            List<ValidationError> errors = new();
            GrouperDocument.FromJson(document, errors);
            return Ok(errors);
        }

        private DocumentDb GetDocumentDb()
        {
            string currentUser = ControllerContext.HttpContext.User.Identity?.Name ??
                throw new InvalidOperationException("Can not determine current user name");
            string connectionString = _config.DocumentDatabaseConnectionString ??
                throw new InvalidOperationException("Connection string missing in configuration");
            return new DocumentDb(connectionString, currentUser);
        }
    }
}
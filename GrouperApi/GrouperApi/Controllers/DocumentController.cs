using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GrouperLib.Backend;
using GrouperLib.Config;
using GrouperLib.Core;
using GrouperLib.Database;
using GrouperLib.Language;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrouperApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly GrouperConfiguration _config;

        public DocumentController(Microsoft.Extensions.Options.IOptions<GrouperConfiguration> config)
        {
            _config = config.Value ?? throw new ArgumentNullException();
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllDocumentsAsync(GroupStores? store, bool unpublished, bool deleted)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetAllEntriesAsync(store, unpublished, deleted);
            return Ok(entries);
        }

        [HttpGet("id/{id:guid}")]
        public async Task<IActionResult> GetByDocumentIdAsync(Guid id, bool unpublished, bool deleted)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetEntriesByDocumentIdAsync(id, unpublished, deleted);
            return Ok(entries);
        }

        [HttpGet("unpublished")]
        public async Task<IActionResult> GetUnpublishedAsync(GroupStores? store)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetUnpublishedEntriesAsync(store);
            return Ok(entries);
        }

        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeletedAsync(GroupStores? store)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetDeletedEntriesAsync(store);
            return Ok(entries);
        }

        [HttpGet("group/name/{name}")]
        public async Task<IActionResult> GetByGroupNameAsync(string name, GroupStores? store, bool unpublished, bool deleted)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetEntriesByGroupNameAsync(name, store, unpublished, deleted);
            return Ok(entries);
        }

        [HttpGet("group/id/{id:guid}")]
        public async Task<IActionResult> GetByGroupIdAsync(Guid id, GroupStores? store, bool unpublished, bool deleted)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetEntriesByGroupIdAsync(id, store, unpublished, deleted);
            return Ok(entries);
        }

        [HttpGet("source/{source}")]
        public async Task<IActionResult> GetByMemberSourceAsync(GroupMemberSources source, GroupStores? store, bool unpublished, bool deleted)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetEntriesByMemberSourceAsync(source, store, unpublished, deleted);
            return Ok(entries);
        }

        [HttpGet("rule/{rule}/{value?}")]
        public async Task<IActionResult> GetByMemberRuleAsync(string rule, string value, GroupStores? store, bool unpublished, bool deleted)
        {
            IEnumerable<GrouperDocumentEntry> entries = await GetDocumentDb().GetEntriesByMemberRuleAsync(ruleName: rule, ruleValue: value, store, unpublished, deleted);
            return Ok(entries);
        }

        [HttpGet("diff/{id:guid}")]
        public async Task<IActionResult> GetDiffForStoredDocumentAsync(Guid id, bool unchanged)
        {
            GrouperDocumentEntry entry = (await GetDocumentDb().GetEntriesByDocumentIdAsync(id)).FirstOrDefault();
            if (entry == null)
            {
                return BadRequest();
            }
            Grouper backend = GetGrouperBackend();
            GroupMemberDiff diff =  await backend.GetMemberDiffAsync(entry.Document, unchanged);
            return Ok(diff);
        }

        [HttpPost("diff")]
        public async Task<IActionResult> GetDiffAsync(bool unchanged)
        {
            Grouper backend = GetGrouperBackend();
            GroupMemberDiff diff = await backend.GetMemberDiffAsync(await Helper.MakeDocumentAsync(Request), unchanged);
            return Ok(diff);
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
            await GetDocumentDb().StoreDocumentAsync(await Helper.MakeDocumentAsync(Request));
            return Ok();
        }

        [HttpPost("tag/{id:guid}")]
        public async Task<IActionResult> AddDocumentTagAsync(Guid id, string tag, bool? useExisting)
        {
            await GetDocumentDb().AddDocumentTagAsync(id, tag, useExisting.GetValueOrDefault());
            return Ok();
        }

        [HttpDelete("tag/{id:guid}")]
        public async Task<IActionResult> RemoveDocumentTagAsync(Guid id, string tag)
        {
            await GetDocumentDb().RemoveDocumentTagAsync(id, tag);
            return Ok();
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateDocument(string lang)
        {
            LanguageHelper.SetLanguage(lang);
            using StreamReader stream = new StreamReader(Request.Body);
            string document = await stream.ReadToEndAsync();
            List<ValidationError> errors = new List<ValidationError>();
            GrouperDocument.FromJson(document, errors);
            return Ok(errors);
        }

        private Grouper GetGrouperBackend()
        {
            return Grouper.CreateFromConfig(_config);
        }

        private DocumentDb GetDocumentDb()
        {
            return new DocumentDb(_config.DocumentDatabaseConnectionString, ControllerContext.HttpContext.User.Identity.Name);
        }
    }
}
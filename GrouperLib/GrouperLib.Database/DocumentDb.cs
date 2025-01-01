using GrouperLib.Config;
using GrouperLib.Core;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Runtime.Versioning;

namespace GrouperLib.Database;

[SupportedOSPlatform("windows")]
public class DocumentDb
{
    private readonly string _connectionString;
    private readonly string? _author;

    public DocumentDb(string connectionString, string? author)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(author);
        _author = author;
        _connectionString = connectionString;
    }

    public DocumentDb(GrouperConfiguration configuration, string? author)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(author);
        _author = author;
        _connectionString = configuration.DocumentDatabaseConnectionString 
            ?? throw new InvalidOperationException("DocumentDatabaseConnectionString is not set in configuration.");
    }

    private async Task<IList<GrouperDocumentEntry>> InternalGetDocumentEntriesAsync(string storedProcedure, IDictionary<string, object?>? parameters)
    {
        List<GrouperDocumentEntry> result = [];
        await using SqlConnection conn = new(_connectionString);
        await conn.OpenAsync();
        await using SqlCommand cmd = new();
        cmd.Connection = conn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = storedProcedure;
        cmd.AddParameters(parameters);
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new GrouperDocumentEntry(
                document: GrouperDocument.FromJson(reader.GetString(5)),
                revision: reader.GetInt32(0),
                revisionCreated: reader.GetDateTime(1),
                isPublished: reader.GetBoolean(2),
                isDeleted: reader.GetBoolean(3),
                tags: (reader.IsDBNull(4) ? [] : reader.GetString(4).Split(','))
            ));
        }

        return result;
    }

    private async Task InternalExecuteStoredProcedureAsync(string storedProcedure, IDictionary<string, object?>? parameters)
    {
        await using SqlConnection conn = new(_connectionString);
        await conn.OpenAsync();
        await using SqlCommand cmd = new();
        cmd.Connection = conn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = storedProcedure;
        cmd.AddParameters(parameters);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task InternalSetPublishedFlagAsync(Guid documentId, bool published)
    {
        await InternalExecuteStoredProcedureAsync("dbo.set_published",
            new Dictionary<string, object?>() {
                { "author", _author },
                { "document_id", documentId },
                { "published", published }
            });
    }

    private async Task InternalSetDeletedFlagAsync(Guid documentId, bool deleted)
    {
        await InternalExecuteStoredProcedureAsync("dbo.set_deleted",
            new Dictionary<string, object?>() {
                { "author", _author },
                { "document_id", documentId },
                { "deleted", deleted }
            });
    }

    public async Task<IList<GrouperDocumentEntry>> GetAllEntriesAsync(GroupStore? store = null, bool includeUnpublished = false, bool includeDeleted = false)
    {
        return await InternalGetDocumentEntriesAsync("dbo.get_all_documents",
            new Dictionary<string, object?>() {
                { "store", store?.ToString() },
                { "include_unpublished", includeUnpublished },
                { "include_deleted", includeDeleted }
            });
    }

    public async Task<IList<GrouperDocumentEntry>> GetEntriesByDocumentIdAsync(Guid documentId, bool includeUnpublished = false, bool includeDeleted = false)
    {
        return await InternalGetDocumentEntriesAsync("dbo.get_document_by_document_id",
            new Dictionary<string, object?>() {
                { "document_id", documentId },
                { "include_unpublished", includeUnpublished },
                { "include_deleted", includeDeleted }
            });
    }

    public async Task<IList<GrouperDocumentEntry>> GetEntriesByGroupIdAsync(Guid groupId, GroupStore? store = null, bool includeUnpublished = false, bool includeDeleted = false)
    {
        return await InternalGetDocumentEntriesAsync("dbo.get_document_by_group_id",
            new Dictionary<string, object?>() {
                { "group_id", groupId },
                { "store", store?.ToString() },
                { "include_unpublished", includeUnpublished },
                { "include_deleted", includeDeleted }
            });
    }

    public async Task<IList<GrouperDocumentEntry>> GetEntriesByGroupNameAsync(string groupName, GroupStore? store = null, bool includeUnpublished = false, bool includeDeleted = false)
    {
        string? translatedGroupName = Helpers.TranslateWildcard(groupName);
        return await InternalGetDocumentEntriesAsync("dbo.get_document_by_group_name",
            new Dictionary<string, object?>() {
                { "group_name", translatedGroupName },
                { "store", store?.ToString() },
                { "include_unpublished", includeUnpublished },
                { "include_deleted", includeDeleted }
            });
    }

    public async Task<IList<GrouperDocumentEntry>> GetEntriesByAgeAsync(DateTime start, DateTime? end = null, GroupStore? store = null, bool includeUnpublished = false, bool includeDeleted = false)
    {
        return await InternalGetDocumentEntriesAsync("dbo.get_document_by_age",
            new Dictionary<string, object?>() {
                { "start", start },
                { "end", end },
                { "store", store?.ToString() },
                { "include_unpublished", includeUnpublished },
                { "include_deleted", includeDeleted }
            });
    }

    public async Task<IList<GrouperDocumentEntry>> GetEntriesByProcessingInterval(int min, int max = int.MaxValue, GroupStore? store = null, bool includeUnpublished = false, bool includeDeleted = false)
    {
        return await InternalGetDocumentEntriesAsync("dbo.get_document_by_processing_interval",
            new Dictionary<string, object?>() {
                { "min", min },
                { "max", max },
                { "store", store?.ToString() },
                { "include_unpublished", includeUnpublished },
                { "include_deleted", includeDeleted }
            });
    }

    public async Task<IList<GrouperDocumentEntry>> GetEntriesByMemberRuleAsync(string? ruleName, string? ruleValue, GroupStore? store = null, bool includeUnpublished = false, bool includeDeleted = false)
    {
        ruleValue = Helpers.TranslateWildcard(ruleValue);
        return await InternalGetDocumentEntriesAsync("dbo.get_document_by_member_rule",
            new Dictionary<string, object?>() {
                { "rule_name", ruleName },
                { "rule_value", ruleValue },
                { "store", store?.ToString() },
                { "include_unpublished", includeUnpublished },
                { "include_deleted", includeDeleted }
            });
    }

    public async Task<IList<GrouperDocumentEntry>> GetEntriesByMemberSourceAsync(GroupMemberSource source, GroupStore? store = null, bool includeUnpublished = false, bool includeDeleted = false)
    {
        return await InternalGetDocumentEntriesAsync("dbo.get_document_by_member_source",
            new Dictionary<string, object?>() {
                { "source", source.ToString() },
                { "store", store?.ToString() },
                { "include_unpublished", includeUnpublished },
                { "include_deleted", includeDeleted }
            });
    }

    public async Task<IList<GrouperDocumentEntry>> GetUnpublishedEntriesAsync(GroupStore? store = null)
    {
        return await InternalGetDocumentEntriesAsync("dbo.get_unpublished_documents",
            new Dictionary<string, object?>() {
                { "store", store?.ToString() }
            });
    }

    public async Task<IList<GrouperDocumentEntry>> GetDeletedEntriesAsync(GroupStore? store = null)
    {
        return await InternalGetDocumentEntriesAsync("dbo.get_deleted_documents",
            new Dictionary<string, object?>() {
                { "store", store?.ToString() }
            });
    }

    public async Task PublishDocumentAsync(Guid documentId)
    {
        await InternalSetPublishedFlagAsync(documentId, published: true);
    }

    public async Task UnpublishDocumentAsync(Guid documentId)
    {
        await InternalSetPublishedFlagAsync(documentId, published: false);
    }

    public async Task DeleteDocumentAsync(Guid documentId)
    {
        await InternalSetDeletedFlagAsync(documentId, deleted: true);
    }

    public async Task RestoreDeletedDocumentAsync(Guid documentId)
    {
        await InternalSetDeletedFlagAsync(documentId, deleted: false);
    }

    public async Task RestoreRevisionAsync(Guid documentId, int revision)
    {
        await InternalExecuteStoredProcedureAsync("dbo.revert_to_revision",
            new Dictionary<string, object?>() {
                { "author", _author },
                { "document_id", documentId },
                { "revision", revision }
            });
    }

    public async Task StoreDocumentAsync(GrouperDocument document)
    {
        await InternalExecuteStoredProcedureAsync("dbo.store_document",
            new Dictionary<string, object?>() {
                { "author", _author },
                { "json", document.ToJson(indented: false) }
            });
    }

    public async Task<IList<GrouperDocumentEntry>> CloneDocumentAsync(Guid documentId)
    {
        Guid newDocumentId = Guid.Empty;
        await using SqlConnection conn = new(_connectionString);
        await conn.OpenAsync();
        await using SqlCommand cmd = new();
        cmd.Connection = conn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.clone_document";
        cmd.Parameters.AddWithValue("author", _author);
        cmd.Parameters.AddWithValue("document_id", documentId);
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (await reader.ReadAsync())
        {
            newDocumentId = reader.GetGuid(0);
        }

        if (newDocumentId == Guid.Empty)
        {
            throw new InvalidOperationException("Cloning document failed.");
        }
        return await GetEntriesByDocumentIdAsync(newDocumentId);
    }

    public async Task AddDocumentTagAsync(Guid documentId, string? tag, bool useExisting = false)
    {
        await InternalExecuteStoredProcedureAsync("dbo.new_document_tag",
            new Dictionary<string, object?>() {
                { "author", _author },
                { "document_id", documentId },
                { "tag", tag },
                { "no_create", useExisting }
            });
    }

    public async Task RemoveDocumentTagAsync(Guid documentId, string? tag)
    {
        await InternalExecuteStoredProcedureAsync("dbo.remove_document_tag",
            new Dictionary<string, object?>() {
                { "author", _author },
                { "document_id", documentId },
                { "tag", tag }
            });
    }
}
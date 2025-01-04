using GrouperLib.Config;
using GrouperLib.Core;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Runtime.Versioning;

namespace GrouperLib.Database;

[SupportedOSPlatform("windows")]
public class LogDb : ILogger
{
    private readonly string _connectionString;

    public LogDb(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        _connectionString = connectionString;
    }

    public LogDb(GrouperConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _connectionString = configuration.LogDatabaseConnectionString
            ?? throw new InvalidOperationException("LogDatabaseConnectionString is not set in configuration.");
    }

    private async Task<IList<EventLogItem>> InternalGetEventLogItemsAsync(string storedProcedure, IDictionary<string, object?>? parameters)
    {
        List<EventLogItem> result = [];
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
            result.Add(new EventLogItem(
                logTime: reader.GetDateTime(0),
                documentId: reader.GetNullable<Guid?>(1),
                groupId: reader.GetNullable<Guid?>(2),
                groupDisplayName: reader.GetNullable<string>(3),
                groupStore: reader.GetNullable<string>(4),
                message: reader.GetString(6),
                logLevel: (LogLevel)reader.GetByte(5)
            ));
        }

        return result;
    }

    private async Task<IList<OperationalLogItem>> InternalGetOperationalLogItemsAsync(string storedProcedure, IDictionary<string, object?>? parameters)
    {
        List<OperationalLogItem> result = [];
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
            result.Add(new OperationalLogItem(
                logTime: reader.GetDateTime(0),
                documentId: reader.GetGuid(1),
                groupId: reader.GetGuid(2),
                groupDisplayName: reader.GetNullable<string>(3),
                groupStore: reader.GetString(4),
                operation: reader.GetString(7),
                targetId: reader.GetGuid(5),
                targetDisplayName: reader.GetNullable<string>(6)
            ));
        }

        return result;
    }

    private async Task<IList<AuditLogItem>> InternalGetAuditLogItemsAsync(string storedProcedure, IDictionary<string, object?>? parameters)
    {
        List<AuditLogItem> result = [];
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
            result.Add(new AuditLogItem(
                logTime: reader.GetDateTime(0),
                documentId: reader.GetGuid(1),
                actor: reader.GetString(2),
                action: reader.GetString(3),
                additionalInformation: reader.GetNullable<string>(4)
            ));
        }

        return result;
    }

    private async Task ExecuteStoredProcedureAsync(string storedProcedure, IDictionary<string, object?>? parameters)
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

    public async Task<IList<EventLogItem>> GetEventLogItemsAsync(EventLogQuery query)
    {
        return await InternalGetEventLogItemsAsync("dbo.get_event_log",
            new Dictionary<string, object?>() {
                { "count", query.Count },
                { "log_level", query.LogLevel },
                { "start", query.StartDate },
                { "end",  query.EndDate },
                { "document_id", query.DocumentId },
                { "group_id",  query.GroupId },
                { "message_contains", query.MessageContains },
                { "group_display_name_contains", query.GroupDisplayNameContains }
            });
    }

    public async Task<IList<OperationalLogItem>> GetOperationalLogItemsAsync(OperationalLogQuery query)
    {
        return await InternalGetOperationalLogItemsAsync("dbo.get_operational_log",
            new Dictionary<string, object?>() { 
                { "count", query.Count },
                { "start", query.StartDate },
                { "end",  query.EndDate },
                { "document_id", query.DocumentId },
                { "group_id",  query.GroupId },
                { "target_id", query.TargetId },
                { "operation", query.Operation?.ToString() },
                { "target_display_name_contains", query.TargetDisplayNameContains },
                { "group_display_name_contains", query.GroupDisplayNameContains }
            });
    }

    public async Task<IList<AuditLogItem>> GetAuditLogItemsAsync(AuditLogQuery query)
    {
        return await InternalGetAuditLogItemsAsync("dbo.get_audit_log",
            new Dictionary<string, object?>() {
                { "count", query.Count },
                { "start", query.StartDate },
                { "end",  query.EndDate },
                { "document_id", query.DocumentId },
                { "actor_contains", query.ActorContains },
                { "action_contains", query.ActionContains },
            });
    }

    public async Task StoreEventLogItemAsync(EventLogItem logItem)
    {
        await ExecuteStoredProcedureAsync("dbo.new_event_log_entry",
            new Dictionary<string, object?>() {
                { "log_time", logItem.LogTime },
                { "document_id", logItem.DocumentId },
                { "group_id", logItem.GroupId },
                { "group_display_name", logItem.GroupDisplayName },
                { "group_store", logItem.GroupStore?.ToString() },
                { "level", logItem.LogLevel },
                { "message", logItem.Message },
            });
    }

    public async Task StoreOperationalLogItemAsync(OperationalLogItem logItem)
    {
        if (logItem.Operation == GroupMemberOperation.None)
        {
            throw new InvalidOperationException("Can not store operational log items with operation 'None'.");
        }
        await ExecuteStoredProcedureAsync("dbo.new_operational_log_entry",
            new Dictionary<string, object?>() {
                { "log_time", logItem.LogTime },
                { "document_id", logItem.DocumentId },
                { "group_id", logItem.GroupId },
                { "group_display_name", logItem.GroupDisplayName },
                { "group_store", logItem.GroupStore.ToString() },
                { "target_id", logItem.TargetId },
                { "target_display_name", logItem.TargetDisplayName },
                { "operation", logItem.Operation.ToString() },
            });
    }
}
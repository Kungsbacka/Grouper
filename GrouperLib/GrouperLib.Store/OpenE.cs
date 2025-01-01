using GrouperLib.Config;
using GrouperLib.Core;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Runtime.Versioning;

namespace GrouperLib.Store;

[SupportedOSPlatform("windows")]
public class OpenE : IGroupStore
{
    private readonly string _connectionString;

    public OpenE(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public OpenE(GrouperConfiguration config)
    {
        _connectionString = config.OpenEDatabaseConnectionString
                            ?? throw new InvalidOperationException($"{nameof(config.OpenEDatabaseConnectionString)} is not set in the configuration.");
    }

    public async Task<GroupInfo> GetGroupInfoAsync(Guid groupId)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand();
        cmd.Connection = conn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.spOpenEGetGroupInfo";
        cmd.Parameters.AddWithValue("groupId", groupId);
        try
        {
            await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new GroupInfo(groupId, reader.GetString(1), GroupStore.OpenE);
            }
        }
        catch (SqlException e)
        {
            if (e.Number == 50001)
            {
                throw GroupNotFoundException.Create(groupId, e);
            }
            throw;
        }

        throw GroupNotFoundException.Create(groupId);
    }

    public async Task GetGroupMembersAsync(GroupMemberCollection memberCollection, Guid groupId)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand();
        cmd.Connection = conn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.spOpenEGetGroupMember";
        cmd.Parameters.AddWithValue("groupId", groupId);
        try
        {
            await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                memberCollection.Add(new GroupMember(
                    reader.GetGuid(1),
                    reader.GetString(2),
                    GroupMemberType.OnPremAd
                ));
            }
        }
        catch (SqlException e)
        {
            if (e.Number == 50001)
            {
                throw GroupNotFoundException.Create(groupId, e);
            }
            throw;
        }
    }

    public async Task AddGroupMemberAsync(GroupMember member, Guid groupId)
    {
        await ExecuteStoreProcedure("dbo.spOpenEAddGroupMember", member, groupId);
    }

    public async Task RemoveGroupMemberAsync(GroupMember member, Guid groupId)
    {
        await ExecuteStoreProcedure("dbo.spOpenERemoveGroupMember", member, groupId);
    }

    private async Task ExecuteStoreProcedure(string storedProcedure, GroupMember member, Guid groupId)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand();
        cmd.Connection = conn;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = storedProcedure;
        cmd.Parameters.AddWithValue("groupId", groupId);
        cmd.Parameters.AddWithValue("memberId", member.Id);
        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException e)
        {
            switch (e.Number)
            {
                case 50001:
                    throw GroupNotFoundException.Create(groupId, e);
                case 50002:
                    throw MemberNotFoundException.Create(member.Id, e);
            }
            throw;
        }
    }

    public IEnumerable<GroupStore> GetSupportedGroupStores() => [GroupStore.OpenE];
}
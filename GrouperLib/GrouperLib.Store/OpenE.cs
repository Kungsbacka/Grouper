using GrouperLib.Config;
using GrouperLib.Core;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GrouperLib.Store
{
    public class OpenE : IGroupStore
    {
        readonly string _connectionString;

        public OpenE(string connectionString)
        {
            if (connectionString is null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }
            _connectionString = connectionString;
        }

        public OpenE(GrouperConfiguration config) : this(config.OpenEDatabaseConnectionString)
        {
        }

        public async Task<GroupInfo> GetGroupInfoAsync(Guid groupId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "dbo.spOpenEGetGroupInfo";
                    cmd.Parameters.AddWithValue("groupId", groupId);
                    SqlDataReader reader = null;
                    try
                    {
                        reader = await cmd.ExecuteReaderAsync();
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
                    finally
                    {
                        if (reader != null)
                        {
                            reader.Dispose();
                        }
                    }
                }
            }
            throw GroupNotFoundException.Create(groupId);
        }

        public async Task GetGroupMembersAsync(GroupMemberCollection memberCollection, Guid groupId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "dbo.spOpenEGetGroupMember";
                    cmd.Parameters.AddWithValue("groupId", groupId);
                    SqlDataReader reader = null;
                    try
                    {
                        reader = await cmd.ExecuteReaderAsync();
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
                    finally
                    {
                        if (reader != null)
                        {
                            reader.Dispose();
                        }
                    }
                }
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
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand())
                {
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
            }
        }

        public IEnumerable<GroupStore> GetSupportedGroupStores()
        {
            return new GroupStore[] { GroupStore.OpenE };
        }
    }
}
using GrouperLib.Config;
using GrouperLib.Core;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace GrouperLib.Database
{
    [SupportedOSPlatform("windows")]
    public class MemberDb : IMemberSource
    {
        readonly string _connectionString;

        public MemberDb(string connectionString)
        {
            if (connectionString is null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }
            _connectionString = connectionString;
        }

        public MemberDb(GrouperConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (configuration.MemberDatabaseConnectionString is null)
            {
                throw new InvalidOperationException("MemberDatabaseConnectionString is missing in configuration.");
            }
            _connectionString = configuration.MemberDatabaseConnectionString;
        }

        private async Task GetMembersAsync(string storedProcedure, GroupMemberType memberType, GroupMemberCollection memberCollection, IDictionary<string, object?> parameters)
        {
            using SqlConnection conn = new(_connectionString);
            await conn.OpenAsync();
            using SqlCommand cmd = new();
            cmd.Connection = conn;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = storedProcedure;
            // Something makes queries that normally takes seconds to sometimes time out
            // This is a workaround until I can find the root cause.
            cmd.CommandTimeout = 300;
            cmd.AddParameters(parameters);
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string? displayName = reader.GetNullable<string>(0);
                if (memberType == GroupMemberType.OnPremAd)
                {
                    Guid id = reader.GetNullable<Guid>(1);
                    displayName ??= id.ToString();
                    if (id != Guid.Empty)
                    {
                        memberCollection.Add(new GroupMember(
                            id: id,
                            displayName: displayName,
                            memberType: GroupMemberType.OnPremAd
                        ));
                    }
                }
                else
                {
                    Guid id = reader.GetNullable<Guid>(2);
                    displayName ??= id.ToString();
                    if (id != Guid.Empty)
                    {
                        memberCollection.Add(new GroupMember(
                            id: id,
                            displayName: displayName,
                            memberType: GroupMemberType.AzureAd
                        ));
                    }
                }
            }
        }

        private async Task GetMembersFromPersonalsystemAsync(GroupMemberCollection memberCollection, GrouperDocumentMember member, GroupMemberType memberType)
        {
            List<string> befattning = new();
            string? organisation = null;
            bool includeManager = false;
            foreach (GrouperDocumentRule rule in member.Rules)
            {
                if (rule.Name.IEquals("Organisation"))
                {
                    organisation = rule.Value.NullIfEmpty();
                }
                else if (rule.Name.IEquals("Befattning") && !string.IsNullOrEmpty(rule.Value))
                {
                    befattning.Add(rule.Value);
                }
                else if (rule.Name.IEquals("IncludeManager"))
                {
                    includeManager = rule.Value.IEquals("true");
                }
            }
            await GetMembersAsync("dbo.spGrouperPersonalsystem", memberType, memberCollection,
                new Dictionary<string, object?>() {
                { "organisation",    organisation },
                { "befattning",      befattning.ToArray() },
                { "include_manager", includeManager }
            });
        }

        private async Task GetMembersFromElevregisterAsync(GroupMemberCollection memberCollection, GrouperDocumentMember member, GroupMemberType memberType)
        {
            List<string> arskurs = new();
            bool elev = true;
            bool personal = true;
            string? skolform = null;
            string? enhet = null;
            string? klass = null;
            string? grupp = null;
            foreach (GrouperDocumentRule rule in member.Rules)
            {
                if (rule.Name.IEquals("Roll"))
                {
                    if (rule.Value.IEquals("Elev"))
                    {
                        personal = false;
                    }
                    else if (rule.Value.IEquals("Personal"))
                    {
                        elev = false;
                    }
                }
                else if (rule.Name.IEquals("Skolform"))
                {
                    skolform = rule.Value.NullIfEmpty();
                }
                else if (rule.Name.IEquals("Enhet"))
                {
                    enhet = rule.Value.NullIfEmpty();
                }
                else if (rule.Name.IEquals("Klass"))
                {
                    klass = rule.Value.NullIfEmpty();
                }
                else if (rule.Name.IEquals("Grupp"))
                {
                    grupp = rule.Value.NullIfEmpty();
                }
                else if (rule.Name.IEquals("Årskurs") && !string.IsNullOrEmpty(rule.Value))
                {
                    arskurs.Add(rule.Value);
                }
            }
            await GetMembersAsync("dbo.spGrouperElevregister", memberType, memberCollection,
                new Dictionary<string, object?>() {
                { "skolform", skolform },
                { "enhet",    enhet },
                { "klass",    klass },
                { "grupp",    grupp },
                { "arskurs",  arskurs.ToArray()},
                { "elev",     elev},
                { "personal", personal}
            });
        }

        private async Task GetMembersFromUpnAsync(GroupMemberCollection memberCollection, GrouperDocumentMember member, GroupMemberType memberType)
        {
            await GetMembersAsync("dbo.spGrouperStaticMember", memberType, memberCollection,
                new Dictionary<string, object?>() {
                { "upn", member.Rules.Where(r => r.Name.IEquals("Upn") && !string.IsNullOrEmpty(r.Value)).Select(r => r.Value).ToArray() }
            });
        }

        private async Task GetMembersFromCustomViewAsync(GroupMemberCollection memberCollection, GrouperDocumentMember member, GroupMemberType memberType)
        {
            await GetMembersAsync("dbo.spGrouperCustomView", memberType, memberCollection,
                new Dictionary<string, object?>() {
                { "view", member.Rules.Where(r => r.Name.IEquals("View")).First().Value }
            });
        }

        public async Task GetMembersFromSourceAsync(GroupMemberCollection memberCollection, GrouperDocumentMember grouperMember, GroupMemberType memberType)
        {
            switch (grouperMember.Source)
            {
                case GroupMemberSource.Elevregister:
                    await GetMembersFromElevregisterAsync(memberCollection, grouperMember, memberType);
                    break;
                case GroupMemberSource.Personalsystem:
                    await GetMembersFromPersonalsystemAsync(memberCollection, grouperMember, memberType);
                    break;
                case GroupMemberSource.Static:
                    await GetMembersFromUpnAsync(memberCollection, grouperMember, memberType);
                    break;
                case GroupMemberSource.CustomView:
                    await GetMembersFromCustomViewAsync(memberCollection, grouperMember, memberType);
                    break;
                default:
                    throw new ArgumentException(nameof(grouperMember.Source));
            }
        }

        public IEnumerable<GroupMemberSource> GetSupportedGrouperMemberSources()
        {
            return new GroupMemberSource[]
            {
                 GroupMemberSource.Elevregister,
                 GroupMemberSource.Personalsystem,
                 GroupMemberSource.Static,
                 GroupMemberSource.CustomView
            };
        }
    }
}

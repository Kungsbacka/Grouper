using GrouperLib.Config;
using GrouperLib.Core;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace GrouperLib.Database
{
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
            : this(configuration.MemberDatabaseConnectionString) { }

        private async Task GetMembersAsync(string storedProcedure, GroupMemberTypes memberType, GroupMemberCollection memberCollection, IDictionary<string, object> parameters)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = storedProcedure;
                    // Something makes queries that normally takes seconds to sometimes time out
                    // This is a workaround until I can find the root cause.
                    cmd.CommandTimeout = 300;
                    cmd.AddParameters(parameters);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (memberType == GroupMemberTypes.OnPremAd)
                            {
                                Guid id = reader.GetNullable<Guid>(1);
                                if (id != Guid.Empty)
                                {
                                    memberCollection.Add(new GroupMember(
                                        id: id,
                                        displayName: reader.GetNullable<string>(0),
                                        memberType: GroupMemberTypes.OnPremAd
                                    ));
                                }
                            }
                            else
                            {
                                Guid id = reader.GetNullable<Guid>(2);
                                if (id != Guid.Empty)
                                {
                                    memberCollection.Add(new GroupMember(
                                        id: id,
                                        displayName: reader.GetNullable<string>(0),
                                        memberType: GroupMemberTypes.AzureAd
                                    ));
                                }
                            }
                        }
                    }
                }
            }
        }

        private async Task GetMembersFromPersonalsystemAsync(GroupMemberCollection memberCollection, GrouperDocumentMember member, GroupMemberTypes memberType)
        {
            var befattning = new List<string>();
            string organisation = null;
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
                new Dictionary<string, object>()
                {
                    { "organisation",    organisation },
                    { "befattning",      befattning.ToArray() },
                    { "include_manager", includeManager }
                }
            );
        }

        private async Task GetMembersFromElevregisterAsync(GroupMemberCollection memberCollection, GrouperDocumentMember member, GroupMemberTypes memberType)
        {
            var arskurs = new List<string>();
            bool elev = true;
            bool personal = true;
            string skolform = null;
            string enhet = null;
            string klass = null;
            string grupp = null;
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
                new Dictionary<string, object>()
                {
                    { "skolform", skolform },
                    { "enhet",    enhet },
                    { "klass",    klass },
                    { "grupp",    grupp },
                    { "arskurs",  arskurs.ToArray()},
                    { "elev",     elev},
                    { "personal", personal}
                }
            );
        }

        private async Task GetMembersFromUpnAsync(GroupMemberCollection memberCollection, GrouperDocumentMember member, GroupMemberTypes memberType)
        {
            await GetMembersAsync("dbo.spGrouperStaticMember", memberType, memberCollection,
                new Dictionary<string, object>()
                {
                    { "upn", member.Rules.Where(r => r.Name.IEquals("Upn") && !string.IsNullOrEmpty(r.Value)).Select(r => r.Value).ToArray() }
                }
            );
        }

        private async Task GetMembersFromCustomViewAsync(GroupMemberCollection memberCollection, GrouperDocumentMember member, GroupMemberTypes memberType)
        {
            await GetMembersAsync("dbo.spGrouperCustomView", memberType, memberCollection,
                new Dictionary<string, object>()
                {
                    { "view", member.Rules.Where(r => r.Name.IEquals("View")).First().Value }
                }
            );
        }

        public async Task GetMembersFromSourceAsync(GroupMemberCollection memberCollection, GrouperDocumentMember grouperMember, GroupMemberTypes memberType)
        {
            switch (grouperMember.Source)
            {
                case GroupMemberSources.Elevregister:
                    await GetMembersFromElevregisterAsync(memberCollection, grouperMember, memberType);
                    break;
                case GroupMemberSources.Personalsystem:
                    await GetMembersFromPersonalsystemAsync(memberCollection, grouperMember, memberType);
                    break;
                case GroupMemberSources.Static:
                    await GetMembersFromUpnAsync(memberCollection, grouperMember, memberType);
                    break;
                case GroupMemberSources.CustomView:
                    await GetMembersFromCustomViewAsync(memberCollection, grouperMember, memberType);
                    break;
                default:
                    throw new ArgumentException(nameof(grouperMember.Source));
            }
        }

        public IEnumerable<GroupMemberSources> GetSupportedGrouperMemberSources()
        {
            return new GroupMemberSources[]
            {
                GroupMemberSources.Elevregister,
                GroupMemberSources.Personalsystem,
                GroupMemberSources.Static,
                GroupMemberSources.CustomView
            };
        }
    }
}

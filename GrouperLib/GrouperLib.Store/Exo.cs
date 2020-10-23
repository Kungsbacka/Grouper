using GrouperLib.Config;
using GrouperLib.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// Reference: https://blogs.msdn.microsoft.com/wushuai/2016/09/18/access-exchange-online-by-powershell-in-c/

namespace GrouperLib.Store
{
    public class Exo : IMemberSource, IGroupStore, IDisposable
    {
        private readonly string _connectionUri = "https://outlook.office365.com/powershell";
        private readonly string _configurationName = "Microsoft.Exchange";
        private readonly PSCredential _credential;
        private Runspace _runspace;
        private object _session;
        private bool _disposed;

        private static readonly Regex guidRegex = new Regex(
            "\"(?<guid>[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12})\"",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        public Exo(string userName, string password, bool passwordIsDpapiProtected)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }
            string plaintextPassword = GrouperConfiguration.GetSensitiveString(password, passwordIsDpapiProtected);
            SecureString securePassword = new SecureString();
            foreach (char c in plaintextPassword)
            {
                securePassword.AppendChar(c);
            }
            _credential = new PSCredential(userName, securePassword);
        }

        public Exo(GrouperConfiguration config) : this(
            userName: config.ExchangeUserName,
            password: config.ExchangePassword,
            passwordIsDpapiProtected: config.ExchangePasswordIsDpapiProtected)
        {
        }

        private void CreateSession()
        {
            if (_runspace == null)
            {
                InitialSessionState iss = InitialSessionState.CreateDefault();
                _runspace = RunspaceFactory.CreateRunspace(iss);
                _runspace.Open();
                PowerShell ps = PowerShell.Create();
                ps.Runspace = _runspace;
                ps.Commands.AddCommand("New-PSSession")
                    .AddParameter("ConfigurationName", _configurationName)
                    .AddParameter("ConnectionUri", _connectionUri)
                    .AddParameter("Credential", _credential)
                    .AddParameter("Authentication", "Basic")
                    .AddParameter("AllowRedirection", true);
                var result = ps.Invoke();
                if (ps.HadErrors || result.Count != 1)
                {
                    Exception exception = null;
                    if (ps.Streams.Error.Count > 0)
                    {
                        exception = ps.Streams.Error[0].Exception;
                    }
                    throw new InvalidOperationException("Failed to connect to Exchange Online", exception);
                }
                _session = result[0];
            }
        }

        // Investigate how to make this async
        // ?? await Task.Factory.FromAsync(_ps.BeginInvoke(), pResult => _ps.EndInvoke(pResult));
        private Collection<PSObject> InvokeCommand(string command)
        {
            CreateSession();
            PowerShell ps = PowerShell.Create()
                .AddCommand("Invoke-Command")
                .AddParameter("ScriptBlock", ScriptBlock.Create(command))
                .AddParameter("Session", _session);
            ps.Runspace = _runspace;
            var result = ps.Invoke();
            if (ps.HadErrors)
            {
                if (ps.Streams.Error.Count > 0)
                {
                    throw ps.Streams.Error[0].Exception;
                }
                throw new InvalidOperationException($"An unknown error occured while executing command {command}");
            }
            return result;
        }

        public async Task GetGroupMembersAsync(GroupMemberCollection memberCollection, Guid groupId)
        {
            string command = $"Get-DistributionGroupMember -Identity '{groupId.ToString()}' -ErrorAction 'Stop'";
            Collection<PSObject> result;
            try
            {
                result = InvokeCommand(command);
            }
            catch (RemoteException ex)
            {
                if (IsNotFoundError(ex))
                {
                    Exception notFoundException = CreateNotFoundException(groupId, null, ex);
                    if (notFoundException != null)
                    {
                        throw notFoundException;
                    }
                }
                throw;
            }
            if (result != null)
            {
                foreach (var member in result)
                {
                    memberCollection.Add(new GroupMember(
                        id: (string)member.Properties["ExternalDirectoryObjectId"].Value,
                        displayName: (string)member.Properties["PrimarySmtpAddress"].Value,
                        memberType: GroupMemberTypes.AzureAd
                    ));
                }
            }
            await Task.FromResult(0);
        }

        public async Task AddGroupMemberAsync(GroupMember member, Guid groupId)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }
            if (member.MemberType != GroupMemberTypes.AzureAd)
            {
                throw new ArgumentException(nameof(member), "Can only add members of type 'AzureAd'");
            }
            string command = $"Add-DistributionGroupMember -Identity '{groupId.ToString()}' -Member '{member.Id.ToString()}' -ErrorAction 'Stop'";
            try
            {
                InvokeCommand(command);
            }
            catch (RemoteException ex)
            {
                if (IsNotFoundError(ex))
                {
                    Exception notFoundException = CreateNotFoundException(groupId, member.Id, ex);
                    if (notFoundException != null)
                    {
                        throw notFoundException;
                    }
                }
                throw;
            }
            await Task.FromResult(0);
        }

        public async Task RemoveGroupMemberAsync(GroupMember member, Guid groupId)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }
            if (member.MemberType != GroupMemberTypes.AzureAd)
            {
                throw new ArgumentException(nameof(member), "Can only remove members of type 'AzureAd'");
            }
            string command = $"Remove-DistributionGroupMember -Identity '{groupId.ToString()}' -Member '{member.Id.ToString()}' -Confirm:$false -ErrorAction 'Stop'";
            try
            {
                InvokeCommand(command);
            }
            catch (RemoteException ex)
            {
                if (IsNotFoundError(ex))
                {
                    Exception notFoundException = CreateNotFoundException(groupId, member.Id, ex);
                    if (notFoundException != null)
                    {
                        throw notFoundException;
                    }
                }
                throw;
            }
            await Task.FromResult(0);
        }

        public async Task<GroupInfo> GetGroupInfoAsync(Guid groupId)
        {
            GroupInfo groupInfo = null;
            string command = $"Get-DistributionGroup -Identity '{groupId.ToString()}' -ErrorAction 'Stop'";
            Collection<PSObject> result;
            try
            {
                result = InvokeCommand(command);
            }
            catch (RemoteException ex)
            {
                if (IsNotFoundError(ex))
                {
                    Exception notFoundException = CreateNotFoundException(groupId, null, ex);
                    if (notFoundException != null)
                    {
                        throw notFoundException;
                    }
                }
                throw;
            }
            if (result != null && result.Count > 0)
            {
                groupInfo = new GroupInfo(
                    id: groupId,
                    displayName: (string)result[0].Properties["DisplayName"].Value,
                    store: GroupStores.Exo
                );
            }
            return await Task.FromResult(groupInfo);
        }

        private bool IsNotFoundError(RemoteException ex)
        {
            return ex.ErrorRecord.CategoryInfo.Reason.IEquals("ManagementObjectNotFoundException");
        }

        private Exception CreateNotFoundException(Guid groupId, Guid? memberId, RemoteException ex)
        {
            Exception exception = null;
            foreach (Match match in guidRegex.Matches(ex.Message))
            {
                Guid? guid = Guid.Parse(match.Groups["guid"].Value);
                if (guid == groupId)
                {
                    exception = GroupNotFoundException.Create(groupId, ex);
                    break;
                }
                if (guid == memberId)
                {
                    exception = MemberNotFoundException.Create(groupId, ex);
                    break;
                }
            }
            return exception;
        }

        public async Task GetMembersFromSourceAsync(GroupMemberCollection memberCollection, GrouperDocumentMember grouperMember)
        {
            await GetGroupMembersAsync(
                memberCollection,
                Guid.Parse(grouperMember.Rules.Where(r => r.Name.IEquals("Group")).First().Value)
            );
        }

        public IEnumerable<GroupMemberSources> GetSupportedGrouperMemberSources()
        {
            return new GroupMemberSources[] { GroupMemberSources.ExoGroup };
        }

        public IEnumerable<GroupStores> GetSupportedGroupStores()
        {
            return new GroupStores[] { GroupStores.Exo };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_runspace != null)
                    {
                        _runspace.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

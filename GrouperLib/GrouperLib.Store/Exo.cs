using GrouperLib.Config;
using GrouperLib.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GrouperLib.Store
{
    public class Exo : IMemberSource, IGroupStore, IDisposable
    {
        private readonly X509Certificate2 _certificate;
        private readonly string _appId;
        private readonly string _organization;
        private Runspace _runspace;
        private bool _initialized;
        private bool _disposed;

        private static readonly Regex guidRegex = new Regex(
            "\"(?<guid>[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12})\"",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        public Exo(string organization, string appId, X509Certificate2 certificate)
        {
            _organization = organization ?? throw new ArgumentNullException(nameof(organization));
            _appId = appId ?? throw new ArgumentNullException(nameof(appId));
            _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
        }

        public Exo(GrouperConfiguration config)
        {
            _organization = config.ExchangeOrganization ?? throw new ArgumentNullException(nameof(config.ExchangeOrganization));
            _appId = config.ExchangeAppId ?? throw new ArgumentNullException(nameof(config.ExchangeAppId));
            
            string certificateFilePath = config.ExchangeCertificateFilePath;
            string certificateThumbprint = config.ExchangeCertificateThumbprint;
            string certificateAsBase64 = config.ExchangeCertificateAsBase64;
            string certificatePassword = "";
            int num = (certificateFilePath   is null ? 0 : 1)
                    + (certificateThumbprint is null ? 0 : 1)
                    + (certificateAsBase64   is null ? 0 : 1);

            if (num != 1)
            {
                throw new ArgumentException(
                    $"You must specify one of {nameof(config.ExchangeCertificateFilePath)}, {nameof(config.ExchangeCertificateThumbprint)} or {nameof(config.ExchangeCertificateAsBase64)} in the configuration"
                );
            }

            if (certificateFilePath is not null || certificateAsBase64 is not null)
            {
                certificatePassword = config.ExchangeCertificatePassword ?? throw new ArgumentNullException(nameof(config.ExchangeCertificatePassword));
            }

            if (certificateFilePath is not null)
            {
                _certificate = Helpers.GetCertificateFromFile(certificateFilePath, certificatePassword);
                return;
            }

            if (certificateThumbprint is not null)
            {
                if (config.ExchangeCertificateStoreLocation is null)
                {
                    throw new ArgumentNullException(
                        $"If certificate is loaded from store {nameof(config.ExchangeCertificateStoreLocation)} must be specified in the configuration"
                    );
                }

                _certificate = Helpers.GetCertificateFromStore(certificateThumbprint, config.ExchangeCertificateStoreLocation.Value);
                return;
            }

            _certificate = Helpers.GetCertificateFromBase64String(certificateAsBase64, certificatePassword);
        }

        private void Connect()
        {
            if (_initialized)
            {
                return;
            }

            if (_runspace != null)
            {
                _runspace.Dispose();
                _runspace = null;
            }

            string script = @"
                param($Organization, $AppId, $Certificate)
                    Set-ExecutionPolicy 'RemoteSigned'
                    Import-Module ExchangeOnlineManagement -MinimumVersion '2.0.5'
                    $params = @{
                        Organization = $Organization
                        AppId = $AppId
                        Certificate = $Certificate
                        CommandName = @('Get-DistributionGroup','Get-DistributionGroupMember','Add-DistributionGroupMember','Remove-DistributionGroupMember')
                        ShowBanner = $false
                        ShowProgress = $false
                    }
                    Connect-ExchangeOnline @params
            ";
            _runspace = RunspaceFactory.CreateRunspace();
            _runspace.Open();
            using PowerShell ps = PowerShell.Create();
            ps.Runspace = _runspace;
            ps.AddScript(script)
                .AddParameter("Organization", _organization)
                .AddParameter("AppId", _appId)
                .AddParameter("Certificate", _certificate);
            var result = ps.Invoke();
            if (ps.HadErrors)
            {
                throw new AggregateException("Error creating Exchange Online PowerShell session",
                    ps.Streams.Error.Select(e => e.Exception).ToArray());
            }

            _initialized = true;
        }     

        private Collection<PSObject> InvokeCommand(string command, IDictionary parameters)
        {
            Connect();
            using PowerShell ps = PowerShell.Create();
            ps.Runspace = _runspace;
            ps.AddCommand(command).AddParameters(parameters);    
            var result = ps.Invoke();
            if (ps.HadErrors)
            {
                if (ps.HadErrors || result.Count != 1)
                {
                    throw new AggregateException($"Error while invoking command {command}",
                        ps.Streams.Error.Select(e => e.Exception).ToArray());
                }
            }

            return result;
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

        public async Task GetGroupMembersAsync(GroupMemberCollection memberCollection, Guid groupId)
        {
            string command = "Get-DistributionGroupMember";
            Hashtable parameters = new()
            {
                { "Identity", groupId.ToString() },
                { "ErrorAction", "Stop"}
            };

            Collection<PSObject> result;
            try
            {
                result = InvokeCommand(command, parameters);
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
                        memberType: GroupMemberType.AzureAd
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

            if (member.MemberType != GroupMemberType.AzureAd)
            {
                throw new ArgumentException(nameof(member), "Can only add members of type 'AzureAd'");
            }

            string command = "Add-DistributionGroupMember";
            Hashtable parameters = new()
            {
                { "Identity", groupId.ToString() },
                { "Member", member.Id.ToString() },
                { "ErrorAction", "Stop"}
            };

            try
            {
                InvokeCommand(command, parameters);
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

            if (member.MemberType != GroupMemberType.AzureAd)
            {
                throw new ArgumentException(nameof(member), "Can only remove members of type 'AzureAd'");
            }

            string command = "Remove-DistributionGroupMember";
            Hashtable parameters = new()
            {
                { "Identity", groupId.ToString() },
                { "Member", member.Id.ToString() },
                { "Confirm", false },
                { "ErrorAction", "Stop"}
            };

            try
            {
                InvokeCommand(command, parameters);
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
            string command = "Get-DistributionGroup";
            Hashtable parameters = new()
            {
                { "Identity", groupId.ToString() },
                { "ErrorAction", "Stop"}
            };

            Collection<PSObject> result;
            try
            {
                result = InvokeCommand(command, parameters);
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
                    store: GroupStore.Exo
                );
            }

            return await Task.FromResult(groupInfo);
        }

        public async Task GetMembersFromSourceAsync(GroupMemberCollection memberCollection, GrouperDocumentMember grouperMember, GroupMemberType memberType)
        {
            if (memberType != GroupMemberType.AzureAd)
            {
                throw new ArgumentException("Invalid member type", nameof(memberType));
            }
            await GetGroupMembersAsync(
                memberCollection,
                Guid.Parse(grouperMember.Rules.Where(r => r.Name.IEquals("Group")).First().Value)
            );
        }

        public IEnumerable<GroupMemberSource> GetSupportedGrouperMemberSources() => 
            new GroupMemberSource[] { GroupMemberSource.ExoGroup };

        public IEnumerable<GroupStore> GetSupportedGroupStores() => 
            new GroupStore[] { GroupStore.Exo };

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

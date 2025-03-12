using GrouperLib.Config;
using GrouperLib.Core;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace GrouperLib.Store;

[SupportedOSPlatform("windows")]
public sealed partial class Exo : IMemberSource, IGroupStore, IDisposable
{
    private readonly X509Certificate2 _certificate;
    private readonly string _appId;
    private readonly string _organization;
    private Runspace? _runspace;
    private bool _initialized;
    private bool _disposed;

    private static readonly Regex guidRegex = GuidRegex();

    public Exo(string organization, string appId, X509Certificate2 certificate)
    {
        _organization = organization ?? throw new ArgumentNullException(nameof(organization));
        _appId = appId ?? throw new ArgumentNullException(nameof(appId));
        _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
    }

    public Exo(GrouperConfiguration config)
    {
        _organization = config.ExchangeOrganization
                        ?? throw new InvalidOperationException($"{nameof(config.ExchangeOrganization)} is not set in the configuration");
        _appId = config.ExchangeAppId
                 ?? throw new InvalidOperationException($"{nameof(config.ExchangeAppId)} is not set in the configuration");
            
        int num = (config.ExchangeCertificateFilePath is null ? 0 : 1)
                  + (config.ExchangeCertificateThumbprint is null ? 0 : 1)
                  + (config.ExchangeCertificateAsBase64 is null ? 0 : 1);

        if (num != 1)
        {
            throw new InvalidOperationException(
                $"You must specify one of {nameof(config.ExchangeCertificateFilePath)}, {nameof(config.ExchangeCertificateThumbprint)} or {nameof(config.ExchangeCertificateAsBase64)} in the configuration"
            );
        }

        if (config.ExchangeCertificateFilePath is not null || config.ExchangeCertificateAsBase64 is not null)
        {
            if (config.ExchangeCertificatePassword is null)
            {
                throw new InvalidOperationException($"{nameof(config.ExchangeCertificatePassword)} is not set in the configuration");
            }

            if (config.ExchangeCertificateFilePath is not null)
            {
                _certificate = Helpers.GetCertificateFromFile(config.ExchangeCertificateFilePath, config.ExchangeCertificatePassword);
                return;
            }

            if (config.ExchangeCertificateAsBase64 is not null)
            {
                _certificate = Helpers.GetCertificateFromBase64String(config.ExchangeCertificateAsBase64, config.ExchangeCertificatePassword);

            }

        }

        if (config.ExchangeCertificateThumbprint is not null)
        {
            if (config.ExchangeCertificateStoreLocation is null)
            {
                throw new InvalidOperationException($"If certificate is loaded from store {nameof(config.ExchangeCertificateStoreLocation)} must be specified in the configuration");
            }
            _certificate = Helpers.GetCertificateFromStore(config.ExchangeCertificateThumbprint, config.ExchangeCertificateStoreLocation.Value);
            return;
        }

        if (_certificate is null)
        {
            throw new InvalidOperationException("No certificate was loaded.");
        }
    }

    private async Task Connect()
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

        const string script = """
            param($Organization, $AppId, $Certificate)
                Set-ExecutionPolicy 'RemoteSigned' -Scope 'CurrentUser'
                Import-Module ExchangeOnlineManagement -MinimumVersion '3.0.0'
                $params = @{
                    Organization = $Organization
                    AppId = $AppId
                    Certificate = $Certificate
                    CommandName = @('Get-DistributionGroup','Get-DistributionGroupMember','Add-DistributionGroupMember','Remove-DistributionGroupMember')
                    ShowBanner = $false
                    ShowProgress = $false
                }
                Connect-ExchangeOnline @params
            """;
        _runspace = RunspaceFactory.CreateRunspace();
        // _runspace.Open();
        await OpenRunspaceAsync(_runspace);
        using PowerShell ps = PowerShell.Create();
        ps.Runspace = _runspace;
        ps.AddScript(script)
            .AddParameter("Organization", _organization)
            .AddParameter("AppId", _appId)
            .AddParameter("Certificate", _certificate);
        await ps.InvokeAsync();
        if (ps.HadErrors)
        {
            throw new AggregateException("Error creating Exchange Online PowerShell session",
                [.. ps.Streams.Error.Select(e => e.Exception)]);
        }

        _initialized = true;
    }     
    
    private static async Task OpenRunspaceAsync(Runspace runspace)
    {
        TaskCompletionSource<bool> tcs = new();

        runspace.StateChanged += (_, args) =>
        {
            if (args.RunspaceStateInfo.State == RunspaceState.Opened)
            {
                tcs.TrySetResult(true);
                return;
            }
            
            if (args.RunspaceStateInfo.State is RunspaceState.Broken or RunspaceState.Closed)
            {
                tcs.TrySetException(new InvalidOperationException($"Runspace failed to open: {args.RunspaceStateInfo.Reason}"));
            }
        };

        runspace.OpenAsync();

        await tcs.Task;
    }
    
    private async Task<PSDataCollection<PSObject>> InvokeCommand(string command, IDictionary parameters)
    {
        await Connect();
        using PowerShell ps = PowerShell.Create();
        ps.Runspace = _runspace;
        ps.AddCommand(command).AddParameters(parameters);
        var result = await ps.InvokeAsync();
        if (!ps.HadErrors)
        {
            return result;
        }

        if (ps.HadErrors || result.Count != 1)
        {
            throw new AggregateException($"Error while invoking command {command}",
                [.. ps.Streams.Error.Select(e => e.Exception)]);
        }

        return result;
    }

    private static bool IsNotFoundError(RemoteException ex)
    {
        return ex.ErrorRecord.CategoryInfo.Reason.IEquals("ManagementObjectNotFoundException");
    }

    private static Exception CreateNotFoundException(Guid groupId, Guid? memberId, RemoteException ex)
    {
        foreach (Match match in guidRegex.Matches(ex.Message).Cast<Match>())
        {
            Guid? guid = Guid.Parse(match.Groups["guid"].Value);
            if (guid == groupId)
            {
                return GroupNotFoundException.Create(groupId, ex);
            }
            if (guid == memberId)
            {
                return MemberNotFoundException.Create(groupId, ex);
            }
        }

        return ex;
    }

    public async Task GetGroupMembersAsync(GroupMemberCollection memberCollection, Guid groupId)
    {
        const string command = "Get-DistributionGroupMember";
        Hashtable parameters = new()
        {
            { "Identity", groupId.ToString() },
            { "ResultSize", "Unlimited" },
            { "ErrorAction", "Stop"}
        };

        PSDataCollection<PSObject> result;
        try
        {
            result = await InvokeCommand(command, parameters);
        }
        catch (RemoteException ex)
        {
            if (IsNotFoundError(ex))
            {
                throw CreateNotFoundException(groupId, null, ex);
            }
            throw;
        }

        foreach (var member in result)
        {
            memberCollection.Add(new GroupMember(
                id: (string)member.Properties["ExternalDirectoryObjectId"].Value,
                displayName: (string)member.Properties["PrimarySmtpAddress"].Value,
                memberType: GroupMemberType.AzureAd
            ));
        }
    }

    public async Task AddGroupMemberAsync(GroupMember member, Guid groupId)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (member.MemberType != GroupMemberType.AzureAd)
        {
            throw new InvalidOperationException($"Can only add members of type {nameof(GroupMemberType.AzureAd)}");
        }

        const string command = "Add-DistributionGroupMember";
        Hashtable parameters = new()
        {
            { "Identity", groupId.ToString() },
            { "Member", member.Id.ToString() },
            { "ErrorAction", "Stop"}
        };

        try
        {
            await InvokeCommand(command, parameters);
        }
        catch (RemoteException ex)
        {
            if (IsNotFoundError(ex))
            {
                throw CreateNotFoundException(groupId, member.Id, ex);
            }
            throw;
        }
    }

    public async Task RemoveGroupMemberAsync(GroupMember member, Guid groupId)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (member.MemberType != GroupMemberType.AzureAd)
        {
            throw new InvalidOperationException($"Can only remove members of type {nameof(GroupMemberType.AzureAd)}");
        }

        const string command = "Remove-DistributionGroupMember";
        Hashtable parameters = new()
        {
            { "Identity", groupId.ToString() },
            { "Member", member.Id.ToString() },
            { "Confirm", false },
            { "ErrorAction", "Stop"}
        };

        try
        {
            await InvokeCommand(command, parameters);
        }
        catch (RemoteException ex)
        {
            if (IsNotFoundError(ex))
            {
                throw CreateNotFoundException(groupId, member.Id, ex);
            }
            throw;
        }
    }

    public async Task<GroupInfo> GetGroupInfoAsync(Guid groupId)
    {
        const string command = "Get-DistributionGroup";
        Hashtable parameters = new()
        {
            { "Identity", groupId.ToString() },
            { "ErrorAction", "Stop"}
        };

        PSDataCollection<PSObject> result;
        try
        {
            result = await InvokeCommand(command, parameters);
        }
        catch (RemoteException ex)
        {
            if (IsNotFoundError(ex))
            {
                throw CreateNotFoundException(groupId, null, ex);
            }
            throw;
        }

        if (result is null || result.Count == 0)
        {
            throw GroupNotFoundException.Create(groupId);
        }

        return new GroupInfo(
            id: groupId,
            displayName: (string)result[0].Properties["DisplayName"].Value,
            store: GroupStore.Exo
        );
    }

    public async Task GetMembersFromSourceAsync(GroupMemberCollection memberCollection, GrouperDocumentMember grouperMember, GroupMemberType memberType)
    {
        if (memberType != GroupMemberType.AzureAd)
        {
            throw new InvalidOperationException($"Can only get members of type {nameof(GroupMemberType.AzureAd)}");
        }

        var groupId = grouperMember.Rules.FirstOrDefault(r => r.Name.IEquals("Group"))?.Value
            ?? throw new InvalidOperationException("Cannot find a 'Group' rule with a group ID.");
       
        await GetGroupMembersAsync(
            memberCollection,
            Guid.Parse(groupId)
        );
    }

    
    
    public IEnumerable<GroupMemberSource> GetSupportedGrouperMemberSources() => [GroupMemberSource.ExoGroup];

    public IEnumerable<GroupStore> GetSupportedGroupStores() => [GroupStore.Exo];

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _runspace?.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
    }

    [GeneratedRegex("\"(?<guid>[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12})\"", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex GuidRegex();
}
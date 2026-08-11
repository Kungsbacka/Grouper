using Azure.Core;
using Azure.Identity;
using GrouperLib.Config;
using GrouperLib.Core;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace GrouperLib.Store;

[SupportedOSPlatform("windows")]
public sealed partial class Exo : IMemberSource, IGroupStore, IDisposable
{
    private static readonly JsonSerializerOptions SerializeOptions = new();
    private static readonly JsonSerializerOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;
    private readonly string _tenantId;
    private readonly X509Certificate2? _certificate;
    private bool _disposed;

    // 105 * odata.maxpagesize=1000 = 105k members. An EXO distribution list can only
    // contain a maximum of 100k members. 105 covers that and gives some headroom if
    // the OData server returns a trailing nextLink.
    private const int MaxPages = 105;

    private static string RequireGuidString(string? value, string settingName)
    {
        if (value is null || !Guid.TryParse(value, out _))
        {
            throw new ArgumentException($"'{settingName}' is not a valid GUID.", settingName);
        }
        return value;
    }

    // Retry sits outside the token handler so every attempt re-stamps the Authorization header.
    // A backoff long enough to outlive the token would otherwise retry its way into a 401.
    private static HttpClient CreateHttpClient(TokenCredential tokenCredential) => new(
        new ThrottleRetryHandler
        {
            InnerHandler = new EntraTokenHandler(tokenCredential, "https://outlook.office365.com/.default")
            {
                InnerHandler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }
            }
        })
    {
        BaseAddress = new Uri("https://outlook.office365.com/"),
        // Covers the whole SendAsync, backoff included. Must exceed ThrottleRetryHandler's
        // worst case - 60s of backoff ((MaxAttempts - 1) * MaxDelay) plus MaxAttempts round
        // trips - or the timeout cuts retries short and hides the 429 behind a cancellation.
        Timeout = TimeSpan.FromMinutes(3)
    };


    public Exo(string tenantId, string clientId, string clientSecret)
    {
        tenantId = RequireGuidString(tenantId, nameof(tenantId));
        clientId = RequireGuidString(clientId, nameof(clientId));
        _httpClient = CreateHttpClient(new ClientSecretCredential(tenantId, clientId, clientSecret));
        _tenantId = tenantId;
    }

    public Exo(string tenantId, string clientId, X509Certificate2 certificate)
    {
        tenantId = RequireGuidString(tenantId, nameof(tenantId));
        clientId = RequireGuidString(clientId, nameof(clientId));
        _httpClient = CreateHttpClient(new ClientCertificateCredential(tenantId, clientId, certificate));
        _tenantId = tenantId;
        // Don't save the certificate in _certificate since we don't own it and should not dispose it.
    }

    public Exo(GrouperConfiguration config)
    {
        string tenantId = RequireGuidString(config.ExoTenantId, nameof(config.ExoTenantId));
        string clientId = RequireGuidString(config.ExoClientId, nameof(config.ExoClientId));

        _tenantId = tenantId;

        int num = (config.ExoClientSecret is null ? 0 : 1)
                  + (config.ExoCertificateFilePath is null ? 0 : 1)
                  + (config.ExoCertificateThumbprint is null ? 0 : 1)
                  + (config.ExoCertificateAsBase64 is null ? 0 : 1);
        if (num != 1)
        {
            throw new InvalidOperationException(
                $"You must specify exactly one of {nameof(config.ExoClientSecret)}, {nameof(config.ExoCertificateFilePath)}, {nameof(config.ExoCertificateThumbprint)} or {nameof(config.ExoCertificateAsBase64)} in the configuration."
            );
        }

        if (config.ExoClientSecret is not null)
        {
            _httpClient = CreateHttpClient(new ClientSecretCredential(tenantId, clientId, config.ExoClientSecret));
            return;
        }

        if (config.ExoCertificateFilePath is not null || config.ExoCertificateAsBase64 is not null)
        {
            if (config.ExoCertificatePassword is null)
            {
                throw new InvalidOperationException($"{nameof(config.ExoCertificatePassword)} is not set in the configuration.");
            }
            if (config.ExoCertificateFilePath is not null)
            {
                _certificate = Helpers.GetCertificateFromFile(config.ExoCertificateFilePath, config.ExoCertificatePassword);
                _httpClient = CreateHttpClient(new ClientCertificateCredential(tenantId, clientId, _certificate));
                return;
            }
            if (config.ExoCertificateAsBase64 is not null)
            {
                _certificate = Helpers.GetCertificateFromBase64String(config.ExoCertificateAsBase64, config.ExoCertificatePassword);
                _httpClient = CreateHttpClient(new ClientCertificateCredential(tenantId, clientId, _certificate));
                return;
            }
        }

        if (config.ExoCertificateThumbprint is not null)
        {
            if (config.ExoCertificateStoreLocation is null)
            {
                throw new InvalidOperationException(
                    $"If certificate is loaded from store {nameof(config.ExoCertificateStoreLocation)} must be specified in the configuration."
                );
            }
            _certificate = Helpers.GetCertificateFromStore(config.ExoCertificateThumbprint, config.ExoCertificateStoreLocation.Value);
            _httpClient = CreateHttpClient(new ClientCertificateCredential(tenantId, clientId, _certificate));
        }

        if (_httpClient is null)
        {
            throw new InvalidOperationException("No HttpClient could be created using the Grouper configuration.");
        }
    }

    private async Task<List<T>> InvokeCommand<T>(string command, object parameters)
    {
        var list = new List<T>();
        var cmdletRequestId = Guid.NewGuid().ToString();
        var connectionId = Guid.NewGuid().ToString();
        var bodyJson = JsonSerializer.Serialize(new
        {
            CmdletInput = new
            {
                CmdletName = command,
                Parameters = parameters
            }
        }, SerializeOptions);

        string? nextPageUri = null;

        int pages = 0;
        while (true)
        {
            var url = nextPageUri ?? $"adminapi/beta/{_tenantId}/InvokeCommand";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("X-AnchorMailbox", $"APP:SystemMailbox{{bb558c35-97f1-4cb9-8ff7-d53741dc928c}}@{_tenantId}");
            req.Headers.Add("X-CmdletName", command);
            req.Headers.Add("client-request-id", cmdletRequestId);
            req.Headers.Add("connection-id", connectionId);
            req.Headers.Add("Prefer", "odata.maxpagesize=1000");
            req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

            using var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                string content = await resp.Content.ReadAsStringAsync();
                string? detail = TryExtractDetailMessage(content);
                ThrowExceptionForKnownErrors(detail);
                throw ExoException.Create(command, resp.StatusCode, detail);
            }

            if (resp.StatusCode == HttpStatusCode.NoContent || resp.Content.Headers.ContentLength == 0)
            {
                return list;
            }

            var page = await resp.Content.ReadFromJsonAsync<ExoResponse<T>>(DeserializeOptions)
                ?? throw new InvalidOperationException("EXO returned an empty response.");

            if (page.Value != null)
            {
                list.AddRange(page.Value);            
            }

            if (page.NextLink is null)
            {
                break;
            }

            // This does not cover the first request since the initial URL is relative. Subsequent requests are covered.
            if (page.NextLink == url)
            {
                throw new ExoException($"Exchange Online returned the same paging link twice for {command}; paging is not advancing.");
            }

            if (++pages >= MaxPages)
            {
                throw new ExoException($"Exchange Online returned more than {MaxPages} pages for {command}.");
            }

            nextPageUri = page.NextLink;
        }

        return list;
    }

    private static string TryExtractDetailMessage(string json)
    {
        string? raw;
        try
        {
            JsonNode? root = JsonNode.Parse(json);
            raw = AsString(root?["error"]?["details"]?[0]?["message"])
                ?? AsString(root?["error"]?["message"]);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return "";
        }

        if (raw is null)
        {
            return "";
        }

        try
        {
            return AsString(JsonNode.Parse(raw)?["Message"]) ?? raw;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return raw;
        }
    }

    private static string? AsString(JsonNode? node) =>
        node is JsonValue value && value.GetValueKind() == JsonValueKind.String
            ? value.GetValue<string>()
            : null;

    private static void ThrowExceptionForKnownErrors(string msg)
    {
        if (GroupNotFoundRegex().Match(msg) is Match groupMatch && groupMatch.Success)
        {
            Guid.TryParse(groupMatch.Groups[1].Value, out Guid groupId);
            throw GroupNotFoundException.Create(groupId, null);
        }
        else if (MemberNotFoundRegex().Match(msg) is Match memberMatch && memberMatch.Success)
        {
            Guid.TryParse(memberMatch.Groups[1].Value, out Guid memberId);
            throw MemberNotFoundException.Create(memberId, null);
        }
        else if (AlreadyMemberRegex().Match(msg) is Match alreadyMemberMatch && alreadyMemberMatch.Success)
        {
            Guid.TryParse(alreadyMemberMatch.Groups[1].Value, out Guid memberId);
            Guid.TryParse(alreadyMemberMatch.Groups[2].Value, out Guid groupId);
            throw ObjectAlreadyMemberException.Create(memberId, groupId, null);
        }
        else if (NotMemberRegex().Match(msg) is Match notMemberMatch && notMemberMatch.Success)
        {
            Guid.TryParse(notMemberMatch.Groups[1].Value, out Guid memberId);
            Guid.TryParse(notMemberMatch.Groups[2].Value, out Guid groupId);
            throw ObjectNotMemberException.Create(memberId, groupId, null);
        }
    }

    public async Task GetGroupMembersAsync(GroupMemberCollection memberCollection, Guid groupId)
    {
        const string command = "Get-DistributionGroupMember";
        object parameters = new
        {
            Identity = groupId.ToString(),
            ResultSize = "Unlimited",
            ErrorAction = "Stop",
        };

        List<ExoGroupMember> result;
        result = await InvokeCommand<ExoGroupMember>(command, parameters);

        foreach (var member in result)
        {
            // Warning: This can cause member drift if someone adds a non-synced member
            // without an ExternalDirectoryObjectId.
            if (member.ExternalDirectoryObjectId is null) continue;

            // Non-mail-enabled users can be members of a distribution group. They don't have a PrimarySmtpAddress,
            // so we use Identity as a fallback.
            string displayName;
            if (!string.IsNullOrEmpty(member.PrimarySmtpAddress))
            {
                displayName = member.PrimarySmtpAddress;
            }
            else if (!string.IsNullOrEmpty(member.Identity))
            {
                displayName = member.Identity;
            }
            else
            {
                continue;
            }

            memberCollection.Add(new GroupMember(
                id: member.ExternalDirectoryObjectId,
                displayName,
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
        object parameters = new
        {
            Identity = groupId.ToString(),
            Member = member.Id.ToString(),
            ErrorAction = "Stop"
        };

        await InvokeCommand<ExoModifyResponse>(command, parameters);
    }

    public async Task RemoveGroupMemberAsync(GroupMember member, Guid groupId)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (member.MemberType != GroupMemberType.AzureAd)
        {
            throw new InvalidOperationException($"Can only remove members of type {nameof(GroupMemberType.AzureAd)}");
        }

        const string command = "Remove-DistributionGroupMember";
        object parameters = new
        {
            Identity = groupId.ToString(),
            Member = member.Id.ToString(),
            Confirm = false,
            ErrorAction = "Stop"
        };

        await InvokeCommand<ExoModifyResponse>(command, parameters);
    }

    public async Task<GroupInfo> GetGroupInfoAsync(Guid groupId)
    {
        const string command = "Get-DistributionGroup";
        object parameters = new
        {
            Identity = groupId.ToString(),
            ErrorAction = "Stop"
        };

        ExoGroup? exoGroup = (await InvokeCommand<ExoGroup>(command, parameters)).FirstOrDefault() 
            ?? throw GroupNotFoundException.Create(groupId);
        

        string? displayName;
        if (!string.IsNullOrEmpty(exoGroup.DisplayName))
        {
            displayName = exoGroup.DisplayName;
        }
        else if (!string.IsNullOrEmpty(exoGroup.Identity))
        {
            displayName = exoGroup.Identity;
        }
        else
        {
            throw new InvalidOperationException($"Invalid group '{groupId}'");
        }

        return new GroupInfo(
            id: groupId,
            displayName: displayName,
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _httpClient.Dispose();
        _certificate?.Dispose();
        _disposed = true;
    }

    [GeneratedRegex("object '([^']+)' couldn't be found", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex GroupNotFoundRegex();

    [GeneratedRegex("^Couldn't find object \"([^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex MemberNotFoundRegex();

    [GeneratedRegex("\"([^\"]+)\" is already a member of the group \"([^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex AlreadyMemberRegex();

    [GeneratedRegex("The recipient \"([^\"]+)\" isn't a member of the group \"([^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex NotMemberRegex();
}

// Retries requests that Exchange Online rejects as throttled or transiently unavailable,
// honouring the Retry-After header when EXO supplies one.
internal sealed class ThrottleRetryHandler : DelegatingHandler
{
    private const int MaxAttempts = 4;

    // Caps what a Retry-After can ask for. Keep MaxAttempts and MaxDelay in step with
    // HttpClient.Timeout in Exo.CreateHttpClient - that timeout spans all attempts.
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(20);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            if (attempt >= MaxAttempts || !IsTransient(response.StatusCode))
            {
                return response;
            }

            TimeSpan delay = GetDelay(response, attempt);
            // Dispose the response we are discarding so the connection returns to the pool.
            response.Dispose();
            // cancellationToken carries HttpClient.Timeout, so the wait aborts instead of
            // sleeping past it. Without this the backoff becomes a hang.
            await Task.Delay(delay, cancellationToken);
        }
    }

    // 500 is deliberately absent: EXO uses it for genuine cmdlet failures, and retrying
    // those only spends more of the throttle budget before reporting the same error.
    private static bool IsTransient(HttpStatusCode status) =>
        status is HttpStatusCode.TooManyRequests
               or HttpStatusCode.ServiceUnavailable
               or HttpStatusCode.GatewayTimeout;

    private static TimeSpan GetDelay(HttpResponseMessage response, int attempt)
    {
        RetryConditionHeaderValue? retryAfter = response.Headers.RetryAfter;
        TimeSpan? advised = retryAfter?.Delta
            ?? (retryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : null);

        TimeSpan delay = advised is { } d && d > TimeSpan.Zero
            ? d                                               // EXO said how long to wait
            : TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)); // otherwise 1s, 2s, 4s

        return delay > MaxDelay ? MaxDelay : delay;
    }
}

internal sealed class EntraTokenHandler(TokenCredential credential, string scope) : DelegatingHandler
{
    private readonly string[] _scopes = [scope];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        AccessToken token = await credential.GetTokenAsync(
            new TokenRequestContext(_scopes), cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}

internal class ExoResponse<T>
{
    [JsonPropertyName("value")]
    public List<T>? Value { get; set; }

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; set; }
}

internal class ExoGroup
{
    public string? Identity { get; set; }
    public string? DisplayName { get; set; }
}

internal class ExoGroupMember
{
    public string? Identity { get; set; }
    public string? PrimarySmtpAddress { get; set; }
    public string? ExternalDirectoryObjectId { get; set; }
}

internal class ExoModifyResponse { }

// Exceptions.cs
public class ExoException : Exception
{
    public HttpStatusCode? StatusCode { get; }
    public string? CmdletName { get; }

    public ExoException(string message) : base(message) { }
    public ExoException(string message, Exception? innerException) : base(message, innerException) { }

    private ExoException(string message, HttpStatusCode statusCode, string cmdletName) : base(message)
    {
        StatusCode = statusCode;
        CmdletName = cmdletName;
    }

    // No inner exception by design - Worker.cs surfaces InnerException.Message over Message.
    public static ExoException Create(string cmdletName, HttpStatusCode statusCode, string? detail) =>
        new(string.IsNullOrWhiteSpace(detail)
                ? $"Exchange Online returned {(int)statusCode} ({statusCode}) for {cmdletName}."
                : $"Exchange Online returned {(int)statusCode} for {cmdletName}: {detail}",
            statusCode,
            cmdletName);
}

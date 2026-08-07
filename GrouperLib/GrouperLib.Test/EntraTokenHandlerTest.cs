using Azure.Core;
using GrouperLib.Store;
using System.Net;

namespace GrouperLib.Test;

/// <summary>
/// Covers <c>EntraTokenHandler</c>, which stamps the bearer token on every Exchange Online request.
///
/// The handler ordering it participates in is deliberate and documented in <c>Exo</c>: retry sits
/// *outside* this handler so each retry attempt passes through here again and re-reads the token.
/// A backoff long enough to outlive the token would otherwise retry its way into a 401. The
/// re-stamping test below is what pins that property.
/// </summary>
public class EntraTokenHandlerTest
{
    private sealed class FakeCredential : TokenCredential
    {
        private int _issued;
        public int Calls { get; private set; }
        public List<string[]> RequestedScopes { get; } = [];

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            Calls++;
            RequestedScopes.Add(requestContext.Scopes);
            return new AccessToken($"token-{++_issued}", DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new(GetToken(requestContext, cancellationToken));
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> _statuses;
        public List<string?> AuthHeaders { get; } = [];

        public CapturingHandler(params HttpStatusCode[] statuses) => _statuses = new(statuses);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            AuthHeaders.Add(request.Headers.Authorization?.ToString());
            HttpStatusCode status = _statuses.Count > 0 ? _statuses.Dequeue() : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status) { RequestMessage = request });
        }
    }

    private const string Scope = "https://outlook.office365.com/.default";

    [Fact]
    public async Task TestBearerTokenIsStamped()
    {
        FakeCredential credential = new();
        CapturingHandler inner = new();
        EntraTokenHandler handler = new(credential, Scope) { InnerHandler = inner };
        using HttpClient client = new(handler);

        using HttpResponseMessage _ = await client.GetAsync("https://outlook.office365.invalid/x");

        Assert.Equal("Bearer token-1", Assert.Single(inner.AuthHeaders));
    }

    [Fact]
    public async Task TestConfiguredScopeIsRequested()
    {
        FakeCredential credential = new();
        EntraTokenHandler handler = new(credential, Scope) { InnerHandler = new CapturingHandler() };
        using HttpClient client = new(handler);

        using HttpResponseMessage _ = await client.GetAsync("https://outlook.office365.invalid/x");

        Assert.Equal([Scope], Assert.Single(credential.RequestedScopes));
    }

    /// <summary>
    /// The token is fetched per request rather than once per handler, so a long-lived HttpClient
    /// does not pin a token past its expiry. TokenCredential implementations cache internally, so
    /// this is cheap.
    /// </summary>
    [Fact]
    public async Task TestTokenIsFetchedForEveryRequest()
    {
        FakeCredential credential = new();
        CapturingHandler inner = new();
        EntraTokenHandler handler = new(credential, Scope) { InnerHandler = inner };
        using HttpClient client = new(handler);

        for (int i = 0; i < 3; i++)
        {
            using HttpResponseMessage _ = await client.GetAsync("https://outlook.office365.invalid/x");
        }

        Assert.Equal(3, credential.Calls);
        Assert.Equal(["Bearer token-1", "Bearer token-2", "Bearer token-3"], inner.AuthHeaders);
    }

    /// <summary>
    /// With the retry handler outside the token handler, every retry attempt re-enters this handler
    /// and gets the Authorization header re-stamped. This is the ordering property the comment in
    /// <c>Exo.CreateHttpClient</c> describes; if the two handlers were ever swapped, retries would
    /// reuse the token captured before the backoff.
    /// </summary>
    [Fact]
    public async Task TestEveryRetryAttemptGetsTheTokenRestamped()
    {
        FakeCredential credential = new();
        CapturingHandler inner = new(
            HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests, HttpStatusCode.OK);

        // Same composition order as Exo.CreateHttpClient: retry outside, token inside.
        ThrottleRetryHandler retry = new()
        {
            InnerHandler = new EntraTokenHandler(credential, Scope) { InnerHandler = inner }
        };
        using HttpClient client = new(retry) { Timeout = TimeSpan.FromMinutes(1) };
        HttpRequestMessage request = new(HttpMethod.Get, "https://outlook.office365.invalid/x");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, credential.Calls);
        Assert.Equal(["Bearer token-1", "Bearer token-2", "Bearer token-3"], inner.AuthHeaders);
    }
}

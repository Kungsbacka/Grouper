using GrouperLib.Store;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace GrouperLib.Test;

/// <summary>
/// Covers <c>ThrottleRetryHandler</c>, the retry policy in front of the Exchange Online REST
/// endpoint. It is the riskiest logic in GrouperLib.Store: a mistake here either hammers a service
/// that is already asking us to back off, or gives up on a request that would have succeeded.
///
/// A caveat worth stating plainly: these tests pin what the handler *does*, not whether Exchange
/// Online actually behaves the way the policy assumes. The adminapi InvokeCommand endpoint is
/// undocumented, so the policy is an informed guess. The value here is that the guess is now
/// executable -- when real behaviour is known, the diff shows exactly what changed.
///
/// Retries are driven by a millisecond-scale Retry-After header so the suite stays fast. The header
/// object is read directly via <c>Headers.RetryAfter.Delta</c> and never serialized, so sub-second
/// values survive even though the wire format is whole seconds.
/// </summary>
public class ThrottleRetryHandlerTest
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(1);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> _statuses;
        public int Calls { get; private set; }
        public List<string?> Bodies { get; } = [];
        public TimeSpan? RetryAfterDelta { get; init; }
        public DateTimeOffset? RetryAfterDate { get; init; }

        public StubHandler(params HttpStatusCode[] statuses) => _statuses = new(statuses);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            Bodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(ct));
            HttpStatusCode status = _statuses.Count > 0 ? _statuses.Dequeue() : HttpStatusCode.OK;
            HttpResponseMessage response = new(status) { RequestMessage = request };
            if (RetryAfterDelta is { } delta)
            {
                response.Headers.RetryAfter = new RetryConditionHeaderValue(delta);
            }
            else if (RetryAfterDate is { } date)
            {
                response.Headers.RetryAfter = new RetryConditionHeaderValue(date);
            }
            return response;
        }
    }

    private static async Task<(HttpResponseMessage Response, StubHandler Stub)> SendAsync(StubHandler stub)
    {
        ThrottleRetryHandler handler = new() { InnerHandler = stub };
        using HttpClient client = new(handler) { Timeout = TimeSpan.FromMinutes(1) };
        HttpRequestMessage request = new(HttpMethod.Post, "https://outlook.office365.invalid/adminapi")
        {
            Content = new StringContent("""{"CmdletInput":{"CmdletName":"Get-DistributionGroup"}}""",
                Encoding.UTF8, "application/json")
        };
        return (await client.SendAsync(request), stub);
    }

    /// <summary>Transient statuses are retried up to the 4-attempt ceiling.</summary>
    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task TestTransientStatusIsRetriedToTheAttemptCeiling(HttpStatusCode status)
    {
        StubHandler stub = new(status, status, status, status) { RetryAfterDelta = Tick };

        (HttpResponseMessage response, StubHandler s) = await SendAsync(stub);
        using (response)
        {
            Assert.Equal(4, s.Calls);
            Assert.Equal(status, response.StatusCode);
        }
    }

    /// <summary>
    /// After exhausting retries the handler **returns** the final response rather than throwing.
    /// <c>Exo.InvokeCommand</c> depends on that: it reads the body to pull out Exchange Online's own
    /// error detail and map it to a typed exception. A handler that threw instead would reduce a
    /// throttle failure to an opaque transport error, which is why Kiota's RetryHandler was not
    /// adopted here.
    /// </summary>
    [Fact]
    public async Task TestExhaustedRetriesReturnTheResponseRatherThanThrowing()
    {
        StubHandler stub = new(
            HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests,
            HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests) { RetryAfterDelta = Tick };

        (HttpResponseMessage response, _) = await SendAsync(stub);
        using (response)
        {
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }

    /// <summary>
    /// 500 is deliberately not retried: Exchange Online uses it for genuine cmdlet failures, so
    /// retrying spends throttle budget to arrive at the same error.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task TestNonTransientStatusIsNotRetried(HttpStatusCode status)
    {
        (HttpResponseMessage response, StubHandler s) = await SendAsync(new StubHandler(status));
        using (response)
        {
            Assert.Equal(1, s.Calls);
            Assert.Equal(status, response.StatusCode);
        }
    }

    [Fact]
    public async Task TestSuccessIsNotRetried()
    {
        (HttpResponseMessage response, StubHandler s) = await SendAsync(new StubHandler(HttpStatusCode.OK));
        using (response)
        {
            Assert.Equal(1, s.Calls);
        }
    }

    /// <summary>A transient failure that then succeeds stops retrying at the success.</summary>
    [Fact]
    public async Task TestRetryStopsOnFirstSuccess()
    {
        StubHandler stub = new(HttpStatusCode.TooManyRequests, HttpStatusCode.OK) { RetryAfterDelta = Tick };

        (HttpResponseMessage response, StubHandler s) = await SendAsync(stub);
        using (response)
        {
            Assert.Equal(2, s.Calls);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    /// <summary>Retry-After may arrive as an absolute HTTP-date rather than a delta.</summary>
    [Fact]
    public async Task TestRetryAfterAsHttpDateIsHonoured()
    {
        StubHandler stub = new(HttpStatusCode.TooManyRequests, HttpStatusCode.OK)
        {
            RetryAfterDate = DateTimeOffset.UtcNow.AddMilliseconds(1)
        };

        (HttpResponseMessage response, StubHandler s) = await SendAsync(stub);
        using (response)
        {
            Assert.Equal(2, s.Calls);
        }
    }

    /// <summary>
    /// A Retry-After date already in the past yields a non-positive delay, which must fall back to
    /// the exponential schedule rather than being used as-is.
    /// </summary>
    [Fact]
    public async Task TestRetryAfterInThePastFallsBackToExponentialBackoff()
    {
        StubHandler stub = new(HttpStatusCode.TooManyRequests, HttpStatusCode.OK)
        {
            RetryAfterDate = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        (HttpResponseMessage response, StubHandler s) = await SendAsync(stub);
        sw.Stop();
        using (response)
        {
            Assert.Equal(2, s.Calls);
            // First exponential step is 1s; proves the past date was not taken literally.
            Assert.True(sw.ElapsedMilliseconds >= 900, $"expected a ~1s backoff, took {sw.ElapsedMilliseconds}ms");
        }
    }

    /// <summary>
    /// The request object is re-sent across attempts, so its content must survive being read more
    /// than once. StringContent is buffered and does, but a future change to a stream-backed body
    /// would silently send an empty payload on every retry.
    /// </summary>
    [Fact]
    public async Task TestRequestBodyIsResentIntactOnEveryAttempt()
    {
        StubHandler stub = new(
            HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests, HttpStatusCode.OK)
        { RetryAfterDelta = Tick };

        (HttpResponseMessage response, StubHandler s) = await SendAsync(stub);
        using (response)
        {
            Assert.Equal(3, s.Bodies.Count);
            Assert.All(s.Bodies, b =>
                Assert.Equal("""{"CmdletInput":{"CmdletName":"Get-DistributionGroup"}}""", b));
        }
    }

    /// <summary>
    /// Cancellation must abort the backoff wait rather than sleeping through it. HttpClient.Timeout
    /// surfaces as this token, so without it a long backoff would become an unkillable hang.
    /// </summary>
    [Fact]
    public async Task TestCancellationDuringBackoffAbortsRatherThanSleeping()
    {
        StubHandler stub = new(HttpStatusCode.TooManyRequests, HttpStatusCode.OK);   // no Retry-After: 1s backoff
        ThrottleRetryHandler handler = new() { InnerHandler = stub };
        using HttpClient client = new(handler);
        using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(50));

        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetAsync("https://outlook.office365.invalid/adminapi", cts.Token));
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 900, $"cancellation should cut the backoff short, took {sw.ElapsedMilliseconds}ms");
    }
}

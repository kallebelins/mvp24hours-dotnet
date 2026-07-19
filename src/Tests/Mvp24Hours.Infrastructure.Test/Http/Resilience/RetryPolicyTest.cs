//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Mvp24Hours.Infrastructure.Http.Options;
using Mvp24Hours.Infrastructure.Http.Resilience;
using Mvp24Hours.Infrastructure.Test.Support;
using Polly;

namespace Mvp24Hours.Infrastructure.Test.Http.Resilience;

[Trait("Category", "Unit")]
public class RetryPolicyTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new RetryPolicy(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void PolicyName_ShouldBeRetryPolicy()
    {
        var policy = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions());
        policy.PolicyName.Should().Be("RetryPolicy");
    }

    [Fact]
    public void GetPollyPolicy_ShouldReturnConfiguredPolicy()
    {
        var policy = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions());
        IAsyncPolicy<HttpResponseMessage> polly = policy.GetPollyPolicy();
        polly.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithNullRequestFactory_ShouldThrowArgumentNullException()
    {
        var policy = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions());

        Func<Task> act = () => policy.ExecuteAsync(
            null!,
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("requestFactory");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullSendAsync_ShouldThrowArgumentNullException()
    {
        var policy = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions());

        Func<Task> act = () => policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("sendAsync");
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldNotRetry()
    {
        var policy = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions(enabled: false, maxRetries: 5));
        int attempts = 0;

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.Counting(
                ResilienceTestHelpers.RespondWith(HttpStatusCode.InternalServerError),
                _ => attempts++));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_ShouldReturnResponseWithoutRetry()
    {
        var policy = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions());
        int attempts = 0;

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.Counting(
                ResilienceTestHelpers.RespondWith(HttpStatusCode.OK),
                _ => attempts++));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_OnTransientHttpError_ShouldRetryUntilSuccess()
    {
        var policy = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions(maxRetries: 3));
        int attempts = 0;

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.Counting(
                ResilienceTestHelpers.FailThenSucceed(2),
                _ => attempts++));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_OnHttpRequestException_ShouldRetryUntilSuccess()
    {
        var policy = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions(maxRetries: 3));
        int attempts = 0;

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.Counting(
                ResilienceTestHelpers.ThrowThenSucceed(2),
                _ => attempts++));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRetriesExhausted_ShouldReturnLastFailure()
    {
        var policy = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions(maxRetries: 2));
        int attempts = 0;

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.Counting(
                ResilienceTestHelpers.RespondWith(HttpStatusCode.ServiceUnavailable),
                _ => attempts++));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        attempts.Should().Be(3); // initial + 2 retries
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomRetryStatusCode_ShouldRetry()
    {
        var policy = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions(
            maxRetries: 2,
            retryStatusCodes: [418]));
        int attempts = 0;

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.Counting(
                ResilienceTestHelpers.FailThenSucceed(1, (HttpStatusCode)418),
                _ => attempts++));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonRetryableStatus_ShouldNotRetry()
    {
        var policy = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions(
            maxRetries: 3,
            retryStatusCodes: [500]));
        int attempts = 0;

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.Counting(
                ResilienceTestHelpers.RespondWith(HttpStatusCode.BadRequest),
                _ => attempts++));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        attempts.Should().Be(1);
    }

    [Theory]
    [InlineData(BackoffType.Constant)]
    [InlineData(BackoffType.Linear)]
    [InlineData(BackoffType.Exponential)]
    [InlineData(BackoffType.DecorrelatedJitter)]
    public async Task ExecuteAsync_WithBackoffTypes_ShouldEventuallySucceed(BackoffType backoffType)
    {
        var policy = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions(
            maxRetries: 2,
            backoffType: backoffType,
            jitterFactor: backoffType == BackoffType.DecorrelatedJitter ? 0.2 : 0));

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.FailThenSucceed(1));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExecuteAsync_WithRetryAfterHeader_ShouldUseHeaderDelay()
    {
        var policy = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions(
            maxRetries: 1,
            initialDelay: TimeSpan.FromMilliseconds(1),
            maxDelay: TimeSpan.FromSeconds(5)));

        int attempts = 0;

        async Task<HttpResponseMessage> SendAsync(HttpRequestMessage _, CancellationToken __)
        {
            attempts++;
            if (attempts == 1)
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
                    TimeSpan.FromMilliseconds(5));
                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        HttpResponseMessage result = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            SendAsync);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateNewRequestPerAttempt()
    {
        var policy = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions(maxRetries: 2));
        var requestUris = new List<Uri?>();
        int factoryCalls = 0;

        await policy.ExecuteAsync(
            () =>
            {
                factoryCalls++;
                return new HttpRequestMessage(HttpMethod.Get, $"https://api.example.com/{factoryCalls}");
            },
            (request, _) =>
            {
                requestUris.Add(request.RequestUri);
                HttpStatusCode status = requestUris.Count < 2
                    ? HttpStatusCode.InternalServerError
                    : HttpStatusCode.OK;
                return Task.FromResult(new HttpResponseMessage(status));
            });

        factoryCalls.Should().Be(2);
        requestUris.Should().HaveCount(2);
        requestUris[0]!.AbsolutePath.Should().Be("/1");
        requestUris[1]!.AbsolutePath.Should().Be("/2");
    }
}

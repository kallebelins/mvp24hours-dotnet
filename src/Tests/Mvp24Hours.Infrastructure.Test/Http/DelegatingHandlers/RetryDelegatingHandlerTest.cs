//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Http.DelegatingHandlers;
using Mvp24Hours.Infrastructure.Http.Options;
using Mvp24Hours.Infrastructure.Test.Support;
using Mvp24Hours.Infrastructure.Testing.Http;

namespace Mvp24Hours.Infrastructure.Test.Http.DelegatingHandlers;

[Trait("Category", "Unit")]
public class RetryDelegatingHandlerTest
{
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new RetryDelegatingHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldUseDefaults()
    {
        var handler = new RetryDelegatingHandler(NullLogger<RetryDelegatingHandler>.Instance, null!);
        handler.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler);

        Func<Task> act = () => client.SendAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_WhenDisabled_ShouldNotRetry()
    {
        var inner = DelegatingHandlerTestHelpers.CreateSequenceHandler(
            HttpStatusCode.InternalServerError,
            HttpStatusCode.OK);
        var handler = CreateHandler(DelegatingHandlerTestHelpers.CreateRetryOptions(enabled: false, maxRetries: 5));
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        inner.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_OnSuccess_ShouldNotRetry()
    {
        var inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        var handler = CreateHandler();
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_OnTransientStatus_ShouldRetryUntilSuccess()
    {
        var inner = DelegatingHandlerTestHelpers.CreateFailThenSucceedHandler(2);
        var handler = CreateHandler(DelegatingHandlerTestHelpers.CreateRetryOptions(maxRetries: 3));
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.RequestCount.Should().Be(3);
    }

    [Fact]
    public async Task SendAsync_OnHttpRequestException_ShouldRetryUntilSuccess()
    {
        var inner = DelegatingHandlerTestHelpers.CreateThrowThenSucceedHandler(2);
        var handler = CreateHandler(DelegatingHandlerTestHelpers.CreateRetryOptions(maxRetries: 3));
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.RequestCount.Should().Be(3);
    }

    [Fact]
    public async Task SendAsync_WhenRetriesExhausted_ShouldReturnLastResponse()
    {
        var inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.ServiceUnavailable);
        var handler = CreateHandler(DelegatingHandlerTestHelpers.CreateRetryOptions(maxRetries: 2));
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        inner.RequestCount.Should().Be(3);
    }

    [Fact]
    public async Task SendAsync_OnConfiguredStatusCode_ShouldRetry()
    {
        var inner = DelegatingHandlerTestHelpers.CreateFailThenSucceedHandler(
            1,
            failureStatus: HttpStatusCode.Conflict);
        var handler = CreateHandler(DelegatingHandlerTestHelpers.CreateRetryOptions(
            maxRetries: 2,
            retryStatusCodes: [409]));
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_WithRetryAfterHeader_ShouldSucceed()
    {
        int attempts = 0;
        var inner = new TestHttpMessageHandler();
        inner.When(_ => true, _ =>
        {
            attempts++;
            if (attempts == 1)
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
                    TimeSpan.FromMilliseconds(5));
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var handler = CreateHandler(DelegatingHandlerTestHelpers.CreateRetryOptions(maxRetries: 2));
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        attempts.Should().Be(2);
    }

    [Theory]
    [InlineData(BackoffType.Constant)]
    [InlineData(BackoffType.Linear)]
    [InlineData(BackoffType.Exponential)]
    [InlineData(BackoffType.DecorrelatedJitter)]
    public async Task SendAsync_WithBackoffTypes_ShouldRetry(BackoffType backoffType)
    {
        var inner = DelegatingHandlerTestHelpers.CreateFailThenSucceedHandler(1);
        var handler = CreateHandler(DelegatingHandlerTestHelpers.CreateRetryOptions(
            maxRetries: 2,
            backoffType: backoffType,
            jitterFactor: 0.1));
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.RequestCount.Should().Be(2);
    }

    private static RetryDelegatingHandler CreateHandler(RetryPolicyOptions? options = null)
    {
        return new RetryDelegatingHandler(
            NullLogger<RetryDelegatingHandler>.Instance,
            options ?? DelegatingHandlerTestHelpers.CreateRetryOptions());
    }
}

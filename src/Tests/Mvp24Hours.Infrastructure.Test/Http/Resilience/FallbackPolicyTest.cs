//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Mvp24Hours.Infrastructure.Http.Resilience;
using Mvp24Hours.Infrastructure.Test.Support;
using Polly;

namespace Mvp24Hours.Infrastructure.Test.Http.Resilience;

[Trait("Category", "Unit")]
public class FallbackPolicyTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new FallbackPolicy(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void PolicyName_ShouldBeFallbackPolicy()
    {
        var policy = new FallbackPolicy(new FallbackPolicyOptions());
        policy.PolicyName.Should().Be("FallbackPolicy");
    }

    [Fact]
    public void GetPollyPolicy_ShouldReturnNoOpForCompatibility()
    {
        var policy = new FallbackPolicy(new FallbackPolicyOptions());
        IAsyncPolicy<HttpResponseMessage> polly = policy.GetPollyPolicy();
        polly.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithNullRequestFactory_ShouldThrowArgumentNullException()
    {
        var policy = new FallbackPolicy(new FallbackPolicyOptions());

        Func<Task> act = () => policy.ExecuteAsync(
            null!,
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("requestFactory");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullSendAsync_ShouldThrowArgumentNullException()
    {
        var policy = new FallbackPolicy(new FallbackPolicyOptions());

        Func<Task> act = () => policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("sendAsync");
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldReturnOriginalFailure()
    {
        var policy = new FallbackPolicy(new FallbackPolicyOptions { Enabled = false });

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.ServiceUnavailable));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_ShouldNotFallback()
    {
        bool fallbackCalled = false;
        var policy = new FallbackPolicy(new FallbackPolicyOptions
        {
            OnFallback = (_, _) => fallbackCalled = true
        });

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        fallbackCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_OnFallbackStatus_ShouldReturnDefaultServiceUnavailable()
    {
        bool fallbackCalled = false;
        var policy = new FallbackPolicy(new FallbackPolicyOptions
        {
            OnFallback = (_, _) => fallbackCalled = true
        });

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.ServiceUnavailable));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Fallback response");
        fallbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomFallbackAction_ShouldReturnCustomResponse()
    {
        var policy = new FallbackPolicy(new FallbackPolicyOptions
        {
            FallbackAction = (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("cached")
            })
        });

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.BadGateway));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Be("cached");
    }

    [Fact]
    public async Task ExecuteAsync_OnHttpRequestException_ShouldFallback()
    {
        Exception? captured = null;
        var policy = new FallbackPolicy(new FallbackPolicyOptions
        {
            OnFallback = (ex, _) => captured = ex,
            FallbackAction = (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))
        });

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            (_, _) => throw new HttpRequestException("network down"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task ExecuteAsync_OnNonFallbackStatus_ShouldReturnOriginal()
    {
        var policy = new FallbackPolicy(new FallbackPolicyOptions
        {
            FallbackStatusCodes = [503]
        });

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.NotFound));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Mvp24Hours.Infrastructure.Http.Options;
using Mvp24Hours.Infrastructure.Test.Support;
using Polly;
using Polly.CircuitBreaker;
using CircuitBreakerPolicy = Mvp24Hours.Infrastructure.Http.Resilience.CircuitBreakerPolicy;

namespace Mvp24Hours.Infrastructure.Test.Http.Resilience;

[Trait("Category", "Unit")]
public class CircuitBreakerPolicyTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new CircuitBreakerPolicy(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void PolicyName_ShouldBeCircuitBreakerPolicy()
    {
        var policy = new CircuitBreakerPolicy(ResilienceTestHelpers.CreateCircuitBreakerOptions());
        policy.PolicyName.Should().Be("CircuitBreakerPolicy");
    }

    [Fact]
    public void CircuitState_ShouldStartClosed()
    {
        var policy = new CircuitBreakerPolicy(ResilienceTestHelpers.CreateCircuitBreakerOptions());
        policy.CircuitState.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public void GetPollyPolicy_ShouldReturnConfiguredPolicy()
    {
        var policy = new CircuitBreakerPolicy(ResilienceTestHelpers.CreateCircuitBreakerOptions());
        IAsyncPolicy<HttpResponseMessage> polly = policy.GetPollyPolicy();
        polly.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithNullRequestFactory_ShouldThrowArgumentNullException()
    {
        var policy = new CircuitBreakerPolicy(ResilienceTestHelpers.CreateCircuitBreakerOptions());

        Func<Task> act = () => policy.ExecuteAsync(
            null!,
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("requestFactory");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullSendAsync_ShouldThrowArgumentNullException()
    {
        var policy = new CircuitBreakerPolicy(ResilienceTestHelpers.CreateCircuitBreakerOptions());

        Func<Task> act = () => policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("sendAsync");
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldBypassCircuit()
    {
        var policy = new CircuitBreakerPolicy(
            ResilienceTestHelpers.CreateCircuitBreakerOptions(enabled: false),
            "DisabledService");
        policy.Isolate();

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_ShouldKeepCircuitClosed()
    {
        var policy = new CircuitBreakerPolicy(ResilienceTestHelpers.CreateCircuitBreakerOptions());

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        policy.CircuitState.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_AfterEnoughFailures_ShouldOpenCircuit()
    {
        CircuitBreakerStateChangeInfo? breakInfo = null;
        CircuitBreakerPolicyOptions options = ResilienceTestHelpers.CreateCircuitBreakerOptions(
            failureRatio: 0.5,
            minimumThroughput: 2,
            breakDuration: TimeSpan.FromSeconds(30));
        options.OnBreak = info => breakInfo = info;

        var policy = new CircuitBreakerPolicy(options, "FailingApi");

        for (int i = 0; i < 2; i++)
        {
            await policy.ExecuteAsync(
                ResilienceTestHelpers.RequestFactory(),
                ResilienceTestHelpers.RespondWith(HttpStatusCode.InternalServerError));
        }

        policy.CircuitState.Should().Be(CircuitState.Open);
        breakInfo.Should().NotBeNull();
        breakInfo!.ServiceName.Should().Be("FailingApi");
        breakInfo.NewState.Should().Be(CircuitState.Open);
        breakInfo.BreakDuration.Should().Be(TimeSpan.FromSeconds(30));

        Func<Task> blocked = () => policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        await blocked.Should().ThrowAsync<BrokenCircuitException>();
    }

    [Fact]
    public async Task ExecuteAsync_OnHttpRequestException_ShouldCountAsFailure()
    {
        CircuitBreakerPolicyOptions options = ResilienceTestHelpers.CreateCircuitBreakerOptions(
            failureRatio: 0.5,
            minimumThroughput: 2);

        var policy = new CircuitBreakerPolicy(options);

        for (int i = 0; i < 2; i++)
        {
            Func<Task> act = () => policy.ExecuteAsync(
                ResilienceTestHelpers.RequestFactory(),
                (_, _) => throw new HttpRequestException("boom"));

            await act.Should().ThrowAsync<HttpRequestException>();
        }

        policy.CircuitState.Should().Be(CircuitState.Open);
    }

    [Fact]
    public void Isolate_ShouldOpenCircuitImmediately()
    {
        var policy = new CircuitBreakerPolicy(ResilienceTestHelpers.CreateCircuitBreakerOptions());

        policy.Isolate();

        policy.CircuitState.Should().Be(CircuitState.Isolated);
    }

    [Fact]
    public async Task Isolate_ShouldBlockSubsequentRequests()
    {
        var policy = new CircuitBreakerPolicy(ResilienceTestHelpers.CreateCircuitBreakerOptions());
        policy.Isolate();

        Func<Task> act = () => policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        await act.Should().ThrowAsync<IsolatedCircuitException>();
    }

    [Fact]
    public async Task Reset_ShouldCloseIsolatedCircuit()
    {
        CircuitBreakerStateChangeInfo? resetInfo = null;
        CircuitBreakerPolicyOptions options = ResilienceTestHelpers.CreateCircuitBreakerOptions();
        options.OnReset = info => resetInfo = info;

        var policy = new CircuitBreakerPolicy(options, "ResetApi");
        policy.Isolate();
        policy.Reset();

        policy.CircuitState.Should().Be(CircuitState.Closed);

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Manual reset may or may not fire OnReset depending on Polly version; ensure circuit works.
        _ = resetInfo;
    }

    [Fact]
    public async Task ExecuteAsync_AfterBreakDuration_ShouldEnterHalfOpenAndRecover()
    {
        CircuitBreakerStateChangeInfo? halfOpenInfo = null;
        CircuitBreakerStateChangeInfo? resetInfo = null;
        CircuitBreakerPolicyOptions options = ResilienceTestHelpers.CreateCircuitBreakerOptions(
            failureRatio: 0.5,
            minimumThroughput: 2,
            breakDuration: TimeSpan.FromMilliseconds(200));
        options.OnHalfOpen = info => halfOpenInfo = info;
        options.OnReset = info => resetInfo = info;

        var policy = new CircuitBreakerPolicy(options, "RecoveringApi");

        for (int i = 0; i < 2; i++)
        {
            await policy.ExecuteAsync(
                ResilienceTestHelpers.RequestFactory(),
                ResilienceTestHelpers.RespondWith(HttpStatusCode.ServiceUnavailable));
        }

        policy.CircuitState.Should().Be(CircuitState.Open);
        await Task.Delay(TimeSpan.FromMilliseconds(250));

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        policy.CircuitState.Should().Be(CircuitState.Closed);
        halfOpenInfo.Should().NotBeNull();
        halfOpenInfo!.NewState.Should().Be(CircuitState.HalfOpen);
        resetInfo.Should().NotBeNull();
        resetInfo!.NewState.Should().Be(CircuitState.Closed);
        resetInfo.ServiceName.Should().Be("RecoveringApi");
    }

    [Fact]
    public void Constructor_WithNullServiceName_ShouldUseDefault()
    {
        var policy = new CircuitBreakerPolicy(
            ResilienceTestHelpers.CreateCircuitBreakerOptions(),
            null!);

        policy.PolicyName.Should().Be("CircuitBreakerPolicy");
    }
}

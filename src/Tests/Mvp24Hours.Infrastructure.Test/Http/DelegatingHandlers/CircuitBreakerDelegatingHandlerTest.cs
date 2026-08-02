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
using Polly.CircuitBreaker;

namespace Mvp24Hours.Infrastructure.Test.Http.DelegatingHandlers;

[Trait("Category", "Unit")]
public class CircuitBreakerDelegatingHandlerTest
{
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new CircuitBreakerDelegatingHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldUseDefaults()
    {
        var handler = new CircuitBreakerDelegatingHandler(
            NullLogger<CircuitBreakerDelegatingHandler>.Instance,
            null!);
        handler.CircuitState.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        CircuitBreakerDelegatingHandler handler = CreateHandler();
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler);

        Func<Task> act = () => client.SendAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_WhenDisabled_ShouldPassThrough()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.InternalServerError);
        CircuitBreakerDelegatingHandler handler = CreateHandler(DelegatingHandlerTestHelpers.CreateCircuitBreakerOptions(enabled: false));
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        for (int i = 0; i < 5; i++)
        {
            HttpResponseMessage response = await client.GetAsync("/resource");
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        handler.CircuitState.Should().Be(CircuitState.Closed);
        inner.RequestCount.Should().Be(5);
    }

    [Fact]
    public async Task SendAsync_OnSuccess_ShouldKeepCircuitClosed()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        CircuitBreakerDelegatingHandler handler = CreateHandler();
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.CircuitState.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task SendAsync_AfterFailures_ShouldOpenCircuitAndBlockRequests()
    {
        CircuitBreakerStateChangeInfo? breakInfo = null;
        CircuitBreakerPolicyOptions options = DelegatingHandlerTestHelpers.CreateCircuitBreakerOptions(
            failureRatio: 1.0,
            minimumThroughput: 2,
            breakDuration: TimeSpan.FromSeconds(5));
        options.SetCallbacks(onBreak: info => breakInfo = info);

        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.InternalServerError);
        CircuitBreakerDelegatingHandler handler = CreateHandler(options, "OrdersApi");
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        await client.GetAsync("/resource");
        await client.GetAsync("/resource");

        handler.CircuitState.Should().Be(CircuitState.Open);
        breakInfo.Should().NotBeNull();
        breakInfo!.ServiceName.Should().Be("OrdersApi");
        breakInfo.NewState.Should().Be(CircuitState.Open);

        Func<Task> act = () => client.GetAsync("/resource");
        await act.Should().ThrowAsync<BrokenCircuitException>();
        inner.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_OnHttpRequestException_ShouldCountAsFailure()
    {
        CircuitBreakerPolicyOptions options = DelegatingHandlerTestHelpers.CreateCircuitBreakerOptions(
            failureRatio: 1.0,
            minimumThroughput: 2);
        TestHttpMessageHandler inner = new TestHttpMessageHandler().SimulateNetworkFailure();
        CircuitBreakerDelegatingHandler handler = CreateHandler(options);
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        await client.Invoking(c => c.GetAsync("/resource")).Should().ThrowAsync<HttpRequestException>();
        await client.Invoking(c => c.GetAsync("/resource")).Should().ThrowAsync<HttpRequestException>();

        handler.CircuitState.Should().Be(CircuitState.Open);
    }

    [Fact]
    public void Isolate_ShouldOpenCircuitImmediately()
    {
        CircuitBreakerDelegatingHandler handler = CreateHandler();
        handler.Isolate();
        handler.CircuitState.Should().Be(CircuitState.Isolated);
    }

    [Fact]
    public void Reset_ShouldCloseCircuit()
    {
        CircuitBreakerDelegatingHandler handler = CreateHandler();
        handler.Isolate();
        handler.Reset();
        handler.CircuitState.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task SendAsync_AfterBreakDuration_ShouldAllowHalfOpenProbe()
    {
        CircuitBreakerStateChangeInfo? halfOpenInfo = null;
        CircuitBreakerStateChangeInfo? resetInfo = null;
        CircuitBreakerPolicyOptions options = DelegatingHandlerTestHelpers.CreateCircuitBreakerOptions(
            failureRatio: 1.0,
            minimumThroughput: 2,
            breakDuration: TimeSpan.FromMilliseconds(100));
        options.SetCallbacks(
            onHalfOpen: info => halfOpenInfo = info,
            onReset: info => resetInfo = info);

        int attempts = 0;
        var inner = new TestHttpMessageHandler();
        inner.When(_ => true, _ =>
        {
            attempts++;
            HttpStatusCode status = attempts <= 2
                ? HttpStatusCode.InternalServerError
                : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status));
        });

        CircuitBreakerDelegatingHandler handler = CreateHandler(options, "CatalogApi");
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        await client.GetAsync("/resource");
        await client.GetAsync("/resource");
        handler.CircuitState.Should().Be(CircuitState.Open);

        await Task.Delay(150);

        HttpResponseMessage response = await client.GetAsync("/resource");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        halfOpenInfo.Should().NotBeNull();
        halfOpenInfo!.NewState.Should().Be(CircuitState.HalfOpen);
        resetInfo.Should().NotBeNull();
        resetInfo!.NewState.Should().Be(CircuitState.Closed);
        handler.CircuitState.Should().Be(CircuitState.Closed);
    }

    private static CircuitBreakerDelegatingHandler CreateHandler(
        CircuitBreakerPolicyOptions? options = null,
        string serviceName = "HttpClient")
    {
        return new CircuitBreakerDelegatingHandler(
            NullLogger<CircuitBreakerDelegatingHandler>.Instance,
            options ?? DelegatingHandlerTestHelpers.CreateCircuitBreakerOptions(),
            serviceName);
    }
}

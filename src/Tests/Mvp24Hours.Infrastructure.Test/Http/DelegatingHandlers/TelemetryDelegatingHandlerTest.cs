//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;
using System.Net;
using Mvp24Hours.Infrastructure.Http.DelegatingHandlers;
using Mvp24Hours.Infrastructure.Test.Support;
using Mvp24Hours.Infrastructure.Testing.Http;
using Mvp24Hours.Infrastructure.Testing.Observability;

namespace Mvp24Hours.Infrastructure.Test.Http.DelegatingHandlers;

[Trait("Category", "Unit")]
public class TelemetryDelegatingHandlerTest
{
    [Fact]
    public async Task SendAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        var handler = new TelemetryDelegatingHandler();
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler);

        Func<Task> act = () => client.SendAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_WithoutListener_ShouldPassThrough()
    {
        var inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK, "ok");
        var handler = new TelemetryDelegatingHandler();
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_WithListener_ShouldRecordActivityWithRequestTags()
    {
        using var listener = new FakeActivityListener("Mvp24Hours.Infrastructure.Http");
        var inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK, "ok");
        var handler = new TelemetryDelegatingHandler(
            DelegatingHandlerTestHelpers.Logger<TelemetryDelegatingHandler>(),
            new TelemetryHandlerOptions
            {
                RecordFullUrl = true,
                RecordUserAgent = true,
                RecordEvents = true,
                CustomTags = new Dictionary<string, object> { ["custom.tag"] = "value" }
            });

        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com:8443/items?q=1");
        request.Headers.UserAgent.ParseAdd("Mvp24HoursTests/1.0");
        request.Content = new StringContent("payload");

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        listener.HasActivity("HTTP GET").Should().BeTrue();

        RecordedActivity activity = listener.GetActivities("HTTP GET").Single();
        activity.Kind.Should().Be(ActivityKind.Client);
        activity.Status.Should().Be(ActivityStatusCode.Ok);
        activity.GetTag("http.request.method").Should().Be("GET");
        activity.GetTag("url.full").Should().Contain("items?q=1");
        activity.GetTag("url.scheme").Should().Be("https");
        activity.GetTag("server.address").Should().Be("api.example.com");
        activity.GetTag("custom.tag").Should().Be("value");
        activity.GetTag("user_agent.original").Should().Contain("Mvp24HoursTests");
        activity.HasEvent("HTTP request started").Should().BeTrue();
        activity.HasEvent("HTTP response received").Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_WhenRecordFullUrlDisabled_ShouldRecordPathOnly()
    {
        using var listener = new FakeActivityListener("Mvp24Hours.Infrastructure.Http");
        var handler = new TelemetryDelegatingHandler(
            null,
            new TelemetryHandlerOptions { RecordFullUrl = false, RecordEvents = false });

        using var client = DelegatingHandlerTestHelpers.CreateClient(handler);
        await client.GetAsync("https://api.example.com/items?secret=1");

        RecordedActivity activity = listener.GetActivities("HTTP GET").Single();
        activity.GetTag("url.full").Should().Be("https://api.example.com/items");
        activity.GetTag("url.full").Should().NotContain("secret");
        activity.HasEvent("HTTP request started").Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_OnErrorStatus_ShouldSetErrorStatus()
    {
        using var listener = new FakeActivityListener("Mvp24Hours.Infrastructure.Http");
        var inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.BadGateway);
        var handler = new TelemetryDelegatingHandler();
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        RecordedActivity activity = listener.GetActivities("HTTP GET").Single();
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Contain("502");
    }

    [Fact]
    public async Task SendAsync_OnException_ShouldRecordErrorAndRethrow()
    {
        using var listener = new FakeActivityListener("Mvp24Hours.Infrastructure.Http");
        var inner = new TestHttpMessageHandler().SimulateNetworkFailure();
        var handler = new TelemetryDelegatingHandler(
            DelegatingHandlerTestHelpers.Logger<TelemetryDelegatingHandler>());
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        Func<Task> act = () => client.GetAsync("/resource");

        await act.Should().ThrowAsync<HttpRequestException>();
        RecordedActivity activity = listener.GetActivities("HTTP GET").Single();
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.HasEvent("exception").Should().BeTrue();
        activity.GetTag("error.type").Should().Contain("HttpRequestException");
    }

    [Fact]
    public void Constructors_ShouldAcceptNullLoggerAndOptions()
    {
        var handler1 = new TelemetryDelegatingHandler();
        var handler2 = new TelemetryDelegatingHandler(null);
        var handler3 = new TelemetryDelegatingHandler(null, null!);

        handler1.Should().NotBeNull();
        handler2.Should().NotBeNull();
        handler3.Should().NotBeNull();
    }
}

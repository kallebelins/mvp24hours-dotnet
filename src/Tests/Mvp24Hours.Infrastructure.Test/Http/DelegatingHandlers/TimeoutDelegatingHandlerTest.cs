//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Http.DelegatingHandlers;
using Mvp24Hours.Infrastructure.Test.Support;
using Mvp24Hours.Infrastructure.Testing.Http;

namespace Mvp24Hours.Infrastructure.Test.Http.DelegatingHandlers;

[Trait("Category", "Unit")]
public class TimeoutDelegatingHandlerTest
{
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new TimeoutDelegatingHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        TimeoutDelegatingHandler handler = CreateHandler(TimeSpan.FromSeconds(1));
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler);

        Func<Task> act = () => client.SendAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_WhenDisabled_ShouldPassThroughWithoutTimeout()
    {
        HttpMessageHandler inner = DelegatingHandlerTestHelpers.CreateDelayedHandler(TimeSpan.FromMilliseconds(50));
        var handler = new TimeoutDelegatingHandler(
            NullLogger<TimeoutDelegatingHandler>.Instance,
            TimeSpan.FromMilliseconds(10),
            enabled: false);
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendAsync_WhenRequestCompletesInTime_ShouldReturnResponse()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        TimeoutDelegatingHandler handler = CreateHandler(TimeSpan.FromSeconds(5));
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendAsync_WhenDefaultTimeoutExceeded_ShouldThrowHttpRequestTimeoutException()
    {
        HttpMessageHandler inner = DelegatingHandlerTestHelpers.CreateDelayedHandler(TimeSpan.FromSeconds(5));
        TimeoutDelegatingHandler handler = CreateHandler(TimeSpan.FromMilliseconds(30));
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        Func<Task> act = () => client.GetAsync("/resource");

        HttpRequestTimeoutException ex = (await act.Should().ThrowAsync<HttpRequestTimeoutException>()).Which;
        ex.Method.Should().Be(HttpMethod.Get);
        ex.RequestUri.Should().NotBeNull();
        ex.Timeout.Should().Be(TimeSpan.FromMilliseconds(30));
    }

    [Fact]
    public async Task SendAsync_WithPerRequestTimeout_ShouldOverrideDefault()
    {
        HttpMessageHandler inner = DelegatingHandlerTestHelpers.CreateDelayedHandler(TimeSpan.FromMilliseconds(80));
        TimeoutDelegatingHandler handler = CreateHandler(TimeSpan.FromMilliseconds(20));
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");
        request.SetTimeout(TimeSpan.FromSeconds(2));

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendAsync_WithNoTimeout_ShouldPassThrough()
    {
        HttpMessageHandler inner = DelegatingHandlerTestHelpers.CreateDelayedHandler(TimeSpan.FromMilliseconds(40));
        TimeoutDelegatingHandler handler = CreateHandler(TimeSpan.FromMilliseconds(10));
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");
        request.NoTimeout();

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendAsync_WithClearTimeout_ShouldUseInfiniteTimeout()
    {
        HttpMessageHandler inner = DelegatingHandlerTestHelpers.CreateDelayedHandler(TimeSpan.FromMilliseconds(40));
        TimeoutDelegatingHandler handler = CreateHandler(TimeSpan.FromMilliseconds(10));
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/resource");
        request.ClearTimeout();

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void SetTimeout_WithNullRequest_ShouldThrowArgumentNullException()
    {
        Action act = () => HttpRequestMessageTimeoutExtensions.SetTimeout(null!, TimeSpan.FromSeconds(1));
        act.Should().Throw<ArgumentNullException>().WithParameterName("request");
    }

    [Fact]
    public void ClearTimeout_WithNullRequest_ShouldThrowArgumentNullException()
    {
        Action act = () => HttpRequestMessageTimeoutExtensions.ClearTimeout(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("request");
    }

    private static TimeoutDelegatingHandler CreateHandler(TimeSpan defaultTimeout)
    {
        return new TimeoutDelegatingHandler(
            NullLogger<TimeoutDelegatingHandler>.Instance,
            defaultTimeout);
    }
}

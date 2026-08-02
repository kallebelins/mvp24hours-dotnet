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
public class LoggingDelegatingHandlerTest
{
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new LoggingDelegatingHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldUseDefaults()
    {
        var handler = new LoggingDelegatingHandler(
            NullLogger<LoggingDelegatingHandler>.Instance,
            null!);
        handler.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        LoggingDelegatingHandler handler = CreateHandler();
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler);

        Func<Task> act = () => client.SendAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_OnSuccess_ShouldReturnResponse()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK, """{"ok":true}""");
        LoggingDelegatingHandler handler = CreateHandler(new HttpLoggingOptions
        {
            LogRequestHeaders = true,
            LogRequestBody = true,
            LogResponseHeaders = true,
            LogResponseBody = true,
            MaxBodyLogSize = 10
        });
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/resource")
        {
            Content = new StringContent("""{"name":"very-long-payload-value"}""")
        };
        request.Headers.TryAddWithoutValidation("Authorization", "secret-token");
        request.Headers.TryAddWithoutValidation("X-Trace", "visible");

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_OnServerError_ShouldStillReturnResponse()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.InternalServerError, "boom");
        LoggingDelegatingHandler handler = CreateHandler();
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SendAsync_OnConnectionRefused_ShouldReturnBadGateway()
    {
        HttpMessageHandler inner = DelegatingHandlerTestHelpers.CreateConnectionRefusedHandler();
        LoggingDelegatingHandler handler = CreateHandler();
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("https://localhost:59999/api");

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        response.ReasonPhrase.Should().Contain("Connection refused");
    }

    [Fact]
    public async Task SendAsync_OnGenericException_ShouldRethrow()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().ThrowException(new InvalidOperationException("boom"));
        LoggingDelegatingHandler handler = CreateHandler();
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        Func<Task> act = () => client.GetAsync("/resource");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }

    [Fact]
    public async Task SendAsync_OnTimeoutCancellation_ShouldRethrow()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().SimulateTimeout(TimeSpan.FromMilliseconds(5));
        LoggingDelegatingHandler handler = CreateHandler();
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        Func<Task> act = () => client.GetAsync("/resource");

        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    private static LoggingDelegatingHandler CreateHandler(HttpLoggingOptions? options = null)
    {
        return options is null
            ? new LoggingDelegatingHandler(NullLogger<LoggingDelegatingHandler>.Instance)
            : new LoggingDelegatingHandler(NullLogger<LoggingDelegatingHandler>.Instance, options);
    }
}

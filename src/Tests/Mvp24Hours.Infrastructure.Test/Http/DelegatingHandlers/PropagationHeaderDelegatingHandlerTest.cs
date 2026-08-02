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
public class PropagationHeaderDelegatingHandlerTest
{
    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new PropagationHeaderDelegatingHandler(
            null!,
            NullLogger<PropagationHeaderDelegatingHandler>.Instance,
            "X-Tenant-Id");

        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new PropagationHeaderDelegatingHandler(
            DelegatingHandlerTestHelpers.CreateEmptyServiceProvider(),
            null!,
            "X-Tenant-Id");

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullKeys_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new PropagationHeaderDelegatingHandler(
            DelegatingHandlerTestHelpers.CreateEmptyServiceProvider(),
            NullLogger<PropagationHeaderDelegatingHandler>.Instance,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("keys");
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        PropagationHeaderDelegatingHandler handler = CreateHandler(["X-Tenant-Id"]);
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler);

        Func<Task> act = () => client.SendAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_WithConfiguredKeys_ShouldPropagateMatchingHeaders()
    {
        IServiceProvider sp = DelegatingHandlerTestHelpers.CreateServiceProviderWithHeaders(
            ("X-Tenant-Id", "tenant-1"),
            ("X-Custom-Trace", "trace-9"),
            ("X-Ignored", "nope"));
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        PropagationHeaderDelegatingHandler handler = CreateHandler(
            ["X-Tenant-Id", "X-Custom-Trace", "", "  "],
            sp);
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        await client.GetAsync("/resource");

        RecordedRequest recorded = inner.ReceivedRequests.Single();
        recorded.Headers.Should().ContainKey("X-Tenant-Id")
            .WhoseValue.Should().Be("tenant-1");
        recorded.Headers.Should().ContainKey("X-Custom-Trace")
            .WhoseValue.Should().Be("trace-9");
        recorded.Headers.Should().NotContainKey("X-Ignored");
    }

    [Fact]
    public async Task SendAsync_WhenPropagationThrows_ShouldContinueRequest()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        PropagationHeaderDelegatingHandler handler = CreateHandler(
            ["X-Tenant-Id"],
            DelegatingHandlerTestHelpers.CreateThrowingServiceProvider());
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static PropagationHeaderDelegatingHandler CreateHandler(
        string[] keys,
        IServiceProvider? sp = null)
    {
        return new PropagationHeaderDelegatingHandler(
            sp ?? DelegatingHandlerTestHelpers.CreateEmptyServiceProvider(),
            NullLogger<PropagationHeaderDelegatingHandler>.Instance,
            keys);
    }
}

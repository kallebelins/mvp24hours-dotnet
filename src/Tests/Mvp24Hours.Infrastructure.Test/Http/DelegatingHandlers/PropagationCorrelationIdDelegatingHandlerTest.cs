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
public class PropagationCorrelationIdDelegatingHandlerTest
{
    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new PropagationCorrelationIdDelegatingHandler(
            null!,
            NullLogger<PropagationCorrelationIdDelegatingHandler>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new PropagationCorrelationIdDelegatingHandler(
            DelegatingHandlerTestHelpers.CreateEmptyServiceProvider(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        PropagationCorrelationIdDelegatingHandler handler = CreateHandler();
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler);

        Func<Task> act = () => client.SendAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_WithCorrelationIdInContext_ShouldPropagateHeader()
    {
        IServiceProvider sp = DelegatingHandlerTestHelpers.CreateServiceProviderWithHeaders(
            ("X-Correlation-Id", "corr-123"));
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        PropagationCorrelationIdDelegatingHandler handler = CreateHandler(sp);
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        await client.GetAsync("/resource");

        inner.ReceivedRequests.Should().ContainSingle();
        inner.ReceivedRequests[0].Headers.Should().ContainKey("X-Correlation-Id");
        inner.ReceivedRequests[0].Headers["X-Correlation-Id"].Should().Be("corr-123");
    }

    [Fact]
    public async Task SendAsync_WithoutHttpContext_ShouldStillSendRequest()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        PropagationCorrelationIdDelegatingHandler handler = CreateHandler(DelegatingHandlerTestHelpers.CreateEmptyServiceProvider());
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.RequestCount.Should().Be(1);
        inner.ReceivedRequests[0].Headers.Should().NotContainKey("X-Correlation-Id");
    }

    [Fact]
    public async Task SendAsync_WhenPropagationThrows_ShouldContinueRequest()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.Accepted);
        PropagationCorrelationIdDelegatingHandler handler = CreateHandler(DelegatingHandlerTestHelpers.CreateThrowingServiceProvider());
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        inner.RequestCount.Should().Be(1);
    }

    private static PropagationCorrelationIdDelegatingHandler CreateHandler(IServiceProvider? sp = null)
    {
        return new PropagationCorrelationIdDelegatingHandler(
            sp ?? DelegatingHandlerTestHelpers.CreateEmptyServiceProvider(),
            NullLogger<PropagationCorrelationIdDelegatingHandler>.Instance);
    }
}

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
public class PropagationAuthorizationDelegatingHandlerTest
{
    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new PropagationAuthorizationDelegatingHandler(
            null!,
            NullLogger<PropagationAuthorizationDelegatingHandler>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new PropagationAuthorizationDelegatingHandler(
            DelegatingHandlerTestHelpers.CreateEmptyServiceProvider(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
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
    public async Task SendAsync_WithAuthorizationInContext_ShouldPropagateHeader()
    {
        IServiceProvider sp = DelegatingHandlerTestHelpers.CreateServiceProviderWithHeaders(
            ("Authorization", "Bearer token-abc"));
        var inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        var handler = CreateHandler(sp);
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        await client.GetAsync("/resource");

        inner.ReceivedRequests[0].Headers.Should().ContainKey("Authorization");
        inner.ReceivedRequests[0].Headers["Authorization"].Should().Be("Bearer token-abc");
    }

    [Fact]
    public async Task SendAsync_WithoutHttpContext_ShouldStillSendRequest()
    {
        var inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        var handler = CreateHandler();
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.ReceivedRequests[0].Headers.Should().NotContainKey("Authorization");
    }

    [Fact]
    public async Task SendAsync_WhenPropagationThrows_ShouldContinueRequest()
    {
        var inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.NoContent);
        var handler = CreateHandler(DelegatingHandlerTestHelpers.CreateThrowingServiceProvider());
        using var client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static PropagationAuthorizationDelegatingHandler CreateHandler(IServiceProvider? sp = null)
    {
        return new PropagationAuthorizationDelegatingHandler(
            sp ?? DelegatingHandlerTestHelpers.CreateEmptyServiceProvider(),
            NullLogger<PropagationAuthorizationDelegatingHandler>.Instance);
    }
}

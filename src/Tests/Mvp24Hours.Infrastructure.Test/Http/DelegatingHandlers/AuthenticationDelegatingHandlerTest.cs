//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Http.DelegatingHandlers;
using Mvp24Hours.Infrastructure.Test.Support;
using Mvp24Hours.Infrastructure.Testing.Http;

namespace Mvp24Hours.Infrastructure.Test.Http.DelegatingHandlers;

[Trait("Category", "Unit")]
public class AuthenticationDelegatingHandlerTest
{
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new AuthenticationDelegatingHandler(
            null!,
            new AuthenticationOptions());

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new AuthenticationDelegatingHandler(
            NullLogger<AuthenticationDelegatingHandler>.Instance,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        AuthenticationDelegatingHandler handler = CreateHandler(new AuthenticationOptions());
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler);

        Func<Task> act = () => client.SendAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_WithBearerTokenProvider_ShouldSetAuthorizationHeader()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        var handler = new AuthenticationDelegatingHandler(
            NullLogger<AuthenticationDelegatingHandler>.Instance,
            AuthenticationScheme.Bearer,
            tokenProvider: () => Task.FromResult<string?>("my-jwt-token"));
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        await client.GetAsync("/resource");

        inner.ReceivedRequests[0].Headers["Authorization"].Should().Be("Bearer my-jwt-token");
    }

    [Fact]
    public async Task SendAsync_WithStaticBearerToken_ShouldStripExistingPrefix()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        AuthenticationDelegatingHandler handler = CreateHandler(new AuthenticationOptions
        {
            Scheme = AuthenticationScheme.Bearer,
            StaticToken = "Bearer already-prefixed"
        });
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        await client.GetAsync("/resource");

        inner.ReceivedRequests[0].Headers["Authorization"].Should().Be("Bearer already-prefixed");
    }

    [Fact]
    public async Task SendAsync_WithApiKeyInHeader_ShouldAddConfiguredHeader()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        var handler = new AuthenticationDelegatingHandler(
            NullLogger<AuthenticationDelegatingHandler>.Instance,
            AuthenticationScheme.ApiKey,
            apiKey: "secret-key",
            apiKeyHeaderName: "X-Custom-Key");
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        await client.GetAsync("/resource");

        inner.ReceivedRequests[0].Headers["X-Custom-Key"].Should().Be("secret-key");
    }

    [Fact]
    public async Task SendAsync_WithApiKeyInQueryString_ShouldAppendQueryParameter()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        AuthenticationDelegatingHandler handler = CreateHandler(new AuthenticationOptions
        {
            Scheme = AuthenticationScheme.ApiKey,
            ApiKey = "query-key",
            ApiKeyLocation = ApiKeyLocation.QueryString,
            ApiKeyQueryParamName = "key"
        });
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        await client.GetAsync("/resource?foo=1");

        inner.ReceivedRequests[0].RequestUri.Should().Contain("key=query-key");
        inner.ReceivedRequests[0].RequestUri.Should().Contain("foo=1");
    }

    [Fact]
    public async Task SendAsync_WithBasicAuth_ShouldSetEncodedCredentials()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        var handler = new AuthenticationDelegatingHandler(
            NullLogger<AuthenticationDelegatingHandler>.Instance,
            AuthenticationScheme.Basic,
            username: "user",
            password: "pass");
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        await client.GetAsync("/resource");

        string expected = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass"));
        inner.ReceivedRequests[0].Headers["Authorization"].Should().Be($"Basic {expected}");
    }

    [Fact]
    public async Task SendAsync_WithNoneScheme_ShouldNotAddAuthHeader()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        AuthenticationDelegatingHandler handler = CreateHandler(new AuthenticationOptions { Scheme = AuthenticationScheme.None });
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        await client.GetAsync("/resource");

        inner.ReceivedRequests[0].Headers.Should().NotContainKey("Authorization");
    }

    [Fact]
    public async Task SendAsync_WhenTokenProviderFailsAndThrowDisabled_ShouldContinue()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        AuthenticationDelegatingHandler handler = CreateHandler(new AuthenticationOptions
        {
            Scheme = AuthenticationScheme.Bearer,
            ThrowOnAuthenticationFailure = false,
            TokenProvider = () => throw new InvalidOperationException("token store down")
        });
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        HttpResponseMessage response = await client.GetAsync("/resource");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.ReceivedRequests[0].Headers.Should().NotContainKey("Authorization");
    }

    [Fact]
    public async Task SendAsync_WhenTokenProviderFailsAndThrowEnabled_ShouldRethrow()
    {
        AuthenticationDelegatingHandler handler = CreateHandler(new AuthenticationOptions
        {
            Scheme = AuthenticationScheme.Bearer,
            ThrowOnAuthenticationFailure = true,
            TokenProvider = () => throw new InvalidOperationException("token store down")
        });
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler);

        Func<Task> act = () => client.GetAsync("/resource");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("token store down");
    }

    [Fact]
    public async Task SendAsync_WithEmptyBearerToken_ShouldNotSetAuthorization()
    {
        TestHttpMessageHandler inner = new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        AuthenticationDelegatingHandler handler = CreateHandler(new AuthenticationOptions
        {
            Scheme = AuthenticationScheme.Bearer,
            TokenProvider = () => Task.FromResult<string?>("  ")
        });
        using HttpClient client = DelegatingHandlerTestHelpers.CreateClient(handler, inner);

        await client.GetAsync("/resource");

        inner.ReceivedRequests[0].Headers.Should().NotContainKey("Authorization");
    }

    private static AuthenticationDelegatingHandler CreateHandler(AuthenticationOptions options)
    {
        return new AuthenticationDelegatingHandler(
            NullLogger<AuthenticationDelegatingHandler>.Instance,
            options);
    }
}

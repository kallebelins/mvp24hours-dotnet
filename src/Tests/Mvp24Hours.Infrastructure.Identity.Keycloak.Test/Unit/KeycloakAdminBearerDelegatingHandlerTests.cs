using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class KeycloakAdminBearerDelegatingHandlerTests
{
    [Fact]
    public async Task SendAsync_ShouldAttachCachedBearerToken()
    {
        IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("Keycloak:AdminToken:test:admin-client", "cached-token", TimeSpan.FromMinutes(5));
        using HttpClient client = CreateClient(cache, tokenResponse: null);
        using HttpRequestMessage request = new(HttpMethod.Get, "https://identity.example/users");

        await client.SendAsync(request);

        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be("cached-token");
    }

    [Fact]
    public async Task SendAsync_ShouldRequestAndCacheTokenWhenMissing()
    {
        using HttpResponseMessage tokenResponse = new(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"access_token":"fresh-token","expires_in":300}""")
        };
        IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        using HttpClient client = CreateClient(cache, tokenResponse);
        using HttpRequestMessage request = new(HttpMethod.Get, "https://identity.example/users");

        await client.SendAsync(request);

        request.Headers.Authorization!.Parameter.Should().Be("fresh-token");
        cache.TryGetValue("Keycloak:AdminToken:test:admin-client", out string? cached).Should().BeTrue();
        cached.Should().Be("fresh-token");
    }

    private static HttpClient CreateClient(
        IMemoryCache cache,
        HttpResponseMessage? tokenResponse)
    {
        Mock<HttpMessageHandler> downstream = new();
        downstream.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        Mock<HttpMessageHandler> tokenHandler = new();
        if (tokenResponse is not null)
        {
            tokenHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(tokenResponse);
        }

        HttpClient tokenHttpClient = new(tokenHandler.Object)
        {
            BaseAddress = new Uri("https://identity.example/")
        };
        Mock<IKeycloakDiscoveryService> discovery = new();
        discovery.Setup(service => service.GetTokenEndpointAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://identity.example/token");
        Mock<IHttpClientFactory> factory = new();
        factory.Setup(value => value.CreateClient("KeycloakToken")).Returns(tokenHttpClient);
        KeycloakTokenClient tokenClient = new(factory.Object, discovery.Object);

        KeycloakAdminBearerDelegatingHandler handler = new(
            tokenClient,
            cache,
            Options.Create(new KeycloakAdminOptions
            {
                AdminBaseUrl = "https://identity.example/admin/realms/test",
                Realm = "test",
                ClientId = "admin-client",
                ClientSecret = "secret"
            }))
        {
            InnerHandler = downstream.Object
        };

        return new HttpClient(handler);
    }
}

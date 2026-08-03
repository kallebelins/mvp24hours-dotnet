using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class KeycloakTokenServiceTests
{
    [Fact]
    public async Task GetClientCredentialsTokenAsync_ShouldReturnCachedToken()
    {
        AccessTokenResponse cached = new()
        {
            AccessToken = "cached-token",
            ExpiresIn = 300
        };
        IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("Keycloak:ClientCredentials:https://identity.example/realms/test:api", cached);
        KeycloakTokenService service = CreateService(cache, tokenResponse: null);

        IBusinessResult<AccessTokenResponse> result = await service.GetClientCredentialsTokenAsync();

        result.HasErrors.Should().BeFalse();
        result.Data!.AccessToken.Should().Be("cached-token");
    }

    [Fact]
    public async Task GetClientCredentialsTokenAsync_ShouldRequestAndCacheToken()
    {
        using HttpResponseMessage response = new(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("""{"access_token":"fresh-token","expires_in":300}""")
        };
        IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        KeycloakTokenService service = CreateService(cache, response);

        IBusinessResult<AccessTokenResponse> result = await service.GetClientCredentialsTokenAsync();

        result.HasErrors.Should().BeFalse();
        result.Data!.AccessToken.Should().Be("fresh-token");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithMissingToken_ShouldFailValidation()
    {
        KeycloakTokenService service = CreateService(new MemoryCache(new MemoryCacheOptions()), null);

        IBusinessResult<AccessTokenResponse> result = await service.RefreshTokenAsync(string.Empty);

        result.HasErrors.Should().BeTrue();
        result.Messages!.Should().Contain(message => message.Message.Contains("Refresh token"));
    }

    [Fact]
    public async Task IntrospectTokenAsync_WithMissingToken_ShouldFailValidation()
    {
        KeycloakTokenService service = CreateService(new MemoryCache(new MemoryCacheOptions()), null);

        IBusinessResult<TokenIntrospectionResponse> result = await service.IntrospectTokenAsync(string.Empty);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task IntrospectTokenAsync_ShouldReturnActiveFlag()
    {
        using HttpResponseMessage response = new(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("""{"active":true,"sub":"user"}""")
        };
        KeycloakTokenService service = CreateService(new MemoryCache(new MemoryCacheOptions()), response);

        IBusinessResult<TokenIntrospectionResponse> result = await service.IntrospectTokenAsync("token");

        result.HasErrors.Should().BeFalse();
        result.Data!.Active.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeTokenAsync_WithMissingToken_ShouldFailValidation()
    {
        KeycloakTokenService service = CreateService(new MemoryCache(new MemoryCacheOptions()), null);

        IBusinessResult<bool> result = await service.RevokeTokenAsync(string.Empty);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeTokenAsync_ShouldReturnSuccess()
    {
        using HttpResponseMessage response = new(System.Net.HttpStatusCode.OK);
        KeycloakTokenService service = CreateService(new MemoryCache(new MemoryCacheOptions()), response);

        IBusinessResult<bool> result = await service.RevokeTokenAsync("token");

        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task GetPasswordTokenAsync_WithMissingCredentials_ShouldFailValidation()
    {
        KeycloakTokenService service = CreateService(new MemoryCache(new MemoryCacheOptions()), null);

        IBusinessResult<AccessTokenResponse> result = await service.GetPasswordTokenAsync(string.Empty, string.Empty);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task GetPasswordTokenAsync_ShouldReturnAccessToken()
    {
        using HttpResponseMessage response = new(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("""{"access_token":"password-token","expires_in":300}""")
        };
        KeycloakTokenService service = CreateService(new MemoryCache(new MemoryCacheOptions()), response);

        IBusinessResult<AccessTokenResponse> result = await service.GetPasswordTokenAsync("alice", "secret", "openid");

        result.HasErrors.Should().BeFalse();
        result.Data!.AccessToken.Should().Be("password-token");
    }

    private static KeycloakTokenService CreateService(
        IMemoryCache cache,
        HttpResponseMessage? tokenResponse)
    {
        Mock<IKeycloakDiscoveryService> discovery = new();
        discovery.Setup(service => service.GetTokenEndpointAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://identity.example/token");
        discovery.Setup(service => service.GetIntrospectionEndpointAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://identity.example/introspect");
        discovery.Setup(service => service.GetRevocationEndpointAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://identity.example/revoke");

        Mock<HttpMessageHandler> handler = new();
        if (tokenResponse is not null)
        {
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(tokenResponse);
        }

        HttpClient httpClient = new(handler.Object);
        Mock<IHttpClientFactory> factory = new();
        factory.Setup(value => value.CreateClient("KeycloakToken")).Returns(httpClient);
        KeycloakTokenClient client = new(factory.Object, discovery.Object);

        return new KeycloakTokenService(
            client,
            cache,
            Options.Create(new KeycloakOptions
            {
                Authority = "https://identity.example/realms/test",
                Realm = "test",
                ClientId = "api",
                Audience = "api",
                ClientSecret = "secret"
            }));
    }
}

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class KeycloakHttpClientsTests
{
    [Fact]
    public async Task DiscoveryService_ShouldReadAndCacheDocument()
    {
        using var server = WireMockServer.Start();
        server.Given(Request.Create()
                .WithPath("/realms/test/.well-known/openid-configuration")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""
                    {
                      "issuer": "{{server.Url}}/realms/test",
                      "token_endpoint": "{{server.Url}}/token",
                      "introspection_endpoint": "{{server.Url}}/introspect",
                      "revocation_endpoint": "{{server.Url}}/revoke",
                      "jwks_uri": "{{server.Url}}/certs"
                    }
                    """));
        KeycloakDiscoveryService service = CreateDiscoveryService(
            $"{server.Url}/realms/test",
            CreateHttpClient());

        string first = await service.GetTokenEndpointAsync();
        string second = await service.GetTokenEndpointAsync();

        first.Should().Be($"{server.Url}/token");
        second.Should().Be(first);
        server.LogEntries.Should().ContainSingle();
        (await service.GetIntrospectionEndpointAsync()).Should().EndWith("/introspect");
        (await service.GetRevocationEndpointAsync()).Should().EndWith("/revoke");
        (await service.GetJwksUriAsync()).Should().EndWith("/certs");
        server.LogEntries.Should().ContainSingle("the discovery document is cached");
    }

    [Fact]
    public async Task DiscoveryService_ShouldRejectHttpWhenHttpsIsRequired()
    {
        KeycloakDiscoveryService service = CreateDiscoveryService(
            "http://identity.example/realms/test",
            CreateHttpClient(),
            requireHttps: true);

        Func<Task> act = () => service.GetConfigurationAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*must use HTTPS*");
    }

    [Fact]
    public async Task TokenClient_ShouldPostFormToDiscoveredEndpoint()
    {
        using var server = WireMockServer.Start();
        server.Given(Request.Create()
                .WithPath("/token")
                .UsingPost()
                .WithBody(body => body is not null
                    && body.Contains("grant_type=client_credentials")
                    && body.Contains("client_id=test-client")))
            .RespondWith(Response.Create()
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"access_token\":\"unit-token\",\"expires_in\":300}"));
        Mock<IKeycloakDiscoveryService> discovery = new();
        discovery.Setup(service => service.GetTokenEndpointAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync($"{server.Url}/token");
        KeycloakTokenClient client = new(
            CreateHttpClientFactory(CreateHttpClient()),
            discovery.Object);

        using HttpResponseMessage response = await client.RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "test-client"
            });

        response.IsSuccessStatusCode.Should().BeTrue();
        (await response.Content.ReadAsStringAsync()).Should().Contain("unit-token");
        server.LogEntries.Should().ContainSingle();
    }

    private static KeycloakDiscoveryService CreateDiscoveryService(
        string authority,
        HttpClient client,
        bool requireHttps = false)
    {
        return new KeycloakDiscoveryService(
            CreateHttpClientFactory(client),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new KeycloakOptions
            {
                Authority = authority,
                Realm = "test",
                ClientId = "test-client",
                Audience = "test-client",
                RequireHttpsMetadata = requireHttps,
                DiscoveryCacheTtl = TimeSpan.FromMinutes(1)
            }));
    }

    private static IHttpClientFactory CreateHttpClientFactory(HttpClient client)
    {
        Mock<IHttpClientFactory> factory = new();
        factory.Setup(value => value.CreateClient(It.IsAny<string>()))
            .Returns(client);
        return factory.Object;
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler? handler = null)
        => new(handler ?? new SocketsHttpHandler());
}

using System.Net;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class TokenClientUnitTests
{
    [Fact]
    public async Task GetClientCredentialsToken_WithMissingAddress_ShouldThrow()
    {
        TokenClient client = CreateClient(new ClientCredentialsTokenRequest
        {
            ClientId = "client"
        });

        Func<Task> act = () => client.GetClientCredentialsToken();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Address is required*");
    }

    [Fact]
    public async Task GetClientCredentialsToken_WithMissingClientId_ShouldThrow()
    {
        TokenClient client = CreateClient(new ClientCredentialsTokenRequest
        {
            Address = "https://identity/token"
        });

        Func<Task> act = () => client.GetClientCredentialsToken();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ClientId is required*");
    }

    [Fact]
    public async Task GetClientCredentialsToken_WithSuccessResponse_ShouldReturnAccessToken()
    {
        using var server = WireMockServer.Start();
        server.Given(Request.Create().WithPath("/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"access_token\":\"raw-token\",\"expires_in\":300}"));

        TokenClient client = CreateClient(new ClientCredentialsTokenRequest
        {
            Address = $"{server.Url}/token",
            ClientId = "unit-client",
            ClientSecret = "secret",
            Scope = "openid"
        });

        string? token = await client.GetClientCredentialsToken();

        token.Should().Be("raw-token");
    }

    [Fact]
    public async Task GetClientCredentialsToken_WithFailureResponse_ShouldThrow()
    {
        using var server = WireMockServer.Start();
        server.Given(Request.Create().WithPath("/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized)
                .WithBody("invalid_client"));

        TokenClient client = CreateClient(new ClientCredentialsTokenRequest
        {
            Address = $"{server.Url}/token",
            ClientId = "unit-client"
        });

        Func<Task> act = () => client.GetClientCredentialsToken();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*401*");
    }

    [Fact]
    public void SetBearerToken_WithNullClient_ShouldThrow()
    {
        Action act = () => TokenClient.SetBearerToken(null!, "token");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetBearerToken_ShouldSetAuthorizationHeader()
    {
        using HttpClient httpClient = new();

        TokenClient.SetBearerToken(httpClient, "abc123");

        httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
        httpClient.DefaultRequestHeaders.Authorization.Parameter.Should().Be("abc123");
    }

    private static TokenClient CreateClient(ClientCredentialsTokenRequest request)
    {
        return new TokenClient(new HttpClient(), request);
    }
}

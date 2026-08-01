using CustomerAPI.Test.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;

namespace CustomerAPI.Test.Integration;

/// <summary>
/// WebApplicationFactory wired to a Keycloak Testcontainer realm.
/// </summary>
public sealed class KeycloakCustomerApiFactory(KeycloakContainerFixture fixture) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = fixture.Authority,
                ["Keycloak:Realm"] = KeycloakContainerFixture.Realm,
                ["Keycloak:ClientId"] = KeycloakContainerFixture.ClientId,
                ["Keycloak:ClientSecret"] = KeycloakContainerFixture.ClientSecret,
                ["Keycloak:Audience"] = KeycloakContainerFixture.Audience,
                ["Keycloak:RequireHttpsMetadata"] = "false",
                ["Keycloak:ValidateIssuer"] = "true",
                ["Keycloak:ValidateAudience"] = "true",
                ["Keycloak:Admin:AdminBaseUrl"] =
                    $"{fixture.BaseAddress.TrimEnd('/')}/admin/realms/{KeycloakContainerFixture.Realm}",
                ["Keycloak:Admin:Realm"] = KeycloakContainerFixture.Realm,
                ["Keycloak:Admin:ClientId"] = KeycloakContainerFixture.ClientId,
                ["Keycloak:Admin:ClientSecret"] = KeycloakContainerFixture.ClientSecret
            });
        });
    }
}

[Collection(KeycloakContainerFixture.CollectionName)]
[Trait("Category", "Integration")]
public sealed class KeycloakAuthenticationIntegrationTests(KeycloakContainerFixture fixture)
{
    [DockerFact]
    public async Task GetCustomers_WithoutToken_ReturnsUnauthorized()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        await using KeycloakCustomerApiFactory factory = new(fixture);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/customers");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [DockerFact]
    public async Task GetCustomers_WithValidJwt_ReturnsOk()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        await using ServiceProvider services = fixture.CreateServiceProvider();
        IKeycloakTokenService tokenService = services.GetRequiredService<IKeycloakTokenService>();
        var tokenResult = await tokenService.GetPasswordTokenAsync(
            KeycloakContainerFixture.Username,
            KeycloakContainerFixture.Password,
            "openid");
        tokenResult.HasErrors.Should().BeFalse();

        await using KeycloakCustomerApiFactory factory = new(fixture);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new("Bearer", tokenResult.Data!.AccessToken);

        HttpResponseMessage response = await client.GetAsync("/api/customers");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}

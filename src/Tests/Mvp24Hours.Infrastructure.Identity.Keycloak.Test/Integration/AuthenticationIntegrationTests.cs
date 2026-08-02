using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Fixtures;
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Integration;

[Collection(KeycloakTestConstants.CollectionName)]
[Trait("Category", "Integration")]
public sealed class AuthenticationIntegrationTests(KeycloakFixture fixture)
{
    [Fact]
    public async Task TokenService_ShouldIssueIntrospectParseAndRevokeToken()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        await using ServiceProvider services = fixture.CreateServiceProvider();
        IKeycloakTokenService tokenService =
            services.GetRequiredService<IKeycloakTokenService>();
        IKeycloakJwtTokenParser parser =
            services.GetRequiredService<IKeycloakJwtTokenParser>();

        IBusinessResult<AccessTokenResponse> tokenResult = await tokenService.GetClientCredentialsTokenAsync();
        tokenResult.HasErrors.Should().BeFalse();
        tokenResult.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        parser.ParseClaims(tokenResult.Data.AccessToken).Should().ContainKey("iss");
        parser.IsExpired(tokenResult.Data.AccessToken).Should().BeFalse();

        IBusinessResult<TokenIntrospectionResponse> introspection = await tokenService.IntrospectTokenAsync(
            tokenResult.Data.AccessToken!);
        introspection.HasErrors.Should().BeFalse();
        introspection.Data!.Active.Should().BeTrue();

        IBusinessResult<bool> revocation = await tokenService.RevokeTokenAsync(
            tokenResult.Data.AccessToken!);
        revocation.HasErrors.Should().BeFalse();
        revocation.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ProtectedEndpoint_ShouldReturnUnauthorizedThenAcceptRealJwt()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        await using ServiceProvider services = fixture.CreateServiceProvider();
        IKeycloakTokenService tokenService =
            services.GetRequiredService<IKeycloakTokenService>();
        IBusinessResult<AccessTokenResponse> tokenResult = await tokenService.GetPasswordTokenAsync(
            KeycloakTestConstants.Username,
            KeycloakTestConstants.Password,
            "openid");
        tokenResult.HasErrors.Should().BeFalse();

        using KeycloakApiFactory factory = new(fixture.Authority);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage unauthorized = await client.GetAsync("/protected");
        unauthorized.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

        client.DefaultRequestHeaders.Authorization =
            new("Bearer", tokenResult.Data!.AccessToken);
        using HttpResponseMessage authorized = await client.GetAsync("/protected");
        authorized.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    private sealed class KeycloakApiFactory(string authority)
        : WebApplicationFactory<KeycloakApiMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(AppContext.BaseDirectory);
        }

        protected override IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webHost => webHost
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddRouting();
                    serviceCollection.AddKeycloakAuthentication(options =>
                    {
                        options.Authority = authority;
                        options.Realm = KeycloakTestConstants.Realm;
                        options.ClientId = KeycloakTestConstants.ClientId;
                        options.ClientSecret = KeycloakTestConstants.ClientSecret;
                        options.Audience = KeycloakTestConstants.Audience;
                        options.RequireHttpsMetadata = false;
                    });
                    serviceCollection.AddAuthorization();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints
                        .MapGet("/protected", () => "ok")
                        .RequireAuthorization());
                }));
        }
    }

    private sealed class KeycloakApiMarker;
}

using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;
using Testcontainers.Keycloak;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Fixtures;

public sealed class KeycloakFixture : IAsyncLifetime
{
    private KeycloakContainer? _container;

    public bool IsAvailable { get; private set; }

    public string BaseAddress { get; private set; } = string.Empty;

    public string Authority =>
        $"{BaseAddress.TrimEnd('/')}/realms/{KeycloakTestConstants.Realm}";

    public async Task InitializeAsync()
    {
        string realmPath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "mvp24hours-test-realm.json");

        try
        {
            _container = new KeycloakBuilder("quay.io/keycloak/keycloak:26.3.3")
                .WithRealm(realmPath)
                .WithCleanUp(true)
                .Build();
            await _container.StartAsync().ConfigureAwait(false);
            BaseAddress = _container.GetBaseAddress();
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            if (RequiresDocker())
            {
                throw new InvalidOperationException(
                    "Keycloak integration tests require a running Docker daemon when KEYCLOAK_REQUIRE_DOCKER is enabled.",
                    ex);
            }
        }
    }

    private static bool RequiresDocker()
    {
        string? value = Environment.GetEnvironmentVariable("KEYCLOAK_REQUIRE_DOCKER");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || value == "1";
    }

    public ServiceProvider CreateServiceProvider()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("The Keycloak test container is unavailable.");
        }

        Dictionary<string, string?> values = new()
        {
            ["Keycloak:Authority"] = Authority,
            ["Keycloak:Realm"] = KeycloakTestConstants.Realm,
            ["Keycloak:ClientId"] = KeycloakTestConstants.ClientId,
            ["Keycloak:ClientSecret"] = KeycloakTestConstants.ClientSecret,
            ["Keycloak:Audience"] = KeycloakTestConstants.Audience,
            ["Keycloak:RequireHttpsMetadata"] = "false",
            ["Keycloak:ValidateIssuer"] = "true",
            ["Keycloak:ValidateAudience"] = "true",
            ["Keycloak:Authorization:ResourceServerClientId"] =
                KeycloakTestConstants.ClientId,
            ["Keycloak:Admin:AdminBaseUrl"] =
                $"{BaseAddress.TrimEnd('/')}/admin/realms/{KeycloakTestConstants.Realm}",
            ["Keycloak:Admin:Realm"] = KeycloakTestConstants.Realm,
            ["Keycloak:Admin:ClientId"] = KeycloakTestConstants.ClientId,
            ["Keycloak:Admin:ClientSecret"] = KeycloakTestConstants.ClientSecret
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeycloakServices(configuration);
        return services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}

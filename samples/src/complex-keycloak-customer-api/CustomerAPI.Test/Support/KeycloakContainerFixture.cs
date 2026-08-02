using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;
using Testcontainers.Keycloak;

namespace CustomerAPI.Test.Support;

public sealed class KeycloakContainerFixture : IAsyncLifetime
{
    public const string CollectionName = "Keycloak";
    public const string Realm = "mvp24hours-test";
    public const string ClientId = "mvp24hours-test-client";
    public const string ClientSecret = "mvp24hours-test-secret";
    public const string Audience = "mvp24hours-api";
    public const string Username = "test-user";
    public const string Password = "test-password";

    private KeycloakContainer? _container;

    public bool IsAvailable { get; private set; }

    public string BaseAddress { get; private set; } = string.Empty;

    public string Authority =>
        $"{BaseAddress.TrimEnd('/')}/realms/{Realm}";

    public async Task InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable)
        {
            return;
        }

        string realmPath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "sample-test-realm.json");

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
        catch (Exception)
        {
            IsAvailable = false;
        }
    }

    public ServiceProvider CreateServiceProvider()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(DockerAvailability.SkipReason);
        }

        Dictionary<string, string?> values = new()
        {
            ["Keycloak:Authority"] = Authority,
            ["Keycloak:Realm"] = Realm,
            ["Keycloak:ClientId"] = ClientId,
            ["Keycloak:ClientSecret"] = ClientSecret,
            ["Keycloak:Audience"] = Audience,
            ["Keycloak:RequireHttpsMetadata"] = "false",
            ["Keycloak:ValidateIssuer"] = "true",
            ["Keycloak:ValidateAudience"] = "true",
            ["Keycloak:Authorization:ResourceServerClientId"] = ClientId,
            ["Keycloak:Admin:AdminBaseUrl"] = $"{BaseAddress.TrimEnd('/')}/admin/realms/{Realm}",
            ["Keycloak:Admin:Realm"] = Realm,
            ["Keycloak:Admin:ClientId"] = ClientId,
            ["Keycloak:Admin:ClientSecret"] = ClientSecret
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

[CollectionDefinition(KeycloakContainerFixture.CollectionName, DisableParallelization = true)]
public sealed class KeycloakCollectionDefinition : ICollectionFixture<KeycloakContainerFixture>
{
}

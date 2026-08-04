using App.Application.Extensions;
using App.Infrastructure.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Caching.HybridCache;
using Mvp24Hours.Infrastructure.Identity.Keycloak.HealthChecks;
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;

namespace App.WebAPI.Extensions;

public static class ServiceBuilderExtensions
{
    public static IServiceCollection AddMyDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddInfrastructureServices(configuration);
    }

    public static void AddMyServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKeycloakServices(configuration);
        services.AddMvpHybridCache();
        services.AddHttpClient("external-dependency")
            .AddStandardResilienceHandler();
        services.AddApplicationServices();
    }

    public static IServiceCollection AddMyHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy())
            .AddKeycloakHealthCheck(
                name: "keycloak",
                failureStatus: HealthStatus.Degraded,
                tags: ["identity"]);
        return services;
    }
}

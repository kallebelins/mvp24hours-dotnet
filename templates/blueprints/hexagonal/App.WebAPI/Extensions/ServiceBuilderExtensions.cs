using App.Application.UseCases;
using App.Core.Ports;
using App.Infrastructure.Adapters.Persistence;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Caching.HybridCache;
using Mvp24Hours.Infrastructure.Identity.Keycloak.HealthChecks;
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;

namespace App.WebAPI.Extensions;

public static class ServiceBuilderExtensions
{
    public static IServiceCollection AddMyDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseName = configuration["Database:Name"] ?? "AppTemplate";
        services.AddDbContext<EFDBContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddMvp24HoursDbContext<EFDBContext>();
        return services;
    }

    public static void AddMyServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKeycloakServices(configuration);
        services.AddMvpHybridCache();
        services.AddHttpClient("external-dependency")
            .AddStandardResilienceHandler();
        services.AddScoped<ItemEFAdapter>();
        services.AddScoped<IItemReadPort>(sp => sp.GetRequiredService<ItemEFAdapter>());
        services.AddScoped<IItemWritePort>(sp => sp.GetRequiredService<ItemEFAdapter>());
        services.AddScoped<IItemUseCase, ItemUseCase>();
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

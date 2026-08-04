using App.Application.Items.Commands.CreateItem;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Caching.HybridCache;
using Mvp24Hours.Infrastructure.Cqrs.Extensions;
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
        services.AddMvp24HoursRepositoryAsync(options =>
        {
            options.MaxQtyByQueryPage = 100;
            options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
        });
        return services;
    }

    public static void AddMyServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKeycloakServices(configuration);
        services.AddMvpHybridCache();
        services.AddHttpClient("external-dependency")
            .AddStandardResilienceHandler();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<CreateItemCommandHandler>();
            options.WithDefaultBehaviors();
        });
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

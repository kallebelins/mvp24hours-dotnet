using App.Application.Extensions;
using App.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace App.WebAPI.Extensions;

public static class ServiceBuilderExtensions
{
    public static IServiceCollection AddMyDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddInfrastructureServices(configuration);
    }

    public static void AddMyServices(this IServiceCollection services)
    {
        services.AddApplicationServices();
    }

    public static IServiceCollection AddMyHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());
        return services;
    }
}

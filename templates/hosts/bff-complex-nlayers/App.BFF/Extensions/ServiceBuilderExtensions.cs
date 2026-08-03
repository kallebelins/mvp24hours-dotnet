using App.Application;
using App.Application.Logic;
using App.Core.Contract.Logic;
using App.Core.Models;
using App.Core.Ports;
using App.Core.Validations;
using App.Infrastructure.Gateways;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace App.BFF.Extensions;

public static class ServiceBuilderExtensions
{
    public static IServiceCollection AddMyServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<FacadeService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddSingleton<IValidator<Item>, ItemValidator>();

        // Default: in-memory stub. Swap to HttpItemGateway for real downstream APIs.
        services.TryAddSingleton<IItemGateway, InMemoryItemGateway>();

        services.Configure<HttpItemGatewayOptions>(configuration.GetSection("Downstream:ItemApi"));
        services.AddHttpClient<HttpItemGateway>();

        return services;
    }

    public static IServiceCollection AddMyHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());
        return services;
    }
}

using App.Application;
using App.Application.Logic;
using App.Core.Contract.Logic;
using App.Core.Models;
using App.Core.Ports;
using App.Core.Validations;
using App.Infrastructure.Gateways;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Caching.HybridCache;
using Mvp24Hours.Infrastructure.Identity.Keycloak.HealthChecks;
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;

namespace App.BFF.Extensions;

public static class ServiceBuilderExtensions
{
    public static IServiceCollection AddMyServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKeycloakServices(configuration);
        services.AddMvpHybridCache();
        services.AddScoped<FacadeService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddSingleton<IValidator<Item>, ItemValidator>();

        services.Configure<HttpItemGatewayOptions>(configuration.GetSection("Downstream:ItemApi"));

        var httpGatewayOptions = configuration.GetSection("Downstream:ItemApi").Get<HttpItemGatewayOptions>()
            ?? new HttpItemGatewayOptions();

        services.AddHttpClient<HttpItemGateway>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<HttpItemGatewayOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseAddress);
                client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
            })
            .AddStandardResilienceHandler();

        if (httpGatewayOptions.UseHttpGateway)
        {
            services.AddScoped<IItemGateway>(sp => sp.GetRequiredService<HttpItemGateway>());
        }
        else
        {
            services.TryAddSingleton<IItemGateway, InMemoryItemGateway>();
        }

        services.AddHttpClient("external-dependency")
            .AddStandardResilienceHandler();

        return services;
    }

    public static IServiceCollection AddMyHealthChecks(this IServiceCollection services, IHostEnvironment environment)
    {
        var builder = services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy());

        if (!environment.IsEnvironment("Testing"))
        {
            builder.AddKeycloakHealthCheck(
                name: "keycloak",
                failureStatus: HealthStatus.Degraded,
                tags: ["identity"],
                timeout: TimeSpan.FromSeconds(2));
        }

        return services;
    }
}

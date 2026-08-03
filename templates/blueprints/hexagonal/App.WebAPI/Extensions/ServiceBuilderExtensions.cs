using App.Application.UseCases;
using App.Core.Ports;
using App.Infrastructure.Adapters.Persistence;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;

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

    public static void AddMyServices(this IServiceCollection services)
    {
        services.AddScoped<ItemEFAdapter>();
        services.AddScoped<IItemReadPort>(sp => sp.GetRequiredService<ItemEFAdapter>());
        services.AddScoped<IItemWritePort>(sp => sp.GetRequiredService<ItemEFAdapter>());
        services.AddScoped<IItemUseCase, ItemUseCase>();
    }

    public static IServiceCollection AddMyHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());
        return services;
    }
}

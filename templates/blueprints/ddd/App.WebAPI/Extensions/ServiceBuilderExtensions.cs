using App.Application.Items.Commands.CreateItem;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Extensions;

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

    public static void AddMyServices(this IServiceCollection services)
    {
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<CreateItemCommandHandler>();
            options.WithDefaultBehaviors();
        });
    }

    public static IServiceCollection AddMyHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());
        return services;
    }
}

using CustomerAPI.Application;
using CustomerAPI.Application.Configuration;
using CustomerAPI.Application.Logic;
using CustomerAPI.Application.Pipe.Operations.Customers;
using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Infrastructure.Data;
using CustomerAPI.WebAPI.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Extensions.Options;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Http.Resilience;
using System;

namespace CustomerAPI.WebAPI.Extensions;

public static class ServiceBuilderExtensions
{
    public static IServiceCollection AddMyDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionStrings = configuration.GetSection(ConnectionStringsOptions.SectionName)
            .Get<ConnectionStringsOptions>()
            ?? throw new InvalidOperationException("ConnectionStrings configuration is required.");
        services.AddDbContext<EFDBContext>(options =>
            options.UseSqlServer(connectionStrings.EFDBContext));
        services.AddMvp24HoursDbContext<EFDBContext>();
        services.AddMvp24HoursRepositoryAsync(options: options =>
        {
            options.MaxQtyByQueryPage = 100;
            options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
        });
        return services;
    }

    public static IServiceCollection AddMyServices(this IServiceCollection services)
    {
        services.AddScoped<FacadeService>();
        services.AddScoped<ICustomerService, CustomerService>();

        // Pipeline steps: remote fetch, ACL map, and persistence boundaries
        services.AddScoped<GetCustomerClientStep>();
        services.AddScoped<GetByCustomerMapperResponseStep>();
        services.AddScoped<ValidateCustomerRepositoryStep>();
        services.AddScoped<CreateCustomerRepositoryStep>();

        services.AddHttpClientWithStandardResilience(GetCustomerClientStep.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    public static IServiceCollection AddMyHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionStrings = configuration.GetSection(ConnectionStringsOptions.SectionName)
            .Get<ConnectionStringsOptions>()
            ?? throw new InvalidOperationException("ConnectionStrings configuration is required.");
        services.AddHealthChecks()
            .AddSqlServer(
                connectionStrings.EFDBContext,
                healthQuery: "SELECT 1;",
                name: "SqlServer",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);
        return services;
    }

    /// <summary>
    /// Binds and validates connection strings and external integration settings.
    /// </summary>
    public static IServiceCollection AddMyOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidation<ConnectionStringsOptions>(
            configuration.GetSection(ConnectionStringsOptions.SectionName));
        services.AddOptionsWithValidation<TypicodeOptions>(
            configuration.GetSection(TypicodeOptions.SectionName));
        return services;
    }
}

using CustomerAPI.Application;
using CustomerAPI.Application.Logic;
using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Validations.Customers;
using CustomerAPI.Infrastructure.Data;
using CustomerAPI.WebAPI.Configuration;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Extensions.Options;
using Mvp24Hours.Extensions;

namespace CustomerAPI.WebAPI.Extensions;

/// <summary>
/// Registers sample-specific services, options, persistence, and health checks.
/// </summary>
public static class ServiceBuilderExtensions
{
    /// <summary>
    /// Registers EF Core, Unit of Work, and repository defaults.
    /// </summary>
    public static IServiceCollection AddMyDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        ConnectionStringsOptions connectionStrings = configuration.GetSection(ConnectionStringsOptions.SectionName)
            .Get<ConnectionStringsOptions>()
            ?? throw new InvalidOperationException("ConnectionStrings configuration is required.");
        services.AddDbContext<EFDBContext>(options =>
            options.UseSqlServer(connectionStrings.EFDBContext)
        );
        services.AddMvp24HoursDbContext<EFDBContext>();
        services.AddMvp24HoursRepositoryAsync(options =>
        {
            options.MaxQtyByQueryPage = 100;
            options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
        });
        return services;
    }

    /// <summary>
    /// Registers Facade, application services, and FluentValidation validators.
    /// </summary>
    public static void AddMyServices(this IServiceCollection services)
    {
        services.AddScoped<FacadeService>();

        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IContactService, ContactService>();

        services.AddSingleton<IValidator<Customer>, CustomerValidator>();
        services.AddSingleton<IValidator<Contact>, ContactValidator>();
    }

    /// <summary>
    /// Registers SQL Server health checks for this host.
    /// </summary>
    public static IServiceCollection AddMyHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        ConnectionStringsOptions connectionStrings = configuration.GetSection(ConnectionStringsOptions.SectionName)
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
    /// Binds and validates connection strings used by this host.
    /// </summary>
    public static IServiceCollection AddMyOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidation<ConnectionStringsOptions>(
            configuration.GetSection(ConnectionStringsOptions.SectionName));
        return services;
    }
}

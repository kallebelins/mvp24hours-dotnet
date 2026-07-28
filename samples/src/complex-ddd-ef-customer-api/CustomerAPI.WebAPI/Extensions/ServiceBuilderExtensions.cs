using CustomerAPI.Application.Contacts.Commands.AddContact;
using CustomerAPI.Application.Contacts.Commands.RemoveContact;
using CustomerAPI.Application.Customers.Commands.CreateCustomer;
using CustomerAPI.Application.Customers.Commands.DeactivateCustomer;
using CustomerAPI.Application.Customers.Commands.UpdateCustomer;
using CustomerAPI.Infrastructure.Data;
using CustomerAPI.WebAPI.Configuration;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Extensions.Options;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Extensions;
using System;

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
        var connectionStrings = configuration.GetSection(ConnectionStringsOptions.SectionName)
            .Get<ConnectionStringsOptions>()
            ?? throw new InvalidOperationException("ConnectionStrings configuration is required.");

        services.AddDbContext<EFDBContext>(options =>
            options.UseSqlServer(connectionStrings.EFDBContext));
        services.AddMvp24HoursDbContext<EFDBContext>();
        services.AddMvp24HoursRepositoryAsync(options =>
        {
            options.MaxQtyByQueryPage = 100;
            options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
        });
        return services;
    }

    /// <summary>
    /// Registers the Mvp24Hours Mediator, default behaviors, and command/query validators.
    /// </summary>
    public static void AddMyServices(this IServiceCollection services)
    {
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<CreateCustomerCommandHandler>();
            options.WithDefaultBehaviors();
            options.RegisterValidationBehavior = true;
        });

        services.AddSingleton<IValidator<CreateCustomerCommand>, CreateCustomerCommandValidator>();
        services.AddSingleton<IValidator<UpdateCustomerCommand>, UpdateCustomerCommandValidator>();
        services.AddSingleton<IValidator<AddContactCommand>, AddContactCommandValidator>();
        services.AddSingleton<IValidator<DeactivateCustomerCommand>, DeactivateCustomerCommandValidator>();
        services.AddSingleton<IValidator<RemoveContactCommand>, RemoveContactCommandValidator>();
    }

    /// <summary>
    /// Registers SQL Server health checks for this host.
    /// </summary>
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
    /// Binds and validates connection strings used by this host.
    /// </summary>
    public static IServiceCollection AddMyOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidation<ConnectionStringsOptions>(
            configuration.GetSection(ConnectionStringsOptions.SectionName));
        return services;
    }
}

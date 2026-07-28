using CustomerAPI.Application.Contacts.Commands.CreateContact;
using CustomerAPI.Application.Contacts.Commands.DeleteContact;
using CustomerAPI.Application.Contacts.Commands.UpdateContact;
using CustomerAPI.Application.Customers.Commands.CreateCustomer;
using CustomerAPI.Application.Customers.Commands.DeleteCustomer;
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
    /// Registers the Mvp24Hours Mediator, default behaviors, validation, and command validators.
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
        services.AddSingleton<IValidator<DeleteCustomerCommand>, DeleteCustomerCommandValidator>();
        services.AddSingleton<IValidator<CreateContactCommand>, CreateContactCommandValidator>();
        services.AddSingleton<IValidator<UpdateContactCommand>, UpdateContactCommandValidator>();
        services.AddSingleton<IValidator<DeleteContactCommand>, DeleteContactCommandValidator>();
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

    public static IServiceCollection AddMyOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidation<ConnectionStringsOptions>(
            configuration.GetSection(ConnectionStringsOptions.SectionName));
        return services;
    }
}

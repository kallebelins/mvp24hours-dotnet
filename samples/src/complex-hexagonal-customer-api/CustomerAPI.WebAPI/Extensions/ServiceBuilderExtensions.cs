using CustomerAPI.Application.DTOs.Contacts;
using CustomerAPI.Application.DTOs.Customers;
using CustomerAPI.Application.Ports;
using CustomerAPI.Application.UseCases;
using CustomerAPI.Application.Validations.Contacts;
using CustomerAPI.Application.Validations.Customers;
using CustomerAPI.Core.Ports;
using CustomerAPI.Infrastructure.Adapters.Http;
using CustomerAPI.Infrastructure.Adapters.Persistence;
using CustomerAPI.Infrastructure.Configuration;
using CustomerAPI.Infrastructure.Data;
using CustomerAPI.WebAPI.Configuration;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Extensions.Options;
using Mvp24Hours.Infrastructure.Http.Resilience;
using System;

namespace CustomerAPI.WebAPI.Extensions;

/// <summary>
/// Composition root — wires inbound ports to use cases, outbound ports to adapters,
/// and HTTP clients with resilience handlers.
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

        return services;
    }

    /// <summary>
    /// Registers all inbound ports → use cases and outbound ports → adapters.
    /// The Application layer is never wired to Infrastructure types directly.
    /// </summary>
    public static IServiceCollection AddMyServices(this IServiceCollection services)
    {
        // Outbound persistence adapters (implement Core ports)
        services.AddScoped<CustomerEFAdapter>();
        services.AddScoped<ICustomerReadPort>(sp => sp.GetRequiredService<CustomerEFAdapter>());
        services.AddScoped<ICustomerWritePort>(sp => sp.GetRequiredService<CustomerEFAdapter>());

        services.AddScoped<ContactEFAdapter>();
        services.AddScoped<IContactReadPort>(sp => sp.GetRequiredService<ContactEFAdapter>());
        services.AddScoped<IContactWritePort>(sp => sp.GetRequiredService<ContactEFAdapter>());

        // Outbound HTTP adapter (implements IExternalProfilePort)
        services.AddScoped<IExternalProfilePort, TypicodeProfileAdapter>();

        // Inbound port implementations (application use cases)
        services.AddScoped<ICustomerUseCase, CustomerUseCase>();
        services.AddScoped<IExternalProfileUseCase, ExternalProfileUseCase>();

        services.AddSingleton<IValidator<CustomerCreate>, CustomerCreateValidator>();
        services.AddSingleton<IValidator<CustomerUpdate>, CustomerUpdateValidator>();
        services.AddSingleton<IValidator<ContactCreate>, ContactCreateValidator>();
        services.AddSingleton<IValidator<ContactUpdate>, ContactUpdateValidator>();

        return services;
    }

    /// <summary>
    /// Registers the resilient named HTTP client consumed by <see cref="TypicodeProfileAdapter"/>.
    /// Uses Microsoft.Extensions.Http.Resilience <c>AddStandardResilienceHandler</c> (retry + circuit-breaker).
    /// </summary>
    public static IServiceCollection AddMyHttpClients(this IServiceCollection services)
    {
        services.AddHttpClientWithStandardResilience(TypicodeProfileAdapter.HttpClientName, client =>
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

    public static IServiceCollection AddMyOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidation<ConnectionStringsOptions>(
            configuration.GetSection(ConnectionStringsOptions.SectionName));

        services.AddOptionsWithValidation<TypicodeOptions>(
            configuration.GetSection(TypicodeOptions.SectionName));

        return services;
    }
}

using CustomerAPI.Application;
using CustomerAPI.Application.Configuration;
using CustomerAPI.Application.Logic;
using CustomerAPI.Application.Pipe.Builders;
using CustomerAPI.Application.Pipe.Operations.Customers;
using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Core.Contract.Pipe.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Extensions.Options;
using Mvp24Hours.Infrastructure.Http.Resilience;
using System;

namespace CustomerAPI.WebAPI.Extensions;

public static class ServiceBuilderExtensions
{
    public static IServiceCollection AddMyServices(this IServiceCollection services)
    {
        services.AddScoped<FacadeService>();
        services.AddScoped<ICustomerService, CustomerService>();

        // Pipeline steps (shared and use-case specific) — registered for DI into builders
        services.AddScoped<GetCustomerClientStep>();
        services.AddScoped<GetByCustomerMapperResponseStep>();
        services.AddScoped<GetByIdCustomerMapperResponseStep>();

        // Use-case builders compose injected steps (testable without service locator)
        services.AddScoped<IGetByCustomerBuilder, GetByCustomerBuilder>();
        services.AddScoped<IGetByIdCustomerBuilder, GetByIdCustomerBuilder>();

        services.AddHttpClientWithStandardResilience(GetCustomerClientStep.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    public static IServiceCollection AddMyHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    /// <summary>
    /// Binds and validates external integration settings used by this host.
    /// </summary>
    public static IServiceCollection AddMyOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidation<TypicodeOptions>(
            configuration.GetSection(TypicodeOptions.SectionName));
        return services;
    }
}

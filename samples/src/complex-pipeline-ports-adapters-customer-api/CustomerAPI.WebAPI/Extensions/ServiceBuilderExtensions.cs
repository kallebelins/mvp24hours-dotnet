using CustomerAPI.Application;
using CustomerAPI.Application.Logic;
using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Core.Contract.Pipe.Builders;
using CustomerAPI.Typicode.Application.Configuration;
using CustomerAPI.Typicode.Application.Pipe.Builders;
using CustomerAPI.Typicode.Application.Pipe.Operations.Customers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Extensions.Options;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Http.Resilience;
using System;

namespace CustomerAPI.WebAPI.Extensions
{
    public static class ServiceBuilderExtensions
    {
        public static IServiceCollection AddMyServices(this IServiceCollection services)
        {
            services.AddScoped<FacadeService>();
            services.AddScoped<ICustomerService, CustomerService>();

            // pipeline - builders
            services.AddScoped<IGetByCustomerBuilder, GetByCustomerBuilder>();
            services.AddScoped<IGetByIdCustomerBuilder, GetByIdCustomerBuilder>();
            services.AddScoped<GetCustomerClientStep>();

            // Use IHttpClientFactory with Microsoft.Extensions.Http.Resilience via the Mvp24Hours helper.
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
}

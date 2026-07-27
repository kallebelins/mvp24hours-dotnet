using CustomerAPI.WebAPI.Pipe.Operations.Customers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Extensions;
using System;
using Mvp24Hours.Core.Extensions.Options;
using CustomerAPI.WebAPI.Configuration;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Http.Resilience;

namespace CustomerAPI.WebAPI.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class ServiceBuilderExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        public static IServiceCollection AddMyServices(this IServiceCollection services)
        {
            services.AddScoped<GetCustomerClientStep>();
            services.AddHttpClientWithStandardResilience(GetCustomerClientStep.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
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

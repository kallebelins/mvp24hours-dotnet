using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Validations.Customers;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Caching;
using System;
using Mvp24Hours.Core.Extensions.Options;
using CustomerAPI.WebAPI.Configuration;
using Microsoft.Extensions.Options;

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
        public static IServiceCollection AddMyCaching(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionStrings = configuration.GetSection(ConnectionStringsOptions.SectionName)
                .Get<ConnectionStringsOptions>()
                ?? throw new InvalidOperationException("ConnectionStrings configuration is required.");
            services.AddMvp24HoursCaching(
                /* Remove an unused item after this duration. */
                SlidingExpiration: System.TimeSpan.FromMinutes(5)
            );
            services.AddMvp24HoursCachingRedis(connectionStrings.RedisDbContext, instanceName: "customerapi");
            services.AddScoped<IRepositoryCacheAsync<CustomerDto>, RepositoryCacheAsync<CustomerDto>>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IServiceCollection AddMyHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionStrings = configuration.GetSection(ConnectionStringsOptions.SectionName)
                .Get<ConnectionStringsOptions>()
                ?? throw new InvalidOperationException("ConnectionStrings configuration is required.");
            services.AddHealthChecks()
                .AddRedis(
                    connectionStrings.RedisDbContext,
                    name: "Redis",
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IServiceCollection AddMyValidators(this IServiceCollection services)
        {
            services.AddSingleton<IValidator<CustomerDto>, CustomerValidator>();
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
}

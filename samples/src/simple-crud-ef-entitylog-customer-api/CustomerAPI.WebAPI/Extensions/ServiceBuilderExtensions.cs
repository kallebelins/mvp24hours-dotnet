using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Validations.Customers;
using CustomerAPI.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Extensions;
using System;
using Mvp24Hours.Core.Extensions.Options;
using CustomerAPI.WebAPI.Configuration;

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
        public static IServiceCollection AddMyDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionStrings = configuration.GetSection(ConnectionStringsOptions.SectionName)
                .Get<ConnectionStringsOptions>()
                ?? throw new InvalidOperationException("ConnectionStrings configuration is required.");
            services.AddDbContext<EFDBContext>(options =>
                options.UseSqlServer(connectionStrings.CustomerDbContext)
            );
            services.AddMvp24HoursDbContext<EFDBContext>();
            services.AddMvp24HoursRepositoryAsync(options: options =>
            {
                options.MaxQtyByQueryPage = 100;
                options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
            });
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
                .AddSqlServer(
                    connectionStrings.CustomerDbContext,
                    healthQuery: "SELECT 1;",
                    name: "SqlServer",
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IServiceCollection AddMyServices(this IServiceCollection services)
        {
            services.AddSingleton<IValidator<Customer>, CustomerValidator>();
            services.AddSingleton<IValidator<Contact>, ContactValidator>();
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

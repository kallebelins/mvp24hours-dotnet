using CustomerAPI.Data;
using CustomerAPI.Entities;
using CustomerAPI.Validations;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using Mvp24Hours.WebAPI.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Extensions.Options;
using CustomerAPI.Configuration;
using System;



namespace CustomerAPI.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class ServiceBuilderExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        public static IServiceCollection Configure(this IServiceCollection services, IConfiguration configuration)
        {
            #region [ Mvp24Hours ]
            services.AddMvp24HoursWebEssential();
            services.AddMvp24HoursWebJson();
            services.AddMvp24HoursNativeOpenApi(options =>
            {
                options.Title = "Customer EF API";
                options.Version = "1.0.0";
                options.EnableSwaggerUI = true;
            });
            services.AddMvp24HoursWebGzip();
            #endregion
            services.AddMyOptions(configuration);
            services.AddMyServices();
            services.AddMyDbContext(configuration);
            services.AddMyHealthChecks(configuration);

            return services;
        }



        /// <summary>
        /// 
        /// </summary>
        public static IServiceCollection AddMyDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionStrings = configuration.GetSection(ConnectionStringsOptions.SectionName)
                .Get<ConnectionStringsOptions>()
                ?? throw new InvalidOperationException("ConnectionStrings configuration is required.");
            services.AddDbContext<EFDBContext>(options =>
                options.UseSqlServer(connectionStrings.EFDBContext)
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
                    connectionStrings.EFDBContext,
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

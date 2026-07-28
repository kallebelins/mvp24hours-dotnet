using CustomerAPI.Application;
using CustomerAPI.Application.Logic;
using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Validations.Customers;
using CustomerAPI.Infrastructure.Data;
using CustomerAPI.WebAPI.Configuration;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Extensions.Options;
using Mvp24Hours.Extensions;
using System;

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
            services.AddMvp24HoursDbContext<MongoDBContext>(options =>
            {
                options.DatabaseName = "complexcustomers";
                options.ConnectionString = connectionStrings.MongoDbContext;
                options.RetryReads = true;
                options.RetryWrites = true;
            });
            services.AddMvp24HoursRepositoryAsync((Mvp24Hours.Infrastructure.Data.MongoDb.Configuration.MongoDbRepositoryOptions options) =>
            {
                options.MaxQtyByQueryPage = 100;
            });
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IServiceCollection AddMyServices(this IServiceCollection services)
        {
            services.AddScoped<FacadeService>();

            services.AddScoped<ICustomerService, CustomerService>();

            services.AddSingleton<IValidator<Customer>, CustomerValidator>();
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
                .AddMongoDb(
                    clientFactory: _ => new MongoDB.Driver.MongoClient(connectionStrings.MongoDbContext),
                    name: "MongoDb",
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
}

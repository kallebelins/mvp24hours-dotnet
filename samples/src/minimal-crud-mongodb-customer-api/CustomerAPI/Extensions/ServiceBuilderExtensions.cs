using CustomerAPI.Data;
using CustomerAPI.Entities;
using CustomerAPI.Validations;
using FluentValidation;
using Mvp24Hours.Extensions;
using Mvp24Hours.WebAPI.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Extensions.Options;
using CustomerAPI.Configuration;
using Microsoft.Extensions.Options;
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
                options.Title = "Customer Mongo API";
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
            services.AddMvp24HoursDbContext<MongoDbContext>(options =>
            {
                options.DatabaseName = "simplecustomers";
                options.ConnectionString = connectionStrings.MongoDbContext;
            });
            services.AddMvp24HoursRepositoryAsync((Mvp24Hours.Infrastructure.Data.MongoDb.Configuration.MongoDbRepositoryOptions _) => { });
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

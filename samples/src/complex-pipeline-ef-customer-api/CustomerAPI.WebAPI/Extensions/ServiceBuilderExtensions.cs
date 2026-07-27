using AutoMapper;
using CustomerAPI.Application;
using CustomerAPI.Application.Configuration;
using CustomerAPI.Application.Logic;
using CustomerAPI.Application.Pipe.Operations.Customers;
using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using System;
using Mvp24Hours.Core.Extensions.Options;
using CustomerAPI.WebAPI.Configuration;
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
        public static void AddMyServices(this IServiceCollection services)
        {
            services.AddScoped<FacadeService>();

            services.AddScoped<ICustomerService, CustomerService>();

            services.AddScoped<GetCustomerClientStep>();
            services.AddHttpClientWithStandardResilience(GetCustomerClientStep.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddScoped<CreateCustomerRepositoryStep>(sp =>
            {
                return new CreateCustomerRepositoryStep(sp.GetRequiredService<IUnitOfWorkAsync>(), sp.GetService<IMapper>());
            });

            services.AddScoped<ValidateCustomerRepositoryStep>(sp =>
            {
                return new ValidateCustomerRepositoryStep(sp.GetRequiredService<IUnitOfWorkAsync>());
            });
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
        /// Binds and validates connection strings used by this host.
        /// </summary>
        public static IServiceCollection AddMyOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptionsWithValidation<ConnectionStringsOptions>(
                configuration.GetSection(ConnectionStringsOptions.SectionName));
            services.AddOptionsWithValidation<TypicodeOptions>(
                configuration.GetSection(TypicodeOptions.SectionName));
            return services;
        }


    }
}

using CustomerAPI.Application;
using CustomerAPI.Application.Brokers.Consumers;
using CustomerAPI.Application.Logic;
using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Validations.Customers;
using CustomerAPI.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
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
        public static IServiceCollection AddMyDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionStrings = configuration.GetSection(ConnectionStringsOptions.SectionName)
                .Get<ConnectionStringsOptions>()
                ?? throw new InvalidOperationException("ConnectionStrings configuration is required.");
            services.AddDbContext<EFDBContext>(options =>
                options.UseSqlServer(connectionStrings.EFDBContext)
            );
            services.AddMvp24HoursDbContext<EFDBContext>();
            services.AddMvp24HoursRepositoryAsync(options =>
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
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded)
                .AddRabbitMQ(
                    connectionStrings.RabbitMQContext,
                    name: "RabbitMQ",
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);
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
        public static IServiceCollection AddMyRabbitMQ(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionStrings = configuration.GetSection(ConnectionStringsOptions.SectionName)
                .Get<ConnectionStringsOptions>()
                ?? throw new InvalidOperationException("ConnectionStrings configuration is required.");
            services.AddMvpRabbitMQ(rabbit =>
            {
                rabbit.Host(
                    connectionStrings.RabbitMQContext,
                    connection =>
                    {
                        connection.DispatchConsumersAsync(true);
                        connection.RetryCount(3);
                    });
                rabbit.AddConsumersFromAssemblyContaining<CreateCustomerConsumer>();
                rabbit.ConfigureClient(clientOptions =>
                {
                    clientOptions.Exchange = "amq.direct";
                    clientOptions.MaxRedeliveredCount = 1;
                    clientOptions.QueueArguments = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "x-queue-mode", "lazy" },
                        { "x-dead-letter-exchange", "dead-letter-amq.direct" }
                    };

                    // dead letter exchanges enabled
                    clientOptions.DeadLetter = new RabbitMQOptions()
                    {
                        Exchange = "dead-letter-amq.direct",
                        QueueArguments = new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "x-queue-mode", "lazy" }
                        }
                    };
                });
            });
            return services;
        }

        /// <summary>
        /// Starts background consumption through the documented RabbitMQ hosted service.
        /// </summary>
        public static IServiceCollection AddMyHostedService(this IServiceCollection services)
        {
            services.AddOptions<RabbitMQHostedOptions>()
                .Configure<IMvpRabbitMQClient>((options, client) =>
                {
                    options.Callback = _ => client.Consume();
                    options.DueTime = TimeSpan.Zero;
                    options.Period = TimeSpan.FromSeconds(3);
                });
            services.AddMvp24HoursHostedService();
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

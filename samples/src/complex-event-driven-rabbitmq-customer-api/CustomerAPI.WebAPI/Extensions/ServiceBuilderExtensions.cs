using CustomerAPI.Application.Customers.Commands.CreateCustomer;
using CustomerAPI.Application.Customers.Queries.GetCustomers;
using CustomerAPI.Infrastructure.Data;
using CustomerAPI.Infrastructure.Data.Stores;
using CustomerAPI.Infrastructure.Mappings;
using CustomerAPI.Infrastructure.Messaging.Consumers;
using CustomerAPI.WebAPI.Configuration;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Extensions.Options;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Implementations;
using Mvp24Hours.Infrastructure.Cqrs.Messaging;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;

namespace CustomerAPI.WebAPI.Extensions;

/// <summary>
/// Registers all application services, infrastructure, messaging, and health checks.
/// </summary>
public static class ServiceBuilderExtensions
{
    public static IServiceCollection AddMyOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptionsWithValidation<ConnectionStringsOptions>(
            configuration.GetSection(ConnectionStringsOptions.SectionName));
        return services;
    }

    public static IServiceCollection AddMyDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionStrings = configuration.GetSection(ConnectionStringsOptions.SectionName)
            .Get<ConnectionStringsOptions>()
            ?? throw new InvalidOperationException("ConnectionStrings configuration is required.");

        services.AddDbContext<EFDBContext>(options =>
            options.UseSqlServer(connectionStrings.EFDBContext));

        services.AddMvp24HoursDbContext<EFDBContext>();
        services.AddMvp24HoursRepositoryAsync(options =>
        {
            options.MaxQtyByQueryPage = 100;
            options.TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
        });

        return services;
    }

    /// <summary>
    /// Registers the Mvp24Hours Mediator with CQRS handlers and validators.
    /// </summary>
    public static IServiceCollection AddMyServices(this IServiceCollection services)
    {
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<CreateCustomerCommandHandler>();
            options.RegisterHandlersFromAssemblyContaining<GetCustomersQueryHandler>();
            options.WithDefaultBehaviors();
            options.RegisterValidationBehavior = true;
        });

        services.AddSingleton<IValidator<CreateCustomerCommand>, CreateCustomerCommandValidator>();

        // AutoMapper profile is in Infrastructure
        services.AddMvp24HoursMapService(typeof(CustomerMappingProfile).Assembly);

        return services;
    }

    /// <summary>
    /// Registers the Inbox/Outbox pattern with EF Core-backed durable stores.
    ///
    /// Design note: <see cref="EfCoreIntegrationEventOutbox"/> and <see cref="EfCoreInboxStore"/>
    /// are registered as <b>Scoped</b> so they share the same <see cref="EFDBContext"/> instance
    /// as the command handler's unit-of-work within a request scope. This allows outbox entries
    /// to be staged in the same change tracker and committed atomically.
    ///
    /// The library's <c>AddMvpInboxOutbox()</c> registers stores as Singleton which conflicts
    /// with Scoped EF DbContext, so we wire up the components manually here.
    /// </summary>
    public static IServiceCollection AddMyInboxOutbox(this IServiceCollection services, IConfiguration configuration)
    {
        // Durable EF-backed stores (Scoped to share DbContext with request handlers)
        services.AddScoped<IIntegrationEventOutbox, EfCoreIntegrationEventOutbox>();
        services.AddScoped<IInboxStore, EfCoreInboxStore>();

        // InboxProcessor (Scoped — uses IInboxStore which is also Scoped)
        services.AddScoped<IInboxProcessor, InboxProcessor>();

        // Publisher: bridges OutboxProcessor → RabbitMQ via IMvpRabbitMQClient (reflection-based)
        services.AddScoped<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();

        // Dead-letter store (in-memory for this sample; replace with EF-backed for production)
        services.AddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>();

        // Configure InboxOutbox options
        services.Configure<InboxOutboxOptions>(options =>
        {
            options.OutboxPollingInterval = TimeSpan.FromSeconds(5);
            options.BatchSize = 50;
            options.MaxRetries = 5;
            options.RetryBaseDelayMilliseconds = 1000;
            options.InboxRetentionDays = 7;
            options.OutboxRetentionDays = 7;
            options.EnableDeadLetterQueue = true;
            options.EnableAutomaticCleanup = true;
            options.CleanupInterval = TimeSpan.FromHours(1);
        });

        // OutboxProcessor background service polls for pending outbox rows and publishes via RabbitMQ
        services.AddHostedService<OutboxProcessor>();

        // Cleanup background services
        services.AddHostedService<OutboxCleanupService>();
        services.AddHostedService<InboxCleanupService>();

        return services;
    }

    /// <summary>
    /// Registers RabbitMQ client, consumers, and the hosted consume loop.
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

            rabbit.AddConsumersFromAssemblyContaining<CustomerCreatedConsumer>();

            rabbit.ConfigureClient(clientOptions =>
            {
                clientOptions.Exchange = "amq.direct";
                clientOptions.MaxRedeliveredCount = 3;
                clientOptions.QueueArguments = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "x-queue-mode", "lazy" },
                    { "x-dead-letter-exchange", "dlx.event-driven-customer" }
                };

                clientOptions.DeadLetter = new RabbitMQOptions
                {
                    Exchange = "dlx.event-driven-customer",
                    QueueArguments = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "x-queue-mode", "lazy" }
                    }
                };
            });
        });

        // Background service that polls RabbitMQ and dispatches to consumers
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
}

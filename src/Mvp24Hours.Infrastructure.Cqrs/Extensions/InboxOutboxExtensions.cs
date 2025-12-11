//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Implementations;
using Mvp24Hours.Infrastructure.Cqrs.Messaging;

namespace Mvp24Hours.Infrastructure.Cqrs.Extensions;

/// <summary>
/// Extension methods for configuring Inbox/Outbox patterns in the DI container.
/// </summary>
public static class InboxOutboxExtensions
{
    /// <summary>
    /// Adds the Inbox pattern components for idempotent message processing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure inbox options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvpInbox(options =>
    /// {
    ///     options.InboxRetentionDays = 7;
    ///     options.EnableAutomaticCleanup = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvpInbox(
        this IServiceCollection services,
        Action<InboxOutboxOptions>? configure = null)
    {
        // Configure options
        var options = new InboxOutboxOptions();
        configure?.Invoke(options);
        services.Configure<InboxOutboxOptions>(opt =>
        {
            opt.InboxRetentionDays = options.InboxRetentionDays;
            opt.CleanupInterval = options.CleanupInterval;
            opt.EnableAutomaticCleanup = options.EnableAutomaticCleanup;
        });

        // Register inbox store
        services.TryAddSingleton<IInboxStore, InMemoryInboxStore>();

        // Register inbox processor
        services.TryAddScoped<IInboxProcessor, InboxProcessor>();

        // Register cleanup service if enabled
        if (options.EnableAutomaticCleanup)
        {
            services.AddHostedService<InboxCleanupService>();
        }

        return services;
    }

    /// <summary>
    /// Adds the Outbox pattern components for reliable message publishing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure outbox options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvpOutbox(options =>
    /// {
    ///     options.OutboxPollingInterval = TimeSpan.FromSeconds(5);
    ///     options.MaxRetries = 5;
    ///     options.BatchSize = 100;
    ///     options.EnableDeadLetterQueue = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvpOutbox(
        this IServiceCollection services,
        Action<InboxOutboxOptions>? configure = null)
    {
        // Configure options
        var options = new InboxOutboxOptions();
        configure?.Invoke(options);
        services.Configure<InboxOutboxOptions>(opt =>
        {
            opt.OutboxPollingInterval = options.OutboxPollingInterval;
            opt.BatchSize = options.BatchSize;
            opt.MaxRetries = options.MaxRetries;
            opt.RetryBaseDelayMilliseconds = options.RetryBaseDelayMilliseconds;
            opt.RetryMaxDelayMilliseconds = options.RetryMaxDelayMilliseconds;
            opt.OutboxRetentionDays = options.OutboxRetentionDays;
            opt.CleanupInterval = options.CleanupInterval;
            opt.EnableAutomaticCleanup = options.EnableAutomaticCleanup;
            opt.EnableDeadLetterQueue = options.EnableDeadLetterQueue;
            opt.EnableParallelProcessing = options.EnableParallelProcessing;
            opt.MaxDegreeOfParallelism = options.MaxDegreeOfParallelism;
        });

        // Register outbox store
        services.TryAddSingleton<IIntegrationEventOutbox, InMemoryIntegrationEventOutbox>();

        // Register dead letter store if enabled
        if (options.EnableDeadLetterQueue)
        {
            services.TryAddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>();
        }

        // Register outbox processor
        services.AddHostedService<OutboxProcessor>();

        // Register cleanup service if enabled
        if (options.EnableAutomaticCleanup)
        {
            services.AddHostedService<OutboxCleanupService>();
        }

        return services;
    }

    /// <summary>
    /// Adds both Inbox and Outbox pattern components.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvpInboxOutbox(options =>
    /// {
    ///     options.OutboxPollingInterval = TimeSpan.FromSeconds(5);
    ///     options.MaxRetries = 5;
    ///     options.InboxRetentionDays = 7;
    ///     options.EnableDeadLetterQueue = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvpInboxOutbox(
        this IServiceCollection services,
        Action<InboxOutboxOptions>? configure = null)
    {
        var options = new InboxOutboxOptions();
        configure?.Invoke(options);

        // Configure options once
        services.Configure<InboxOutboxOptions>(opt =>
        {
            // Inbox options
            opt.InboxRetentionDays = options.InboxRetentionDays;
            
            // Outbox options
            opt.OutboxPollingInterval = options.OutboxPollingInterval;
            opt.BatchSize = options.BatchSize;
            opt.MaxRetries = options.MaxRetries;
            opt.RetryBaseDelayMilliseconds = options.RetryBaseDelayMilliseconds;
            opt.RetryMaxDelayMilliseconds = options.RetryMaxDelayMilliseconds;
            opt.OutboxRetentionDays = options.OutboxRetentionDays;
            
            // Common options
            opt.CleanupInterval = options.CleanupInterval;
            opt.EnableAutomaticCleanup = options.EnableAutomaticCleanup;
            opt.EnableDeadLetterQueue = options.EnableDeadLetterQueue;
            opt.DeadLetterRetentionDays = options.DeadLetterRetentionDays;
            opt.EnableParallelProcessing = options.EnableParallelProcessing;
            opt.MaxDegreeOfParallelism = options.MaxDegreeOfParallelism;
        });

        // Register stores
        services.TryAddSingleton<IInboxStore, InMemoryInboxStore>();
        services.TryAddSingleton<IIntegrationEventOutbox, InMemoryIntegrationEventOutbox>();

        // Register dead letter store if enabled
        if (options.EnableDeadLetterQueue)
        {
            services.TryAddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>();
        }

        // Register processors
        services.TryAddScoped<IInboxProcessor, InboxProcessor>();
        services.AddHostedService<OutboxProcessor>();

        // Register cleanup services if enabled
        if (options.EnableAutomaticCleanup)
        {
            services.AddHostedService<InboxCleanupService>();
            services.AddHostedService<OutboxCleanupService>();
        }

        return services;
    }

    /// <summary>
    /// Adds a custom inbox store implementation.
    /// </summary>
    /// <typeparam name="TStore">The type of inbox store to use.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvpInbox()
    ///         .UseInboxStore&lt;EfCoreInboxStore&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection UseInboxStore<TStore>(this IServiceCollection services)
        where TStore : class, IInboxStore
    {
        services.AddSingleton<IInboxStore, TStore>();
        return services;
    }

    /// <summary>
    /// Adds a custom outbox store implementation.
    /// </summary>
    /// <typeparam name="TStore">The type of outbox store to use.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvpOutbox()
    ///         .UseOutboxStore&lt;EfCoreOutboxStore&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection UseOutboxStore<TStore>(this IServiceCollection services)
        where TStore : class, IIntegrationEventOutbox
    {
        services.AddSingleton<IIntegrationEventOutbox, TStore>();
        return services;
    }

    /// <summary>
    /// Adds a custom dead letter store implementation.
    /// </summary>
    /// <typeparam name="TStore">The type of dead letter store to use.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvpOutbox()
    ///         .UseDeadLetterStore&lt;EfCoreDeadLetterStore&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection UseDeadLetterStore<TStore>(this IServiceCollection services)
        where TStore : class, IDeadLetterStore
    {
        services.AddSingleton<IDeadLetterStore, TStore>();
        return services;
    }

    /// <summary>
    /// Adds a custom integration event publisher implementation.
    /// </summary>
    /// <typeparam name="TPublisher">The type of publisher to use.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvpOutbox()
    ///         .UseIntegrationEventPublisher&lt;RabbitMqIntegrationEventPublisher&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection UseIntegrationEventPublisher<TPublisher>(this IServiceCollection services)
        where TPublisher : class, IIntegrationEventPublisher
    {
        services.AddSingleton<IIntegrationEventPublisher, TPublisher>();
        return services;
    }
}



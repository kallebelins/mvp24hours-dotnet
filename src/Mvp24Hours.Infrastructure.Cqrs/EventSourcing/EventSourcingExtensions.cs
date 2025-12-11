//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

/// <summary>
/// Extension methods for configuring Event Sourcing services.
/// </summary>
public static class EventSourcingExtensions
{
    /// <summary>
    /// Adds Event Sourcing services with in-memory implementations.
    /// Useful for testing and development.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEventSourcingInMemory();
    /// </code>
    /// </example>
    public static IServiceCollection AddEventSourcingInMemory(this IServiceCollection services)
    {
        services.TryAddSingleton<IEventSerializer, JsonEventSerializer>();
        services.TryAddSingleton<InMemoryEventStore>();
        services.TryAddSingleton<IEventStore>(sp => sp.GetRequiredService<InMemoryEventStore>());
        services.TryAddSingleton<IEventStoreWithSubscription>(sp => sp.GetRequiredService<InMemoryEventStore>());
        services.TryAddSingleton<ISnapshotStore, InMemorySnapshotStore>();
        services.TryAddSingleton<ISnapshotStrategy, EventCountSnapshotStrategy>();
        services.TryAddSingleton<IEventTypeResolver, DefaultEventTypeResolver>();

        return services;
    }

    /// <summary>
    /// Adds the Event Store service.
    /// </summary>
    /// <typeparam name="TEventStore">The event store implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventStore<TEventStore>(this IServiceCollection services)
        where TEventStore : class, IEventStore
    {
        services.AddScoped<IEventStore, TEventStore>();
        return services;
    }

    /// <summary>
    /// Adds the Snapshot Store service.
    /// </summary>
    /// <typeparam name="TSnapshotStore">The snapshot store implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSnapshotStore<TSnapshotStore>(this IServiceCollection services)
        where TSnapshotStore : class, ISnapshotStore
    {
        services.AddScoped<ISnapshotStore, TSnapshotStore>();
        return services;
    }

    /// <summary>
    /// Adds a snapshot strategy.
    /// </summary>
    /// <typeparam name="TStrategy">The strategy type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSnapshotStrategy<TStrategy>(this IServiceCollection services)
        where TStrategy : class, ISnapshotStrategy
    {
        services.AddSingleton<ISnapshotStrategy, TStrategy>();
        return services;
    }

    /// <summary>
    /// Adds an event-sourced repository.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventStoreRepository<TAggregate>(this IServiceCollection services)
        where TAggregate : IAggregate, new()
    {
        services.AddScoped<IEventStoreRepository<TAggregate>>(sp =>
        {
            var eventStore = sp.GetRequiredService<IEventStore>();
            var snapshotStore = sp.GetService<ISnapshotStore>();
            var snapshotStrategy = sp.GetService<ISnapshotStrategy>();
            var serializer = sp.GetService<IEventSerializer>();

            if (snapshotStore != null && snapshotStrategy != null)
            {
                return new EventStoreRepository<TAggregate>(
                    eventStore, snapshotStore, snapshotStrategy, serializer);
            }

            return new EventStoreRepository<TAggregate>(eventStore);
        });

        return services;
    }

    /// <summary>
    /// Adds an event-sourced repository with a custom factory.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">The factory function.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventStoreRepository<TAggregate>(
        this IServiceCollection services,
        Func<IServiceProvider, IEventStoreRepository<TAggregate>> factory)
        where TAggregate : IAggregate
    {
        services.AddScoped(factory);
        return services;
    }

    /// <summary>
    /// Configures the event count snapshot strategy.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="threshold">Number of events between snapshots.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventCountSnapshotStrategy(
        this IServiceCollection services,
        int threshold = 100)
    {
        services.AddSingleton<ISnapshotStrategy>(new EventCountSnapshotStrategy(threshold));
        return services;
    }

    /// <summary>
    /// Configures no snapshot strategy (never take snapshots).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNoSnapshotStrategy(this IServiceCollection services)
    {
        services.AddSingleton<ISnapshotStrategy>(NeverSnapshotStrategy.Instance);
        return services;
    }

    /// <summary>
    /// Configures always snapshot strategy (snapshot after every change).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAlwaysSnapshotStrategy(this IServiceCollection services)
    {
        services.AddSingleton<ISnapshotStrategy>(AlwaysSnapshotStrategy.Instance);
        return services;
    }

    /// <summary>
    /// Adds an event type resolver with a configuration action.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventTypeResolver(
        this IServiceCollection services,
        Action<RegistryEventTypeResolver> configure)
    {
        var resolver = new RegistryEventTypeResolver();
        configure(resolver);
        services.AddSingleton<IEventTypeResolver>(resolver);
        return services;
    }
}

/// <summary>
/// Options for Event Sourcing configuration.
/// </summary>
public class EventSourcingOptions
{
    /// <summary>
    /// Gets or sets the number of events between snapshots.
    /// Default is 100.
    /// </summary>
    public int SnapshotThreshold { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to enable snapshots.
    /// Default is true.
    /// </summary>
    public bool EnableSnapshots { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use JSON serialization.
    /// Default is true.
    /// </summary>
    public bool UseJsonSerialization { get; set; } = true;
}


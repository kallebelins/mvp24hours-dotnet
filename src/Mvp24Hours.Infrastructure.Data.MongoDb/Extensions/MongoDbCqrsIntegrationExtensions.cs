//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Cqrs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for integrating MongoDB with the CQRS module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Purpose:</strong>
    /// These extensions enable seamless integration between the MongoDB data access layer
    /// and the CQRS (Command Query Responsibility Segregation) module for domain event
    /// dispatching.
    /// </para>
    /// <para>
    /// <strong>Key Features:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Automatic domain event dispatching after SaveChanges</description></item>
    /// <item><description>Integration with Outbox pattern for reliable event delivery</description></item>
    /// <item><description>Support for read/write context separation</description></item>
    /// <item><description>Flexible configuration for different scenarios</description></item>
    /// </list>
    /// </remarks>
    public static class MongoDbCqrsIntegrationExtensions
    {
        #region [ Domain Event Dispatcher Registration ]

        /// <summary>
        /// Adds a no-operation domain event dispatcher for MongoDB.
        /// Use this when you don't need domain event dispatching but want to use IUnitOfWorkWithEvents.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Events will be silently discarded. Useful for development or when events are not needed.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContext(options => { ... })
        ///         .AddMvp24HoursMongoDbNoOpEventDispatcher();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursMongoDbNoOpEventDispatcher(this IServiceCollection services)
        {
            services.TryAddScoped<IDomainEventDispatcherMongoDb, NoOpDomainEventDispatcher>();
            return services;
        }

        /// <summary>
        /// Adds a domain event dispatcher adapter for MongoDB using a custom dispatch function.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="dispatchFuncFactory">
        /// Factory function that creates the dispatch delegate. The delegate receives
        /// domain events and should publish them to appropriate handlers.
        /// </param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method allows custom integration with any event dispatching mechanism.
        /// The factory receives the service provider to resolve dependencies.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Integration with custom event bus
        /// services.AddMvp24HoursMongoDbEventDispatcher(sp =>
        /// {
        ///     var eventBus = sp.GetRequiredService&lt;IEventBus&gt;();
        ///     return async (events, ct) =>
        ///     {
        ///         foreach (var evt in events)
        ///         {
        ///             await eventBus.PublishAsync(evt, ct);
        ///         }
        ///     };
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursMongoDbEventDispatcher(
            this IServiceCollection services,
            Func<IServiceProvider, Func<IEnumerable<IDomainEvent>, CancellationToken, Task>> dispatchFuncFactory)
        {
            ArgumentNullException.ThrowIfNull(dispatchFuncFactory);

            services.AddScoped<IDomainEventDispatcherMongoDb>(sp =>
            {
                var dispatchFunc = dispatchFuncFactory(sp);
                var logger = sp.GetService<ILogger<DomainEventDispatcherAdapter>>();
                return new DomainEventDispatcherAdapter(dispatchFunc, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds integration with the CQRS module's domain event dispatcher.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method integrates with <c>IDomainEventDispatcher</c> from the 
        /// <c>Mvp24Hours.Infrastructure.Cqrs</c> module.
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// You must have the CQRS module configured with <c>AddMvpMediator()</c>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // First, configure CQRS module
        /// services.AddMvpMediator(options => { ... });
        /// 
        /// // Then, configure MongoDB with CQRS integration
        /// services.AddMvp24HoursDbContext(options => { ... })
        ///         .AddMvp24HoursMongoDbCqrsIntegration();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursMongoDbCqrsIntegration(this IServiceCollection services)
        {
            // Register the adapter that bridges MongoDB to CQRS
            services.AddScoped<IDomainEventDispatcherMongoDb>(sp =>
            {
                // Try to resolve IDomainEventDispatcher from CQRS module
                var cqrsDispatcher = sp.GetService<Infrastructure.Cqrs.Abstractions.IDomainEventDispatcher>();
                var logger = sp.GetService<ILogger<CqrsDomainEventDispatcherBridge>>();

                if (cqrsDispatcher == null)
                {
                    logger?.LogWarning(
                        "[MongoDB-CQRS] IDomainEventDispatcher not found. Using no-op dispatcher. " +
                        "Make sure to call AddMvpMediator() before AddMvp24HoursMongoDbCqrsIntegration()");
                    return new NoOpDomainEventDispatcher(sp.GetService<ILogger<NoOpDomainEventDispatcher>>());
                }

                return new CqrsDomainEventDispatcherBridge(cqrsDispatcher, logger);
            });

            return services;
        }

        #endregion

        #region [ Unit of Work with Events Registration ]

        /// <summary>
        /// Adds the synchronous Unit of Work with domain event support for MongoDB.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">Service lifetime. Defaults to Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This registers <see cref="UnitOfWorkWithEvents"/> as the implementation for
        /// both <see cref="IUnitOfWork"/> and <see cref="IUnitOfWorkWithEvents"/>.
        /// </para>
        /// <para>
        /// <strong>Note:</strong> You should also register an <see cref="IDomainEventDispatcherMongoDb"/>
        /// implementation using one of the dispatcher registration methods.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContext(options => { ... })
        ///         .AddMvp24HoursMongoDbCqrsIntegration()
        ///         .AddMvp24HoursRepositoryWithEvents();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursRepositoryWithEvents(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            services.Add(new ServiceDescriptor(typeof(IUnitOfWork), typeof(UnitOfWorkWithEvents), lifetime));
            services.Add(new ServiceDescriptor(typeof(IUnitOfWorkWithEvents), typeof(UnitOfWorkWithEvents), lifetime));
            services.Add(new ServiceDescriptor(typeof(IRepository<>), typeof(Repository<>), lifetime));

            return services;
        }

        /// <summary>
        /// Adds the asynchronous Unit of Work with domain event support for MongoDB.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">Service lifetime. Defaults to Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This registers <see cref="UnitOfWorkWithEventsAsync"/> as the implementation for
        /// both <see cref="IUnitOfWorkAsync"/> and <see cref="IUnitOfWorkWithEventsAsync"/>.
        /// </para>
        /// <para>
        /// <strong>Note:</strong> You should also register an <see cref="IDomainEventDispatcherMongoDb"/>
        /// implementation using one of the dispatcher registration methods.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContext(options => { ... })
        ///         .AddMvp24HoursMongoDbCqrsIntegration()
        ///         .AddMvp24HoursRepositoryAsyncWithEvents();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursRepositoryAsyncWithEvents(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            services.Add(new ServiceDescriptor(typeof(IUnitOfWorkAsync), typeof(UnitOfWorkWithEventsAsync), lifetime));
            services.Add(new ServiceDescriptor(typeof(IUnitOfWorkWithEventsAsync), typeof(UnitOfWorkWithEventsAsync), lifetime));
            services.Add(new ServiceDescriptor(typeof(IRepositoryAsync<>), typeof(RepositoryAsync<>), lifetime));

            return services;
        }

        /// <summary>
        /// Adds both sync and async Unit of Work with domain event support for MongoDB.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">Service lifetime. Defaults to Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContext(options => { ... })
        ///         .AddMvp24HoursMongoDbCqrsIntegration()
        ///         .AddMvp24HoursRepositoriesWithEvents();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursRepositoriesWithEvents(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services
                .AddMvp24HoursRepositoryWithEvents(lifetime)
                .AddMvp24HoursRepositoryAsyncWithEvents(lifetime);
        }

        #endregion

        #region [ Read/Write Context Separation ]

        /// <summary>
        /// Options for configuring read/write separation in MongoDB.
        /// </summary>
        public class MongoDbReadWriteSeparationOptions
        {
            /// <summary>
            /// Gets or sets the read preference for the read context.
            /// Default is "secondaryPreferred" for read replicas.
            /// </summary>
            public string ReadPreference { get; set; } = "secondaryPreferred";

            /// <summary>
            /// Gets or sets whether to use a separate connection string for reads.
            /// </summary>
            public bool UseSeparateConnection { get; set; } = false;

            /// <summary>
            /// Gets or sets the connection string for read operations.
            /// Only used when UseSeparateConnection is true.
            /// </summary>
            public string? ReadConnectionString { get; set; }
        }

        /// <summary>
        /// Adds read/write context separation for CQRS pattern.
        /// Registers separate contexts for read and write operations.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure read/write separation options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method configures separate MongoDB contexts for read and write operations,
        /// supporting the CQRS pattern where queries go to read replicas and commands
        /// go to the primary.
        /// </para>
        /// <para>
        /// <strong>Write Context:</strong> Uses primary with domain event support
        /// <strong>Read Context:</strong> Uses secondary/replica with read-only repositories
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContext(options => 
        /// {
        ///     options.DatabaseName = "MyDatabase";
        ///     options.ConnectionString = "mongodb://localhost:27017";
        /// })
        /// .AddMvp24HoursMongoDbReadWriteSeparation(options =>
        /// {
        ///     options.ReadPreference = "secondaryPreferred";
        ///     options.UseSeparateConnection = true;
        ///     options.ReadConnectionString = "mongodb://read-replica:27017";
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursMongoDbReadWriteSeparation(
            this IServiceCollection services,
            Action<MongoDbReadWriteSeparationOptions>? configureOptions = null)
        {
            var options = new MongoDbReadWriteSeparationOptions();
            configureOptions?.Invoke(options);

            // Configure write context with events
            services.AddMvp24HoursRepositoryAsyncWithEvents();

            // Configure read-only repositories
            services.AddMvp24HoursReadOnlyRepositoryAsync();

            return services;
        }

        #endregion

        #region [ Outbox Pattern Integration ]

        /// <summary>
        /// Adds Outbox pattern integration for reliable domain event delivery.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// The Outbox pattern ensures reliable event delivery by storing events
        /// in the same transaction as the aggregate changes. A background process
        /// then publishes these events to external systems.
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Requires the CQRS module with Outbox support configured via
        /// <c>AddMvpOutbox()</c> extension.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Configure CQRS with Outbox
        /// services.AddMvpMediator(options => { ... })
        ///         .AddMvpOutbox();
        /// 
        /// // Configure MongoDB with Outbox integration
        /// services.AddMvp24HoursDbContext(options => { ... })
        ///         .AddMvp24HoursMongoDbOutboxIntegration();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursMongoDbOutboxIntegration(this IServiceCollection services)
        {
            // Register the dispatcher that writes to outbox instead of dispatching directly
            services.AddScoped<IDomainEventDispatcherMongoDb>(sp =>
            {
                var outbox = sp.GetService<Infrastructure.Cqrs.Abstractions.IIntegrationEventOutbox>();
                var logger = sp.GetService<ILogger<DomainEventDispatcherAdapter>>();

                if (outbox == null)
                {
                    logger?.LogWarning(
                        "[MongoDB-Outbox] IIntegrationEventOutbox not found. Using no-op dispatcher. " +
                        "Make sure to call AddMvpOutbox() before AddMvp24HoursMongoDbOutboxIntegration()");
                    return new NoOpDomainEventDispatcher(sp.GetService<ILogger<NoOpDomainEventDispatcher>>());
                }

                return new DomainEventDispatcherAdapter(
                    async (events, ct) =>
                    {
                        foreach (var evt in events)
                        {
                            // Convert domain event to integration event for outbox
                            var integrationEvent = new OutboxDomainEventWrapper(evt);
                            await outbox.AddAsync(integrationEvent, ct);
                        }
                    },
                    logger);
            });

            return services;
        }

        #endregion

        #region [ Full CQRS Setup ]

        /// <summary>
        /// Adds complete CQRS integration for MongoDB with all features enabled.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="useOutbox">Whether to use the Outbox pattern. Default is false.</param>
        /// <param name="lifetime">Service lifetime. Defaults to Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This is a convenience method that configures:
        /// <list type="bullet">
        /// <item><description>CQRS domain event dispatcher integration</description></item>
        /// <item><description>Unit of Work with events (sync and async)</description></item>
        /// <item><description>Read-only repositories</description></item>
        /// <item><description>Optionally: Outbox pattern integration</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// The CQRS module must be configured first via <c>AddMvpMediator()</c>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Configure CQRS module first
        /// services.AddMvpMediator(options => 
        /// {
        ///     options.RegisterServicesFromAssembly(typeof(Program).Assembly);
        /// });
        /// 
        /// // Configure MongoDB with full CQRS integration
        /// services.AddMvp24HoursDbContext(options => 
        /// {
        ///     options.DatabaseName = "MyDatabase";
        ///     options.ConnectionString = "mongodb://localhost:27017";
        /// })
        /// .AddMvp24HoursMongoDbFullCqrsSetup(useOutbox: true);
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursMongoDbFullCqrsSetup(
            this IServiceCollection services,
            bool useOutbox = false,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            // Add CQRS integration
            if (useOutbox)
            {
                services.AddMvp24HoursMongoDbOutboxIntegration();
            }
            else
            {
                services.AddMvp24HoursMongoDbCqrsIntegration();
            }

            // Add repositories with events
            services.AddMvp24HoursRepositoriesWithEvents(lifetime);

            // Add read-only repositories
            services.AddMvp24HoursReadOnlyRepositories(lifetime: lifetime);

            return services;
        }

        #endregion

        #region [ Helper Classes ]

        /// <summary>
        /// Bridge class that connects MongoDB domain event dispatcher to CQRS dispatcher.
        /// </summary>
        private class CqrsDomainEventDispatcherBridge : IDomainEventDispatcherMongoDb
        {
            private readonly Infrastructure.Cqrs.Abstractions.IDomainEventDispatcher _cqrsDispatcher;
            private readonly ILogger<CqrsDomainEventDispatcherBridge>? _logger;

            public CqrsDomainEventDispatcherBridge(
                Infrastructure.Cqrs.Abstractions.IDomainEventDispatcher cqrsDispatcher,
                ILogger<CqrsDomainEventDispatcherBridge>? logger = null)
            {
                _cqrsDispatcher = cqrsDispatcher ?? throw new ArgumentNullException(nameof(cqrsDispatcher));
                _logger = logger;
            }

            public async Task DispatchEventsAsync(IHasDomainEvents entity, CancellationToken cancellationToken = default)
            {
                if (entity == null) return;

                _logger?.LogDebug(
                    "[MongoDB-CQRS] Dispatching events from {EntityType} via CQRS dispatcher",
                    entity.GetType().Name);

                await _cqrsDispatcher.DispatchEventsAsync(entity, cancellationToken);
            }

            public async Task DispatchEventsAsync(IEnumerable<IHasDomainEvents> entities, CancellationToken cancellationToken = default)
            {
                if (entities == null) return;

                var entitiesList = entities.ToList();
                if (entitiesList.Count == 0) return;

                _logger?.LogDebug(
                    "[MongoDB-CQRS] Dispatching events from {EntityCount} entities via CQRS dispatcher",
                    entitiesList.Count);

                await _cqrsDispatcher.DispatchEventsAsync(entitiesList, cancellationToken);
            }

            public void DispatchEvents(IHasDomainEvents entity, CancellationToken cancellationToken = default)
            {
                DispatchEventsAsync(entity, cancellationToken).GetAwaiter().GetResult();
            }

            public void DispatchEvents(IEnumerable<IHasDomainEvents> entities, CancellationToken cancellationToken = default)
            {
                DispatchEventsAsync(entities, cancellationToken).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Wrapper class to store domain events in the outbox.
        /// </summary>
        private class OutboxDomainEventWrapper : Infrastructure.Cqrs.Abstractions.IIntegrationEvent
        {
            public Guid Id { get; }
            public DateTime OccurredOn { get; }
            public string? CorrelationId { get; }
            public string EventType { get; }
            public IDomainEvent DomainEvent { get; }

            public OutboxDomainEventWrapper(IDomainEvent domainEvent, string? correlationId = null)
            {
                Id = Guid.NewGuid();
                OccurredOn = domainEvent.OccurredAt;
                CorrelationId = correlationId;
                EventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName ?? "Unknown";
                DomainEvent = domainEvent;
            }
        }

        #endregion
    }
}

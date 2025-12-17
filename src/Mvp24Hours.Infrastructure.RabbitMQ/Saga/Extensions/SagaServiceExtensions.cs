//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Persistence;
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga.Extensions
{
    /// <summary>
    /// Extension methods for registering saga services in the DI container.
    /// </summary>
    public static class SagaServiceExtensions
    {
        /// <summary>
        /// Adds saga support with in-memory persistence.
        /// Suitable for testing and development.
        /// </summary>
        /// <typeparam name="TData">The type of saga data.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSagaInMemory<TData>(this IServiceCollection services)
            where TData : class, new()
        {
            services.TryAddSingleton<ISagaRepository<TData>, InMemorySagaRepository<TData>>();
            return services;
        }

        /// <summary>
        /// Adds saga support with Redis persistence.
        /// </summary>
        /// <typeparam name="TData">The type of saga data.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSagaRedis<TData>(
            this IServiceCollection services,
            Action<RedisSagaRepositoryOptions>? configureOptions = null)
            where TData : class, new()
        {
            var options = new RedisSagaRepositoryOptions();
            configureOptions?.Invoke(options);

            services.TryAddSingleton(options);
            services.TryAddScoped<ISagaRepository<TData>, RedisSagaRepository<TData>>();

            return services;
        }

        /// <summary>
        /// Adds saga support with EF Core persistence.
        /// Requires ISagaDbContext to be registered.
        /// </summary>
        /// <typeparam name="TData">The type of saga data.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSagaEFCore<TData>(this IServiceCollection services)
            where TData : class, new()
        {
            services.TryAddScoped<ISagaRepository<TData>, EFCoreSagaRepository<TData>>();
            return services;
        }

        /// <summary>
        /// Adds saga support with MongoDB persistence.
        /// Requires IMongoSagaCollection to be registered.
        /// </summary>
        /// <typeparam name="TData">The type of saga data.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSagaMongoDB<TData>(this IServiceCollection services)
            where TData : class, new()
        {
            services.TryAddScoped<ISagaRepository<TData>, MongoDbSagaRepository<TData>>();
            return services;
        }

        /// <summary>
        /// Adds a saga consumer to the service collection.
        /// </summary>
        /// <typeparam name="TData">The type of saga data.</typeparam>
        /// <typeparam name="TMessage">The type of message.</typeparam>
        /// <typeparam name="TConsumer">The type of saga consumer.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSagaConsumer<TData, TMessage, TConsumer>(this IServiceCollection services)
            where TData : class, new()
            where TMessage : class
            where TConsumer : class, ISagaConsumer<TData, TMessage>
        {
            services.TryAddScoped<TConsumer>();
            services.TryAddScoped<SagaMessageConsumerAdapter<TData, TMessage, TConsumer>>();
            services.TryAddScoped<SagaConsumerProcessor<TData, TMessage, TConsumer>>();

            return services;
        }

        /// <summary>
        /// Adds a saga state machine to the service collection.
        /// </summary>
        /// <typeparam name="TData">The type of saga data.</typeparam>
        /// <typeparam name="TMachine">The type of saga state machine.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSagaStateMachine<TData, TMachine>(this IServiceCollection services)
            where TData : class, new()
            where TMachine : SagaStateMachine<TData>
        {
            services.TryAddScoped<TMachine>();
            return services;
        }

        /// <summary>
        /// Adds a saga state machine consumer to the service collection.
        /// </summary>
        /// <typeparam name="TData">The type of saga data.</typeparam>
        /// <typeparam name="TMessage">The type of message.</typeparam>
        /// <typeparam name="TMachine">The type of saga state machine.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSagaStateMachineConsumer<TData, TMessage, TMachine>(this IServiceCollection services)
            where TData : class, new()
            where TMessage : class
            where TMachine : SagaStateMachine<TData>
        {
            services.TryAddScoped<TMachine>();
            services.TryAddScoped<SagaStateMachineConsumer<TData, TMessage, TMachine>>();

            return services;
        }

        /// <summary>
        /// Adds all saga services for a saga with multiple events.
        /// </summary>
        /// <typeparam name="TData">The type of saga data.</typeparam>
        /// <typeparam name="TMachine">The type of saga state machine.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configuration action for saga options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSaga<TData, TMachine>(
            this IServiceCollection services,
            Action<SagaOptions<TData>>? configureOptions = null)
            where TData : class, new()
            where TMachine : SagaStateMachine<TData>
        {
            var options = new SagaOptions<TData>();
            configureOptions?.Invoke(options);

            // Register state machine
            services.AddSagaStateMachine<TData, TMachine>();

            // Register persistence based on options
            switch (options.PersistenceType)
            {
                case SagaPersistenceType.InMemory:
                    services.AddSagaInMemory<TData>();
                    break;
                case SagaPersistenceType.Redis:
                    services.AddSagaRedis<TData>(o =>
                    {
                        o.DefaultExpiration = options.DefaultExpiration;
                        o.CompletedExpiration = options.CompletedExpiration;
                    });
                    break;
                case SagaPersistenceType.EFCore:
                    services.AddSagaEFCore<TData>();
                    break;
                case SagaPersistenceType.MongoDB:
                    services.AddSagaMongoDB<TData>();
                    break;
            }

            return services;
        }
    }

    /// <summary>
    /// Configuration options for saga registration.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    public class SagaOptions<TData> where TData : class, new()
    {
        /// <summary>
        /// Gets or sets the persistence type.
        /// Default: InMemory.
        /// </summary>
        public SagaPersistenceType PersistenceType { get; set; } = SagaPersistenceType.InMemory;

        /// <summary>
        /// Gets or sets the default expiration for active sagas.
        /// Default: 24 hours.
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Gets or sets the expiration for completed sagas.
        /// Default: 1 hour.
        /// </summary>
        public TimeSpan CompletedExpiration { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets whether to enable timeout scheduling.
        /// Default: true.
        /// </summary>
        public bool EnableTimeouts { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout check interval.
        /// Default: 1 minute.
        /// </summary>
        public TimeSpan TimeoutCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Saga persistence types.
    /// </summary>
    public enum SagaPersistenceType
    {
        /// <summary>
        /// In-memory persistence (for testing).
        /// </summary>
        InMemory,

        /// <summary>
        /// Redis-based persistence.
        /// </summary>
        Redis,

        /// <summary>
        /// Entity Framework Core-based persistence.
        /// </summary>
        EFCore,

        /// <summary>
        /// MongoDB-based persistence.
        /// </summary>
        MongoDB
    }
}


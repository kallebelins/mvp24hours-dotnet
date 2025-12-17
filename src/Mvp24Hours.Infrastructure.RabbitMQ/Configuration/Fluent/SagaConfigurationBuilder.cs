//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Persistence;
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration.Fluent
{
    /// <summary>
    /// Builder for configuring saga state machines.
    /// </summary>
    /// <typeparam name="TInstance">The type of saga instance data.</typeparam>
    /// <remarks>
    /// <para>
    /// A saga is a long-running business transaction that spans multiple services
    /// and handles failures through compensating transactions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// cfg.AddSaga&lt;OrderSaga, OrderSagaData&gt;(s =>
    /// {
    ///     s.UseRedis(options =>
    ///     {
    ///         options.DefaultExpiration = TimeSpan.FromHours(24);
    ///     });
    ///     s.EnableTimeouts();
    /// });
    /// </code>
    /// </example>
    public class SagaConfigurationBuilder<TInstance> where TInstance : class, new()
    {
        /// <summary>
        /// Gets or sets the persistence type.
        /// </summary>
        internal SagaPersistenceType PersistenceType { get; private set; } = SagaPersistenceType.InMemory;

        /// <summary>
        /// Gets or sets the default expiration for active sagas.
        /// </summary>
        internal TimeSpan DefaultExpiration { get; private set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Gets or sets the expiration for completed sagas.
        /// </summary>
        internal TimeSpan CompletedExpiration { get; private set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets whether to enable timeout scheduling.
        /// </summary>
        internal bool EnableTimeouts { get; private set; } = true;

        /// <summary>
        /// Gets or sets the timeout check interval.
        /// </summary>
        internal TimeSpan TimeoutCheckInterval { get; private set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the Redis options (when using Redis persistence).
        /// </summary>
        internal RedisSagaRepositoryOptions? RedisOptions { get; private set; }

        /// <summary>
        /// Configures the saga to use in-memory persistence.
        /// Suitable for testing and development.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// <strong>Warning:</strong> In-memory persistence is not suitable for production
        /// as saga state will be lost on application restart.
        /// </para>
        /// </remarks>
        public SagaConfigurationBuilder<TInstance> UseInMemory()
        {
            PersistenceType = SagaPersistenceType.InMemory;
            return this;
        }

        /// <summary>
        /// Configures the saga to use Redis persistence.
        /// </summary>
        /// <param name="configure">Optional configuration for Redis options.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// s.UseRedis(options =>
        /// {
        ///     options.DefaultExpiration = TimeSpan.FromHours(24);
        ///     options.CompletedExpiration = TimeSpan.FromHours(1);
        ///     options.KeyPrefix = "saga:order:";
        /// });
        /// </code>
        /// </example>
        public SagaConfigurationBuilder<TInstance> UseRedis(Action<RedisSagaRepositoryOptions>? configure = null)
        {
            PersistenceType = SagaPersistenceType.Redis;

            RedisOptions = new RedisSagaRepositoryOptions
            {
                DefaultExpiration = DefaultExpiration,
                CompletedExpiration = CompletedExpiration
            };

            configure?.Invoke(RedisOptions);

            return this;
        }

        /// <summary>
        /// Configures the saga to use Entity Framework Core persistence.
        /// Requires ISagaDbContext to be registered.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Ensure that your DbContext implements ISagaDbContext and has the
        /// required DbSet&lt;SagaState&gt; configured.
        /// </para>
        /// </remarks>
        public SagaConfigurationBuilder<TInstance> UseEntityFramework()
        {
            PersistenceType = SagaPersistenceType.EFCore;
            return this;
        }

        /// <summary>
        /// Configures the saga to use MongoDB persistence.
        /// Requires IMongoSagaCollection to be registered.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        public SagaConfigurationBuilder<TInstance> UseMongoDB()
        {
            PersistenceType = SagaPersistenceType.MongoDB;
            return this;
        }

        /// <summary>
        /// Sets the default expiration time for active sagas.
        /// Default is 24 hours.
        /// </summary>
        /// <param name="expiration">The expiration time.</param>
        /// <returns>The builder for chaining.</returns>
        public SagaConfigurationBuilder<TInstance> SetDefaultExpiration(TimeSpan expiration)
        {
            DefaultExpiration = expiration;
            return this;
        }

        /// <summary>
        /// Sets the expiration time for completed sagas.
        /// Default is 1 hour.
        /// </summary>
        /// <param name="expiration">The expiration time.</param>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Completed sagas are kept for auditing and debugging purposes.
        /// Set to TimeSpan.Zero to delete immediately after completion.
        /// </para>
        /// </remarks>
        public SagaConfigurationBuilder<TInstance> SetCompletedExpiration(TimeSpan expiration)
        {
            CompletedExpiration = expiration;
            return this;
        }

        /// <summary>
        /// Enables timeout scheduling for the saga.
        /// Default is true.
        /// </summary>
        /// <param name="checkInterval">The interval for checking timeouts. Default is 1 minute.</param>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// When enabled, the saga can schedule timeout events that will be
        /// delivered after a specified delay.
        /// </para>
        /// </remarks>
        public SagaConfigurationBuilder<TInstance> WithTimeouts(TimeSpan? checkInterval = null)
        {
            EnableTimeouts = true;
            if (checkInterval.HasValue)
            {
                TimeoutCheckInterval = checkInterval.Value;
            }
            return this;
        }

        /// <summary>
        /// Disables timeout scheduling for the saga.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        public SagaConfigurationBuilder<TInstance> WithoutTimeouts()
        {
            EnableTimeouts = false;
            return this;
        }

        /// <summary>
        /// Sets the interval for checking saga timeouts.
        /// Default is 1 minute.
        /// </summary>
        /// <param name="interval">The check interval.</param>
        /// <returns>The builder for chaining.</returns>
        public SagaConfigurationBuilder<TInstance> SetTimeoutCheckInterval(TimeSpan interval)
        {
            TimeoutCheckInterval = interval;
            return this;
        }

        /// <summary>
        /// Configures the saga for high availability.
        /// Uses longer expirations and more frequent timeout checks.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        public SagaConfigurationBuilder<TInstance> HighAvailability()
        {
            DefaultExpiration = TimeSpan.FromDays(7);
            CompletedExpiration = TimeSpan.FromDays(1);
            TimeoutCheckInterval = TimeSpan.FromSeconds(30);
            return this;
        }

        /// <summary>
        /// Configures the saga for short-lived transactions.
        /// Uses shorter expirations.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        public SagaConfigurationBuilder<TInstance> ShortLived()
        {
            DefaultExpiration = TimeSpan.FromHours(1);
            CompletedExpiration = TimeSpan.FromMinutes(5);
            TimeoutCheckInterval = TimeSpan.FromSeconds(10);
            return this;
        }
    }

    /// <summary>
    /// Base class for saga configuration builders (non-generic).
    /// </summary>
    public abstract class SagaConfigurationBuilderBase
    {
        /// <summary>
        /// Gets the persistence type.
        /// </summary>
        public abstract SagaPersistenceType PersistenceType { get; }

        /// <summary>
        /// Gets the default expiration.
        /// </summary>
        public abstract TimeSpan DefaultExpiration { get; }

        /// <summary>
        /// Gets the completed expiration.
        /// </summary>
        public abstract TimeSpan CompletedExpiration { get; }

        /// <summary>
        /// Gets whether timeouts are enabled.
        /// </summary>
        public abstract bool EnableTimeoutsFlag { get; }

        /// <summary>
        /// Gets the timeout check interval.
        /// </summary>
        public abstract TimeSpan TimeoutCheckInterval { get; }
    }
}


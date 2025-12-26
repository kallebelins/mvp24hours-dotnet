//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Providers;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.DistributedLocking.Extensions
{
    /// <summary>
    /// Extension methods for registering distributed locking services.
    /// </summary>
    public static class DistributedLockingServiceExtensions
    {
        /// <summary>
        /// Adds distributed locking services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method registers the <see cref="IDistributedLockFactory"/> and allows
        /// configuration of multiple providers. At least one provider must be added.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddDistributedLocking(builder =>
        /// {
        ///     builder.AddRedisProvider("Redis", connectionMultiplexer);
        ///     builder.AddInMemoryProvider("InMemory");
        ///     builder.SetDefaultProvider("Redis");
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddDistributedLocking(
            this IServiceCollection services,
            Action<IDistributedLockingBuilder>? configure = null)
        {
            // Register metrics as singleton
            services.AddSingleton<Metrics.DistributedLockMetrics>();

            var builder = new DistributedLockingBuilder(services);
            configure?.Invoke(builder);

            // Register factory
            services.AddSingleton<IDistributedLockFactory>(serviceProvider =>
            {
                var providers = builder.BuildProviders(serviceProvider);
                return new DistributedLockFactory(providers, builder.DefaultProviderName);
            });

            return services;
        }

        /// <summary>
        /// Adds an in-memory distributed lock provider.
        /// </summary>
        /// <param name="builder">The distributed locking builder.</param>
        /// <param name="name">The provider name.</param>
        /// <returns>The builder for chaining.</returns>
        public static IDistributedLockingBuilder AddInMemoryProvider(
            this IDistributedLockingBuilder builder,
            string name = "InMemory")
        {
            builder.RegisterProvider(name, serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<InMemoryDistributedLockProvider>>();
                var metrics = serviceProvider.GetService<Metrics.DistributedLockMetrics>();
                return new InMemoryDistributedLockProvider(logger, metrics);
            });

            return builder;
        }

        /// <summary>
        /// Adds a Redis distributed lock provider.
        /// </summary>
        /// <param name="builder">The distributed locking builder.</param>
        /// <param name="name">The provider name.</param>
        /// <param name="redisConnection">The Redis connection multiplexer.</param>
        /// <returns>The builder for chaining.</returns>
        public static IDistributedLockingBuilder AddRedisProvider(
            this IDistributedLockingBuilder builder,
            string name,
            IConnectionMultiplexer redisConnection)
        {
            if (redisConnection == null)
                throw new ArgumentNullException(nameof(redisConnection));

            builder.RegisterProvider(name, serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<RedisDistributedLockProvider>>();
                var metrics = serviceProvider.GetService<Metrics.DistributedLockMetrics>();
                return new RedisDistributedLockProvider(redisConnection, logger, metrics);
            });

            return builder;
        }

        /// <summary>
        /// Adds a Redis RedLock distributed lock provider (multiple Redis instances).
        /// </summary>
        /// <param name="builder">The distributed locking builder.</param>
        /// <param name="name">The provider name.</param>
        /// <param name="redisConnections">Array of Redis connection multiplexers.</param>
        /// <returns>The builder for chaining.</returns>
        public static IDistributedLockingBuilder AddRedisRedLockProvider(
            this IDistributedLockingBuilder builder,
            string name,
            IConnectionMultiplexer[] redisConnections)
        {
            if (redisConnections == null || redisConnections.Length == 0)
                throw new ArgumentException("At least one Redis connection is required.", nameof(redisConnections));

            builder.RegisterProvider(name, serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<RedisDistributedLockProvider>>();
                var metrics = serviceProvider.GetService<Metrics.DistributedLockMetrics>();
                return new RedisDistributedLockProvider(redisConnections, logger, metrics);
            });

            return builder;
        }

        /// <summary>
        /// Adds a SQL Server distributed lock provider.
        /// </summary>
        /// <param name="builder">The distributed locking builder.</param>
        /// <param name="name">The provider name.</param>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="lockOwner">Lock owner: "Session" (default) or "Transaction".</param>
        /// <param name="lockMode">Lock mode: "Exclusive" (default), "Shared", "Update", etc.</param>
        /// <returns>The builder for chaining.</returns>
        public static IDistributedLockingBuilder AddSqlServerProvider(
            this IDistributedLockingBuilder builder,
            string name,
            string connectionString,
            string lockOwner = "Session",
            string lockMode = "Exclusive")
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            builder.RegisterProvider(name, serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<SqlServerDistributedLockProvider>>();
                var metrics = serviceProvider.GetService<Metrics.DistributedLockMetrics>();
                return new SqlServerDistributedLockProvider(connectionString, logger, metrics, lockOwner, lockMode);
            });

            return builder;
        }

        /// <summary>
        /// Adds a PostgreSQL distributed lock provider.
        /// </summary>
        /// <param name="builder">The distributed locking builder.</param>
        /// <param name="name">The provider name.</param>
        /// <param name="connectionString">PostgreSQL connection string.</param>
        /// <param name="useSharedLock">Whether to use shared locks instead of exclusive locks.</param>
        /// <returns>The builder for chaining.</returns>
        public static IDistributedLockingBuilder AddPostgreSqlProvider(
            this IDistributedLockingBuilder builder,
            string name,
            string connectionString,
            bool useSharedLock = false)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            builder.RegisterProvider(name, serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<PostgreSqlDistributedLockProvider>>();
                var metrics = serviceProvider.GetService<Metrics.DistributedLockMetrics>();
                return new PostgreSqlDistributedLockProvider(connectionString, logger, metrics, useSharedLock);
            });

            return builder;
        }

        /// <summary>
        /// Sets the default provider name.
        /// </summary>
        /// <param name="builder">The distributed locking builder.</param>
        /// <param name="providerName">The default provider name.</param>
        /// <returns>The builder for chaining.</returns>
        public static IDistributedLockingBuilder SetDefaultProvider(
            this IDistributedLockingBuilder builder,
            string providerName)
        {
            builder.DefaultProviderName = providerName;
            return builder;
        }
    }

    /// <summary>
    /// Builder interface for configuring distributed locking.
    /// </summary>
    public interface IDistributedLockingBuilder
    {
        /// <summary>
        /// Gets the service collection.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Gets or sets the default provider name.
        /// </summary>
        string? DefaultProviderName { get; set; }

        /// <summary>
        /// Registers a provider with a factory function.
        /// </summary>
        /// <param name="name">The provider name.</param>
        /// <param name="factory">Factory function to create the provider.</param>
        void RegisterProvider(string name, Func<IServiceProvider, IDistributedLock> factory);
    }

    /// <summary>
    /// Builder implementation for configuring distributed locking.
    /// </summary>
    internal class DistributedLockingBuilder : IDistributedLockingBuilder
    {
        private readonly Dictionary<string, Func<IServiceProvider, IDistributedLock>> _providerFactories = new();

        public IServiceCollection Services { get; }
        public string? DefaultProviderName { get; set; }

        public DistributedLockingBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public void RegisterProvider(string name, Func<IServiceProvider, IDistributedLock> factory)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Provider name cannot be null or empty.", nameof(name));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _providerFactories[name] = factory;
        }

        internal Dictionary<string, IDistributedLock> BuildProviders(IServiceProvider serviceProvider)
        {
            var providers = new Dictionary<string, IDistributedLock>();

            foreach (var (name, factory) in _providerFactories)
            {
                var provider = factory(serviceProvider);
                providers[name] = provider;
            }

            return providers;
        }
    }
}


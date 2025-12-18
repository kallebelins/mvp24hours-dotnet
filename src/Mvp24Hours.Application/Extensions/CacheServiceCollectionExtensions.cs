//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Application.Contract.Cache;
using Mvp24Hours.Application.Logic.Cache;
using System;

namespace Mvp24Hours.Application.Extensions
{
    /// <summary>
    /// Extension methods for configuring query cache services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class CacheServiceCollectionExtensions
    {
        /// <summary>
        /// Adds query cache services to the service collection.
        /// Includes cache provider, key generator, and cache invalidator.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers:
        /// <list type="bullet">
        /// <item><see cref="IQueryCacheProvider"/> - Default implementation using IDistributedCache</item>
        /// <item><see cref="IQueryCacheKeyGenerator"/> - Default key generation strategy</item>
        /// <item><see cref="ICacheInvalidator"/> - Automatic cache invalidation service</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// You must register either:
        /// <list type="bullet">
        /// <item><c>services.AddDistributedMemoryCache()</c> for in-memory distributed cache</item>
        /// <item><c>services.AddStackExchangeRedisCache(...)</c> for Redis cache</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Example usage:</strong>
        /// <code>
        /// services.AddDistributedMemoryCache();
        /// services.AddMvpApplicationQueryCache();
        /// </code>
        /// </para>
        /// </remarks>
        public static IServiceCollection AddMvpApplicationQueryCache(this IServiceCollection services)
        {
            return services.AddMvpApplicationQueryCache(options => { });
        }

        /// <summary>
        /// Adds query cache services to the service collection with custom options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure cache options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// <strong>Example usage:</strong>
        /// <code>
        /// services.AddMvpApplicationQueryCache(options =>
        /// {
        ///     options.EnableL1Cache = true;
        ///     options.L1CacheDuration = TimeSpan.FromMinutes(2);
        ///     options.DefaultDuration = TimeSpan.FromMinutes(10);
        ///     options.KeyPrefix = "myapp:query:";
        /// });
        /// </code>
        /// </para>
        /// </remarks>
        public static IServiceCollection AddMvpApplicationQueryCache(
            this IServiceCollection services,
            Action<QueryCacheOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            // Configure options
            services.Configure(configureOptions);

            // Register core cache services
            services.TryAddSingleton<IQueryCacheKeyGenerator, QueryCacheKeyGenerator>();
            services.TryAddScoped<IQueryCacheProvider, QueryCacheProvider>();
            services.TryAddScoped<ICacheInvalidator, CacheInvalidator>();

            return services;
        }

        /// <summary>
        /// Adds query cache services with memory cache as L1 and distributed cache as L2.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure cache options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method enables hybrid caching with:
        /// <list type="bullet">
        /// <item>L1: In-memory cache for fast local access</item>
        /// <item>L2: Distributed cache for cross-instance consistency</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Example usage:</strong>
        /// <code>
        /// services.AddMemoryCache();
        /// services.AddStackExchangeRedisCache(options => options.Configuration = "localhost");
        /// services.AddMvpApplicationQueryCacheHybrid();
        /// </code>
        /// </para>
        /// </remarks>
        public static IServiceCollection AddMvpApplicationQueryCacheHybrid(
            this IServiceCollection services,
            Action<QueryCacheOptions>? configureOptions = null)
        {
            // Ensure memory cache is available
            services.AddMemoryCache();

            return services.AddMvpApplicationQueryCache(options =>
            {
                options.EnableL1Cache = true;
                options.L1CacheDuration = TimeSpan.FromMinutes(1);
                configureOptions?.Invoke(options);
            });
        }

        /// <summary>
        /// Adds query cache services optimized for distributed environments.
        /// Disables L1 cache to ensure consistency across instances.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure cache options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Use this method when running multiple instances of your application
        /// and cache consistency is more important than performance.
        /// </para>
        /// <para>
        /// <strong>Example usage:</strong>
        /// <code>
        /// services.AddStackExchangeRedisCache(options => options.Configuration = "localhost");
        /// services.AddMvpApplicationQueryCacheDistributed();
        /// </code>
        /// </para>
        /// </remarks>
        public static IServiceCollection AddMvpApplicationQueryCacheDistributed(
            this IServiceCollection services,
            Action<QueryCacheOptions>? configureOptions = null)
        {
            return services.AddMvpApplicationQueryCache(options =>
            {
                options.EnableL1Cache = false; // Disable L1 for distributed consistency
                configureOptions?.Invoke(options);
            });
        }

        /// <summary>
        /// Replaces the default cache key generator with a custom implementation.
        /// </summary>
        /// <typeparam name="TGenerator">The custom key generator type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection UseCacheKeyGenerator<TGenerator>(this IServiceCollection services)
            where TGenerator : class, IQueryCacheKeyGenerator
        {
            services.RemoveAll<IQueryCacheKeyGenerator>();
            services.AddSingleton<IQueryCacheKeyGenerator, TGenerator>();
            return services;
        }

        /// <summary>
        /// Replaces the default cache provider with a custom implementation.
        /// </summary>
        /// <typeparam name="TProvider">The custom cache provider type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection UseCacheProvider<TProvider>(this IServiceCollection services)
            where TProvider : class, IQueryCacheProvider
        {
            services.RemoveAll<IQueryCacheProvider>();
            services.AddScoped<IQueryCacheProvider, TProvider>();
            return services;
        }

        /// <summary>
        /// Replaces the default cache invalidator with a custom implementation.
        /// </summary>
        /// <typeparam name="TInvalidator">The custom cache invalidator type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection UseCacheInvalidator<TInvalidator>(this IServiceCollection services)
            where TInvalidator : class, ICacheInvalidator
        {
            services.RemoveAll<ICacheInvalidator>();
            services.AddScoped<ICacheInvalidator, TInvalidator>();
            return services;
        }
    }
}


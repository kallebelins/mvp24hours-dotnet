//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;

namespace Mvp24Hours.Infrastructure.Caching.HybridCache
{
    /// <summary>
    /// Extension methods for configuring HybridCache services.
    /// </summary>
    /// <remarks>
    /// <para>
    /// HybridCache is the .NET 9+ native solution for multi-level caching that combines:
    /// <list type="bullet">
    /// <item><strong>L1 (In-Memory):</strong> Fast, local cache per application instance</item>
    /// <item><strong>L2 (Distributed):</strong> Shared cache via IDistributedCache (Redis, SQL, etc.)</item>
    /// <item><strong>Stampede Protection:</strong> Automatic prevention of cache stampedes</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Recommended over custom MultiLevelCache:</strong>
    /// HybridCache provides better performance, simpler API, and native .NET integration.
    /// </para>
    /// </remarks>
    public static class HybridCacheServiceExtensions
    {
        /// <summary>
        /// Adds HybridCache services with default configuration.
        /// Uses in-memory cache only (no distributed cache).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvpHybridCache();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvpHybridCache(this IServiceCollection services)
        {
            return services.AddMvpHybridCache(_ => { });
        }

        /// <summary>
        /// Adds HybridCache services with custom configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvpHybridCache(options =>
        /// {
        ///     options.DefaultExpiration = TimeSpan.FromMinutes(10);
        ///     options.UseRedisAsL2 = true;
        ///     options.RedisConnectionString = "localhost:6379";
        ///     options.EnableStampedeProtection = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvpHybridCache(
            this IServiceCollection services,
            Action<MvpHybridCacheOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new MvpHybridCacheOptions();
            configure(options);

            // Register options
            services.Configure<MvpHybridCacheOptions>(opt =>
            {
                opt.DefaultExpiration = options.DefaultExpiration;
                opt.DefaultLocalCacheExpiration = options.DefaultLocalCacheExpiration;
                opt.MaximumPayloadBytes = options.MaximumPayloadBytes;
                opt.MaximumKeyLength = options.MaximumKeyLength;
                opt.UseRedisAsL2 = options.UseRedisAsL2;
                opt.RedisConnectionString = options.RedisConnectionString;
                opt.RedisInstanceName = options.RedisInstanceName;
                opt.EnableStampedeProtection = options.EnableStampedeProtection;
                opt.ReportTagStatistics = options.ReportTagStatistics;
                opt.DefaultTags = options.DefaultTags;
                opt.EnableCompression = options.EnableCompression;
                opt.CompressionThresholdBytes = options.CompressionThresholdBytes;
                opt.EnableDetailedLogging = options.EnableDetailedLogging;
                opt.KeyPrefix = options.KeyPrefix;
                opt.SerializerType = options.SerializerType;
                opt.SerializerOptions = options.SerializerOptions;
            });

            // Add Redis as L2 if configured
            if (options.UseRedisAsL2 && !string.IsNullOrEmpty(options.RedisConnectionString))
            {
                services.AddStackExchangeRedisCache(redisOptions =>
                {
                    redisOptions.Configuration = options.RedisConnectionString;
                    redisOptions.InstanceName = options.RedisInstanceName;
                });
            }

            // Add HybridCache with configuration
#pragma warning disable EXTEXP0018 // HybridCache is experimental in .NET 9
            services.AddHybridCache(hybridOptions =>
            {
                hybridOptions.MaximumPayloadBytes = options.MaximumPayloadBytes;
                hybridOptions.MaximumKeyLength = options.MaximumKeyLength;
                hybridOptions.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = options.DefaultExpiration,
                    LocalCacheExpiration = options.DefaultLocalCacheExpiration ?? options.DefaultExpiration
                };
            });
#pragma warning restore EXTEXP0018

            // Register tag manager
            services.TryAddSingleton<IHybridCacheTagManager, InMemoryHybridCacheTagManager>();

            // Register HybridCacheProvider as ICacheProvider
            services.TryAddSingleton<ICacheProvider, HybridCacheProvider>();

            return services;
        }

        /// <summary>
        /// Adds HybridCache services with Redis as L2 (distributed) cache.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="redisConnectionString">Redis connection string.</param>
        /// <param name="configure">Optional additional configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvpHybridCacheWithRedis("localhost:6379", options =>
        /// {
        ///     options.DefaultExpiration = TimeSpan.FromMinutes(30);
        ///     options.RedisInstanceName = "myapp:";
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvpHybridCacheWithRedis(
            this IServiceCollection services,
            string redisConnectionString,
            Action<MvpHybridCacheOptions>? configure = null)
        {
            return services.AddMvpHybridCache(options =>
            {
                options.UseRedisAsL2 = true;
                options.RedisConnectionString = redisConnectionString;
                configure?.Invoke(options);
            });
        }

        /// <summary>
        /// Replaces existing ICacheProvider with HybridCacheProvider.
        /// Use this when migrating from MultiLevelCache or other providers.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// // Replace existing MultiLevelCache with HybridCache
        /// services.ReplaceCacheProviderWithHybridCache(options =>
        /// {
        ///     options.UseRedisAsL2 = true;
        ///     options.RedisConnectionString = "localhost:6379";
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection ReplaceCacheProviderWithHybridCache(
            this IServiceCollection services,
            Action<MvpHybridCacheOptions>? configure = null)
        {
            // Remove existing ICacheProvider registrations
            services.RemoveAll<ICacheProvider>();

            // Add HybridCache
            return services.AddMvpHybridCache(configure ?? (_ => { }));
        }

        /// <summary>
        /// Adds a custom tag manager for HybridCache.
        /// </summary>
        /// <typeparam name="TTagManager">The tag manager implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddHybridCacheTagManager<TTagManager>(this IServiceCollection services)
            where TTagManager : class, IHybridCacheTagManager
        {
            services.RemoveAll<IHybridCacheTagManager>();
            services.AddSingleton<IHybridCacheTagManager, TTagManager>();
            return services;
        }
    }
}


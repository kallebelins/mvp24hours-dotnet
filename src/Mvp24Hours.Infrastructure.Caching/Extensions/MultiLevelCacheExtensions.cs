//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Providers;
using Mvp24Hours.Infrastructure.Caching.Synchronization;
using System;

namespace Mvp24Hours.Infrastructure.Caching.Extensions
{
    /// <summary>
    /// Extension methods for registering multi-level cache services.
    /// </summary>
    public static class MultiLevelCacheExtensions
    {
        /// <summary>
        /// Adds multi-level cache (L1 + L2) to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers a multi-level cache that combines:
        /// <list type="bullet">
        /// <item><strong>L1:</strong> IMemoryCache (local, fast)</item>
        /// <item><strong>L2:</strong> IDistributedCache (distributed, shared)</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// <list type="bullet">
        /// <item>IMemoryCache must be registered (via AddMemoryCache())</item>
        /// <item>IDistributedCache must be registered (via AddStackExchangeRedisCache() or similar)</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic setup
        /// services.AddMemoryCache();
        /// services.AddStackExchangeRedisCache(options => 
        /// {
        ///     options.Configuration = "localhost:6379";
        /// });
        /// services.AddMultiLevelCache();
        /// 
        /// // With synchronization
        /// services.AddMultiLevelCache(options =>
        /// {
        ///     options.EnableSynchronization = true;
        ///     options.Synchronizer = new RedisCacheSynchronizer(redisConnection);
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMultiLevelCache(
            this IServiceCollection services,
            Action<MultiLevelCacheOptions>? configure = null)
        {
            var options = new MultiLevelCacheOptions();
            configure?.Invoke(options);

            // Store options for later resolution
            var l1CacheInstance = options.L1Cache;
            var l2CacheInstance = options.L2Cache;
            var synchronizerInstance = options.Synchronizer;

            // Register synchronizer if enabled
            if (options.EnableSynchronization && synchronizerInstance != null)
            {
                services.AddSingleton(synchronizerInstance);
            }
            else if (options.EnableSynchronization)
            {
                // Use in-memory synchronizer as default (for single-instance scenarios)
                services.AddSingleton<ICacheSynchronizer, InMemoryCacheSynchronizer>();
            }

            // Register multi-level cache
            services.AddSingleton<IMultiLevelCache>(sp =>
            {
                ICacheProvider l1Cache;
                if (l1CacheInstance != null)
                {
                    l1Cache = l1CacheInstance;
                }
                else
                {
                    var memoryCache = sp.GetRequiredService<IMemoryCache>();
                    var serializer = options.Serializer ?? sp.GetService<ICacheSerializer>();
                    var logger = sp.GetService<ILogger<MemoryCacheProvider>>();
                    l1Cache = new MemoryCacheProvider(memoryCache, serializer, logger);
                }

                ICacheProvider l2Cache;
                if (l2CacheInstance != null)
                {
                    l2Cache = l2CacheInstance;
                }
                else
                {
                    var distributedCache = sp.GetRequiredService<IDistributedCache>();
                    var serializer = options.Serializer ?? sp.GetService<ICacheSerializer>();
                    var logger = sp.GetService<ILogger<DistributedCacheProvider>>();
                    l2Cache = new DistributedCacheProvider(distributedCache, serializer, logger);
                }

                ICacheSynchronizer? synchronizer = null;
                if (options.EnableSynchronization)
                {
                    synchronizer = synchronizerInstance ?? sp.GetService<ICacheSynchronizer>();
                }

                var logger2 = sp.GetService<ILogger<MultiLevelCache>>();
                return new MultiLevelCache(l1Cache, l2Cache, synchronizer, logger2);
            });

            // Also register as ICacheProvider for backward compatibility
            services.AddSingleton<ICacheProvider>(sp => sp.GetRequiredService<IMultiLevelCache>());

            return services;
        }

        /// <summary>
        /// Adds multi-level cache with in-memory synchronization (for single-instance scenarios).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMultiLevelCacheWithInMemorySync(this IServiceCollection services)
        {
            return services.AddMultiLevelCache(options =>
            {
                options.EnableSynchronization = true;
                options.Synchronizer = null; // Will use InMemoryCacheSynchronizer
            });
        }
    }

    /// <summary>
    /// Configuration options for multi-level cache.
    /// </summary>
    public class MultiLevelCacheOptions
    {
        /// <summary>
        /// Gets or sets the L1 cache provider (memory cache).
        /// If null, a MemoryCacheProvider will be created from IMemoryCache.
        /// </summary>
        public ICacheProvider? L1Cache { get; set; }

        /// <summary>
        /// Gets or sets the L2 cache provider (distributed cache).
        /// If null, a DistributedCacheProvider will be created from IDistributedCache.
        /// </summary>
        public ICacheProvider? L2Cache { get; set; }

        /// <summary>
        /// Gets or sets whether to enable synchronization between instances.
        /// Default is false (single-instance scenario).
        /// </summary>
        public bool EnableSynchronization { get; set; } = false;

        /// <summary>
        /// Gets or sets the cache synchronizer implementation.
        /// If null and EnableSynchronization is true, InMemoryCacheSynchronizer will be used.
        /// </summary>
        public ICacheSynchronizer? Synchronizer { get; set; }

        /// <summary>
        /// Gets or sets the cache serializer.
        /// If null, JsonCacheSerializer will be used.
        /// </summary>
        public ICacheSerializer? Serializer { get; set; }
    }
}


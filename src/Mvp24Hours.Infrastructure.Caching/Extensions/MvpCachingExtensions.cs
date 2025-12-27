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
using Mvp24Hours.Infrastructure.Caching.KeyGenerators;
using Mvp24Hours.Infrastructure.Caching.Providers;
using Mvp24Hours.Infrastructure.Caching.Serializers;
using System;

namespace Mvp24Hours.Infrastructure.Caching.Extensions
{
    /// <summary>
    /// Extension methods for registering Mvp24Hours caching infrastructure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This extension provides a unified way to register all caching services including:
    /// <list type="bullet">
    /// <item>Cache providers (Memory, Distributed, Multi-level)</item>
    /// <item>Cache serializers (JSON, MessagePack)</item>
    /// <item>Cache key generators</item>
    /// <item>Cache patterns (Cache-aside, Read-through, Write-through, etc.)</item>
    /// <item>Cache invalidation services</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup with memory cache
    /// services.AddMvpCaching(options =>
    /// {
    ///     options.UseMemoryCache();
    /// });
    /// 
    /// // Setup with Redis distributed cache
    /// services.AddStackExchangeRedisCache(opts => 
    /// {
    ///     opts.Configuration = "localhost:6379";
    /// });
    /// services.AddMvpCaching(options =>
    /// {
    ///     options.UseDistributedCache();
    /// });
    /// 
    /// // Setup with multi-level cache (L1 + L2)
    /// services.AddMemoryCache();
    /// services.AddStackExchangeRedisCache(opts => 
    /// {
    ///     opts.Configuration = "localhost:6379";
    /// });
    /// services.AddMvpCaching(options =>
    /// {
    ///     options.UseMultiLevelCache();
    /// });
    /// </code>
    /// </example>
    public static class MvpCachingExtensions
    {
        /// <summary>
        /// Adds Mvp24Hours caching infrastructure to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional action to configure caching options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpCaching(
            this IServiceCollection services,
            Action<MvpCachingOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var options = new MvpCachingOptions();
            configure?.Invoke(options);

            // Register serializers
            if (options.Serializer == CacheSerializer.Json)
            {
                services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();
            }
            else if (options.Serializer == CacheSerializer.MessagePack)
            {
                services.AddSingleton<ICacheSerializer, MessagePackCacheSerializer>();
            }
            else
            {
                // Default to JSON
                services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();
            }

            // Register key generator
            services.AddSingleton<ICacheKeyGenerator>(sp =>
            {
                var logger = sp.GetService<ILogger<DefaultCacheKeyGenerator>>();
                return new DefaultCacheKeyGenerator(
                    options.KeyPrefix,
                    options.KeySeparator,
                    logger);
            });

            // Register cache provider based on configuration
            if (options.CacheType == CacheType.Memory)
            {
                RegisterMemoryCache(services, options);
            }
            else if (options.CacheType == CacheType.Distributed)
            {
                RegisterDistributedCache(services, options);
            }
            else if (options.CacheType == CacheType.MultiLevel)
            {
                RegisterMultiLevelCache(services, options);
            }
            else
            {
                // Default to memory cache
                RegisterMemoryCache(services, options);
            }

            return services;
        }

        private static void RegisterMemoryCache(IServiceCollection services, MvpCachingOptions options)
        {
            // Register memory cache if not already registered
            services.AddMemoryCache();

            // Register memory cache provider
            services.AddSingleton<ICacheProvider>(sp =>
            {
                var memoryCache = sp.GetRequiredService<IMemoryCache>();
                var serializer = sp.GetRequiredService<ICacheSerializer>();
                var logger = sp.GetService<ILogger<MemoryCacheProvider>>();
                return new MemoryCacheProvider(memoryCache, serializer, logger);
            });
        }

        private static void RegisterDistributedCache(IServiceCollection services, MvpCachingOptions options)
        {
            // IDistributedCache should be registered by the caller (AddStackExchangeRedisCache, etc.)
            // We just register the provider wrapper

            services.AddSingleton<ICacheProvider>(sp =>
            {
                var distributedCache = sp.GetRequiredService<IDistributedCache>();
                var serializer = sp.GetRequiredService<ICacheSerializer>();
                var logger = sp.GetService<ILogger<DistributedCacheProvider>>();
                return new DistributedCacheProvider(distributedCache, serializer, logger);
            });
        }

        private static void RegisterMultiLevelCache(IServiceCollection services, MvpCachingOptions options)
        {
            // Use the existing MultiLevelCacheExtensions
            services.AddMultiLevelCache(multiLevelOptions =>
            {
                if (options.MultiLevelOptions != null)
                {
                    multiLevelOptions.EnableSynchronization = options.MultiLevelOptions.EnableSynchronization;
                    multiLevelOptions.Synchronizer = options.MultiLevelOptions.Synchronizer;
                    multiLevelOptions.Serializer = options.MultiLevelOptions.Serializer;
                }
            });
        }
    }

    /// <summary>
    /// Configuration options for Mvp24Hours caching.
    /// </summary>
    public class MvpCachingOptions
    {
        /// <summary>
        /// Gets or sets the cache type to use.
        /// Default is Memory.
        /// </summary>
        public CacheType CacheType { get; set; } = CacheType.Memory;

        /// <summary>
        /// Gets or sets the cache serializer to use.
        /// Default is JSON.
        /// </summary>
        public CacheSerializer Serializer { get; set; } = CacheSerializer.Json;

        /// <summary>
        /// Gets or sets the default prefix for cache keys.
        /// Default is "mvp24hours".
        /// </summary>
        public string KeyPrefix { get; set; } = "mvp24hours";

        /// <summary>
        /// Gets or sets the separator for cache key parts.
        /// Default is ":".
        /// </summary>
        public string KeySeparator { get; set; } = ":";

        /// <summary>
        /// Gets or sets options for multi-level cache (only used when CacheType is MultiLevel).
        /// </summary>
        public MultiLevelCacheOptions? MultiLevelOptions { get; set; }
    }

    /// <summary>
    /// Cache type enumeration.
    /// </summary>
    public enum CacheType
    {
        /// <summary>
        /// In-memory cache (IMemoryCache).
        /// </summary>
        Memory,

        /// <summary>
        /// Distributed cache (IDistributedCache - Redis, SQL Server, etc.).
        /// </summary>
        Distributed,

        /// <summary>
        /// Multi-level cache (L1 Memory + L2 Distributed).
        /// </summary>
        MultiLevel
    }

    /// <summary>
    /// Cache serializer enumeration.
    /// </summary>
    public enum CacheSerializer
    {
        /// <summary>
        /// JSON serializer (System.Text.Json).
        /// </summary>
        Json,

        /// <summary>
        /// MessagePack serializer (binary format).
        /// </summary>
        MessagePack
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.KeyGenerators;
using Mvp24Hours.Infrastructure.Caching.Providers;
using Mvp24Hours.Infrastructure.Caching.Serializers;
using System;

namespace Mvp24Hours.Infrastructure.Caching.Extensions
{
    /// <summary>
    /// Extension methods for registering cache providers and related services.
    /// </summary>
    public static class CacheProviderExtensions
    {
        /// <summary>
        /// Adds the memory cache provider to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure cache options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMemoryCacheProvider(
            this IServiceCollection services,
            Action<CacheOptions>? configureOptions = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register memory cache if not already registered
            services.AddMemoryCache();

            // Register options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            // Register serializer
            services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();

            // Register key generator
            services.AddSingleton<ICacheKeyGenerator>(sp =>
            {
                var options = sp.GetService<IOptions<CacheOptions>>()?.Value ?? new CacheOptions();
                return new DefaultCacheKeyGenerator(options.DefaultKeyPrefix, options.KeySeparator);
            });

            // Register cache provider
            services.AddSingleton<ICacheProvider>(sp =>
            {
                var memoryCache = sp.GetRequiredService<IMemoryCache>();
                var serializer = sp.GetRequiredService<ICacheSerializer>();
                var logger = sp.GetService<ILogger<MemoryCacheProvider>>();
                return new MemoryCacheProvider(memoryCache, serializer, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds the distributed cache provider to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure cache options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Requires IDistributedCache to be registered separately (e.g., AddStackExchangeRedisCache).
        /// </remarks>
        public static IServiceCollection AddDistributedCacheProvider(
            this IServiceCollection services,
            Action<CacheOptions>? configureOptions = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            // Register serializer
            services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();

            // Register key generator
            services.AddSingleton<ICacheKeyGenerator>(sp =>
            {
                var options = sp.GetService<IOptions<CacheOptions>>()?.Value ?? new CacheOptions();
                return new DefaultCacheKeyGenerator(options.DefaultKeyPrefix, options.KeySeparator);
            });

            // Register cache provider
            services.AddSingleton<ICacheProvider>(sp =>
            {
                var distributedCache = sp.GetRequiredService<IDistributedCache>();
                var serializer = sp.GetRequiredService<ICacheSerializer>();
                var logger = sp.GetService<ILogger<DistributedCacheProvider>>();
                return new DistributedCacheProvider(distributedCache, serializer, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds cache infrastructure with automatic provider selection.
        /// Uses distributed cache if available, otherwise falls back to memory cache.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure cache options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursCaching(
            this IServiceCollection services,
            Action<CacheOptions>? configureOptions = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register memory cache as fallback
            services.AddMemoryCache();

            // Register options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            // Register serializer
            services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();

            // Register key generator
            services.AddSingleton<ICacheKeyGenerator>(sp =>
            {
                var options = sp.GetService<IOptions<CacheOptions>>()?.Value ?? new CacheOptions();
                return new DefaultCacheKeyGenerator(options.DefaultKeyPrefix, options.KeySeparator);
            });

            // Register cache provider with automatic selection
            services.AddSingleton<ICacheProvider>(sp =>
            {
                // Try distributed cache first
                var distributedCache = sp.GetService<IDistributedCache>();
                if (distributedCache != null)
                {
                    var serializer = sp.GetRequiredService<ICacheSerializer>();
                    var logger = sp.GetService<ILogger<DistributedCacheProvider>>();
                    return new DistributedCacheProvider(distributedCache, serializer, logger);
                }

                // Fallback to memory cache
                var memoryCache = sp.GetRequiredService<IMemoryCache>();
                var memSerializer = sp.GetRequiredService<ICacheSerializer>();
                var memLogger = sp.GetService<ILogger<MemoryCacheProvider>>();
                return new MemoryCacheProvider(memoryCache, memSerializer, memLogger);
            });

            return services;
        }

        /// <summary>
        /// Adds the memory cache provider with MessagePack serializer to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure cache options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMemoryCacheProviderWithMessagePack(
            this IServiceCollection services,
            Action<CacheOptions>? configureOptions = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register memory cache if not already registered
            services.AddMemoryCache();

            // Register options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            // Register MessagePack serializer
            services.AddSingleton<ICacheSerializer, MessagePackCacheSerializer>();

            // Register key generator
            services.AddSingleton<ICacheKeyGenerator>(sp =>
            {
                var options = sp.GetService<IOptions<CacheOptions>>()?.Value ?? new CacheOptions();
                return new DefaultCacheKeyGenerator(options.DefaultKeyPrefix, options.KeySeparator);
            });

            // Register cache provider
            services.AddSingleton<ICacheProvider>(sp =>
            {
                var memoryCache = sp.GetRequiredService<IMemoryCache>();
                var serializer = sp.GetRequiredService<ICacheSerializer>();
                var logger = sp.GetService<ILogger<MemoryCacheProvider>>();
                return new MemoryCacheProvider(memoryCache, serializer, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds the distributed cache provider with MessagePack serializer to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure cache options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Requires IDistributedCache to be registered separately (e.g., AddStackExchangeRedisCache).
        /// </remarks>
        public static IServiceCollection AddDistributedCacheProviderWithMessagePack(
            this IServiceCollection services,
            Action<CacheOptions>? configureOptions = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            // Register MessagePack serializer
            services.AddSingleton<ICacheSerializer, MessagePackCacheSerializer>();

            // Register key generator
            services.AddSingleton<ICacheKeyGenerator>(sp =>
            {
                var options = sp.GetService<IOptions<CacheOptions>>()?.Value ?? new CacheOptions();
                return new DefaultCacheKeyGenerator(options.DefaultKeyPrefix, options.KeySeparator);
            });

            // Register cache provider
            services.AddSingleton<ICacheProvider>(sp =>
            {
                var distributedCache = sp.GetRequiredService<IDistributedCache>();
                var serializer = sp.GetRequiredService<ICacheSerializer>();
                var logger = sp.GetService<ILogger<DistributedCacheProvider>>();
                return new DistributedCacheProvider(distributedCache, serializer, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds a custom cache serializer to the service collection.
        /// </summary>
        /// <typeparam name="TSerializer">The type of serializer to register.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCacheSerializer<TSerializer>(this IServiceCollection services)
            where TSerializer : class, ICacheSerializer
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<ICacheSerializer, TSerializer>();
            return services;
        }
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;

namespace Mvp24Hours.Infrastructure.Caching.Patterns
{
    /// <summary>
    /// Extension methods for registering cache patterns.
    /// </summary>
    public static class CachePatternExtensions
    {
        /// <summary>
        /// Registers a Read-Through cache implementation.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="loadFromSource">The factory function to load data from source on cache miss.</param>
        /// <param name="getCacheOptions">Optional function to get cache options for a given key.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddReadThroughCache<T>(
            this IServiceCollection services,
            Func<string, System.Threading.CancellationToken, System.Threading.Tasks.Task<T?>> loadFromSource,
            Func<string, CacheEntryOptions>? getCacheOptions = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped) where T : class
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (loadFromSource == null)
                throw new ArgumentNullException(nameof(loadFromSource));

            services.Add(new ServiceDescriptor(
                typeof(IReadThroughCache<T>),
                sp =>
                {
                    var cache = sp.GetRequiredService<ICacheProvider>();
                    var logger = sp.GetService<ILogger<ReadThroughCache<T>>>();
                    return new ReadThroughCache<T>(cache, loadFromSource, getCacheOptions, logger);
                },
                lifetime));

            return services;
        }

        /// <summary>
        /// Registers a Write-Through cache implementation.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="saveToSource">The function to persist data to the data source.</param>
        /// <param name="getCacheOptions">Optional function to get cache options for a given key.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWriteThroughCache<T>(
            this IServiceCollection services,
            Func<string, T, System.Threading.CancellationToken, System.Threading.Tasks.Task> saveToSource,
            Func<string, CacheEntryOptions>? getCacheOptions = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped) where T : class
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (saveToSource == null)
                throw new ArgumentNullException(nameof(saveToSource));

            services.Add(new ServiceDescriptor(
                typeof(IWriteThroughCache<T>),
                sp =>
                {
                    var cache = sp.GetRequiredService<ICacheProvider>();
                    var logger = sp.GetService<ILogger<WriteThroughCache<T>>>();
                    return new WriteThroughCache<T>(cache, saveToSource, getCacheOptions, logger);
                },
                lifetime));

            return services;
        }

        /// <summary>
        /// Registers a Write-Behind cache implementation.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="saveToSource">The function to persist data to the data source.</param>
        /// <param name="getCacheOptions">Optional function to get cache options for a given key.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWriteBehindCache<T>(
            this IServiceCollection services,
            Func<string, T, System.Threading.CancellationToken, System.Threading.Tasks.Task> saveToSource,
            Func<string, CacheEntryOptions>? getCacheOptions = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped) where T : class
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (saveToSource == null)
                throw new ArgumentNullException(nameof(saveToSource));

            services.Add(new ServiceDescriptor(
                typeof(IWriteBehindCache<T>),
                sp =>
                {
                    var cache = sp.GetRequiredService<ICacheProvider>();
                    var logger = sp.GetService<ILogger<WriteBehindCache<T>>>();
                    return new WriteBehindCache<T>(cache, saveToSource, getCacheOptions, logger);
                },
                lifetime));

            return services;
        }

        /// <summary>
        /// Registers a Refresh-Ahead cache implementation.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="loadFromSource">The factory function to load data from source.</param>
        /// <param name="expiration">The expiration duration for cached items.</param>
        /// <param name="refreshThreshold">The time before expiration when refresh should be triggered.</param>
        /// <param name="getCacheOptions">Optional function to get cache options for a given key.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRefreshAheadCache<T>(
            this IServiceCollection services,
            Func<string, System.Threading.CancellationToken, System.Threading.Tasks.Task<T?>> loadFromSource,
            TimeSpan expiration,
            TimeSpan refreshThreshold,
            Func<string, CacheEntryOptions>? getCacheOptions = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped) where T : class
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (loadFromSource == null)
                throw new ArgumentNullException(nameof(loadFromSource));

            services.Add(new ServiceDescriptor(
                typeof(IRefreshAheadCache<T>),
                sp =>
                {
                    var cache = sp.GetRequiredService<ICacheProvider>();
                    var logger = sp.GetService<ILogger<RefreshAheadCache<T>>>();
                    return new RefreshAheadCache<T>(cache, loadFromSource, expiration, refreshThreshold, getCacheOptions, logger);
                },
                lifetime));

            return services;
        }

        /// <summary>
        /// Registers the Write-Behind background service.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure Write-Behind options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWriteBehindBackgroundService(
            this IServiceCollection services,
            Action<WriteBehindOptions>? configureOptions = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<WriteBehindOptions>(options =>
                {
                    options.FlushInterval = TimeSpan.FromSeconds(30);
                    options.BatchSize = 100;
                });
            }

            services.AddHostedService<WriteBehindBackgroundService>();

            return services;
        }
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Invalidation;
using System;

namespace Mvp24Hours.Infrastructure.Caching.Extensions
{
    /// <summary>
    /// Extension methods for registering cache invalidation services.
    /// </summary>
    public static class CacheInvalidationServiceExtensions
    {
        /// <summary>
        /// Adds cache tag manager to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCacheTagManager(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<ICacheTagManager>(sp =>
            {
                var cacheProvider = sp.GetRequiredService<ICacheProvider>();
                var logger = sp.GetService<ILogger<CacheTagManager>>();
                return new CacheTagManager(cacheProvider, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds cache dependency manager to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCacheDependencyManager(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<CacheDependencyManager>(sp =>
            {
                var cacheProvider = sp.GetRequiredService<ICacheProvider>();
                var logger = sp.GetService<ILogger<CacheDependencyManager>>();
                return new CacheDependencyManager(cacheProvider, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds cache stampede prevention to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCacheStampedePrevention(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<ICacheStampedePrevention>(sp =>
            {
                var logger = sp.GetService<ILogger<CacheStampedePrevention>>();
                return new CacheStampedePrevention(logger);
            });

            return services;
        }

        /// <summary>
        /// Adds in-memory cache invalidation event publisher (for testing).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddInMemoryCacheInvalidationEvents(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<ICacheInvalidationEventPublisher>(sp =>
            {
                var logger = sp.GetService<ILogger<InMemoryCacheInvalidationEventPublisher>>();
                return new InMemoryCacheInvalidationEventPublisher(logger);
            });

            return services;
        }

        /// <summary>
        /// Adds all cache invalidation features (tags, dependencies, stampede prevention, events).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCacheInvalidationFeatures(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddCacheTagManager();
            services.AddCacheDependencyManager();
            services.AddCacheStampedePrevention();
            services.AddInMemoryCacheInvalidationEvents();

            return services;
        }
    }
}


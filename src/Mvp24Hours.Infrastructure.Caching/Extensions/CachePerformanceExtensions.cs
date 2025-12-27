//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Compression;
using Mvp24Hours.Infrastructure.Caching.Prefetching;
using Mvp24Hours.Infrastructure.Caching.Serializers;
using Mvp24Hours.Infrastructure.Caching.Warming;
using System;
using System.IO.Compression;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Caching.Extensions
{
    /// <summary>
    /// Extension methods for registering cache performance features (compression, prefetching, warming).
    /// </summary>
    public static class CachePerformanceExtensions
    {
        /// <summary>
        /// Adds compression support to the cache serializer.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="algorithm">The compression algorithm to use (defaults to Brotli).</param>
        /// <param name="compressionLevel">The compression level (defaults to Optimal).</param>
        /// <param name="compressionThresholdBytes">Minimum size in bytes to trigger compression (defaults to 1024).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCacheCompression(
            this IServiceCollection services,
            CompressionAlgorithm algorithm = CompressionAlgorithm.Brotli,
            CompressionLevel compressionLevel = CompressionLevel.Optimal,
            int compressionThresholdBytes = 1024)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register compressor
            services.AddSingleton<ICacheCompressor>(sp =>
            {
                var logger = sp.GetService<ILogger<CacheCompressor>>();
                return new CacheCompressor(algorithm, compressionLevel, logger);
            });

            // Wrap existing serializer with compression
            // Note: This requires the serializer to be registered before calling this method
            // We'll wrap it when the provider is created instead
            services.AddSingleton<ICacheSerializer>(sp =>
            {
                // Try to get existing serializer, or create default
                var existingSerializer = sp.GetService<ICacheSerializer>();
                if (existingSerializer == null)
                {
                    existingSerializer = new JsonCacheSerializer();
                }

                var compressor = sp.GetRequiredService<ICacheCompressor>();
                var logger = sp.GetService<ILogger<CompressedCacheSerializer>>();
                return new CompressedCacheSerializer(existingSerializer, compressor, compressionThresholdBytes, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds cache prefetching support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCachePrefetching(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<ICachePrefetcher>(sp =>
            {
                var cacheProvider = sp.GetRequiredService<ICacheProvider>();
                var logger = sp.GetService<ILogger<CachePrefetcher>>();
                return new CachePrefetcher(cacheProvider, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds cache warming support with automatic execution on startup.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="enableAutoWarmup">Whether to automatically execute warmup on startup (defaults to true).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCacheWarming(
            this IServiceCollection services,
            bool enableAutoWarmup = true)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register cache warmer
            services.AddSingleton<ICacheWarmer>(sp =>
            {
                var warmupOperations = sp.GetServices<ICacheWarmupOperation>();
                var logger = sp.GetService<ILogger<CacheWarmer>>();
                return new CacheWarmer(warmupOperations, logger);
            });

            // Register hosted service for automatic warmup
            if (enableAutoWarmup)
            {
                services.AddHostedService<CacheWarmupHostedService>();
            }

            return services;
        }

        /// <summary>
        /// Registers a cache warmup operation.
        /// </summary>
        /// <typeparam name="TOperation">The type of warmup operation.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCacheWarmupOperation<TOperation>(this IServiceCollection services)
            where TOperation : class, ICacheWarmupOperation
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<ICacheWarmupOperation, TOperation>();
            return services;
        }

        /// <summary>
        /// Configures cache options with compression enabled.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="compressionThresholdBytes">Minimum size in bytes to trigger compression.</param>
        /// <param name="algorithm">The compression algorithm to use.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ConfigureCacheCompression(
            this IServiceCollection services,
            int compressionThresholdBytes = 1024,
            CompressionAlgorithm algorithm = CompressionAlgorithm.Brotli)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.Configure<CacheOptions>(options =>
            {
                options.EnableCompression = true;
                options.CompressionThresholdBytes = compressionThresholdBytes;
            });

            return services.AddCacheCompression(algorithm, CompressionLevel.Optimal, compressionThresholdBytes);
        }

    }
}


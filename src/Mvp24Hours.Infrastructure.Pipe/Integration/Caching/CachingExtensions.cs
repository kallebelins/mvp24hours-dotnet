//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Integration.Caching;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for caching integration with Mvp24Hours pipelines.
    /// </summary>
    public static class CachingExtensions
    {
        /// <summary>
        /// Adds pipeline caching support using IDistributedCache.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineCaching(
            this IServiceCollection services,
            Action<CacheOperationOptions>? configure = null)
        {
            var options = new CacheOperationOptions();
            configure?.Invoke(options);
            services.TryAddSingleton(options);

            return services;
        }

        /// <summary>
        /// Adds the cache results middleware to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineCacheMiddleware(
            this IServiceCollection services,
            Action<CacheOperationOptions>? configure = null)
        {
            services.AddPipelineCaching(configure);

            services.AddSingleton<IPipelineMiddleware>(sp =>
            {
                var cache = sp.GetRequiredService<IDistributedCache>();
                var logger = sp.GetService<ILogger<CacheResultsMiddleware>>();
                var options = sp.GetService<CacheOperationOptions>() ?? new CacheOperationOptions();
                return new CacheResultsMiddleware(cache, logger, options);
            });

            return services;
        }

        /// <summary>
        /// Wraps an operation with caching support.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="operation">The operation to wrap.</param>
        /// <param name="cache">The distributed cache.</param>
        /// <param name="keyGenerator">Function to generate cache keys.</param>
        /// <param name="options">Optional cache options.</param>
        /// <returns>A caching operation wrapper.</returns>
        public static CachingOperation<TInput, TOutput> WithCaching<TInput, TOutput>(
            this ITypedOperationAsync<TInput, TOutput> operation,
            IDistributedCache cache,
            Func<TInput, string> keyGenerator,
            CacheOperationOptions? options = null)
        {
            return new CachingOperation<TInput, TOutput>(operation, cache, keyGenerator, null, options);
        }

        /// <summary>
        /// Adds a cached operation to a typed pipeline.
        /// </summary>
        /// <typeparam name="TInput">The pipeline input type.</typeparam>
        /// <typeparam name="TOutput">The pipeline output type.</typeparam>
        /// <typeparam name="TOpInput">The operation input type.</typeparam>
        /// <typeparam name="TOpOutput">The operation output type.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="operation">The operation to add.</param>
        /// <param name="cache">The distributed cache.</param>
        /// <param name="keyGenerator">Function to generate cache keys.</param>
        /// <param name="absoluteExpiration">Optional absolute expiration.</param>
        /// <param name="slidingExpiration">Optional sliding expiration.</param>
        /// <returns>The pipeline for chaining.</returns>
        public static TypedPipelineAsync<TInput, TOutput> AddWithCaching<TInput, TOutput, TOpInput, TOpOutput>(
            this TypedPipelineAsync<TInput, TOutput> pipeline,
            ITypedOperationAsync<TOpInput, TOpOutput> operation,
            IDistributedCache cache,
            Func<TOpInput, string> keyGenerator,
            TimeSpan? absoluteExpiration = null,
            TimeSpan? slidingExpiration = null)
            where TOpInput : TInput
            where TOpOutput : TOutput
        {
            var cachingOperation = new CachingOperation<TOpInput, TOpOutput>(operation, cache, keyGenerator)
            {
                AbsoluteExpiration = absoluteExpiration,
                SlidingExpiration = slidingExpiration
            };

            pipeline.Add<TOpInput, TOpOutput>(cachingOperation);
            return pipeline;
        }

        /// <summary>
        /// Registers a caching operation wrapper for a specific operation type.
        /// </summary>
        /// <typeparam name="TOperation">The operation type.</typeparam>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="keyGenerator">Function to generate cache keys.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCachedOperation<TOperation, TInput, TOutput>(
            this IServiceCollection services,
            Func<TInput, string> keyGenerator,
            Action<CacheOperationOptions>? configure = null)
            where TOperation : class, ITypedOperationAsync<TInput, TOutput>
        {
            // Register the inner operation
            services.TryAddTransient<TOperation>();

            // Register the caching wrapper
            services.AddTransient<ITypedOperationAsync<TInput, TOutput>>(sp =>
            {
                var innerOperation = sp.GetRequiredService<TOperation>();
                var cache = sp.GetRequiredService<IDistributedCache>();
                var logger = sp.GetService<ILogger<CachingOperation<TInput, TOutput>>>();
                var globalOptions = sp.GetService<CacheOperationOptions>() ?? new CacheOperationOptions();

                var options = new CacheOperationOptions
                {
                    DefaultAbsoluteExpiration = globalOptions.DefaultAbsoluteExpiration,
                    DefaultSlidingExpiration = globalOptions.DefaultSlidingExpiration,
                    CacheFailedResults = globalOptions.CacheFailedResults,
                    CacheKeyPrefix = globalOptions.CacheKeyPrefix,
                    UseCompression = globalOptions.UseCompression,
                    CompressionThreshold = globalOptions.CompressionThreshold
                };

                configure?.Invoke(options);

                return new CachingOperation<TInput, TOutput>(innerOperation, cache, keyGenerator, logger, options);
            });

            return services;
        }
    }
}


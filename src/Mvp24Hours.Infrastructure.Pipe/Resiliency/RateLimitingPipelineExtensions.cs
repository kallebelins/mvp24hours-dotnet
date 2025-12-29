//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.Infrastructure.RateLimiting;
using Mvp24Hours.Core.Infrastructure.RateLimiting;
using System;

namespace Mvp24Hours.Infrastructure.Pipe.Resiliency
{
    /// <summary>
    /// Extension methods for configuring rate limiting in pipelines.
    /// </summary>
    public static class RateLimitingPipelineExtensions
    {
        /// <summary>
        /// Adds rate limiting middleware to the pipeline using System.Threading.RateLimiting.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration for rate limiting options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineRateLimiting(
            this IServiceCollection services,
            Action<RateLimitingPipelineOptions>? configureOptions = null)
        {
            var options = new RateLimitingPipelineOptions();
            configureOptions?.Invoke(options);

            // Register the rate limiter provider
            services.TryAddSingleton<IRateLimiterProvider, NativeRateLimiterProvider>();

            // Register the middleware
            services.AddSingleton<IPipelineMiddleware>(sp =>
                new RateLimitingPipelineMiddleware(
                    sp.GetRequiredService<IRateLimiterProvider>(),
                    options,
                    sp.GetService<Microsoft.Extensions.Logging.ILogger<RateLimitingPipelineMiddleware>>()));

            return services;
        }

        /// <summary>
        /// Adds rate limiting with fixed window algorithm.
        /// </summary>
        public static IServiceCollection AddPipelineRateLimitingFixedWindow(
            this IServiceCollection services,
            int permitLimit = 100,
            TimeSpan? window = null,
            Action<RateLimitingPipelineOptions>? configureOptions = null)
        {
            return services.AddPipelineRateLimiting(options =>
            {
                options.DefaultRateLimiterOptions = NativeRateLimiterOptions.FixedWindow(
                    permitLimit,
                    window);
                configureOptions?.Invoke(options);
            });
        }

        /// <summary>
        /// Adds rate limiting with sliding window algorithm.
        /// </summary>
        public static IServiceCollection AddPipelineRateLimitingSlidingWindow(
            this IServiceCollection services,
            int permitLimit = 100,
            TimeSpan? window = null,
            int segmentsPerWindow = 4,
            Action<RateLimitingPipelineOptions>? configureOptions = null)
        {
            return services.AddPipelineRateLimiting(options =>
            {
                options.DefaultRateLimiterOptions = NativeRateLimiterOptions.SlidingWindow(
                    permitLimit,
                    window,
                    segmentsPerWindow);
                configureOptions?.Invoke(options);
            });
        }

        /// <summary>
        /// Adds rate limiting with token bucket algorithm.
        /// </summary>
        public static IServiceCollection AddPipelineRateLimitingTokenBucket(
            this IServiceCollection services,
            int tokenLimit = 100,
            TimeSpan? replenishmentPeriod = null,
            int tokensPerPeriod = 10,
            Action<RateLimitingPipelineOptions>? configureOptions = null)
        {
            return services.AddPipelineRateLimiting(options =>
            {
                options.DefaultRateLimiterOptions = NativeRateLimiterOptions.TokenBucket(
                    tokenLimit,
                    replenishmentPeriod,
                    tokensPerPeriod);
                configureOptions?.Invoke(options);
            });
        }

        /// <summary>
        /// Adds rate limiting with concurrency limiter algorithm.
        /// </summary>
        public static IServiceCollection AddPipelineRateLimitingConcurrency(
            this IServiceCollection services,
            int permitLimit = 10,
            int queueLimit = 0,
            Action<RateLimitingPipelineOptions>? configureOptions = null)
        {
            return services.AddPipelineRateLimiting(options =>
            {
                options.DefaultRateLimiterOptions = NativeRateLimiterOptions.Concurrency(
                    permitLimit,
                    queueLimit);
                configureOptions?.Invoke(options);
            });
        }
    }
}


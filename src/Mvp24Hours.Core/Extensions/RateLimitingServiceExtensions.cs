//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Contract.Infrastructure.RateLimiting;
using Mvp24Hours.Core.Infrastructure.RateLimiting;
using System;
using System.Threading.RateLimiting;

namespace Mvp24Hours.Core.Extensions
{
    /// <summary>
    /// Extension methods for configuring native rate limiting services.
    /// Uses System.Threading.RateLimiting from .NET 7+.
    /// </summary>
    public static class RateLimitingServiceExtensions
    {
        /// <summary>
        /// Adds the native rate limiter provider to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeRateLimiting(this IServiceCollection services)
        {
            services.TryAddSingleton<IRateLimiterProvider, NativeRateLimiterProvider>();
            return services;
        }

        /// <summary>
        /// Adds a configured rate limiter with a specific key.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="key">The rate limiter key.</param>
        /// <param name="configureOptions">The options configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeRateLimiter(
            this IServiceCollection services,
            string key,
            Action<NativeRateLimiterOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(configureOptions);

            services.AddNativeRateLimiting();

            var options = new NativeRateLimiterOptions();
            configureOptions(options);

            // Pre-register the rate limiter
            services.AddSingleton<RateLimiterRegistration>(new RateLimiterRegistration(key, options));

            return services;
        }

        /// <summary>
        /// Adds a fixed window rate limiter with a specific key.
        /// </summary>
        public static IServiceCollection AddFixedWindowRateLimiter(
            this IServiceCollection services,
            string key,
            int permitLimit = 100,
            TimeSpan? window = null,
            int queueLimit = 0)
        {
            return services.AddNativeRateLimiter(key, options =>
            {
                options.Algorithm = RateLimitingAlgorithm.FixedWindow;
                options.PermitLimit = permitLimit;
                options.Window = window ?? TimeSpan.FromMinutes(1);
                options.QueueLimit = queueLimit;
            });
        }

        /// <summary>
        /// Adds a sliding window rate limiter with a specific key.
        /// </summary>
        public static IServiceCollection AddSlidingWindowRateLimiter(
            this IServiceCollection services,
            string key,
            int permitLimit = 100,
            TimeSpan? window = null,
            int segmentsPerWindow = 4,
            int queueLimit = 0)
        {
            return services.AddNativeRateLimiter(key, options =>
            {
                options.Algorithm = RateLimitingAlgorithm.SlidingWindow;
                options.PermitLimit = permitLimit;
                options.Window = window ?? TimeSpan.FromMinutes(1);
                options.SegmentsPerWindow = segmentsPerWindow;
                options.QueueLimit = queueLimit;
            });
        }

        /// <summary>
        /// Adds a token bucket rate limiter with a specific key.
        /// </summary>
        public static IServiceCollection AddTokenBucketRateLimiter(
            this IServiceCollection services,
            string key,
            int tokenLimit = 100,
            TimeSpan? replenishmentPeriod = null,
            int tokensPerPeriod = 10,
            int queueLimit = 0)
        {
            return services.AddNativeRateLimiter(key, options =>
            {
                options.Algorithm = RateLimitingAlgorithm.TokenBucket;
                options.PermitLimit = tokenLimit;
                options.ReplenishmentPeriod = replenishmentPeriod ?? TimeSpan.FromSeconds(10);
                options.TokensPerPeriod = tokensPerPeriod;
                options.QueueLimit = queueLimit;
            });
        }

        /// <summary>
        /// Adds a concurrency limiter with a specific key.
        /// </summary>
        public static IServiceCollection AddConcurrencyRateLimiter(
            this IServiceCollection services,
            string key,
            int permitLimit = 10,
            int queueLimit = 0,
            QueueProcessingOrder queueProcessingOrder = QueueProcessingOrder.OldestFirst)
        {
            return services.AddNativeRateLimiter(key, options =>
            {
                options.Algorithm = RateLimitingAlgorithm.Concurrency;
                options.PermitLimit = permitLimit;
                options.QueueLimit = queueLimit;
                options.QueueProcessingOrder = queueProcessingOrder;
            });
        }
    }

    /// <summary>
    /// Registration record for a rate limiter configuration.
    /// </summary>
    public record RateLimiterRegistration(string Key, NativeRateLimiterOptions Options);
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Contract.Infrastructure.RateLimiting;
using Mvp24Hours.Core.Infrastructure.RateLimiting;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Extensions
{
    /// <summary>
    /// Extension methods for configuring rate limiting in RabbitMQ consumers and publishers.
    /// </summary>
    public static class RateLimitingExtensions
    {
        /// <summary>
        /// Adds rate limiting for RabbitMQ consumers using System.Threading.RateLimiting.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration for consume filter options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRabbitMQConsumerRateLimiting(
            this IServiceCollection services,
            Action<RateLimitingConsumeFilterOptions>? configureOptions = null)
        {
            var options = new RateLimitingConsumeFilterOptions();
            configureOptions?.Invoke(options);

            // Register the rate limiter provider
            services.TryAddSingleton<IRateLimiterProvider, NativeRateLimiterProvider>();

            // Register the consume filter
            services.AddSingleton<IConsumeFilter>(sp =>
                new RateLimitingConsumeFilter(
                    sp.GetRequiredService<IRateLimiterProvider>(),
                    options,
                    sp.GetService<Microsoft.Extensions.Logging.ILogger<RateLimitingConsumeFilter>>()));

            return services;
        }

        /// <summary>
        /// Adds rate limiting for RabbitMQ publishers using System.Threading.RateLimiting.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration for publish filter options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRabbitMQPublisherRateLimiting(
            this IServiceCollection services,
            Action<RateLimitingPublishFilterOptions>? configureOptions = null)
        {
            var options = new RateLimitingPublishFilterOptions();
            configureOptions?.Invoke(options);

            // Register the rate limiter provider
            services.TryAddSingleton<IRateLimiterProvider, NativeRateLimiterProvider>();

            // Register the publish filter
            services.AddSingleton<IPublishFilter>(sp =>
                new RateLimitingPublishFilter(
                    sp.GetRequiredService<IRateLimiterProvider>(),
                    options,
                    sp.GetService<Microsoft.Extensions.Logging.ILogger<RateLimitingPublishFilter>>()));

            return services;
        }

        /// <summary>
        /// Adds rate limiting for both RabbitMQ consumers and publishers.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureConsumeOptions">Optional configuration for consume filter options.</param>
        /// <param name="configurePublishOptions">Optional configuration for publish filter options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRabbitMQRateLimiting(
            this IServiceCollection services,
            Action<RateLimitingConsumeFilterOptions>? configureConsumeOptions = null,
            Action<RateLimitingPublishFilterOptions>? configurePublishOptions = null)
        {
            return services
                .AddRabbitMQConsumerRateLimiting(configureConsumeOptions)
                .AddRabbitMQPublisherRateLimiting(configurePublishOptions);
        }

        /// <summary>
        /// Adds rate limiting for consumers with sliding window algorithm.
        /// </summary>
        public static IServiceCollection AddRabbitMQConsumerRateLimitingSlidingWindow(
            this IServiceCollection services,
            int permitLimit = 100,
            TimeSpan? window = null,
            int segmentsPerWindow = 4,
            RateLimitKeyMode keyMode = RateLimitKeyMode.ByQueue,
            RateLimitExceededBehavior exceededBehavior = RateLimitExceededBehavior.Retry)
        {
            return services.AddRabbitMQConsumerRateLimiting(options =>
            {
                options.KeyMode = keyMode;
                options.ExceededBehavior = exceededBehavior;
                options.DefaultRateLimiterOptions = NativeRateLimiterOptions.SlidingWindow(
                    permitLimit,
                    window ?? TimeSpan.FromSeconds(1),
                    segmentsPerWindow);
            });
        }

        /// <summary>
        /// Adds rate limiting for consumers with token bucket algorithm.
        /// </summary>
        public static IServiceCollection AddRabbitMQConsumerRateLimitingTokenBucket(
            this IServiceCollection services,
            int tokenLimit = 100,
            TimeSpan? replenishmentPeriod = null,
            int tokensPerPeriod = 10,
            RateLimitKeyMode keyMode = RateLimitKeyMode.ByQueue,
            RateLimitExceededBehavior exceededBehavior = RateLimitExceededBehavior.Retry)
        {
            return services.AddRabbitMQConsumerRateLimiting(options =>
            {
                options.KeyMode = keyMode;
                options.ExceededBehavior = exceededBehavior;
                options.DefaultRateLimiterOptions = NativeRateLimiterOptions.TokenBucket(
                    tokenLimit,
                    replenishmentPeriod ?? TimeSpan.FromSeconds(1),
                    tokensPerPeriod);
            });
        }

        /// <summary>
        /// Adds rate limiting for consumers with concurrency limiter.
        /// </summary>
        public static IServiceCollection AddRabbitMQConsumerRateLimitingConcurrency(
            this IServiceCollection services,
            int permitLimit = 10,
            int queueLimit = 0,
            RateLimitKeyMode keyMode = RateLimitKeyMode.ByQueue,
            RateLimitExceededBehavior exceededBehavior = RateLimitExceededBehavior.Retry)
        {
            return services.AddRabbitMQConsumerRateLimiting(options =>
            {
                options.KeyMode = keyMode;
                options.ExceededBehavior = exceededBehavior;
                options.DefaultRateLimiterOptions = NativeRateLimiterOptions.Concurrency(
                    permitLimit,
                    queueLimit);
            });
        }
    }
}


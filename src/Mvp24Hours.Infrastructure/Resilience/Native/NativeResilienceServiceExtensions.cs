//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System;

namespace Mvp24Hours.Infrastructure.Resilience.Native
{
    /// <summary>
    /// Extension methods for configuring native resilience pipelines using Microsoft.Extensions.Resilience.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions provide a modern, .NET 9-native approach to resilience configuration
    /// that supersedes custom Polly implementations.
    /// </para>
    /// <para>
    /// <b>Migration from custom implementations:</b>
    /// <list type="bullet">
    ///   <item>Replace <c>AddMvpRetryPolicy</c> with <c>AddNativeResilience</c></item>
    ///   <item>Replace <c>AddMvpCircuitBreaker</c> with <c>AddNativeResilience</c></item>
    ///   <item>Replace custom <c>MvpExecutionStrategy</c> with <c>AddNativeDbResilience</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic configuration
    /// services.AddNativeResilience(options =>
    /// {
    ///     options.EnableRetry = true;
    ///     options.EnableCircuitBreaker = true;
    /// });
    /// 
    /// // Using presets
    /// services.AddNativeResilience(NativeResilienceOptions.Database);
    /// 
    /// // Named pipelines
    /// services.AddNativeResilience("database", NativeResilienceOptions.Database);
    /// services.AddNativeResilience("messaging", NativeResilienceOptions.Messaging);
    /// </code>
    /// </example>
    public static class NativeResilienceServiceExtensions
    {
        /// <summary>
        /// Adds a native resilience pipeline with default options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeResilience(this IServiceCollection services)
        {
            return services.AddNativeResilience(NativeResilienceOptions.Default);
        }

        /// <summary>
        /// Adds a native resilience pipeline with the specified options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">The resilience options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeResilience(
            this IServiceCollection services,
            NativeResilienceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            services.TryAddSingleton(options);

            services.TryAddSingleton<INativeResiliencePipeline>(sp =>
            {
                var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<NativeResiliencePipeline>();
                return new NativeResiliencePipeline(options, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds a native resilience pipeline with configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeResilience(
            this IServiceCollection services,
            Action<NativeResilienceOptions> configure)
        {
            var options = new NativeResilienceOptions();
            configure(options);
            return services.AddNativeResilience(options);
        }

        /// <summary>
        /// Adds a named native resilience pipeline.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the pipeline.</param>
        /// <param name="options">The resilience options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeResilience(
            this IServiceCollection services,
            string name,
            NativeResilienceOptions options)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(options);

            options.Name = name;

            // Register as keyed service for .NET 8+ Keyed Services
            services.AddKeyedSingleton<INativeResiliencePipeline>(name, (sp, _) =>
            {
                var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<NativeResiliencePipeline>();
                return new NativeResiliencePipeline(options, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds a named native resilience pipeline with configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the pipeline.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeResilience(
            this IServiceCollection services,
            string name,
            Action<NativeResilienceOptions> configure)
        {
            var options = new NativeResilienceOptions { Name = name };
            configure(options);
            return services.AddNativeResilience(name, options);
        }

        /// <summary>
        /// Adds a typed native resilience pipeline.
        /// </summary>
        /// <typeparam name="TResult">The result type of the operations.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="options">The resilience options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeResilience<TResult>(
            this IServiceCollection services,
            NativeResilienceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            services.TryAddSingleton<INativeResiliencePipeline<TResult>>(sp =>
            {
                var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<NativeResiliencePipeline<TResult>>();
                return new NativeResiliencePipeline<TResult>(options, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds a typed native resilience pipeline with configuration.
        /// </summary>
        /// <typeparam name="TResult">The result type of the operations.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeResilience<TResult>(
            this IServiceCollection services,
            Action<NativeResilienceOptions> configure)
        {
            var options = new NativeResilienceOptions();
            configure(options);
            return services.AddNativeResilience<TResult>(options);
        }

        /// <summary>
        /// Adds a native resilience pipeline optimized for database operations.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeDbResilience(this IServiceCollection services)
        {
            return services.AddNativeResilience("database", NativeResilienceOptions.Database);
        }

        /// <summary>
        /// Adds a native resilience pipeline optimized for database operations with configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Additional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeDbResilience(
            this IServiceCollection services,
            Action<NativeResilienceOptions> configure)
        {
            var options = NativeResilienceOptions.Database;
            configure(options);
            return services.AddNativeResilience("database", options);
        }

        /// <summary>
        /// Adds a native resilience pipeline optimized for messaging operations.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeMessagingResilience(this IServiceCollection services)
        {
            return services.AddNativeResilience("messaging", NativeResilienceOptions.Messaging);
        }

        /// <summary>
        /// Adds a native resilience pipeline optimized for messaging operations with configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Additional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeMessagingResilience(
            this IServiceCollection services,
            Action<NativeResilienceOptions> configure)
        {
            var options = NativeResilienceOptions.Messaging;
            configure(options);
            return services.AddNativeResilience("messaging", options);
        }

        /// <summary>
        /// Adds resilience using the Polly ResiliencePipelineBuilder directly.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the pipeline.</param>
        /// <param name="configure">The pipeline builder configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeResiliencePipeline(
            this IServiceCollection services,
            string name,
            Action<ResiliencePipelineBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(configure);

            // Use Polly's ResiliencePipelineBuilder directly
            services.AddResiliencePipeline(name, configure);

            return services;
        }

        /// <summary>
        /// Adds resilience using the Polly ResiliencePipelineBuilder directly with typed result.
        /// </summary>
        /// <typeparam name="TResult">The result type of the operations.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the pipeline.</param>
        /// <param name="configure">The pipeline builder configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeResiliencePipeline<TResult>(
            this IServiceCollection services,
            string name,
            Action<ResiliencePipelineBuilder<TResult>> configure)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(configure);

            // Use Polly's ResiliencePipelineBuilder directly
            services.AddResiliencePipeline(name, configure);

            return services;
        }

        /// <summary>
        /// Adds a standard resilience pipeline with sensible defaults for general operations.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the pipeline.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method uses the native .NET 9 standard resilience configuration which includes:
        /// <list type="bullet">
        ///   <item>Total request timeout</item>
        ///   <item>Retry with exponential backoff and jitter</item>
        ///   <item>Circuit breaker</item>
        ///   <item>Attempt timeout</item>
        /// </list>
        /// </remarks>
        public static IServiceCollection AddStandardNativeResilience(
            this IServiceCollection services,
            string name = "standard")
        {
            services.AddResiliencePipeline(name, builder =>
            {
                builder
                    // Timeout for entire operation
                    .AddTimeout(new TimeoutStrategyOptions
                    {
                        Timeout = TimeSpan.FromSeconds(60)
                    })
                    // Circuit breaker
                    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                    {
                        FailureRatio = 0.5,
                        MinimumThroughput = 10,
                        SamplingDuration = TimeSpan.FromSeconds(30),
                        BreakDuration = TimeSpan.FromSeconds(30)
                    })
                    // Retry with exponential backoff
                    .AddRetry(new RetryStrategyOptions
                    {
                        MaxRetryAttempts = 3,
                        Delay = TimeSpan.FromSeconds(1),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true
                    });
            });

            return services;
        }
    }
}


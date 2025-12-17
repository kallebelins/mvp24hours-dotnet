//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;

namespace Mvp24Hours.Infrastructure.Pipe.Resiliency
{
    /// <summary>
    /// Extension methods for registering pipeline resiliency middleware.
    /// </summary>
    public static class PipelineResiliencyExtensions
    {
        /// <summary>
        /// Adds all pipeline resiliency middlewares with default options.
        /// Includes: Retry, Circuit Breaker, Fallback, Bulkhead, and Dead Letter.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpPipelineResiliency(this IServiceCollection services)
        {
            return services.AddMvpPipelineResiliency(_ => { });
        }

        /// <summary>
        /// Adds all pipeline resiliency middlewares with custom configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpPipelineResiliency(
            this IServiceCollection services,
            Action<PipelineResiliencyOptions> configure)
        {
            var options = new PipelineResiliencyOptions();
            configure(options);

            if (options.EnableRetry)
            {
                services.AddMvpPipelineRetry(options.RetryOptions);
            }

            if (options.EnableCircuitBreaker)
            {
                services.AddMvpPipelineCircuitBreaker(options.CircuitBreakerOptions);
            }

            if (options.EnableFallback)
            {
                services.AddMvpPipelineFallback(options.FallbackOptions);
            }

            if (options.EnableBulkhead)
            {
                services.AddMvpPipelineBulkhead(options.BulkheadOptions);
            }

            if (options.EnableDeadLetter)
            {
                services.AddMvpPipelineDeadLetter(options.DeadLetterOptions, options.DeadLetterStoreType);
            }

            return services;
        }

        /// <summary>
        /// Adds retry middleware to the pipeline.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Retry options. Default options used if null.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpPipelineRetry(
            this IServiceCollection services,
            RetryOptions? options = null)
        {
            options ??= RetryOptions.Default;

            services.TryAddSingleton(options);
            services.AddSingleton<IPipelineMiddleware>(sp =>
            {
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<RetryPipelineMiddleware>>();
                return new RetryPipelineMiddleware(options, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds circuit breaker middleware to the pipeline.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Circuit breaker options. Default options used if null.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpPipelineCircuitBreaker(
            this IServiceCollection services,
            CircuitBreakerOptions? options = null)
        {
            options ??= CircuitBreakerOptions.Default;

            services.TryAddSingleton(options);
            services.AddSingleton<CircuitBreakerPipelineMiddleware>(sp =>
            {
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<CircuitBreakerPipelineMiddleware>>();
                return new CircuitBreakerPipelineMiddleware(options, logger);
            });
            services.AddSingleton<IPipelineMiddleware>(sp => sp.GetRequiredService<CircuitBreakerPipelineMiddleware>());

            return services;
        }

        /// <summary>
        /// Adds fallback middleware to the pipeline.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Fallback options. Default options used if null.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpPipelineFallback(
            this IServiceCollection services,
            FallbackOptions? options = null)
        {
            options ??= FallbackOptions.Default;

            services.TryAddSingleton(options);
            services.AddSingleton<IPipelineMiddleware>(sp =>
            {
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<FallbackPipelineMiddleware>>();
                return new FallbackPipelineMiddleware(options, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds bulkhead middleware to the pipeline.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Bulkhead options. Default options used if null.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpPipelineBulkhead(
            this IServiceCollection services,
            BulkheadOptions? options = null)
        {
            options ??= BulkheadOptions.Default;

            services.TryAddSingleton(options);
            services.AddSingleton<BulkheadPipelineMiddleware>(sp =>
            {
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<BulkheadPipelineMiddleware>>();
                return new BulkheadPipelineMiddleware(options, logger);
            });
            services.AddSingleton<IPipelineMiddleware>(sp => sp.GetRequiredService<BulkheadPipelineMiddleware>());

            return services;
        }

        /// <summary>
        /// Adds dead letter middleware to the pipeline.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Dead letter options. Default options used if null.</param>
        /// <param name="storeType">Type of dead letter store to use.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpPipelineDeadLetter(
            this IServiceCollection services,
            DeadLetterOptions? options = null,
            DeadLetterStoreType storeType = DeadLetterStoreType.InMemory)
        {
            options ??= DeadLetterOptions.Default;

            services.TryAddSingleton(options);

            // Register store
            switch (storeType)
            {
                case DeadLetterStoreType.InMemory:
                    services.TryAddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>();
                    break;
                // Additional stores can be added here
                default:
                    services.TryAddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>();
                    break;
            }

            services.AddSingleton<IPipelineMiddleware>(sp =>
            {
                var store = sp.GetRequiredService<IDeadLetterStore>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<DeadLetterPipelineMiddleware>>();
                return new DeadLetterPipelineMiddleware(store, options, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds a custom dead letter store.
        /// </summary>
        /// <typeparam name="TStore">The type of dead letter store.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpPipelineDeadLetterStore<TStore>(this IServiceCollection services)
            where TStore : class, IDeadLetterStore
        {
            services.AddSingleton<IDeadLetterStore, TStore>();
            return services;
        }
    }

    /// <summary>
    /// Combined options for all pipeline resiliency features.
    /// </summary>
    public class PipelineResiliencyOptions
    {
        /// <summary>
        /// Gets or sets whether retry middleware is enabled.
        /// Default: true.
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// Gets or sets retry options.
        /// </summary>
        public RetryOptions RetryOptions { get; set; } = RetryOptions.Default;

        /// <summary>
        /// Gets or sets whether circuit breaker middleware is enabled.
        /// Default: true.
        /// </summary>
        public bool EnableCircuitBreaker { get; set; } = true;

        /// <summary>
        /// Gets or sets circuit breaker options.
        /// </summary>
        public CircuitBreakerOptions CircuitBreakerOptions { get; set; } = CircuitBreakerOptions.Default;

        /// <summary>
        /// Gets or sets whether fallback middleware is enabled.
        /// Default: false (requires explicit configuration).
        /// </summary>
        public bool EnableFallback { get; set; } = false;

        /// <summary>
        /// Gets or sets fallback options.
        /// </summary>
        public FallbackOptions FallbackOptions { get; set; } = FallbackOptions.Default;

        /// <summary>
        /// Gets or sets whether bulkhead middleware is enabled.
        /// Default: false (can impact performance if not needed).
        /// </summary>
        public bool EnableBulkhead { get; set; } = false;

        /// <summary>
        /// Gets or sets bulkhead options.
        /// </summary>
        public BulkheadOptions BulkheadOptions { get; set; } = BulkheadOptions.Default;

        /// <summary>
        /// Gets or sets whether dead letter middleware is enabled.
        /// Default: true.
        /// </summary>
        public bool EnableDeadLetter { get; set; } = true;

        /// <summary>
        /// Gets or sets dead letter options.
        /// </summary>
        public DeadLetterOptions DeadLetterOptions { get; set; } = DeadLetterOptions.Default;

        /// <summary>
        /// Gets or sets the type of dead letter store.
        /// Default: InMemory.
        /// </summary>
        public DeadLetterStoreType DeadLetterStoreType { get; set; } = DeadLetterStoreType.InMemory;

        /// <summary>
        /// Configures retry options.
        /// </summary>
        public PipelineResiliencyOptions ConfigureRetry(Action<RetryOptions> configure)
        {
            configure(RetryOptions);
            return this;
        }

        /// <summary>
        /// Configures circuit breaker options.
        /// </summary>
        public PipelineResiliencyOptions ConfigureCircuitBreaker(Action<CircuitBreakerOptions> configure)
        {
            configure(CircuitBreakerOptions);
            return this;
        }

        /// <summary>
        /// Configures fallback options.
        /// </summary>
        public PipelineResiliencyOptions ConfigureFallback(Action<FallbackOptions> configure)
        {
            EnableFallback = true;
            configure(FallbackOptions);
            return this;
        }

        /// <summary>
        /// Configures bulkhead options.
        /// </summary>
        public PipelineResiliencyOptions ConfigureBulkhead(Action<BulkheadOptions> configure)
        {
            EnableBulkhead = true;
            configure(BulkheadOptions);
            return this;
        }

        /// <summary>
        /// Configures dead letter options.
        /// </summary>
        public PipelineResiliencyOptions ConfigureDeadLetter(Action<DeadLetterOptions> configure)
        {
            configure(DeadLetterOptions);
            return this;
        }

        /// <summary>
        /// Disables all resiliency features.
        /// </summary>
        public PipelineResiliencyOptions DisableAll()
        {
            EnableRetry = false;
            EnableCircuitBreaker = false;
            EnableFallback = false;
            EnableBulkhead = false;
            EnableDeadLetter = false;
            return this;
        }

        /// <summary>
        /// Configures aggressive retry policy (more retries, shorter delays).
        /// </summary>
        public PipelineResiliencyOptions UseAggressiveRetry()
        {
            RetryOptions = RetryOptions.Aggressive;
            return this;
        }

        /// <summary>
        /// Configures conservative retry policy (fewer retries, longer delays).
        /// </summary>
        public PipelineResiliencyOptions UseConservativeRetry()
        {
            RetryOptions = RetryOptions.Conservative;
            return this;
        }

        /// <summary>
        /// Configures sensitive circuit breaker (trips quickly).
        /// </summary>
        public PipelineResiliencyOptions UseSensitiveCircuitBreaker()
        {
            CircuitBreakerOptions = CircuitBreakerOptions.Sensitive;
            return this;
        }

        /// <summary>
        /// Configures tolerant circuit breaker (allows more failures).
        /// </summary>
        public PipelineResiliencyOptions UseTolerantCircuitBreaker()
        {
            CircuitBreakerOptions = CircuitBreakerOptions.Tolerant;
            return this;
        }
    }

    /// <summary>
    /// Types of dead letter stores.
    /// </summary>
    public enum DeadLetterStoreType
    {
        /// <summary>
        /// In-memory store (for testing/single instance).
        /// </summary>
        InMemory,

        /// <summary>
        /// Custom store (register via AddMvpPipelineDeadLetterStore).
        /// </summary>
        Custom
    }
}


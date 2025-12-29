//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Resiliency
{
    /// <summary>
    /// Extension methods for configuring native .NET 9 resilience for Pipeline operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions provide modern resilience capabilities for Pipeline operations using 
    /// <c>Microsoft.Extensions.Resilience</c> and Polly v8.
    /// </para>
    /// <para>
    /// <b>Migration from custom middlewares:</b>
    /// <list type="bullet">
    ///   <item>Replace <c>RetryPipelineMiddleware</c> with <c>NativePipelineResilienceMiddleware</c></item>
    ///   <item>Replace <c>CircuitBreakerPipelineMiddleware</c> with <c>NativePipelineResilienceMiddleware</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register native pipeline resilience
    /// services.AddNativePipelineResilience(options =>
    /// {
    ///     options.EnableRetry = true;
    ///     options.RetryMaxAttempts = 3;
    ///     options.EnableCircuitBreaker = true;
    /// });
    /// 
    /// // Or use as middleware in a custom pipeline
    /// var pipeline = serviceProvider.GetRequiredService&lt;IPipeline&gt;();
    /// pipeline.AddMiddleware&lt;NativePipelineResilienceMiddleware&gt;();
    /// </code>
    /// </example>
    public static class NativePipelineResilienceExtensions
    {
        /// <summary>
        /// Adds native resilience for pipeline operations using Microsoft.Extensions.Resilience.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativePipelineResilience(this IServiceCollection services)
        {
            return services.AddNativePipelineResilience(new NativePipelineResilienceOptions());
        }

        /// <summary>
        /// Adds native resilience for pipeline operations with configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativePipelineResilience(
            this IServiceCollection services,
            Action<NativePipelineResilienceOptions> configure)
        {
            var options = new NativePipelineResilienceOptions();
            configure(options);
            return services.AddNativePipelineResilience(options);
        }

        /// <summary>
        /// Adds native resilience for pipeline operations with options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">The resilience options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativePipelineResilience(
            this IServiceCollection services,
            NativePipelineResilienceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            // Register options
            services.TryAddSingleton(options);

            // Register the resilience pipeline
            services.AddResiliencePipeline("pipeline", (builder, context) =>
            {
                var logger = context.ServiceProvider.GetService<ILoggerFactory>()
                    ?.CreateLogger("Mvp24Hours.PipelineResilience");

                ConfigurePipeline(builder, options, logger);
            });

            // Register the middleware
            services.TryAddSingleton<NativePipelineResilienceMiddleware>();
            services.AddSingleton<IPipelineMiddleware>(sp =>
                sp.GetRequiredService<NativePipelineResilienceMiddleware>());

            return services;
        }

        private static void ConfigurePipeline(
            ResiliencePipelineBuilder builder,
            NativePipelineResilienceOptions options,
            ILogger? logger)
        {
            // 1. Timeout (outermost)
            if (options.EnableTimeout)
            {
                builder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = options.TimeoutDuration,
                    OnTimeout = args =>
                    {
                        logger?.LogWarning(
                            "Pipeline operation timed out after {Timeout}",
                            args.Timeout);
                        options.OnTimeout?.Invoke(args.Timeout);
                        return default;
                    }
                });
            }

            // 2. Circuit Breaker
            if (options.EnableCircuitBreaker)
            {
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = options.CircuitBreakerFailureRatio,
                    MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                    SamplingDuration = options.CircuitBreakerSamplingDuration,
                    BreakDuration = options.CircuitBreakerBreakDuration,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<Exception>(ex => ShouldHandleForCircuitBreaker(ex, options)),
                    OnOpened = args =>
                    {
                        logger?.LogWarning(
                            args.Outcome.Exception,
                            "Pipeline circuit breaker OPENED. Break duration: {BreakDuration}",
                            args.BreakDuration);
                        options.OnCircuitBreakerOpen?.Invoke(args.Outcome.Exception);
                        return default;
                    },
                    OnClosed = args =>
                    {
                        logger?.LogInformation("Pipeline circuit breaker CLOSED");
                        options.OnCircuitBreakerReset?.Invoke();
                        return default;
                    },
                    OnHalfOpened = args =>
                    {
                        logger?.LogInformation("Pipeline circuit breaker HALF-OPEN, testing...");
                        return default;
                    }
                });
            }

            // 3. Retry (innermost)
            if (options.EnableRetry)
            {
                builder.AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = options.RetryMaxAttempts,
                    Delay = options.RetryDelay,
                    MaxDelay = options.RetryMaxDelay,
                    BackoffType = options.RetryBackoffType switch
                    {
                        PipelineResilienceBackoffType.Constant => DelayBackoffType.Constant,
                        PipelineResilienceBackoffType.Linear => DelayBackoffType.Linear,
                        _ => DelayBackoffType.Exponential
                    },
                    UseJitter = options.RetryUseJitter,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<Exception>(ex => ShouldHandleForRetry(ex, options)),
                    OnRetry = args =>
                    {
                        logger?.LogWarning(
                            args.Outcome.Exception,
                            "Pipeline operation retry {Attempt}/{MaxAttempts} after {Delay}ms",
                            args.AttemptNumber,
                            options.RetryMaxAttempts,
                            args.RetryDelay.TotalMilliseconds);
                        options.OnRetry?.Invoke(
                            args.Outcome.Exception!,
                            args.AttemptNumber,
                            args.RetryDelay);
                        return default;
                    }
                });
            }
        }

        private static bool ShouldHandleForRetry(Exception ex, NativePipelineResilienceOptions options)
        {
            // Never retry cancellation
            if (ex is OperationCanceledException)
            {
                return false;
            }

            // Use custom predicate if provided
            if (options.ShouldRetryOnException != null)
            {
                return options.ShouldRetryOnException(ex);
            }

            // Use exception type list if provided
            if (options.RetryableExceptionTypes is { Count: > 0 })
            {
                foreach (var type in options.RetryableExceptionTypes)
                {
                    if (type.IsInstanceOfType(ex))
                    {
                        return true;
                    }
                }
                return false;
            }

            // Default: retry all exceptions except specific ones
            return ex is not (ArgumentException or InvalidOperationException or NotSupportedException);
        }

        private static bool ShouldHandleForCircuitBreaker(Exception ex, NativePipelineResilienceOptions options)
        {
            // Never count cancellation as failure
            if (ex is OperationCanceledException)
            {
                return false;
            }

            // Use exception type list if provided
            if (options.CircuitBreakerExceptionTypes is { Count: > 0 })
            {
                foreach (var type in options.CircuitBreakerExceptionTypes)
                {
                    if (type.IsInstanceOfType(ex))
                    {
                        return true;
                    }
                }
                return false;
            }

            // Default: count most exceptions as failures
            return true;
        }
    }

    /// <summary>
    /// Middleware that applies native resilience to pipeline operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This middleware uses the native .NET 9 <c>ResiliencePipeline</c> to provide
    /// retry, circuit breaker, and timeout functionality for pipeline operations.
    /// </para>
    /// <para>
    /// It replaces the legacy <c>RetryPipelineMiddleware</c> and <c>CircuitBreakerPipelineMiddleware</c>.
    /// </para>
    /// </remarks>
    public class NativePipelineResilienceMiddleware : IPipelineMiddleware
    {
        private readonly ResiliencePipeline _pipeline;
        private readonly ILogger<NativePipelineResilienceMiddleware>? _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="NativePipelineResilienceMiddleware"/>.
        /// </summary>
        /// <param name="pipelineProvider">The resilience pipeline provider.</param>
        /// <param name="logger">Optional logger.</param>
        public NativePipelineResilienceMiddleware(
            ResiliencePipelineProvider<string> pipelineProvider,
            ILogger<NativePipelineResilienceMiddleware>? logger = null)
        {
            _pipeline = pipelineProvider.GetPipeline("pipeline");
            _logger = logger;
        }

        /// <inheritdoc />
        public int Order => -500; // Run early in the pipeline

        /// <inheritdoc />
        public async Task ExecuteAsync(
            IPipelineMessage message,
            Func<Task> next,
            CancellationToken cancellationToken = default)
        {
            await _pipeline.ExecuteAsync(async ct =>
            {
                await next();
            }, cancellationToken);
        }
    }

    /// <summary>
    /// Configuration options for native pipeline resilience.
    /// </summary>
    public class NativePipelineResilienceOptions
    {
        #region Retry Configuration

        /// <summary>
        /// Gets or sets whether retry is enabled.
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        public int RetryMaxAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the backoff type for retries.
        /// </summary>
        public PipelineResilienceBackoffType RetryBackoffType { get; set; } = PipelineResilienceBackoffType.ExponentialWithJitter;

        /// <summary>
        /// Gets or sets the initial delay for retry operations.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Gets or sets the maximum delay between retries.
        /// </summary>
        public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets whether to use jitter in retry delays.
        /// </summary>
        public bool RetryUseJitter { get; set; } = true;

        /// <summary>
        /// Gets or sets the exception types that should trigger a retry.
        /// </summary>
        public ICollection<Type>? RetryableExceptionTypes { get; set; }

        /// <summary>
        /// Gets or sets a custom predicate to determine if an exception should trigger a retry.
        /// </summary>
        public Func<Exception, bool>? ShouldRetryOnException { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked on each retry attempt.
        /// </summary>
        public Action<Exception, int, TimeSpan>? OnRetry { get; set; }

        #endregion

        #region Circuit Breaker Configuration

        /// <summary>
        /// Gets or sets whether the circuit breaker is enabled.
        /// </summary>
        public bool EnableCircuitBreaker { get; set; } = true;

        /// <summary>
        /// Gets or sets the failure ratio threshold (0.0 to 1.0) that opens the circuit.
        /// </summary>
        public double CircuitBreakerFailureRatio { get; set; } = 0.5;

        /// <summary>
        /// Gets or sets the minimum throughput before the circuit breaker can open.
        /// </summary>
        public int CircuitBreakerMinimumThroughput { get; set; } = 10;

        /// <summary>
        /// Gets or sets the sampling duration for calculating the failure ratio.
        /// </summary>
        public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the duration the circuit stays open before transitioning to half-open.
        /// </summary>
        public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the exception types that should count as failures for the circuit breaker.
        /// </summary>
        public ICollection<Type>? CircuitBreakerExceptionTypes { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when the circuit breaker opens.
        /// </summary>
        public Action<Exception?>? OnCircuitBreakerOpen { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when the circuit breaker resets.
        /// </summary>
        public Action? OnCircuitBreakerReset { get; set; }

        #endregion

        #region Timeout Configuration

        /// <summary>
        /// Gets or sets whether timeout is enabled.
        /// </summary>
        public bool EnableTimeout { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout duration for pipeline operations.
        /// </summary>
        public TimeSpan TimeoutDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets a callback invoked when a timeout occurs.
        /// </summary>
        public Action<TimeSpan>? OnTimeout { get; set; }

        #endregion

        #region Presets

        /// <summary>
        /// Creates default options.
        /// </summary>
        public static NativePipelineResilienceOptions Default => new();

        /// <summary>
        /// Creates options optimized for long-running operations.
        /// </summary>
        public static NativePipelineResilienceOptions LongRunning => new()
        {
            EnableRetry = true,
            RetryMaxAttempts = 5,
            RetryBackoffType = PipelineResilienceBackoffType.ExponentialWithJitter,
            RetryDelay = TimeSpan.FromSeconds(1),
            RetryMaxDelay = TimeSpan.FromMinutes(1),
            EnableCircuitBreaker = false, // Disable for long-running
            EnableTimeout = false // Disable for long-running
        };

        /// <summary>
        /// Creates options optimized for quick operations.
        /// </summary>
        public static NativePipelineResilienceOptions QuickOperations => new()
        {
            EnableRetry = true,
            RetryMaxAttempts = 2,
            RetryBackoffType = PipelineResilienceBackoffType.Linear,
            RetryDelay = TimeSpan.FromMilliseconds(100),
            RetryMaxDelay = TimeSpan.FromSeconds(1),
            EnableCircuitBreaker = true,
            CircuitBreakerMinimumThroughput = 20,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(10),
            EnableTimeout = true,
            TimeoutDuration = TimeSpan.FromSeconds(5)
        };

        #endregion
    }

    /// <summary>
    /// Backoff types for pipeline resilience retries.
    /// </summary>
    public enum PipelineResilienceBackoffType
    {
        /// <summary>
        /// Constant delay between retries.
        /// </summary>
        Constant,

        /// <summary>
        /// Linearly increasing delay.
        /// </summary>
        Linear,

        /// <summary>
        /// Exponentially increasing delay.
        /// </summary>
        Exponential,

        /// <summary>
        /// Exponentially increasing delay with jitter.
        /// </summary>
        ExponentialWithJitter
    }
}


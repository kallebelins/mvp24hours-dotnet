//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Resilience.Native
{
    /// <summary>
    /// Implementation of <see cref="INativeResiliencePipeline{TResult}"/> using 
    /// Microsoft.Extensions.Resilience and Polly v8.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <remarks>
    /// <para>
    /// This implementation uses the native .NET 9 <c>ResiliencePipeline</c> from Polly v8,
    /// which is the foundation of <c>Microsoft.Extensions.Resilience</c>.
    /// </para>
    /// <para>
    /// The pipeline is constructed based on <see cref="NativeResilienceOptions"/> and
    /// applies strategies in the following order:
    /// <list type="number">
    ///   <item>Rate Limiting (if enabled)</item>
    ///   <item>Circuit Breaker (if enabled)</item>
    ///   <item>Timeout (if enabled)</item>
    ///   <item>Retry (if enabled)</item>
    ///   <item>Operation Execution</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = NativeResilienceOptions.Database;
    /// var pipeline = new NativeResiliencePipeline&lt;Customer&gt;(options, logger);
    /// 
    /// var customer = await pipeline.ExecuteAsync(async ct =>
    /// {
    ///     return await dbContext.Customers.FindAsync(customerId, ct);
    /// }, cancellationToken);
    /// </code>
    /// </example>
    public class NativeResiliencePipeline<TResult> : INativeResiliencePipeline<TResult>
    {
        private readonly ResiliencePipeline<TResult> _pipeline;
        private readonly NativeResilienceOptions _options;
        private readonly ILogger? _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="NativeResiliencePipeline{TResult}"/>.
        /// </summary>
        /// <param name="options">The resilience options.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        public NativeResiliencePipeline(
            NativeResilienceOptions options,
            ILogger? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _pipeline = BuildPipeline(options);
        }

        /// <inheritdoc />
        public string Name => _options.Name;

        /// <inheritdoc />
        public ValueTask<TResult> ExecuteAsync(
            Func<CancellationToken, ValueTask<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            return _pipeline.ExecuteAsync(operation, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TResult> ExecuteTaskAsync(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            return await _pipeline.ExecuteAsync(
                async ct => await operation(ct),
                cancellationToken);
        }

        /// <summary>
        /// Gets the underlying Polly ResiliencePipeline for advanced scenarios.
        /// </summary>
        public ResiliencePipeline<TResult> UnderlyingPipeline => _pipeline;

        private ResiliencePipeline<TResult> BuildPipeline(NativeResilienceOptions options)
        {
            var builder = new ResiliencePipelineBuilder<TResult>();

            // Add timeout strategy (outermost - applies to entire operation)
            if (options.EnableTimeout)
            {
                builder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = options.TimeoutDuration,
                    OnTimeout = args =>
                    {
                        _logger?.LogWarning(
                            "Operation timed out after {Timeout}",
                            args.Timeout);

                        options.OnTimeout?.Invoke(args.Timeout);
                        return default;
                    }
                });

                _logger?.LogDebug(
                    "Added timeout strategy: {Timeout}",
                    options.TimeoutDuration);
            }

            // Add circuit breaker strategy
            if (options.EnableCircuitBreaker)
            {
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<TResult>
                {
                    FailureRatio = options.CircuitBreakerFailureRatio,
                    MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                    SamplingDuration = options.CircuitBreakerSamplingDuration,
                    BreakDuration = options.CircuitBreakerBreakDuration,
                    ShouldHandle = new PredicateBuilder<TResult>()
                        .Handle<Exception>(ex => ShouldHandleForCircuitBreaker(ex, options)),
                    OnOpened = args =>
                    {
                        _logger?.LogWarning(
                            "Circuit breaker opened due to {ExceptionType}. Break duration: {BreakDuration}",
                            args.Outcome.Exception?.GetType().Name ?? "unknown",
                            args.BreakDuration);

                        options.OnCircuitBreakerOpen?.Invoke(args.Outcome.Exception!);
                        options.OnCircuitStateChange?.Invoke(ResilienceCircuitState.Closed, ResilienceCircuitState.Open);
                        return default;
                    },
                    OnClosed = args =>
                    {
                        _logger?.LogInformation("Circuit breaker closed, normal operation resumed");

                        options.OnCircuitBreakerReset?.Invoke();
                        options.OnCircuitStateChange?.Invoke(ResilienceCircuitState.Open, ResilienceCircuitState.Closed);
                        return default;
                    },
                    OnHalfOpened = args =>
                    {
                        _logger?.LogInformation("Circuit breaker half-opened, testing service");

                        options.OnCircuitStateChange?.Invoke(ResilienceCircuitState.Open, ResilienceCircuitState.HalfOpen);
                        return default;
                    }
                });

                _logger?.LogDebug(
                    "Added circuit breaker strategy: FailureRatio={FailureRatio}, MinThroughput={MinThroughput}",
                    options.CircuitBreakerFailureRatio,
                    options.CircuitBreakerMinimumThroughput);
            }

            // Add retry strategy (innermost - retries individual attempts)
            if (options.EnableRetry)
            {
                var retryOptions = new RetryStrategyOptions<TResult>
                {
                    MaxRetryAttempts = options.RetryMaxAttempts,
                    Delay = options.RetryDelay,
                    MaxDelay = options.RetryMaxDelay,
                    UseJitter = options.RetryUseJitter,
                    BackoffType = ConvertBackoffType(options.RetryBackoffType),
                    ShouldHandle = new PredicateBuilder<TResult>()
                        .Handle<Exception>(ex => ShouldHandleForRetry(ex, options)),
                    OnRetry = args =>
                    {
                        _logger?.LogWarning(
                            args.Outcome.Exception,
                            "Retry attempt {AttemptNumber}/{MaxAttempts} after {DelayMs}ms",
                            args.AttemptNumber,
                            options.RetryMaxAttempts,
                            args.RetryDelay.TotalMilliseconds);

                        options.OnRetry?.Invoke(
                            args.Outcome.Exception!,
                            args.AttemptNumber,
                            args.RetryDelay);

                        return default;
                    }
                };

                builder.AddRetry(retryOptions);

                _logger?.LogDebug(
                    "Added retry strategy: MaxAttempts={MaxAttempts}, BackoffType={BackoffType}",
                    options.RetryMaxAttempts,
                    options.RetryBackoffType);
            }

            var pipeline = builder.Build();

            _logger?.LogInformation(
                "Built resilience pipeline '{Name}' with Retry={EnableRetry}, CircuitBreaker={EnableCB}, Timeout={EnableTimeout}",
                options.Name,
                options.EnableRetry,
                options.EnableCircuitBreaker,
                options.EnableTimeout);

            return pipeline;
        }

        private static bool ShouldHandleForRetry(Exception exception, NativeResilienceOptions options)
        {
            // Use custom predicate if provided
            if (options.ShouldRetryOnException != null)
            {
                return options.ShouldRetryOnException(exception);
            }

            // Use exception type list if provided
            if (options.RetryableExceptionTypes is { Count: > 0 })
            {
                foreach (var type in options.RetryableExceptionTypes)
                {
                    if (type.IsInstanceOfType(exception))
                    {
                        return true;
                    }
                }
                return false;
            }

            // Default: retry all exceptions except cancellation
            return exception is not OperationCanceledException;
        }

        private static bool ShouldHandleForCircuitBreaker(Exception exception, NativeResilienceOptions options)
        {
            // Use exception type list if provided
            if (options.CircuitBreakerExceptionTypes is { Count: > 0 })
            {
                foreach (var type in options.CircuitBreakerExceptionTypes)
                {
                    if (type.IsInstanceOfType(exception))
                    {
                        return true;
                    }
                }
                return false;
            }

            // Default: count all exceptions except cancellation as failures
            return exception is not OperationCanceledException;
        }

        private static DelayBackoffType ConvertBackoffType(ResilienceBackoffType backoffType)
        {
            return backoffType switch
            {
                ResilienceBackoffType.Constant => DelayBackoffType.Constant,
                ResilienceBackoffType.Linear => DelayBackoffType.Linear,
                ResilienceBackoffType.Exponential => DelayBackoffType.Exponential,
                ResilienceBackoffType.ExponentialWithJitter => DelayBackoffType.Exponential,
                _ => DelayBackoffType.Exponential
            };
        }
    }

    /// <summary>
    /// Implementation of <see cref="INativeResiliencePipeline"/> for void operations.
    /// </summary>
    public class NativeResiliencePipeline : INativeResiliencePipeline
    {
        private readonly ResiliencePipeline _pipeline;
        private readonly NativeResilienceOptions _options;
        private readonly ILogger? _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="NativeResiliencePipeline"/>.
        /// </summary>
        /// <param name="options">The resilience options.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        public NativeResiliencePipeline(
            NativeResilienceOptions options,
            ILogger? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _pipeline = BuildPipeline(options);
        }

        /// <inheritdoc />
        public string Name => _options.Name;

        /// <inheritdoc />
        public ValueTask ExecuteAsync(
            Func<CancellationToken, ValueTask> operation,
            CancellationToken cancellationToken = default)
        {
            return _pipeline.ExecuteAsync(operation, cancellationToken);
        }

        /// <inheritdoc />
        public async Task ExecuteTaskAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await _pipeline.ExecuteAsync(
                async ct =>
                {
                    await operation(ct);
                },
                cancellationToken);
        }

        /// <summary>
        /// Gets the underlying Polly ResiliencePipeline for advanced scenarios.
        /// </summary>
        public ResiliencePipeline UnderlyingPipeline => _pipeline;

        private ResiliencePipeline BuildPipeline(NativeResilienceOptions options)
        {
            var builder = new ResiliencePipelineBuilder();

            // Add timeout strategy
            if (options.EnableTimeout)
            {
                builder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = options.TimeoutDuration,
                    OnTimeout = args =>
                    {
                        _logger?.LogWarning("Operation timed out after {Timeout}", args.Timeout);
                        options.OnTimeout?.Invoke(args.Timeout);
                        return default;
                    }
                });
            }

            // Add circuit breaker strategy
            if (options.EnableCircuitBreaker)
            {
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = options.CircuitBreakerFailureRatio,
                    MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                    SamplingDuration = options.CircuitBreakerSamplingDuration,
                    BreakDuration = options.CircuitBreakerBreakDuration,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<Exception>(ex => ex is not OperationCanceledException),
                    OnOpened = args =>
                    {
                        _logger?.LogWarning(
                            "Circuit breaker opened. Break duration: {BreakDuration}",
                            args.BreakDuration);
                        options.OnCircuitBreakerOpen?.Invoke(args.Outcome.Exception!);
                        return default;
                    },
                    OnClosed = args =>
                    {
                        _logger?.LogInformation("Circuit breaker closed");
                        options.OnCircuitBreakerReset?.Invoke();
                        return default;
                    }
                });
            }

            // Add retry strategy
            if (options.EnableRetry)
            {
                builder.AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = options.RetryMaxAttempts,
                    Delay = options.RetryDelay,
                    MaxDelay = options.RetryMaxDelay,
                    UseJitter = options.RetryUseJitter,
                    BackoffType = options.RetryBackoffType switch
                    {
                        ResilienceBackoffType.Constant => DelayBackoffType.Constant,
                        ResilienceBackoffType.Linear => DelayBackoffType.Linear,
                        _ => DelayBackoffType.Exponential
                    },
                    ShouldHandle = new PredicateBuilder()
                        .Handle<Exception>(ex => ex is not OperationCanceledException),
                    OnRetry = args =>
                    {
                        _logger?.LogWarning(
                            args.Outcome.Exception,
                            "Retry attempt {AttemptNumber}/{MaxAttempts}",
                            args.AttemptNumber,
                            options.RetryMaxAttempts);
                        options.OnRetry?.Invoke(args.Outcome.Exception!, args.AttemptNumber, args.RetryDelay);
                        return default;
                    }
                });
            }

            return builder.Build();
        }
    }
}


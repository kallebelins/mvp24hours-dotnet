//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors
{
    /// <summary>
    /// Pipeline behavior that applies native .NET 9 resilience to CQRS requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This behavior uses <c>Microsoft.Extensions.Resilience</c> and Polly v8 to provide
    /// retry, circuit breaker, and timeout functionality for mediator requests.
    /// </para>
    /// <para>
    /// <b>Migration from custom behaviors:</b>
    /// <list type="bullet">
    ///   <item>Replace <c>RetryBehavior</c> with <c>NativeResilienceBehavior</c></item>
    ///   <item>Replace <c>CircuitBreakerBehavior</c> with <c>NativeResilienceBehavior</c></item>
    ///   <item>Replace <c>TimeoutBehavior</c> with <c>NativeResilienceBehavior</c></item>
    /// </list>
    /// </para>
    /// <para>
    /// Requests can opt-in to resilience by implementing <see cref="INativeResilient"/>
    /// or using global options.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Request with resilience
    /// public class GetCustomerQuery : IMediatorQuery&lt;Customer&gt;, INativeResilient
    /// {
    ///     public int CustomerId { get; set; }
    ///     
    ///     // Optional: override default options
    ///     public NativeCqrsResilienceOptions? ResilienceOptions => new()
    ///     {
    ///         RetryMaxAttempts = 5,
    ///         TimeoutDuration = TimeSpan.FromSeconds(10)
    ///     };
    /// }
    /// 
    /// // Register the behavior
    /// services.AddMvpMediator(options =>
    /// {
    ///     options.AddNativeResilienceBehavior();
    /// });
    /// </code>
    /// </example>
    public class NativeResilienceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly NativeCqrsResilienceOptions _defaultOptions;
        private readonly ResiliencePipelineProvider<string>? _pipelineProvider;
        private readonly ILogger<NativeResilienceBehavior<TRequest, TResponse>>? _logger;
        private ResiliencePipeline<TResponse>? _pipeline;

        /// <summary>
        /// Initializes a new instance of <see cref="NativeResilienceBehavior{TRequest, TResponse}"/>.
        /// </summary>
        /// <param name="options">The default resilience options.</param>
        /// <param name="pipelineProvider">Optional pipeline provider for keyed pipelines.</param>
        /// <param name="logger">Optional logger.</param>
        public NativeResilienceBehavior(
            NativeCqrsResilienceOptions options,
            ResiliencePipelineProvider<string>? pipelineProvider = null,
            ILogger<NativeResilienceBehavior<TRequest, TResponse>>? logger = null)
        {
            _defaultOptions = options ?? new NativeCqrsResilienceOptions();
            _pipelineProvider = pipelineProvider;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Check if request opts-in to resilience
            var resilientRequest = request as INativeResilient;
            
            if (!_defaultOptions.ApplyToAllRequests && resilientRequest == null)
            {
                // Not a resilient request and not applying globally
                return await next();
            }

            // Get effective options
            var options = resilientRequest?.ResilienceOptions ?? _defaultOptions;

            // Build or get the pipeline
            var pipeline = GetOrBuildPipeline(options, typeof(TRequest).Name);

            _logger?.LogDebug(
                "Executing request {RequestType} with native resilience",
                typeof(TRequest).Name);

            return await pipeline.ExecuteAsync(
                async ct => await next(),
                cancellationToken);
        }

        private ResiliencePipeline<TResponse> GetOrBuildPipeline(
            NativeCqrsResilienceOptions options,
            string requestTypeName)
        {
            // If we have a provider and a pipeline name, try to get it
            if (_pipelineProvider != null && !string.IsNullOrEmpty(options.PipelineName))
            {
                try
                {
                    return _pipelineProvider.GetPipeline<TResponse>(options.PipelineName);
                }
                catch
                {
                    // Pipeline not found, build one
                }
            }

            // Build pipeline if not cached
            if (_pipeline == null)
            {
                _pipeline = BuildPipeline(options, requestTypeName);
            }

            return _pipeline;
        }

        private ResiliencePipeline<TResponse> BuildPipeline(
            NativeCqrsResilienceOptions options,
            string requestTypeName)
        {
            var builder = new ResiliencePipelineBuilder<TResponse>();

            // 1. Timeout (outermost)
            if (options.EnableTimeout)
            {
                builder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = options.TimeoutDuration,
                    OnTimeout = args =>
                    {
                        _logger?.LogWarning(
                            "CQRS request {RequestType} timed out after {Timeout}",
                            requestTypeName,
                            args.Timeout);
                        options.OnTimeout?.Invoke(args.Timeout);
                        return default;
                    }
                });
            }

            // 2. Circuit Breaker
            if (options.EnableCircuitBreaker)
            {
                builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<TResponse>
                {
                    FailureRatio = options.CircuitBreakerFailureRatio,
                    MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                    SamplingDuration = options.CircuitBreakerSamplingDuration,
                    BreakDuration = options.CircuitBreakerBreakDuration,
                    ShouldHandle = new PredicateBuilder<TResponse>()
                        .Handle<Exception>(ex => ShouldHandleForCircuitBreaker(ex, options)),
                    OnOpened = args =>
                    {
                        _logger?.LogWarning(
                            args.Outcome.Exception,
                            "CQRS circuit breaker OPENED for {RequestType}. Break: {BreakDuration}",
                            requestTypeName,
                            args.BreakDuration);
                        options.OnCircuitBreakerOpen?.Invoke(args.Outcome.Exception);
                        return default;
                    },
                    OnClosed = args =>
                    {
                        _logger?.LogInformation(
                            "CQRS circuit breaker CLOSED for {RequestType}",
                            requestTypeName);
                        options.OnCircuitBreakerReset?.Invoke();
                        return default;
                    }
                });
            }

            // 3. Retry (innermost)
            if (options.EnableRetry)
            {
                builder.AddRetry(new RetryStrategyOptions<TResponse>
                {
                    MaxRetryAttempts = options.RetryMaxAttempts,
                    Delay = options.RetryDelay,
                    MaxDelay = options.RetryMaxDelay,
                    BackoffType = options.RetryBackoffType switch
                    {
                        CqrsResilienceBackoffType.Constant => DelayBackoffType.Constant,
                        CqrsResilienceBackoffType.Linear => DelayBackoffType.Linear,
                        _ => DelayBackoffType.Exponential
                    },
                    UseJitter = options.RetryUseJitter,
                    ShouldHandle = new PredicateBuilder<TResponse>()
                        .Handle<Exception>(ex => ShouldHandleForRetry(ex, options)),
                    OnRetry = args =>
                    {
                        _logger?.LogWarning(
                            args.Outcome.Exception,
                            "CQRS request {RequestType} retry {Attempt}/{Max} after {Delay}ms",
                            requestTypeName,
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

            return builder.Build();
        }

        private static bool ShouldHandleForRetry(Exception ex, NativeCqrsResilienceOptions options)
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

            // Default: don't retry validation or authorization exceptions
            var exTypeName = ex.GetType().Name;
            return !exTypeName.Contains("Validation") &&
                   !exTypeName.Contains("Unauthorized") &&
                   !exTypeName.Contains("Forbidden") &&
                   !exTypeName.Contains("NotFound");
        }

        private static bool ShouldHandleForCircuitBreaker(Exception ex, NativeCqrsResilienceOptions options)
        {
            // Never count cancellation as failure
            if (ex is OperationCanceledException)
            {
                return false;
            }

            // Don't open circuit for business logic exceptions
            var exTypeName = ex.GetType().Name;
            return !exTypeName.Contains("Validation") &&
                   !exTypeName.Contains("NotFound") &&
                   !exTypeName.Contains("Unauthorized") &&
                   !exTypeName.Contains("Forbidden");
        }
    }

    /// <summary>
    /// Marker interface for requests that should have native resilience applied.
    /// </summary>
    public interface INativeResilient
    {
        /// <summary>
        /// Gets the resilience options for this request. Return null to use defaults.
        /// </summary>
        NativeCqrsResilienceOptions? ResilienceOptions => null;
    }

    /// <summary>
    /// Configuration options for native CQRS resilience.
    /// </summary>
    public class NativeCqrsResilienceOptions
    {
        /// <summary>
        /// Gets or sets whether to apply resilience to all requests.
        /// If false, only requests implementing <see cref="INativeResilient"/> will be affected.
        /// </summary>
        public bool ApplyToAllRequests { get; set; } = false;

        /// <summary>
        /// Gets or sets the name of a registered resilience pipeline to use.
        /// If set, the pre-configured pipeline will be used instead of building one.
        /// </summary>
        public string? PipelineName { get; set; }

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
        public CqrsResilienceBackoffType RetryBackoffType { get; set; } = CqrsResilienceBackoffType.ExponentialWithJitter;

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
        /// Gets or sets the timeout duration.
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
        public static NativeCqrsResilienceOptions Default => new();

        /// <summary>
        /// Creates options for commands (more retries, longer timeouts).
        /// </summary>
        public static NativeCqrsResilienceOptions ForCommands => new()
        {
            EnableRetry = true,
            RetryMaxAttempts = 3,
            RetryBackoffType = CqrsResilienceBackoffType.ExponentialWithJitter,
            RetryDelay = TimeSpan.FromMilliseconds(500),
            EnableCircuitBreaker = true,
            CircuitBreakerMinimumThroughput = 5,
            EnableTimeout = true,
            TimeoutDuration = TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// Creates options for queries (fewer retries, shorter timeouts).
        /// </summary>
        public static NativeCqrsResilienceOptions ForQueries => new()
        {
            EnableRetry = true,
            RetryMaxAttempts = 2,
            RetryBackoffType = CqrsResilienceBackoffType.Linear,
            RetryDelay = TimeSpan.FromMilliseconds(100),
            EnableCircuitBreaker = true,
            CircuitBreakerMinimumThroughput = 20,
            EnableTimeout = true,
            TimeoutDuration = TimeSpan.FromSeconds(10)
        };

        #endregion
    }

    /// <summary>
    /// Backoff types for CQRS resilience retries.
    /// </summary>
    public enum CqrsResilienceBackoffType
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

    /// <summary>
    /// Extension methods for registering native resilience behavior with the mediator.
    /// </summary>
    public static class NativeResilienceBehaviorExtensions
    {
        /// <summary>
        /// Adds the native resilience behavior to the mediator pipeline.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeCqrsResilience(this IServiceCollection services)
        {
            return services.AddNativeCqrsResilience(new NativeCqrsResilienceOptions());
        }

        /// <summary>
        /// Adds the native resilience behavior to the mediator pipeline with configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeCqrsResilience(
            this IServiceCollection services,
            Action<NativeCqrsResilienceOptions> configure)
        {
            var options = new NativeCqrsResilienceOptions();
            configure(options);
            return services.AddNativeCqrsResilience(options);
        }

        /// <summary>
        /// Adds the native resilience behavior to the mediator pipeline with options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">The resilience options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeCqrsResilience(
            this IServiceCollection services,
            NativeCqrsResilienceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            services.AddSingleton(options);
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(NativeResilienceBehavior<,>));

            return services;
        }
    }
}


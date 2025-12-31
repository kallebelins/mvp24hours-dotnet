//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.CronJob.Resiliency
{
    /// <summary>
    /// Default implementation of <see cref="ICronJobResilienceConfig{T}"/> providing
    /// sensible defaults for resilience policies.
    /// </summary>
    /// <typeparam name="T">The type of the CronJob service being configured.</typeparam>
    /// <remarks>
    /// <para>
    /// This class provides default values that work well for most scenarios:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Retry:</b> 3 attempts with exponential backoff (1s, 2s, 4s)</item>
    /// <item><b>Circuit Breaker:</b> Opens after 5 failures, stays open for 30s</item>
    /// <item><b>Overlapping:</b> Prevented by default, skips if previous execution is running</item>
    /// <item><b>Graceful Shutdown:</b> 30 second timeout</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new CronJobResilienceConfig&lt;MyJob&gt;
    /// {
    ///     EnableRetry = true,
    ///     MaxRetryAttempts = 5,
    ///     EnableCircuitBreaker = true,
    ///     CircuitBreakerFailureThreshold = 10,
    ///     PreventOverlapping = true,
    ///     GracefulShutdownTimeout = TimeSpan.FromMinutes(1)
    /// };
    /// </code>
    /// </example>
    public class CronJobResilienceConfig<T> : ICronJobResilienceConfig<T>
    {
        #region Retry Configuration

        /// <inheritdoc />
        public bool EnableRetry { get; set; }

        /// <inheritdoc />
        public int MaxRetryAttempts { get; set; } = 3;

        /// <inheritdoc />
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <inheritdoc />
        public bool UseExponentialBackoff { get; set; } = true;

        /// <inheritdoc />
        public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <inheritdoc />
        public double RetryJitterFactor { get; set; } = 0.2;

        /// <inheritdoc />
        public Func<Exception, bool>? ShouldRetryOnException { get; set; }

        #endregion

        #region Circuit Breaker Configuration

        /// <inheritdoc />
        public bool EnableCircuitBreaker { get; set; }

        /// <inheritdoc />
        public int CircuitBreakerFailureThreshold { get; set; } = 5;

        /// <inheritdoc />
        public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <inheritdoc />
        public int CircuitBreakerSuccessThreshold { get; set; } = 1;

        /// <inheritdoc />
        public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromSeconds(60);

        #endregion

        #region Overlapping Execution Control

        /// <inheritdoc />
        public bool PreventOverlapping { get; set; } = true;

        /// <inheritdoc />
        public bool LogOverlappingSkipped { get; set; } = true;

        /// <inheritdoc />
        public TimeSpan OverlappingWaitTimeout { get; set; } = TimeSpan.Zero;

        #endregion

        #region Graceful Shutdown Configuration

        /// <inheritdoc />
        public TimeSpan GracefulShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <inheritdoc />
        public bool WaitForExecutionOnShutdown { get; set; } = true;

        #endregion

        #region Cancellation Token Configuration

        /// <inheritdoc />
        public bool PropagateCancellation { get; set; } = true;

        /// <inheritdoc />
        public TimeSpan? ExecutionTimeout { get; set; }

        #endregion

        #region Callback Hooks

        /// <inheritdoc />
        public Action<Exception, int, TimeSpan>? OnRetry { get; set; }

        /// <inheritdoc />
        public Action<CircuitBreakerState, CircuitBreakerState>? OnCircuitBreakerStateChange { get; set; }

        /// <inheritdoc />
        public Action? OnOverlappingSkipped { get; set; }

        /// <inheritdoc />
        public Action<Exception>? OnJobFailed { get; set; }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a default resilience configuration with no resilience features enabled.
        /// </summary>
        /// <returns>A new <see cref="CronJobResilienceConfig{T}"/> with default values.</returns>
        public static CronJobResilienceConfig<T> Default() => new();

        /// <summary>
        /// Creates a resilience configuration with retry enabled.
        /// </summary>
        /// <param name="maxAttempts">Maximum number of retry attempts.</param>
        /// <param name="useExponentialBackoff">Whether to use exponential backoff.</param>
        /// <returns>A new <see cref="CronJobResilienceConfig{T}"/> with retry enabled.</returns>
        public static CronJobResilienceConfig<T> WithRetry(int maxAttempts = 3, bool useExponentialBackoff = true)
        {
            return new CronJobResilienceConfig<T>
            {
                EnableRetry = true,
                MaxRetryAttempts = maxAttempts,
                UseExponentialBackoff = useExponentialBackoff
            };
        }

        /// <summary>
        /// Creates a resilience configuration with circuit breaker enabled.
        /// </summary>
        /// <param name="failureThreshold">Number of failures before opening the circuit.</param>
        /// <param name="duration">Duration the circuit stays open.</param>
        /// <returns>A new <see cref="CronJobResilienceConfig{T}"/> with circuit breaker enabled.</returns>
        public static CronJobResilienceConfig<T> WithCircuitBreaker(int failureThreshold = 5, TimeSpan? duration = null)
        {
            return new CronJobResilienceConfig<T>
            {
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = failureThreshold,
                CircuitBreakerDuration = duration ?? TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// Creates a resilience configuration with all resilience features enabled.
        /// </summary>
        /// <returns>A new <see cref="CronJobResilienceConfig{T}"/> with full resilience.</returns>
        public static CronJobResilienceConfig<T> FullResilience()
        {
            return new CronJobResilienceConfig<T>
            {
                EnableRetry = true,
                MaxRetryAttempts = 3,
                UseExponentialBackoff = true,
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 5,
                PreventOverlapping = true,
                GracefulShutdownTimeout = TimeSpan.FromSeconds(30),
                PropagateCancellation = true
            };
        }

        #endregion

        /// <summary>
        /// Returns a string representation of the resilience configuration.
        /// </summary>
        /// <returns>A string containing the key configuration values.</returns>
        public override string ToString()
        {
            var features = new System.Collections.Generic.List<string>();
            
            if (EnableRetry)
                features.Add($"Retry({MaxRetryAttempts}x)");
            
            if (EnableCircuitBreaker)
                features.Add($"CircuitBreaker({CircuitBreakerFailureThreshold}/{CircuitBreakerDuration.TotalSeconds}s)");
            
            if (PreventOverlapping)
                features.Add("PreventOverlapping");
            
            if (ExecutionTimeout.HasValue)
                features.Add($"Timeout({ExecutionTimeout.Value.TotalSeconds}s)");

            var featuresStr = features.Count > 0 ? string.Join(", ", features) : "None";
            return $"CronJobResilienceConfig<{typeof(T).Name}>[{featuresStr}]";
        }
    }
}


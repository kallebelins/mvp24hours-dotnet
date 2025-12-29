//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Resilience.Native
{
    /// <summary>
    /// Configuration options for native resilience pipelines using Microsoft.Extensions.Resilience.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options configure the behavior of <c>ResiliencePipeline</c> from .NET 9's
    /// <c>Microsoft.Extensions.Resilience</c> package. They provide a unified configuration
    /// model for retry, circuit breaker, timeout, rate limiting, and hedging strategies.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddNativeResilience(options =>
    /// {
    ///     options.EnableRetry = true;
    ///     options.RetryMaxAttempts = 3;
    ///     options.RetryBackoffType = BackoffType.Exponential;
    ///     
    ///     options.EnableCircuitBreaker = true;
    ///     options.CircuitBreakerFailureRatio = 0.5;
    ///     
    ///     options.EnableTimeout = true;
    ///     options.TimeoutDuration = TimeSpan.FromSeconds(30);
    /// });
    /// </code>
    /// </example>
    public class NativeResilienceOptions
    {
        /// <summary>
        /// Gets or sets the name of the resilience pipeline.
        /// </summary>
        public string Name { get; set; } = "Mvp24Hours-Resilience";

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
        /// Gets or sets the type of backoff strategy.
        /// </summary>
        public ResilienceBackoffType RetryBackoffType { get; set; } = ResilienceBackoffType.Exponential;

        /// <summary>
        /// Gets or sets the initial delay for retry operations.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum delay between retries.
        /// </summary>
        public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to use jitter in retry delays.
        /// </summary>
        public bool RetryUseJitter { get; set; } = true;

        /// <summary>
        /// Gets or sets the exception types that should trigger a retry.
        /// If null or empty, all exceptions will be retried.
        /// </summary>
        public ICollection<Type>? RetryableExceptionTypes { get; set; }

        /// <summary>
        /// Gets or sets a custom predicate to determine if an exception should trigger a retry.
        /// </summary>
        public Func<Exception, bool>? ShouldRetryOnException { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked on each retry attempt.
        /// Parameters: exception, attempt number, delay.
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
        /// If null or empty, all exceptions count as failures.
        /// </summary>
        public ICollection<Type>? CircuitBreakerExceptionTypes { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when the circuit breaker state changes.
        /// Parameters: old state, new state.
        /// </summary>
        public Action<ResilienceCircuitState, ResilienceCircuitState>? OnCircuitStateChange { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when the circuit opens.
        /// </summary>
        public Action<Exception>? OnCircuitBreakerOpen { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when the circuit resets to closed.
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

        #region Rate Limiting Configuration

        /// <summary>
        /// Gets or sets whether rate limiting is enabled.
        /// </summary>
        public bool EnableRateLimiting { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of permits per window.
        /// </summary>
        public int RateLimitPermitLimit { get; set; } = 100;

        /// <summary>
        /// Gets or sets the time window for rate limiting.
        /// </summary>
        public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the queue limit for rate limiting.
        /// </summary>
        public int RateLimitQueueLimit { get; set; } = 0;

        #endregion

        #region Telemetry Configuration

        /// <summary>
        /// Gets or sets whether telemetry is enabled.
        /// </summary>
        public bool EnableTelemetry { get; set; } = true;

        /// <summary>
        /// Gets or sets the service name for telemetry.
        /// </summary>
        public string? TelemetryServiceName { get; set; }

        #endregion

        #region Presets

        /// <summary>
        /// Creates a preset for high availability scenarios (more retries, longer timeouts).
        /// </summary>
        public static NativeResilienceOptions HighAvailability => new()
        {
            Name = "Mvp24Hours-HighAvailability",
            EnableRetry = true,
            RetryMaxAttempts = 5,
            RetryBackoffType = ResilienceBackoffType.Exponential,
            RetryDelay = TimeSpan.FromSeconds(2),
            RetryMaxDelay = TimeSpan.FromSeconds(60),
            RetryUseJitter = true,
            EnableCircuitBreaker = true,
            CircuitBreakerFailureRatio = 0.5,
            CircuitBreakerMinimumThroughput = 5,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(60),
            EnableTimeout = true,
            TimeoutDuration = TimeSpan.FromSeconds(60)
        };

        /// <summary>
        /// Creates a preset for low latency scenarios (fewer retries, shorter timeouts).
        /// </summary>
        public static NativeResilienceOptions LowLatency => new()
        {
            Name = "Mvp24Hours-LowLatency",
            EnableRetry = true,
            RetryMaxAttempts = 2,
            RetryBackoffType = ResilienceBackoffType.Linear,
            RetryDelay = TimeSpan.FromMilliseconds(100),
            RetryMaxDelay = TimeSpan.FromSeconds(2),
            RetryUseJitter = true,
            EnableCircuitBreaker = true,
            CircuitBreakerFailureRatio = 0.3,
            CircuitBreakerMinimumThroughput = 20,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(15),
            EnableTimeout = true,
            TimeoutDuration = TimeSpan.FromSeconds(5)
        };

        /// <summary>
        /// Creates a preset for batch processing (more retries, no timeout, no circuit breaker).
        /// </summary>
        public static NativeResilienceOptions BatchProcessing => new()
        {
            Name = "Mvp24Hours-BatchProcessing",
            EnableRetry = true,
            RetryMaxAttempts = 10,
            RetryBackoffType = ResilienceBackoffType.Exponential,
            RetryDelay = TimeSpan.FromSeconds(5),
            RetryMaxDelay = TimeSpan.FromMinutes(5),
            RetryUseJitter = true,
            EnableCircuitBreaker = false,
            EnableTimeout = false
        };

        /// <summary>
        /// Creates a preset for database operations.
        /// </summary>
        public static NativeResilienceOptions Database => new()
        {
            Name = "Mvp24Hours-Database",
            EnableRetry = true,
            RetryMaxAttempts = 3,
            RetryBackoffType = ResilienceBackoffType.ExponentialWithJitter,
            RetryDelay = TimeSpan.FromMilliseconds(500),
            RetryMaxDelay = TimeSpan.FromSeconds(10),
            RetryUseJitter = true,
            EnableCircuitBreaker = true,
            CircuitBreakerFailureRatio = 0.5,
            CircuitBreakerMinimumThroughput = 5,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30),
            EnableTimeout = true,
            TimeoutDuration = TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// Creates a preset for messaging operations.
        /// </summary>
        public static NativeResilienceOptions Messaging => new()
        {
            Name = "Mvp24Hours-Messaging",
            EnableRetry = true,
            RetryMaxAttempts = 5,
            RetryBackoffType = ResilienceBackoffType.Exponential,
            RetryDelay = TimeSpan.FromSeconds(1),
            RetryMaxDelay = TimeSpan.FromSeconds(30),
            RetryUseJitter = true,
            EnableCircuitBreaker = true,
            CircuitBreakerFailureRatio = 0.5,
            CircuitBreakerMinimumThroughput = 10,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30),
            EnableTimeout = true,
            TimeoutDuration = TimeSpan.FromSeconds(10)
        };

        /// <summary>
        /// Creates default options.
        /// </summary>
        public static NativeResilienceOptions Default => new();

        #endregion
    }

    /// <summary>
    /// Types of backoff strategies for retry operations.
    /// </summary>
    public enum ResilienceBackoffType
    {
        /// <summary>
        /// Constant delay between retries.
        /// </summary>
        Constant,

        /// <summary>
        /// Linearly increasing delay between retries.
        /// </summary>
        Linear,

        /// <summary>
        /// Exponentially increasing delay between retries (2^n).
        /// </summary>
        Exponential,

        /// <summary>
        /// Exponentially increasing delay with jitter to prevent thundering herd.
        /// </summary>
        ExponentialWithJitter
    }

    /// <summary>
    /// States of the circuit breaker.
    /// </summary>
    public enum ResilienceCircuitState
    {
        /// <summary>
        /// The circuit is closed and requests pass through normally.
        /// </summary>
        Closed,

        /// <summary>
        /// The circuit is open and requests fail immediately.
        /// </summary>
        Open,

        /// <summary>
        /// The circuit is half-open, allowing limited requests to test recovery.
        /// </summary>
        HalfOpen,

        /// <summary>
        /// The circuit has been manually isolated.
        /// </summary>
        Isolated
    }
}


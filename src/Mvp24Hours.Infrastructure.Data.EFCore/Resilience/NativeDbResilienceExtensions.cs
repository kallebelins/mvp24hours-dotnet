//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Resilience
{
    /// <summary>
    /// Extension methods for configuring native .NET 9 resilience for EF Core database operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions provide modern resilience capabilities for EF Core using 
    /// <c>Microsoft.Extensions.Resilience</c> and Polly v8.
    /// </para>
    /// <para>
    /// <b>Key Features:</b>
    /// <list type="bullet">
    ///   <item>Retry with exponential backoff for transient database errors</item>
    ///   <item>Circuit breaker to prevent cascading failures</item>
    ///   <item>Timeout enforcement for long-running queries</item>
    ///   <item>Automatic telemetry integration</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Migration from MvpExecutionStrategy:</b>
    /// The <c>MvpExecutionStrategy</c> class is now deprecated. Use these native extensions instead
    /// for better performance and simpler configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register the resilience pipeline
    /// services.AddNativeDbResilience(options =>
    /// {
    ///     options.EnableRetry = true;
    ///     options.RetryMaxAttempts = 5;
    ///     options.EnableCircuitBreaker = true;
    /// });
    /// 
    /// // Use in a repository
    /// public class CustomerRepository
    /// {
    ///     private readonly ResiliencePipeline _pipeline;
    ///     
    ///     public CustomerRepository(
    ///         [FromKeyedServices("database")] ResiliencePipeline pipeline)
    ///     {
    ///         _pipeline = pipeline;
    ///     }
    ///     
    ///     public async Task&lt;Customer&gt; GetByIdAsync(int id, CancellationToken ct)
    ///     {
    ///         return await _pipeline.ExecuteAsync(async token =>
    ///         {
    ///             return await _dbContext.Customers.FindAsync(id, token);
    ///         }, ct);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static class NativeDbResilienceExtensions
    {
        /// <summary>
        /// Adds native database resilience pipeline using Microsoft.Extensions.Resilience.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the pipeline (default: "database").</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeDbResilience(
            this IServiceCollection services,
            string name = "database")
        {
            return services.AddNativeDbResilience(name, new NativeDbResilienceOptions());
        }

        /// <summary>
        /// Adds native database resilience pipeline with configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeDbResilience(
            this IServiceCollection services,
            Action<NativeDbResilienceOptions> configure)
        {
            var options = new NativeDbResilienceOptions();
            configure(options);
            return services.AddNativeDbResilience("database", options);
        }

        /// <summary>
        /// Adds native database resilience pipeline with options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the pipeline.</param>
        /// <param name="options">The resilience options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeDbResilience(
            this IServiceCollection services,
            string name,
            NativeDbResilienceOptions options)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(options);

            services.AddResiliencePipeline(name, (builder, context) =>
            {
                var logger = context.ServiceProvider.GetService<ILoggerFactory>()
                    ?.CreateLogger("Mvp24Hours.DbResilience");

                // 1. Timeout (outermost)
                if (options.EnableTimeout)
                {
                    builder.AddTimeout(new TimeoutStrategyOptions
                    {
                        Timeout = options.TimeoutDuration,
                        OnTimeout = args =>
                        {
                            logger?.LogWarning(
                                "Database operation timed out after {Timeout}",
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
                            .Handle<DbException>()
                            .Handle<TimeoutException>()
                            .Handle<InvalidOperationException>(ex =>
                                ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)),
                        OnOpened = args =>
                        {
                            logger?.LogWarning(
                                args.Outcome.Exception,
                                "Database circuit breaker OPENED. Break duration: {BreakDuration}",
                                args.BreakDuration);
                            options.OnCircuitBreakerOpen?.Invoke(args.Outcome.Exception);
                            return default;
                        },
                        OnClosed = args =>
                        {
                            logger?.LogInformation("Database circuit breaker CLOSED");
                            options.OnCircuitBreakerReset?.Invoke();
                            return default;
                        },
                        OnHalfOpened = args =>
                        {
                            logger?.LogInformation("Database circuit breaker HALF-OPEN, testing...");
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
                            DbResilienceBackoffType.Constant => DelayBackoffType.Constant,
                            DbResilienceBackoffType.Linear => DelayBackoffType.Linear,
                            _ => DelayBackoffType.Exponential
                        },
                        UseJitter = options.RetryUseJitter,
                        ShouldHandle = new PredicateBuilder()
                            .Handle<DbException>(ex => IsTransientException(ex, options))
                            .Handle<TimeoutException>()
                            .Handle<InvalidOperationException>(ex =>
                                ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)),
                        OnRetry = args =>
                        {
                            logger?.LogWarning(
                                args.Outcome.Exception,
                                "Database operation retry {Attempt}/{MaxAttempts} after {Delay}ms",
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
            });

            return services;
        }

        /// <summary>
        /// Determines if a database exception is transient and should be retried.
        /// </summary>
        private static bool IsTransientException(DbException ex, NativeDbResilienceOptions options)
        {
            // Use custom predicate if provided
            if (options.ShouldRetryOnException != null)
            {
                return options.ShouldRetryOnException(ex);
            }

            // Check for specific error numbers if provided
            if (options.TransientErrorNumbers is { Count: > 0 })
            {
                // For SQL Server: check ErrorCode/Number
                // This is a simplified check - actual implementation may need provider-specific logic
                var errorCode = GetErrorCode(ex);
                return options.TransientErrorNumbers.Contains(errorCode);
            }

            // Default transient detection - check common patterns
            return IsDefaultTransientException(ex);
        }

        /// <summary>
        /// Gets the error code from a database exception.
        /// </summary>
        private static int GetErrorCode(DbException ex)
        {
            // Try to get error code via reflection or known properties
            // This handles SQL Server, PostgreSQL, MySQL etc.
            var errorCodeProp = ex.GetType().GetProperty("Number") ??
                               ex.GetType().GetProperty("ErrorCode");

            if (errorCodeProp != null)
            {
                var value = errorCodeProp.GetValue(ex);
                if (value is int code)
                {
                    return code;
                }
            }

            return ex.ErrorCode;
        }

        /// <summary>
        /// Default transient exception detection based on common patterns.
        /// </summary>
        private static bool IsDefaultTransientException(DbException ex)
        {
            var message = ex.Message?.ToLowerInvariant() ?? string.Empty;

            // Common transient patterns
            return message.Contains("timeout") ||
                   message.Contains("deadlock") ||
                   message.Contains("connection") ||
                   message.Contains("transport") ||
                   message.Contains("network") ||
                   message.Contains("login failed") ||
                   message.Contains("server is not available") ||
                   message.Contains("too many connections") ||
                   message.Contains("resource busy");
        }
    }

    /// <summary>
    /// Configuration options for native database resilience.
    /// </summary>
    public class NativeDbResilienceOptions
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
        public DbResilienceBackoffType RetryBackoffType { get; set; } = DbResilienceBackoffType.ExponentialWithJitter;

        /// <summary>
        /// Gets or sets the initial delay for retry operations.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets or sets the maximum delay between retries.
        /// </summary>
        public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to use jitter in retry delays.
        /// </summary>
        public bool RetryUseJitter { get; set; } = true;

        /// <summary>
        /// Gets or sets specific error numbers that should trigger a retry.
        /// </summary>
        public ICollection<int>? TransientErrorNumbers { get; set; }

        /// <summary>
        /// Gets or sets a custom predicate to determine if an exception should trigger a retry.
        /// </summary>
        public Func<DbException, bool>? ShouldRetryOnException { get; set; }

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
        public int CircuitBreakerMinimumThroughput { get; set; } = 5;

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
        /// Gets or sets the timeout duration for database operations.
        /// </summary>
        public TimeSpan TimeoutDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets a callback invoked when a timeout occurs.
        /// </summary>
        public Action<TimeSpan>? OnTimeout { get; set; }

        #endregion

        #region Presets

        /// <summary>
        /// Creates options optimized for SQL Server.
        /// </summary>
        public static NativeDbResilienceOptions SqlServer => new()
        {
            EnableRetry = true,
            RetryMaxAttempts = 5,
            RetryBackoffType = DbResilienceBackoffType.ExponentialWithJitter,
            RetryDelay = TimeSpan.FromMilliseconds(500),
            RetryMaxDelay = TimeSpan.FromSeconds(30),
            TransientErrorNumbers = new HashSet<int>
            {
                // SQL Server transient error codes
                -2, // Timeout
                20, // Instance does not support encryption
                64, // Connection was successfully established with the server
                233, // Connection initialization error
                10053, // Error occurred during connection establishment
                10054, // Connection was forcibly closed
                10060, // Connection attempt failed
                40197, // Error processing request
                40501, // Service is busy
                40613, // Database is unavailable
                49918, // Cannot process request
                49919, // Cannot process create or update request
                49920, // Cannot process request due to too many operations
            },
            EnableCircuitBreaker = true,
            EnableTimeout = true
        };

        /// <summary>
        /// Creates options optimized for PostgreSQL.
        /// </summary>
        public static NativeDbResilienceOptions PostgreSql => new()
        {
            EnableRetry = true,
            RetryMaxAttempts = 3,
            RetryBackoffType = DbResilienceBackoffType.ExponentialWithJitter,
            RetryDelay = TimeSpan.FromMilliseconds(500),
            RetryMaxDelay = TimeSpan.FromSeconds(30),
            EnableCircuitBreaker = true,
            EnableTimeout = true
        };

        /// <summary>
        /// Creates options optimized for MySQL.
        /// </summary>
        public static NativeDbResilienceOptions MySql => new()
        {
            EnableRetry = true,
            RetryMaxAttempts = 3,
            RetryBackoffType = DbResilienceBackoffType.ExponentialWithJitter,
            RetryDelay = TimeSpan.FromMilliseconds(500),
            RetryMaxDelay = TimeSpan.FromSeconds(30),
            TransientErrorNumbers = new HashSet<int>
            {
                // MySQL transient error codes
                1040, // Too many connections
                1205, // Lock wait timeout exceeded
                1213, // Deadlock found
                2006, // MySQL server has gone away
                2013, // Lost connection to MySQL server
            },
            EnableCircuitBreaker = true,
            EnableTimeout = true
        };

        #endregion
    }

    /// <summary>
    /// Backoff types for database resilience retries.
    /// </summary>
    public enum DbResilienceBackoffType
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


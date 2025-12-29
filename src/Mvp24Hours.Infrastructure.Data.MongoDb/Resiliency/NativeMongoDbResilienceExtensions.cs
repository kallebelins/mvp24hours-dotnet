//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency
{
    /// <summary>
    /// Extension methods for configuring native .NET 9 resilience for MongoDB operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions provide modern resilience capabilities for MongoDB using 
    /// <c>Microsoft.Extensions.Resilience</c> and Polly v8.
    /// </para>
    /// <para>
    /// <b>Key Features:</b>
    /// <list type="bullet">
    ///   <item>Retry with exponential backoff for transient MongoDB errors</item>
    ///   <item>Circuit breaker to prevent cascading failures</item>
    ///   <item>Timeout enforcement for long-running operations</item>
    ///   <item>Automatic telemetry integration</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Migration from MongoDbResiliencyPolicy:</b>
    /// The <c>MongoDbResiliencyPolicy</c> class is now deprecated. Use these native extensions instead
    /// for better performance and simpler configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register the resilience pipeline
    /// services.AddNativeMongoDbResilience(options =>
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
    ///         [FromKeyedServices("mongodb")] ResiliencePipeline pipeline)
    ///     {
    ///         _pipeline = pipeline;
    ///     }
    ///     
    ///     public async Task&lt;Customer&gt; GetByIdAsync(string id, CancellationToken ct)
    ///     {
    ///         return await _pipeline.ExecuteAsync(async token =>
    ///         {
    ///             return await _collection.Find(c => c.Id == id).FirstOrDefaultAsync(token);
    ///         }, ct);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static class NativeMongoDbResilienceExtensions
    {
        /// <summary>
        /// Adds native MongoDB resilience pipeline using Microsoft.Extensions.Resilience.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the pipeline (default: "mongodb").</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeMongoDbResilience(
            this IServiceCollection services,
            string name = "mongodb")
        {
            return services.AddNativeMongoDbResilience(name, new NativeMongoDbResilienceOptions());
        }

        /// <summary>
        /// Adds native MongoDB resilience pipeline with configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeMongoDbResilience(
            this IServiceCollection services,
            Action<NativeMongoDbResilienceOptions> configure)
        {
            var options = new NativeMongoDbResilienceOptions();
            configure(options);
            return services.AddNativeMongoDbResilience("mongodb", options);
        }

        /// <summary>
        /// Adds native MongoDB resilience pipeline with options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the pipeline.</param>
        /// <param name="options">The resilience options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNativeMongoDbResilience(
            this IServiceCollection services,
            string name,
            NativeMongoDbResilienceOptions options)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(options);

            services.AddResiliencePipeline(name, (builder, context) =>
            {
                var logger = context.ServiceProvider.GetService<ILoggerFactory>()
                    ?.CreateLogger("Mvp24Hours.MongoDbResilience");

                // 1. Timeout (outermost)
                if (options.EnableTimeout)
                {
                    builder.AddTimeout(new TimeoutStrategyOptions
                    {
                        Timeout = options.TimeoutDuration,
                        OnTimeout = args =>
                        {
                            logger?.LogWarning(
                                "MongoDB operation timed out after {Timeout}",
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
                            .Handle<MongoException>(ex => IsCircuitBreakerException(ex, options))
                            .Handle<TimeoutException>()
                            .Handle<System.Net.Sockets.SocketException>(),
                        OnOpened = args =>
                        {
                            logger?.LogWarning(
                                args.Outcome.Exception,
                                "MongoDB circuit breaker OPENED. Break duration: {BreakDuration}",
                                args.BreakDuration);
                            options.OnCircuitBreakerOpen?.Invoke(args.Outcome.Exception);
                            return default;
                        },
                        OnClosed = args =>
                        {
                            logger?.LogInformation("MongoDB circuit breaker CLOSED");
                            options.OnCircuitBreakerReset?.Invoke();
                            return default;
                        },
                        OnHalfOpened = args =>
                        {
                            logger?.LogInformation("MongoDB circuit breaker HALF-OPEN, testing...");
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
                            MongoDbResilienceBackoffType.Constant => DelayBackoffType.Constant,
                            MongoDbResilienceBackoffType.Linear => DelayBackoffType.Linear,
                            _ => DelayBackoffType.Exponential
                        },
                        UseJitter = options.RetryUseJitter,
                        ShouldHandle = new PredicateBuilder()
                            .Handle<MongoException>(ex => IsTransientException(ex, options))
                            .Handle<TimeoutException>()
                            .Handle<System.Net.Sockets.SocketException>(),
                        OnRetry = args =>
                        {
                            logger?.LogWarning(
                                args.Outcome.Exception,
                                "MongoDB operation retry {Attempt}/{MaxAttempts} after {Delay}ms",
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
        /// Determines if a MongoDB exception is transient and should be retried.
        /// </summary>
        private static bool IsTransientException(MongoException ex, NativeMongoDbResilienceOptions options)
        {
            // Use custom predicate if provided
            if (options.ShouldRetryOnException != null)
            {
                return options.ShouldRetryOnException(ex);
            }

            // MongoDB driver already has transient labels
            if (ex.HasErrorLabel("TransientTransactionError"))
            {
                return true;
            }

            // Check for specific exception types that are typically transient
            return ex switch
            {
                MongoConnectionException => true,
                MongoNodeIsRecoveringException => true,
                MongoNotPrimaryException => true,
                MongoCommandException cmdEx when IsTransientCommandError(cmdEx) => true,
                MongoWriteException writeEx when writeEx.WriteError?.Category == ServerErrorCategory.ExecutionTimeout => true,
                _ => IsTransientByMessage(ex)
            };
        }

        /// <summary>
        /// Determines if a MongoDB exception should trigger the circuit breaker.
        /// </summary>
        private static bool IsCircuitBreakerException(MongoException ex, NativeMongoDbResilienceOptions options)
        {
            // Connection errors should trigger circuit breaker
            if (ex is MongoConnectionException)
            {
                return true;
            }

            // Server not available
            if (ex is MongoNotPrimaryException or MongoNodeIsRecoveringException)
            {
                return true;
            }

            // Authentication failures should NOT trigger circuit breaker
            if (ex is MongoAuthenticationException)
            {
                return false;
            }

            return IsTransientByMessage(ex);
        }

        /// <summary>
        /// Checks if a command error is transient.
        /// </summary>
        private static bool IsTransientCommandError(MongoCommandException ex)
        {
            // Common transient command error codes
            var transientCodes = new HashSet<int>
            {
                6, // HostUnreachable
                7, // HostNotFound
                89, // NetworkTimeout
                91, // ShutdownInProgress
                189, // PrimarySteppedDown
                10107, // NotMaster (legacy)
                11600, // InterruptedAtShutdown
                11602, // InterruptedDueToReplStateChange
                13435, // NotMasterNoSlaveOk
                13436, // NotMasterOrSecondary
            };

            return transientCodes.Contains(ex.Code);
        }

        /// <summary>
        /// Checks if an exception message indicates a transient error.
        /// </summary>
        private static bool IsTransientByMessage(MongoException ex)
        {
            var message = ex.Message?.ToLowerInvariant() ?? string.Empty;

            return message.Contains("timeout") ||
                   message.Contains("connection") ||
                   message.Contains("network") ||
                   message.Contains("socket") ||
                   message.Contains("unavailable") ||
                   message.Contains("not primary") ||
                   message.Contains("recovering");
        }
    }

    /// <summary>
    /// Configuration options for native MongoDB resilience.
    /// </summary>
    public class NativeMongoDbResilienceOptions
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
        public MongoDbResilienceBackoffType RetryBackoffType { get; set; } = MongoDbResilienceBackoffType.ExponentialWithJitter;

        /// <summary>
        /// Gets or sets the initial delay for retry operations.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets the maximum delay between retries.
        /// </summary>
        public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets whether to use jitter in retry delays.
        /// </summary>
        public bool RetryUseJitter { get; set; } = true;

        /// <summary>
        /// Gets or sets a custom predicate to determine if an exception should trigger a retry.
        /// </summary>
        public Func<MongoException, bool>? ShouldRetryOnException { get; set; }

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
        /// Gets or sets the timeout duration for MongoDB operations.
        /// </summary>
        public TimeSpan TimeoutDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets a callback invoked when a timeout occurs.
        /// </summary>
        public Action<TimeSpan>? OnTimeout { get; set; }

        #endregion

        #region Presets

        /// <summary>
        /// Creates options optimized for replica set environments.
        /// </summary>
        public static NativeMongoDbResilienceOptions ReplicaSet => new()
        {
            EnableRetry = true,
            RetryMaxAttempts = 5,
            RetryBackoffType = MongoDbResilienceBackoffType.ExponentialWithJitter,
            RetryDelay = TimeSpan.FromMilliseconds(100),
            RetryMaxDelay = TimeSpan.FromSeconds(10),
            EnableCircuitBreaker = true,
            CircuitBreakerMinimumThroughput = 10,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30),
            EnableTimeout = true,
            TimeoutDuration = TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// Creates options optimized for sharded clusters.
        /// </summary>
        public static NativeMongoDbResilienceOptions ShardedCluster => new()
        {
            EnableRetry = true,
            RetryMaxAttempts = 3,
            RetryBackoffType = MongoDbResilienceBackoffType.ExponentialWithJitter,
            RetryDelay = TimeSpan.FromMilliseconds(50),
            RetryMaxDelay = TimeSpan.FromSeconds(5),
            EnableCircuitBreaker = true,
            CircuitBreakerMinimumThroughput = 20,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(15),
            EnableTimeout = true,
            TimeoutDuration = TimeSpan.FromSeconds(15)
        };

        /// <summary>
        /// Creates options for standalone MongoDB instances.
        /// </summary>
        public static NativeMongoDbResilienceOptions Standalone => new()
        {
            EnableRetry = true,
            RetryMaxAttempts = 3,
            RetryBackoffType = MongoDbResilienceBackoffType.ExponentialWithJitter,
            RetryDelay = TimeSpan.FromMilliseconds(200),
            RetryMaxDelay = TimeSpan.FromSeconds(10),
            EnableCircuitBreaker = true,
            CircuitBreakerMinimumThroughput = 5,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(60),
            EnableTimeout = true,
            TimeoutDuration = TimeSpan.FromSeconds(30)
        };

        #endregion
    }

    /// <summary>
    /// Backoff types for MongoDB resilience retries.
    /// </summary>
    public enum MongoDbResilienceBackoffType
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


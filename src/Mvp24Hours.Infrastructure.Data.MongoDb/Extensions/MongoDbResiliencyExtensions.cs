//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for registering MongoDB resiliency services in the DI container.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides enterprise-grade resiliency features for MongoDB:
    /// <list type="bullet">
    ///   <item><b>Connection Resiliency</b>: Auto-reconnect with exponential backoff</item>
    ///   <item><b>Retry Policies</b>: Configurable retry for transient failures</item>
    ///   <item><b>Circuit Breaker</b>: Fail-fast when MongoDB is unavailable</item>
    ///   <item><b>Timeouts</b>: Per-operation timeout configuration</item>
    ///   <item><b>Failover</b>: Automatic failover for replica sets</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup with default settings
    /// services.AddMvp24HoursDbContext(options =>
    /// {
    ///     options.DatabaseName = "mydb";
    ///     options.ConnectionString = "mongodb://localhost:27017";
    /// })
    /// .AddMongoDbResiliency();
    /// 
    /// // Production configuration with custom options
    /// services.AddMvp24HoursDbContext(options =>
    /// {
    ///     options.DatabaseName = "mydb";
    ///     options.ConnectionString = "mongodb://replicaset:27017";
    /// })
    /// .AddMongoDbResiliency(resiliency =>
    /// {
    ///     resiliency.EnableCircuitBreaker = true;
    ///     resiliency.CircuitBreakerFailureThreshold = 5;
    ///     resiliency.RetryCount = 3;
    ///     resiliency.UseExponentialBackoff = true;
    /// });
    /// 
    /// // Use preset configurations
    /// services.AddMongoDbResiliencyForProduction();
    /// // or
    /// services.AddMongoDbResiliencyForDevelopment();
    /// </code>
    /// </example>
    public static class MongoDbResiliencyExtensions
    {
        /// <summary>
        /// Adds MongoDB resiliency services with default configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMongoDbResiliency(this IServiceCollection services)
        {
            return services.AddMongoDbResiliency(options => { });
        }

        /// <summary>
        /// Adds MongoDB resiliency services with custom configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Action to configure resiliency options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMongoDbResiliency(options =>
        /// {
        ///     // Connection resiliency
        ///     options.EnableAutoReconnect = true;
        ///     options.MaxReconnectAttempts = 10;
        ///     
        ///     // Retry policy
        ///     options.EnableRetry = true;
        ///     options.RetryCount = 3;
        ///     options.RetryBaseDelayMilliseconds = 100;
        ///     options.UseExponentialBackoff = true;
        ///     
        ///     // Circuit breaker
        ///     options.EnableCircuitBreaker = true;
        ///     options.CircuitBreakerFailureThreshold = 5;
        ///     options.CircuitBreakerDurationSeconds = 30;
        ///     
        ///     // Timeouts
        ///     options.EnableOperationTimeout = true;
        ///     options.DefaultOperationTimeoutSeconds = 30;
        ///     options.BulkOperationTimeoutSeconds = 120;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMongoDbResiliency(
            this IServiceCollection services,
            Action<MongoDbResiliencyOptions> configure)
        {
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<MongoDbResiliencyOptions>(options => { });
            }

            // Register resiliency services
            services.TryAddSingleton<MongoDbResiliencyOptions>(sp =>
            {
                var options = new MongoDbResiliencyOptions();
                configure?.Invoke(options);
                return options;
            });

            services.TryAddSingleton<IMongoDbResiliencyPolicy>(sp =>
            {
                var options = sp.GetRequiredService<MongoDbResiliencyOptions>();
                return new MongoDbResiliencyPolicy(options);
            });

            services.TryAddSingleton<MongoDbConnectionManager>(sp =>
            {
                var options = sp.GetRequiredService<MongoDbResiliencyOptions>();
                return new MongoDbConnectionManager(options);
            });

            return services;
        }

        /// <summary>
        /// Adds MongoDB resiliency services optimized for production environments.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Production configuration includes:
        /// <list type="bullet">
        ///   <item>Auto-reconnect with up to 10 attempts</item>
        ///   <item>3 retries with exponential backoff</item>
        ///   <item>Circuit breaker with 5 failure threshold</item>
        ///   <item>30 second default operation timeout</item>
        ///   <item>Full logging enabled</item>
        /// </list>
        /// </para>
        /// </remarks>
        public static IServiceCollection AddMongoDbResiliencyForProduction(this IServiceCollection services)
        {
            var options = MongoDbResiliencyOptions.CreateProduction();
            return services.AddMongoDbResiliency(opt =>
            {
                opt.EnableAutoReconnect = options.EnableAutoReconnect;
                opt.MaxReconnectAttempts = options.MaxReconnectAttempts;
                opt.ReconnectDelayMilliseconds = options.ReconnectDelayMilliseconds;
                opt.MaxReconnectDelayMilliseconds = options.MaxReconnectDelayMilliseconds;
                opt.UseExponentialBackoffForReconnect = options.UseExponentialBackoffForReconnect;
                opt.ReconnectJitterFactor = options.ReconnectJitterFactor;

                opt.EnableRetry = options.EnableRetry;
                opt.RetryCount = options.RetryCount;
                opt.RetryBaseDelayMilliseconds = options.RetryBaseDelayMilliseconds;
                opt.RetryMaxDelayMilliseconds = options.RetryMaxDelayMilliseconds;
                opt.UseExponentialBackoff = options.UseExponentialBackoff;
                opt.RetryJitterFactor = options.RetryJitterFactor;

                opt.EnableCircuitBreaker = options.EnableCircuitBreaker;
                opt.CircuitBreakerFailureThreshold = options.CircuitBreakerFailureThreshold;
                opt.CircuitBreakerSamplingDurationSeconds = options.CircuitBreakerSamplingDurationSeconds;
                opt.CircuitBreakerDurationSeconds = options.CircuitBreakerDurationSeconds;
                opt.CircuitBreakerMinimumThroughput = options.CircuitBreakerMinimumThroughput;

                opt.EnableOperationTimeout = options.EnableOperationTimeout;
                opt.DefaultOperationTimeoutSeconds = options.DefaultOperationTimeoutSeconds;
                opt.BulkOperationTimeoutSeconds = options.BulkOperationTimeoutSeconds;

                opt.EnableAutomaticFailover = options.EnableAutomaticFailover;
                opt.ServerSelectionTimeoutSeconds = options.ServerSelectionTimeoutSeconds;
                opt.HeartbeatFrequencySeconds = options.HeartbeatFrequencySeconds;

                opt.LogRetryAttempts = options.LogRetryAttempts;
                opt.LogCircuitBreakerStateChanges = options.LogCircuitBreakerStateChanges;
                opt.LogConnectionEvents = options.LogConnectionEvents;
                opt.LogTimeoutEvents = options.LogTimeoutEvents;
            });
        }

        /// <summary>
        /// Adds MongoDB resiliency services optimized for development environments.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Development configuration includes:
        /// <list type="bullet">
        ///   <item>Auto-reconnect with up to 3 attempts (faster feedback)</item>
        ///   <item>2 retries with shorter delays</item>
        ///   <item>Circuit breaker disabled for easier debugging</item>
        ///   <item>60 second operation timeout (longer for debugging)</item>
        ///   <item>Verbose logging enabled</item>
        /// </list>
        /// </para>
        /// </remarks>
        public static IServiceCollection AddMongoDbResiliencyForDevelopment(this IServiceCollection services)
        {
            var options = MongoDbResiliencyOptions.CreateDevelopment();
            return services.AddMongoDbResiliency(opt =>
            {
                opt.EnableAutoReconnect = options.EnableAutoReconnect;
                opt.MaxReconnectAttempts = options.MaxReconnectAttempts;
                opt.ReconnectDelayMilliseconds = options.ReconnectDelayMilliseconds;
                opt.MaxReconnectDelayMilliseconds = options.MaxReconnectDelayMilliseconds;
                opt.UseExponentialBackoffForReconnect = options.UseExponentialBackoffForReconnect;
                opt.ReconnectJitterFactor = options.ReconnectJitterFactor;

                opt.EnableRetry = options.EnableRetry;
                opt.RetryCount = options.RetryCount;
                opt.RetryBaseDelayMilliseconds = options.RetryBaseDelayMilliseconds;
                opt.RetryMaxDelayMilliseconds = options.RetryMaxDelayMilliseconds;
                opt.UseExponentialBackoff = options.UseExponentialBackoff;
                opt.RetryJitterFactor = options.RetryJitterFactor;

                opt.EnableCircuitBreaker = options.EnableCircuitBreaker;
                opt.CircuitBreakerFailureThreshold = options.CircuitBreakerFailureThreshold;
                opt.CircuitBreakerSamplingDurationSeconds = options.CircuitBreakerSamplingDurationSeconds;
                opt.CircuitBreakerDurationSeconds = options.CircuitBreakerDurationSeconds;
                opt.CircuitBreakerMinimumThroughput = options.CircuitBreakerMinimumThroughput;

                opt.EnableOperationTimeout = options.EnableOperationTimeout;
                opt.DefaultOperationTimeoutSeconds = options.DefaultOperationTimeoutSeconds;
                opt.BulkOperationTimeoutSeconds = options.BulkOperationTimeoutSeconds;

                opt.EnableAutomaticFailover = options.EnableAutomaticFailover;
                opt.ServerSelectionTimeoutSeconds = options.ServerSelectionTimeoutSeconds;
                opt.HeartbeatFrequencySeconds = options.HeartbeatFrequencySeconds;

                opt.LogRetryAttempts = options.LogRetryAttempts;
                opt.LogCircuitBreakerStateChanges = options.LogCircuitBreakerStateChanges;
                opt.LogConnectionEvents = options.LogConnectionEvents;
                opt.LogTimeoutEvents = options.LogTimeoutEvents;
            });
        }

        /// <summary>
        /// Adds MongoDB resiliency services with minimal configuration (retry only).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="retryCount">Number of retry attempts.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Minimal configuration includes:
        /// <list type="bullet">
        ///   <item>Retry with exponential backoff</item>
        ///   <item>No circuit breaker</item>
        ///   <item>No connection manager events</item>
        /// </list>
        /// </para>
        /// </remarks>
        public static IServiceCollection AddMongoDbRetryPolicy(
            this IServiceCollection services,
            int retryCount = 3)
        {
            return services.AddMongoDbResiliency(options =>
            {
                options.EnableRetry = true;
                options.RetryCount = retryCount;
                options.UseExponentialBackoff = true;
                options.EnableCircuitBreaker = false;
                options.EnableAutoReconnect = false;
                options.LogRetryAttempts = false;
                options.LogConnectionEvents = false;
            });
        }

        /// <summary>
        /// Adds MongoDB circuit breaker only.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="failureThreshold">Number of failures before opening the circuit.</param>
        /// <param name="durationSeconds">How long the circuit stays open.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMongoDbCircuitBreaker(
            this IServiceCollection services,
            int failureThreshold = 5,
            int durationSeconds = 30)
        {
            return services.AddMongoDbResiliency(options =>
            {
                options.EnableCircuitBreaker = true;
                options.CircuitBreakerFailureThreshold = failureThreshold;
                options.CircuitBreakerDurationSeconds = durationSeconds;
                options.EnableRetry = false;
                options.EnableAutoReconnect = false;
            });
        }

        /// <summary>
        /// Adds custom exception types that should trigger a retry.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="exceptionTypes">The exception types to add.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Use this to extend the default list of retryable exceptions.
        /// By default, the following are retried:
        /// <list type="bullet">
        ///   <item>MongoConnectionException</item>
        ///   <item>MongoNotPrimaryException</item>
        ///   <item>MongoNodeIsRecoveringException</item>
        ///   <item>TimeoutException</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMongoDbResiliency()
        ///         .AddRetryableExceptions(typeof(MyCustomException));
        /// </code>
        /// </example>
        public static IServiceCollection AddRetryableExceptions(
            this IServiceCollection services,
            params Type[] exceptionTypes)
        {
            services.PostConfigure<MongoDbResiliencyOptions>(options =>
            {
                options.AdditionalRetryableExceptions.AddRange(exceptionTypes);
            });

            return services;
        }

        /// <summary>
        /// Adds custom exception types that should never trigger a retry.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="exceptionTypes">The exception types to add.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Use this to specify exceptions that should fail immediately.
        /// Useful for business logic exceptions that shouldn't be retried.
        /// </para>
        /// </remarks>
        public static IServiceCollection AddNonRetryableExceptions(
            this IServiceCollection services,
            params Type[] exceptionTypes)
        {
            services.PostConfigure<MongoDbResiliencyOptions>(options =>
            {
                options.NonRetryableExceptions.AddRange(exceptionTypes);
            });

            return services;
        }
    }
}


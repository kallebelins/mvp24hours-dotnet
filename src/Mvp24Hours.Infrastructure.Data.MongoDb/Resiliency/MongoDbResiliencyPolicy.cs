//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency
{
    /// <summary>
    /// Implements a comprehensive resiliency policy for MongoDB operations,
    /// combining retry, circuit breaker, and timeout policies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This policy orchestrates multiple resiliency patterns:
    /// <list type="bullet">
    ///   <item><b>Circuit Breaker</b>: Fails fast when MongoDB is unavailable</item>
    ///   <item><b>Timeout</b>: Enforces time limits on operations</item>
    ///   <item><b>Retry</b>: Automatically retries transient failures</item>
    /// </list>
    /// </para>
    /// <para>
    /// The execution order is: Circuit Breaker → Timeout → Retry → Operation
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new MongoDbResiliencyOptions
    /// {
    ///     EnableCircuitBreaker = true,
    ///     EnableRetry = true,
    ///     RetryCount = 3
    /// };
    /// 
    /// var policy = new MongoDbResiliencyPolicy(options);
    /// 
    /// var result = await policy.ExecuteAsync(async ct =>
    /// {
    ///     return await collection.Find(filter).ToListAsync(ct);
    /// }, cancellationToken);
    /// </code>
    /// </example>
    [Obsolete("Deprecated: Use NativeMongoDbResilienceExtensions with Microsoft.Extensions.Resilience instead. " +
              "This class will be removed in a future version. " +
              "See docs/en-us/modernization/generic-resilience.md for migration guide.", false)]
    public sealed class MongoDbResiliencyPolicy : IMongoDbResiliencyPolicy
    {
        private readonly MongoDbResiliencyOptions _options;
        private readonly MongoDbCircuitBreaker _circuitBreaker;
        private readonly MongoDbRetryPolicy _retryPolicy;
        private readonly ILogger<MongoDbResiliencyPolicy> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbResiliencyPolicy"/> class.
        /// </summary>
        /// <param name="options">The resiliency options.</param>
        /// <param name="logger">The logger instance.</param>
        public MongoDbResiliencyPolicy(MongoDbResiliencyOptions options, ILogger<MongoDbResiliencyPolicy> logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _circuitBreaker = new MongoDbCircuitBreaker(options);
            _retryPolicy = new MongoDbRetryPolicy(options, null);
        }

        /// <inheritdoc />
        public CircuitBreakerState CircuitState => _circuitBreaker.State;

        /// <inheritdoc />
        public ICircuitBreakerMetrics Metrics => _circuitBreaker;

        /// <inheritdoc />
        public async Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            // Step 1: Check circuit breaker
            if (_options.EnableCircuitBreaker && !_circuitBreaker.AllowRequest())
            {
                var remaining = _circuitBreaker.GetRemainingOpenDuration();
                throw new MongoDbCircuitBreakerOpenException(remaining ?? TimeSpan.Zero);
            }

            try
            {
                TResult result;

                // Step 2: Apply timeout if enabled
                if (_options.EnableOperationTimeout && _options.DefaultOperationTimeoutSeconds > 0)
                {
                    result = await ExecuteWithTimeoutInternalAsync(
                        operation,
                        _options.GetReadTimeout(),
                        cancellationToken);
                }
                else
                {
                    // Step 3: Apply retry policy
                    result = await _retryPolicy.ExecuteAsync(operation, cancellationToken);
                }

                // Record success for circuit breaker
                if (_options.EnableCircuitBreaker)
                {
                    _circuitBreaker.RecordSuccess();
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (MongoDbCircuitBreakerOpenException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Record failure for circuit breaker
                if (_options.EnableCircuitBreaker)
                {
                    _circuitBreaker.RecordFailure(ex);
                }

                throw;
            }
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async ct =>
            {
                await operation(ct);
                return true;
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TResult> ExecuteWithFallbackAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            TResult fallbackValue,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await ExecuteAsync(operation, cancellationToken);
            }
            catch (MongoDbCircuitBreakerOpenException ex)
            {
                LogFallback(ex, "circuit_breaker_open");
                return fallbackValue;
            }
            catch (MongoDbRetryExhaustedException ex)
            {
                LogFallback(ex, "retry_exhausted");
                return fallbackValue;
            }
            catch (MongoDbOperationTimeoutException ex)
            {
                LogFallback(ex, "timeout");
                return fallbackValue;
            }
        }

        /// <inheritdoc />
        public async Task<TResult> ExecuteWithFallbackAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            Func<Exception, TResult> fallbackFactory,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await ExecuteAsync(operation, cancellationToken);
            }
            catch (MongoDbCircuitBreakerOpenException ex)
            {
                LogFallback(ex, "circuit_breaker_open");
                return fallbackFactory(ex);
            }
            catch (MongoDbRetryExhaustedException ex)
            {
                LogFallback(ex, "retry_exhausted");
                return fallbackFactory(ex);
            }
            catch (MongoDbOperationTimeoutException ex)
            {
                LogFallback(ex, "timeout");
                return fallbackFactory(ex);
            }
        }

        /// <inheritdoc />
        public async Task<TResult> ExecuteWithTimeoutAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            // Check circuit breaker first
            if (_options.EnableCircuitBreaker && !_circuitBreaker.AllowRequest())
            {
                var remaining = _circuitBreaker.GetRemainingOpenDuration();
                throw new MongoDbCircuitBreakerOpenException(remaining ?? TimeSpan.Zero);
            }

            try
            {
                var result = await ExecuteWithTimeoutInternalAsync(operation, timeout, cancellationToken);

                if (_options.EnableCircuitBreaker)
                {
                    _circuitBreaker.RecordSuccess();
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (MongoDbCircuitBreakerOpenException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (_options.EnableCircuitBreaker)
                {
                    _circuitBreaker.RecordFailure(ex);
                }

                throw;
            }
        }

        /// <inheritdoc />
        public void ResetCircuitBreaker()
        {
            _circuitBreaker.ResetState();

            _logger?.LogInformation("MongoDB circuit breaker manually reset");
        }

        /// <inheritdoc />
        public void TripCircuitBreaker()
        {
            _circuitBreaker.Trip();

            _logger?.LogWarning("MongoDB circuit breaker manually tripped");
        }

        private async Task<TResult> ExecuteWithTimeoutInternalAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            try
            {
                // Apply retry within the timeout
                return await _retryPolicy.ExecuteAsync(operation, linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                if (_options.LogTimeoutEvents)
                {
                    _logger?.LogWarning(
                        "MongoDB operation timeout: Timeout={Timeout}ms, OperationType={OperationType}",
                        timeout.TotalMilliseconds,
                        "read");
                }

                throw new MongoDbOperationTimeoutException(timeout, "database");
            }
        }

        private void LogFallback(Exception ex, string reason)
        {
            _logger?.LogInformation(
                "MongoDB fallback used: Reason={Reason}, ExceptionType={ExceptionType}, Message={Message}",
                reason,
                ex.GetType().Name,
                ex.Message);
        }
    }
}


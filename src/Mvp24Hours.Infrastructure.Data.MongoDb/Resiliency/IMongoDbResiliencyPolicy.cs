//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency
{
    /// <summary>
    /// Defines a resiliency policy for executing MongoDB operations with automatic
    /// retry, circuit breaker, and timeout handling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations of this interface provide enterprise-grade resiliency features:
    /// <list type="bullet">
    ///   <item><b>Retry</b>: Automatically retry transient failures with configurable backoff</item>
    ///   <item><b>Circuit Breaker</b>: Fail fast when the database is unavailable</item>
    ///   <item><b>Timeout</b>: Enforce time limits on operations</item>
    ///   <item><b>Fallback</b>: Provide fallback values when all retries fail</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomerRepository
    /// {
    ///     private readonly IMongoDbResiliencyPolicy _policy;
    ///     private readonly IMongoCollection&lt;Customer&gt; _collection;
    ///     
    ///     public async Task&lt;Customer&gt; GetByIdAsync(string id, CancellationToken ct)
    ///     {
    ///         return await _policy.ExecuteAsync(
    ///             async token => await _collection
    ///                 .Find(c => c.Id == id)
    ///                 .FirstOrDefaultAsync(token),
    ///             ct);
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IMongoDbResiliencyPolicy
    {
        /// <summary>
        /// Executes an asynchronous operation with resiliency policies applied.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="MongoDbCircuitBreakerOpenException">Thrown when the circuit breaker is open.</exception>
        /// <exception cref="MongoDbOperationTimeoutException">Thrown when the operation times out.</exception>
        /// <exception cref="MongoDbRetryExhaustedException">Thrown when all retry attempts are exhausted.</exception>
        Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an asynchronous operation with resiliency policies applied.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an asynchronous operation with a fallback value if all retries fail.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="fallbackValue">The value to return if the operation fails.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation or the fallback value.</returns>
        Task<TResult> ExecuteWithFallbackAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            TResult fallbackValue,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an asynchronous operation with a fallback factory if all retries fail.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="fallbackFactory">A factory to create the fallback value.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation or the fallback value.</returns>
        Task<TResult> ExecuteWithFallbackAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            Func<Exception, TResult> fallbackFactory,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation with a specific timeout, independent of default settings.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="timeout">The timeout for this operation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        Task<TResult> ExecuteWithTimeoutAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            TimeSpan timeout,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current state of the circuit breaker.
        /// </summary>
        CircuitBreakerState CircuitState { get; }

        /// <summary>
        /// Gets the metrics for the circuit breaker.
        /// </summary>
        ICircuitBreakerMetrics Metrics { get; }

        /// <summary>
        /// Manually resets the circuit breaker to closed state.
        /// </summary>
        void ResetCircuitBreaker();

        /// <summary>
        /// Manually trips the circuit breaker to open state.
        /// </summary>
        void TripCircuitBreaker();
    }

    /// <summary>
    /// Represents the state of a circuit breaker.
    /// </summary>
    public enum CircuitBreakerState
    {
        /// <summary>
        /// The circuit is closed and operations are allowed.
        /// </summary>
        Closed,

        /// <summary>
        /// The circuit is open and operations are blocked.
        /// </summary>
        Open,

        /// <summary>
        /// The circuit is testing if the system has recovered.
        /// </summary>
        HalfOpen
    }

    /// <summary>
    /// Provides metrics for circuit breaker monitoring.
    /// </summary>
    public interface ICircuitBreakerMetrics
    {
        /// <summary>
        /// Gets the total number of successful operations.
        /// </summary>
        long TotalSuccessCount { get; }

        /// <summary>
        /// Gets the total number of failed operations.
        /// </summary>
        long TotalFailureCount { get; }

        /// <summary>
        /// Gets the total number of rejected operations (when circuit is open).
        /// </summary>
        long TotalRejectedCount { get; }

        /// <summary>
        /// Gets the number of times the circuit has tripped open.
        /// </summary>
        long CircuitTripCount { get; }

        /// <summary>
        /// Gets the current failure rate (0.0 to 1.0).
        /// </summary>
        double CurrentFailureRate { get; }

        /// <summary>
        /// Gets the timestamp of the last successful operation.
        /// </summary>
        DateTimeOffset? LastSuccessTime { get; }

        /// <summary>
        /// Gets the timestamp of the last failure.
        /// </summary>
        DateTimeOffset? LastFailureTime { get; }

        /// <summary>
        /// Gets the timestamp of when the circuit last opened.
        /// </summary>
        DateTimeOffset? LastOpenTime { get; }

        /// <summary>
        /// Resets all metrics.
        /// </summary>
        void Reset();
    }
}


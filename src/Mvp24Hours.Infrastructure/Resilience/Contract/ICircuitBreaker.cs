//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Resilience.Contract
{
    /// <summary>
    /// Generic interface for circuit breaker patterns that can protect any operation.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <remarks>
    /// <para>
    /// A circuit breaker prevents cascading failures by temporarily stopping requests
    /// to failing services. It has three states:
    /// <list type="bullet">
    /// <item><strong>Closed:</strong> Normal operation, requests pass through</item>
    /// <item><strong>Open:</strong> Circuit is open, requests fail immediately</item>
    /// <item><strong>Half-Open:</strong> Allowing limited requests to test if service recovered</item>
    /// </list>
    /// </para>
    /// <para>
    /// This interface provides a generic way to protect operations of any type,
    /// not just HTTP requests. It can be used for database operations, messaging,
    /// file operations, or any other async operation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var circuitBreaker = new CircuitBreaker&lt;string&gt;(options);
    /// var result = await circuitBreaker.ExecuteAsync(async () =>
    /// {
    ///     return await database.GetDataAsync();
    /// });
    /// </code>
    /// </example>
    public interface ICircuitBreaker<TResult>
    {
        /// <summary>
        /// Gets the current state of the circuit breaker.
        /// </summary>
        CircuitBreakerState State { get; }

        /// <summary>
        /// Executes an operation protected by the circuit breaker.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit is open.</exception>
        Task<TResult> ExecuteAsync(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation protected by the circuit breaker with context.
        /// </summary>
        /// <param name="operation">The operation to execute with context.</param>
        /// <param name="context">Context data to pass to the operation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit is open.</exception>
        Task<TResult> ExecuteAsync(
            Func<object?, CancellationToken, Task<TResult>> operation,
            object? context = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manually isolates the circuit breaker, opening it immediately.
        /// </summary>
        void Isolate();

        /// <summary>
        /// Manually resets the circuit breaker to the closed state.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Generic interface for circuit breaker patterns that execute operations without return values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface is for operations that don't return a value (void operations).
    /// </para>
    /// </remarks>
    public interface ICircuitBreaker
    {
        /// <summary>
        /// Gets the current state of the circuit breaker.
        /// </summary>
        CircuitBreakerState State { get; }

        /// <summary>
        /// Executes an operation protected by the circuit breaker.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit is open.</exception>
        Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation protected by the circuit breaker with context.
        /// </summary>
        /// <param name="operation">The operation to execute with context.</param>
        /// <param name="context">Context data to pass to the operation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit is open.</exception>
        Task ExecuteAsync(
            Func<object?, CancellationToken, Task> operation,
            object? context = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manually isolates the circuit breaker, opening it immediately.
        /// </summary>
        void Isolate();

        /// <summary>
        /// Manually resets the circuit breaker to the closed state.
        /// </summary>
        void Reset();
    }
}


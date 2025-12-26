//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Resilience.Contract
{
    /// <summary>
    /// Generic interface for bulkhead pattern that limits concurrent execution of operations.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <remarks>
    /// <para>
    /// The bulkhead pattern isolates resources to prevent a single failure from consuming
    /// all available resources. It limits the number of concurrent operations that can
    /// be executed, similar to how bulkheads on a ship prevent flooding from spreading.
    /// </para>
    /// <para>
    /// This interface provides a generic way to limit concurrency for operations of any type,
    /// not just HTTP requests. It can be used for database operations, messaging,
    /// file operations, or any other async operation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var bulkhead = new Bulkhead&lt;string&gt;(maxConcurrency: 10);
    /// var result = await bulkhead.ExecuteAsync(async () =>
    /// {
    ///     return await database.GetDataAsync();
    /// });
    /// </code>
    /// </example>
    public interface IBulkhead<TResult>
    {
        /// <summary>
        /// Gets the maximum number of concurrent operations allowed.
        /// </summary>
        int MaxConcurrency { get; }

        /// <summary>
        /// Gets the current number of operations being executed.
        /// </summary>
        int CurrentConcurrency { get; }

        /// <summary>
        /// Gets the number of operations waiting to be executed.
        /// </summary>
        int QueuedOperations { get; }

        /// <summary>
        /// Executes an operation within the bulkhead's concurrency limit.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="BulkheadRejectedException">Thrown when the bulkhead is full and cannot accept more operations.</exception>
        Task<TResult> ExecuteAsync(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation within the bulkhead's concurrency limit with context.
        /// </summary>
        /// <param name="operation">The operation to execute with context.</param>
        /// <param name="context">Context data to pass to the operation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="BulkheadRejectedException">Thrown when the bulkhead is full and cannot accept more operations.</exception>
        Task<TResult> ExecuteAsync(
            Func<object?, CancellationToken, Task<TResult>> operation,
            object? context = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Generic interface for bulkhead pattern that executes operations without return values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface is for operations that don't return a value (void operations).
    /// </para>
    /// </remarks>
    public interface IBulkhead
    {
        /// <summary>
        /// Gets the maximum number of concurrent operations allowed.
        /// </summary>
        int MaxConcurrency { get; }

        /// <summary>
        /// Gets the current number of operations being executed.
        /// </summary>
        int CurrentConcurrency { get; }

        /// <summary>
        /// Gets the number of operations waiting to be executed.
        /// </summary>
        int QueuedOperations { get; }

        /// <summary>
        /// Executes an operation within the bulkhead's concurrency limit.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="BulkheadRejectedException">Thrown when the bulkhead is full and cannot accept more operations.</exception>
        Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation within the bulkhead's concurrency limit with context.
        /// </summary>
        /// <param name="operation">The operation to execute with context.</param>
        /// <param name="context">Context data to pass to the operation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="BulkheadRejectedException">Thrown when the bulkhead is full and cannot accept more operations.</exception>
        Task ExecuteAsync(
            Func<object?, CancellationToken, Task> operation,
            object? context = null,
            CancellationToken cancellationToken = default);
    }
}


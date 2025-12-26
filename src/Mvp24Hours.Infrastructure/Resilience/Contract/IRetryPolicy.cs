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
    /// Generic interface for retry policies that can retry any operation.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface provides a generic way to retry operations of any type,
    /// not just HTTP requests. It can be used for database operations, messaging,
    /// file operations, or any other async operation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var retryPolicy = new RetryPolicy&lt;string&gt;(options);
    /// var result = await retryPolicy.ExecuteAsync(async () =>
    /// {
    ///     return await database.GetDataAsync();
    /// });
    /// </code>
    /// </example>
    public interface IRetryPolicy<TResult>
    {
        /// <summary>
        /// Executes an operation with retry logic.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        Task<TResult> ExecuteAsync(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation with retry logic and context.
        /// </summary>
        /// <param name="operation">The operation to execute with context.</param>
        /// <param name="context">Context data to pass to the operation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        Task<TResult> ExecuteAsync(
            Func<object?, CancellationToken, Task<TResult>> operation,
            object? context = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Generic interface for retry policies that execute operations without return values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface is for operations that don't return a value (void operations).
    /// </para>
    /// </remarks>
    public interface IRetryPolicy
    {
        /// <summary>
        /// Executes an operation with retry logic.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation with retry logic and context.
        /// </summary>
        /// <param name="operation">The operation to execute with context.</param>
        /// <param name="context">Context data to pass to the operation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        Task ExecuteAsync(
            Func<object?, CancellationToken, Task> operation,
            object? context = null,
            CancellationToken cancellationToken = default);
    }
}


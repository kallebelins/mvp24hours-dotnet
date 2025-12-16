//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Transactions
{
    /// <summary>
    /// Interface for managing MongoDB multi-document transactions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides comprehensive transaction management including:
    /// <list type="bullet">
    ///   <item>Multi-document ACID transactions (requires MongoDB 4.0+ with replica set)</item>
    ///   <item>Transaction options configuration (read concern, write concern, timeout)</item>
    ///   <item>Automatic retry on transient errors</item>
    ///   <item>Nested transaction support via savepoints (logical)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IMongoDbTransactionManager : IDisposable
    {
        /// <summary>
        /// Gets the current session handle.
        /// </summary>
        IClientSessionHandle CurrentSession { get; }

        /// <summary>
        /// Gets whether a transaction is currently active.
        /// </summary>
        bool IsTransactionActive { get; }

        /// <summary>
        /// Begins a new transaction with default options.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The session handle for the transaction.</returns>
        Task<IClientSessionHandle> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a new transaction with custom options.
        /// </summary>
        /// <param name="options">Transaction options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The session handle for the transaction.</returns>
        Task<IClientSessionHandle> BeginTransactionAsync(
            TransactionOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Aborts the current transaction and rolls back all changes.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task AbortTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation within a transaction with automatic retry on transient errors.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="options">Transaction options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<IClientSessionHandle, CancellationToken, Task<TResult>> operation,
            TransactionOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation within a transaction with automatic retry on transient errors.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="options">Transaction options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ExecuteInTransactionAsync(
            Func<IClientSessionHandle, CancellationToken, Task> operation,
            TransactionOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a savepoint (logical checkpoint) within the current transaction.
        /// </summary>
        /// <param name="savepointName">Name of the savepoint.</param>
        /// <remarks>
        /// MongoDB doesn't natively support savepoints, but this provides logical
        /// savepoint functionality by tracking state for potential rollback scenarios.
        /// </remarks>
        void CreateSavepoint(string savepointName);

        /// <summary>
        /// Rolls back to a previously created savepoint.
        /// </summary>
        /// <param name="savepointName">Name of the savepoint.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// Since MongoDB doesn't support partial rollback, this aborts the entire
        /// transaction. Use with caution in complex scenarios.
        /// </remarks>
        Task RollbackToSavepointAsync(string savepointName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases a savepoint.
        /// </summary>
        /// <param name="savepointName">Name of the savepoint.</param>
        void ReleaseSavepoint(string savepointName);
    }
}


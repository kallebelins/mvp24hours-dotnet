//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Contract.Transaction
{
    /// <summary>
    /// Represents the current status of a transaction scope.
    /// </summary>
    public enum TransactionStatus
    {
        /// <summary>
        /// Transaction has not been started.
        /// </summary>
        NotStarted,

        /// <summary>
        /// Transaction is currently active.
        /// </summary>
        Active,

        /// <summary>
        /// Transaction has been committed successfully.
        /// </summary>
        Committed,

        /// <summary>
        /// Transaction has been rolled back.
        /// </summary>
        RolledBack,

        /// <summary>
        /// Transaction is in an error state.
        /// </summary>
        Error
    }

    /// <summary>
    /// Provides explicit transaction control with Begin/Commit/Rollback operations.
    /// Supports nested transactions and cross-repository coordination.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Design Pattern:</strong> This interface implements the Transaction Script pattern
    /// combined with Unit of Work, providing explicit transaction boundaries for complex
    /// business operations.
    /// </para>
    /// <para>
    /// <strong>Key Features:</strong>
    /// <list type="bullet">
    /// <item>Explicit transaction control (Begin, Commit, Rollback)</item>
    /// <item>Cross-repository transaction support</item>
    /// <item>Nested transaction support via savepoints</item>
    /// <item>Async-first design with CancellationToken support</item>
    /// <item>IDisposable/IAsyncDisposable for automatic cleanup</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Usage Patterns:</strong>
    /// </para>
    /// <code>
    /// // Explicit control pattern
    /// await using var scope = transactionScope;
    /// await scope.BeginAsync();
    /// try
    /// {
    ///     await customerRepo.AddAsync(customer);
    ///     await orderRepo.AddAsync(order);
    ///     await scope.CommitAsync();
    /// }
    /// catch
    /// {
    ///     await scope.RollbackAsync();
    ///     throw;
    /// }
    /// 
    /// // Using pattern (auto-commit on success, auto-rollback on exception)
    /// await transactionScope.ExecuteAsync(async () =>
    /// {
    ///     await customerRepo.AddAsync(customer);
    ///     await orderRepo.AddAsync(order);
    /// });
    /// </code>
    /// </remarks>
    public interface ITransactionScope : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Gets the unique identifier for this transaction scope.
        /// </summary>
        Guid TransactionId { get; }

        /// <summary>
        /// Gets the current status of the transaction.
        /// </summary>
        TransactionStatus Status { get; }

        /// <summary>
        /// Gets whether the transaction is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the nesting depth of the current transaction.
        /// Returns 0 for root transactions, 1+ for nested transactions.
        /// </summary>
        int NestingLevel { get; }

        /// <summary>
        /// Begins a new transaction or creates a savepoint for nested transactions.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when a transaction is already active.</exception>
        /// <remarks>
        /// <para>
        /// If called within an existing transaction context, this creates a savepoint
        /// instead of a new transaction, enabling nested transaction semantics.
        /// </para>
        /// </remarks>
        Task BeginAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits all changes made within the transaction scope.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The number of entities affected by the commit.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no transaction is active.</exception>
        /// <exception cref="TransactionException">Thrown when commit fails.</exception>
        /// <remarks>
        /// <para>
        /// For nested transactions (savepoints), this releases the savepoint.
        /// For root transactions, this commits all changes to the database.
        /// </para>
        /// </remarks>
        Task<int> CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back all changes made within the transaction scope.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no transaction is active.</exception>
        /// <remarks>
        /// <para>
        /// For nested transactions (savepoints), this rolls back to the savepoint.
        /// For root transactions, this rolls back all changes.
        /// </para>
        /// </remarks>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a named savepoint within the current transaction.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no transaction is active.</exception>
        /// <exception cref="ArgumentNullException">Thrown when savepointName is null or empty.</exception>
        Task CreateSavepointAsync(string savepointName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back to a named savepoint within the current transaction.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint to roll back to.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no transaction is active.</exception>
        /// <exception cref="ArgumentNullException">Thrown when savepointName is null or empty.</exception>
        Task RollbackToSavepointAsync(string savepointName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases a named savepoint within the current transaction.
        /// </summary>
        /// <param name="savepointName">The name of the savepoint to release.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ReleaseSavepointAsync(string savepointName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an action within the transaction scope with automatic commit/rollback.
        /// </summary>
        /// <param name="action">The action to execute within the transaction.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The number of entities affected.</returns>
        /// <remarks>
        /// <para>
        /// This method automatically:
        /// <list type="bullet">
        /// <item>Begins a transaction</item>
        /// <item>Executes the action</item>
        /// <item>Commits on success</item>
        /// <item>Rolls back on exception</item>
        /// </list>
        /// </para>
        /// </remarks>
        Task<int> ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a function within the transaction scope with automatic commit/rollback.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the function.</typeparam>
        /// <param name="func">The function to execute within the transaction.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A tuple containing the result and the number of entities affected.</returns>
        Task<(TResult Result, int AffectedRows)> ExecuteAsync<TResult>(
            Func<Task<TResult>> func,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Provides synchronous transaction control operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides synchronous versions of transaction operations for
    /// scenarios where async is not practical or needed.
    /// </para>
    /// </remarks>
    public interface ITransactionScopeSync : IDisposable
    {
        /// <summary>
        /// Gets the unique identifier for this transaction scope.
        /// </summary>
        Guid TransactionId { get; }

        /// <summary>
        /// Gets the current status of the transaction.
        /// </summary>
        TransactionStatus Status { get; }

        /// <summary>
        /// Gets whether the transaction is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Begins a new transaction or creates a savepoint for nested transactions.
        /// </summary>
        void Begin();

        /// <summary>
        /// Commits all changes made within the transaction scope.
        /// </summary>
        /// <returns>The number of entities affected by the commit.</returns>
        int Commit();

        /// <summary>
        /// Rolls back all changes made within the transaction scope.
        /// </summary>
        void Rollback();

        /// <summary>
        /// Executes an action within the transaction scope with automatic commit/rollback.
        /// </summary>
        /// <param name="action">The action to execute within the transaction.</param>
        /// <returns>The number of entities affected.</returns>
        int Execute(Action action);

        /// <summary>
        /// Executes a function within the transaction scope with automatic commit/rollback.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the function.</typeparam>
        /// <param name="func">The function to execute within the transaction.</param>
        /// <returns>A tuple containing the result and the number of entities affected.</returns>
        (TResult Result, int AffectedRows) Execute<TResult>(Func<TResult> func);
    }
}


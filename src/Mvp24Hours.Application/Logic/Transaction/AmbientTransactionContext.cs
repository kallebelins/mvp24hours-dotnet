//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Transaction;
using System;
using System.Threading;

namespace Mvp24Hours.Application.Logic.Transaction
{
    /// <summary>
    /// Provides ambient transaction context using AsyncLocal for automatic propagation
    /// across async operations and service boundaries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Design Pattern:</strong> This class implements the Ambient Context pattern,
    /// providing transparent access to the current transaction without explicit parameter passing.
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong> Uses AsyncLocal&lt;T&gt; for safe propagation across
    /// async/await boundaries and parallel operations.
    /// </para>
    /// <para>
    /// <strong>Usage:</strong>
    /// <code>
    /// // Automatically set when BeginAsync is called
    /// await transactionScope.BeginAsync();
    /// 
    /// // Anywhere in the call stack:
    /// if (AmbientTransactionContext.HasCurrent)
    /// {
    ///     var tx = AmbientTransactionContext.Current;
    ///     // Use current transaction
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public static class AmbientTransactionContext
    {
        private static readonly AsyncLocal<TransactionContextHolder?> _currentHolder = new();

        /// <summary>
        /// Gets the current ambient transaction scope, or null if none is active.
        /// </summary>
        public static ITransactionScope? Current => _currentHolder.Value?.TransactionScope;

        /// <summary>
        /// Gets whether there is an active ambient transaction.
        /// </summary>
        public static bool HasCurrent => _currentHolder.Value?.TransactionScope?.IsActive == true;

        /// <summary>
        /// Gets the current transaction ID, or null if none is active.
        /// </summary>
        public static Guid? CurrentTransactionId => Current?.TransactionId;

        /// <summary>
        /// Gets the current transaction status, or null if none is active.
        /// </summary>
        public static TransactionStatus? CurrentStatus => Current?.Status;

        /// <summary>
        /// Gets the nesting level of the current transaction.
        /// Returns 0 if no transaction is active.
        /// </summary>
        public static int CurrentNestingLevel => Current?.NestingLevel ?? 0;

        /// <summary>
        /// Sets the current ambient transaction scope.
        /// </summary>
        /// <param name="scope">The transaction scope to set as current.</param>
        /// <remarks>
        /// <para>
        /// This method is called automatically by <see cref="TransactionScope.BeginAsync"/>.
        /// In most cases, you should not need to call this directly.
        /// </para>
        /// </remarks>
        internal static void SetCurrent(ITransactionScope scope)
        {
            _currentHolder.Value = new TransactionContextHolder { TransactionScope = scope };
        }

        /// <summary>
        /// Clears the current ambient transaction scope.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called automatically by <see cref="TransactionScope.CommitAsync"/>
        /// and <see cref="TransactionScope.RollbackAsync"/>. In most cases, you should not
        /// need to call this directly.
        /// </para>
        /// </remarks>
        internal static void Clear()
        {
            _currentHolder.Value = null;
        }

        /// <summary>
        /// Executes an action within a transaction scope, using the ambient transaction if one exists,
        /// or creating a new one if not.
        /// </summary>
        /// <param name="scopeFactory">The factory to create a new scope if needed.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async System.Threading.Tasks.Task ExecuteInTransactionAsync(
            ITransactionScopeFactory scopeFactory,
            Func<System.Threading.Tasks.Task> action,
            CancellationToken cancellationToken = default)
        {
            if (scopeFactory == null) throw new ArgumentNullException(nameof(scopeFactory));
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (HasCurrent)
            {
                // Participate in existing transaction
                await action();
            }
            else
            {
                // Create new transaction
                await using var scope = scopeFactory.Create();
                await scope.ExecuteAsync(action, cancellationToken);
            }
        }

        /// <summary>
        /// Executes a function within a transaction scope, using the ambient transaction if one exists,
        /// or creating a new one if not.
        /// </summary>
        /// <typeparam name="TResult">The type of result.</typeparam>
        /// <param name="scopeFactory">The factory to create a new scope if needed.</param>
        /// <param name="func">The function to execute.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the function.</returns>
        public static async System.Threading.Tasks.Task<TResult> ExecuteInTransactionAsync<TResult>(
            ITransactionScopeFactory scopeFactory,
            Func<System.Threading.Tasks.Task<TResult>> func,
            CancellationToken cancellationToken = default)
        {
            if (scopeFactory == null) throw new ArgumentNullException(nameof(scopeFactory));
            if (func == null) throw new ArgumentNullException(nameof(func));

            if (HasCurrent)
            {
                // Participate in existing transaction
                return await func();
            }
            else
            {
                // Create new transaction
                await using var scope = scopeFactory.Create();
                var (result, _) = await scope.ExecuteAsync(func, cancellationToken);
                return result;
            }
        }

        /// <summary>
        /// Holder class to allow null assignment with AsyncLocal.
        /// </summary>
        private sealed class TransactionContextHolder
        {
            public ITransactionScope? TransactionScope { get; set; }
        }
    }
}


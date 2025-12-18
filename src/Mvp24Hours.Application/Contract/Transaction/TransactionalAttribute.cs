//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Application.Contract.Transaction
{
    /// <summary>
    /// Indicates that a method should execute within a transactional context.
    /// When applied to a method, the framework will automatically wrap the execution
    /// in a transaction with commit on success and rollback on failure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Behavior:</strong>
    /// <list type="bullet">
    /// <item>If no ambient transaction exists, a new transaction is created</item>
    /// <item>If an ambient transaction exists, the method participates in it (nested)</item>
    /// <item>On successful completion, the transaction is committed (root) or savepoint released (nested)</item>
    /// <item>On exception, the transaction is rolled back (root) or rolled back to savepoint (nested)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example:</strong>
    /// <code>
    /// public class OrderService
    /// {
    ///     [Transactional]
    ///     public async Task&lt;Order&gt; PlaceOrderAsync(CreateOrderRequest request)
    ///     {
    ///         var order = await _orderRepository.AddAsync(new Order(request));
    ///         await _inventoryService.ReserveItemsAsync(order.Items);
    ///         await _paymentService.ProcessPaymentAsync(order);
    ///         return order;
    ///     }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Integration:</strong> This attribute works with:
    /// <list type="bullet">
    /// <item>Aspect-oriented frameworks (e.g., Castle DynamicProxy, PostSharp)</item>
    /// <item>Method interceptors via DI container</item>
    /// <item>Custom middleware/filters in ASP.NET Core</item>
    /// </list>
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class TransactionalAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether the transaction should be read-only.
        /// Default is false (read-write transaction).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Read-only transactions may be optimized by some database providers
        /// for better performance on read operations.
        /// </para>
        /// </remarks>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the transaction timeout in seconds.
        /// Default is 0, meaning no explicit timeout (uses provider default).
        /// </summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets whether to create a new transaction scope even if
        /// an ambient transaction already exists.
        /// Default is false (participate in existing transaction).
        /// </summary>
        /// <remarks>
        /// <para>
        /// When true, creates an independent transaction that commits/rolls back
        /// independently of any outer transaction. Use with caution as this can
        /// lead to partial commits in case of outer transaction failure.
        /// </para>
        /// </remarks>
        public bool RequiresNew { get; set; }

        /// <summary>
        /// Gets or sets whether to suppress the ambient transaction.
        /// When true, the method executes outside of any transaction context.
        /// Default is false.
        /// </summary>
        public bool Suppress { get; set; }

        /// <summary>
        /// Gets or sets the isolation level for the transaction.
        /// Default is null, meaning use the provider default.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Common isolation levels:
        /// <list type="bullet">
        /// <item><strong>ReadUncommitted:</strong> Lowest isolation, dirty reads possible</item>
        /// <item><strong>ReadCommitted:</strong> Prevents dirty reads (most common default)</item>
        /// <item><strong>RepeatableRead:</strong> Prevents dirty and non-repeatable reads</item>
        /// <item><strong>Serializable:</strong> Highest isolation, prevents phantoms</item>
        /// <item><strong>Snapshot:</strong> Uses row versioning (SQL Server specific)</item>
        /// </list>
        /// </para>
        /// </remarks>
        public TransactionIsolationLevel IsolationLevel { get; set; } = TransactionIsolationLevel.Default;

        /// <summary>
        /// Gets or sets whether to automatically retry on transient failures.
        /// Default is false.
        /// </summary>
        public bool RetryOnTransientFailure { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// Only applicable when <see cref="RetryOnTransientFailure"/> is true.
        /// Default is 3.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets custom exception types that should trigger a rollback.
        /// If empty, any exception triggers rollback (default behavior).
        /// </summary>
        public Type[]? RollbackFor { get; set; }

        /// <summary>
        /// Gets or sets exception types that should NOT trigger a rollback.
        /// Takes precedence over <see cref="RollbackFor"/>.
        /// </summary>
        public Type[]? NoRollbackFor { get; set; }

        /// <summary>
        /// Gets or sets an optional name for the transaction/savepoint.
        /// Useful for debugging and logging.
        /// </summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Represents transaction isolation levels.
    /// </summary>
    public enum TransactionIsolationLevel
    {
        /// <summary>
        /// Use the provider's default isolation level.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Allows dirty reads. Lowest isolation.
        /// </summary>
        ReadUncommitted = 1,

        /// <summary>
        /// Prevents dirty reads. Most common default for databases.
        /// </summary>
        ReadCommitted = 2,

        /// <summary>
        /// Prevents dirty reads and non-repeatable reads.
        /// </summary>
        RepeatableRead = 3,

        /// <summary>
        /// Full isolation. Prevents dirty reads, non-repeatable reads, and phantom reads.
        /// </summary>
        Serializable = 4,

        /// <summary>
        /// Row versioning-based isolation. SQL Server specific.
        /// </summary>
        Snapshot = 5
    }
}


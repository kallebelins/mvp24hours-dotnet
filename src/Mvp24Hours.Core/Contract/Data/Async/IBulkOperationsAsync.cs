//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Callback delegate for bulk operation progress reporting.
    /// </summary>
    /// <param name="processedCount">Number of items processed so far.</param>
    /// <param name="totalCount">Total number of items to process.</param>
    public delegate void BulkProgressCallback(int processedCount, int totalCount);

    /// <summary>
    /// Result of a bulk operation containing statistics about the operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this class to get detailed information about bulk operation results:
    /// <list type="bullet">
    /// <item>Number of rows affected</item>
    /// <item>Execution time</item>
    /// <item>Success/failure status</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class BulkOperationResult
    {
        /// <summary>
        /// Number of rows affected by the operation.
        /// </summary>
        public int RowsAffected { get; init; }

        /// <summary>
        /// Total execution time of the operation.
        /// </summary>
        public TimeSpan ElapsedTime { get; init; }

        /// <summary>
        /// Indicates whether the operation completed successfully.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Error message if the operation failed.
        /// </summary>
        public string ErrorMessage { get; init; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static BulkOperationResult Success(int rowsAffected, TimeSpan elapsedTime)
            => new() { RowsAffected = rowsAffected, ElapsedTime = elapsedTime, IsSuccess = true };

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        public static BulkOperationResult Failure(string errorMessage, TimeSpan elapsedTime)
            => new() { IsSuccess = false, ErrorMessage = errorMessage, ElapsedTime = elapsedTime };
    }

    /// <summary>
    /// Configuration options for bulk operations.
    /// </summary>
    public sealed class BulkOperationOptions
    {
        /// <summary>
        /// Number of entities to process in each batch. Default is 1000.
        /// </summary>
        /// <remarks>
        /// Higher values can improve performance but use more memory.
        /// Lower values reduce memory footprint but may increase execution time.
        /// </remarks>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// When true, wraps the entire operation in a transaction.
        /// </summary>
        public bool UseTransaction { get; set; } = true;

        /// <summary>
        /// Optional progress callback for long-running operations.
        /// </summary>
        public BulkProgressCallback ProgressCallback { get; set; }

        /// <summary>
        /// Timeout for the bulk operation in seconds. Default is 300 (5 minutes).
        /// </summary>
        public int TimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// When true, includes identity column in bulk insert (for databases that support it).
        /// </summary>
        public bool KeepIdentity { get; set; }

        /// <summary>
        /// When true, uses temporary tables for better performance (SQL Server).
        /// </summary>
        public bool UseTempTable { get; set; }

        /// <summary>
        /// When true, bypasses EF Core change tracking for better performance.
        /// </summary>
        public bool BypassChangeTracking { get; set; } = true;
    }

    /// <summary>
    /// Interface for high-performance bulk database operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface provides efficient bulk operations that bypass EF Core's
    /// change tracking for better performance with large datasets:
    /// <list type="bullet">
    /// <item><see cref="BulkInsertAsync"/> - Insert thousands of entities efficiently</item>
    /// <item><see cref="BulkUpdateAsync(IList{TEntity}, BulkOperationOptions, CancellationToken)"/> - Update entities by primary key</item>
    /// <item><see cref="BulkDeleteAsync(IList{TEntity}, BulkOperationOptions, CancellationToken)"/> - Delete entities by primary key</item>
    /// <item><see cref="ExecuteUpdateAsync"/> - Update entities matching a condition (.NET 7+)</item>
    /// <item><see cref="ExecuteDeleteAsync"/> - Delete entities matching a condition (.NET 7+)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Bulk insert 10,000 customers
    /// var customers = GenerateCustomers(10000);
    /// var result = await repository.BulkInsertAsync(customers, new BulkOperationOptions
    /// {
    ///     BatchSize = 2000,
    ///     ProgressCallback = (processed, total) => 
    ///         Console.WriteLine($"Processed {processed}/{total}")
    /// });
    /// 
    /// // Bulk update all inactive customers
    /// var updated = await repository.ExecuteUpdateAsync(
    ///     c => c.IsActive == false,
    ///     setters => setters.SetProperty(c => c.LastNotificationDate, DateTime.UtcNow)
    /// );
    /// 
    /// // Bulk delete old records
    /// var deleted = await repository.ExecuteDeleteAsync(
    ///     c => c.CreatedAt &lt; DateTime.UtcNow.AddYears(-5)
    /// );
    /// </code>
    /// </example>
    public interface IBulkOperationsAsync<TEntity>
        where TEntity : IEntityBase
    {
        #region Bulk Insert

        /// <summary>
        /// Inserts a large collection of entities efficiently using bulk operations.
        /// </summary>
        /// <param name="entities">Collection of entities to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of inserted rows and execution statistics.</returns>
        /// <remarks>
        /// <para>
        /// This method bypasses EF Core's change tracking for significantly better performance
        /// when inserting large numbers of entities. The entities are NOT added to the DbContext
        /// after this operation.
        /// </para>
        /// </remarks>
        Task<BulkOperationResult> BulkInsertAsync(
            IList<TEntity> entities,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts a large collection of entities efficiently using bulk operations with custom options.
        /// </summary>
        /// <param name="entities">Collection of entities to insert.</param>
        /// <param name="options">Configuration options for the bulk operation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of inserted rows and execution statistics.</returns>
        Task<BulkOperationResult> BulkInsertAsync(
            IList<TEntity> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion

        #region Bulk Update

        /// <summary>
        /// Updates a large collection of entities efficiently using bulk operations.
        /// </summary>
        /// <param name="entities">Collection of entities to update (identified by primary key).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of updated rows and execution statistics.</returns>
        /// <remarks>
        /// <para>
        /// Entities are matched by their primary key. All non-key properties are updated.
        /// </para>
        /// </remarks>
        Task<BulkOperationResult> BulkUpdateAsync(
            IList<TEntity> entities,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a large collection of entities efficiently using bulk operations with custom options.
        /// </summary>
        /// <param name="entities">Collection of entities to update (identified by primary key).</param>
        /// <param name="options">Configuration options for the bulk operation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of updated rows and execution statistics.</returns>
        Task<BulkOperationResult> BulkUpdateAsync(
            IList<TEntity> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion

        #region Bulk Delete

        /// <summary>
        /// Deletes a large collection of entities efficiently using bulk operations.
        /// </summary>
        /// <param name="entities">Collection of entities to delete (identified by primary key).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of deleted rows and execution statistics.</returns>
        Task<BulkOperationResult> BulkDeleteAsync(
            IList<TEntity> entities,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a large collection of entities efficiently using bulk operations with custom options.
        /// </summary>
        /// <param name="entities">Collection of entities to delete (identified by primary key).</param>
        /// <param name="options">Configuration options for the bulk operation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of deleted rows and execution statistics.</returns>
        Task<BulkOperationResult> BulkDeleteAsync(
            IList<TEntity> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion

        #region Execute Update (.NET 7+ style)

        /// <summary>
        /// Updates all entities matching the specified condition using a single SQL statement.
        /// </summary>
        /// <typeparam name="TProperty">Type of the property to update.</typeparam>
        /// <param name="predicate">Condition to filter entities to update.</param>
        /// <param name="property">Property selector expression.</param>
        /// <param name="value">New value for the property.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of rows affected.</returns>
        /// <remarks>
        /// <para>
        /// This method executes directly against the database without loading entities into memory.
        /// It does NOT trigger EF Core events or interceptors.
        /// </para>
        /// <para>
        /// Uses EF Core 7.0+ ExecuteUpdateAsync internally.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Update all inactive customers' notification date
        /// await repository.ExecuteUpdateAsync(
        ///     c => c.IsActive == false,
        ///     c => c.LastNotificationDate,
        ///     DateTime.UtcNow
        /// );
        /// </code>
        /// </example>
        Task<int> ExecuteUpdateAsync<TProperty>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TProperty>> property,
            TProperty value,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates all entities matching the specified condition using a single SQL statement with multiple property setters.
        /// </summary>
        /// <param name="predicate">Condition to filter entities to update.</param>
        /// <param name="setPropertyCalls">Action to configure property setters using SetProperty calls.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of rows affected.</returns>
        /// <remarks>
        /// <para>
        /// This overload allows updating multiple properties in a single operation.
        /// Uses EF Core 7.0+ ExecuteUpdateAsync with SetPropertyCalls internally.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Update multiple properties
        /// await repository.ExecuteUpdateAsync(
        ///     c => c.IsActive == false,
        ///     setters => setters
        ///         .SetProperty(c => c.LastNotificationDate, DateTime.UtcNow)
        ///         .SetProperty(c => c.NotificationCount, c => c.NotificationCount + 1)
        /// );
        /// </code>
        /// </example>
        Task<int> ExecuteUpdateAsync(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls,
            CancellationToken cancellationToken = default);

        #endregion

        #region Execute Delete (.NET 7+ style)

        /// <summary>
        /// Deletes all entities matching the specified condition using a single SQL statement.
        /// </summary>
        /// <param name="predicate">Condition to filter entities to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of rows affected.</returns>
        /// <remarks>
        /// <para>
        /// This method executes directly against the database without loading entities into memory.
        /// It does NOT trigger EF Core events, interceptors, or cascade deletes configured in the model.
        /// </para>
        /// <para>
        /// Uses EF Core 7.0+ ExecuteDeleteAsync internally.
        /// </para>
        /// <para>
        /// <b>Warning:</b> This performs a hard delete. For soft delete, use the standard
        /// RemoveAsync method or ExecuteUpdateAsync to set a deleted flag.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Delete all records older than 5 years
        /// var deleted = await repository.ExecuteDeleteAsync(
        ///     c => c.CreatedAt &lt; DateTime.UtcNow.AddYears(-5)
        /// );
        /// Console.WriteLine($"Deleted {deleted} old records");
        /// </code>
        /// </example>
        Task<int> ExecuteDeleteAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);

        #endregion
    }

    /// <summary>
    /// Represents a single property setter call for bulk update operations.
    /// </summary>
    public sealed class SetPropertyCall
    {
        /// <summary>
        /// Property selector expression.
        /// </summary>
        public LambdaExpression Property { get; init; }

        /// <summary>
        /// Constant value to set (if not using expression).
        /// </summary>
        public object Value { get; init; }

        /// <summary>
        /// Value expression to compute the new value (if not using constant).
        /// </summary>
        public LambdaExpression ValueExpression { get; init; }
    }

    /// <summary>
    /// Represents property setter calls for bulk update operations.
    /// This is a wrapper around EF Core's SetPropertyCalls for fluent API.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    public sealed class SetPropertyCalls<TEntity>
    {
        private readonly List<SetPropertyCall> _setters = [];

        /// <summary>
        /// Gets all configured property setters.
        /// </summary>
        public IReadOnlyList<SetPropertyCall> Setters => _setters;

        /// <summary>
        /// Sets a property to a constant value.
        /// </summary>
        /// <typeparam name="TProperty">Property type.</typeparam>
        /// <param name="propertyExpression">Property selector.</param>
        /// <param name="value">Constant value to set.</param>
        /// <returns>This instance for method chaining.</returns>
        public SetPropertyCalls<TEntity> SetProperty<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression,
            TProperty value)
        {
            _setters.Add(new SetPropertyCall
            {
                Property = propertyExpression,
                Value = value,
                ValueExpression = null
            });
            return this;
        }

        /// <summary>
        /// Sets a property using a value expression (e.g., increment based on current value).
        /// </summary>
        /// <typeparam name="TProperty">Property type.</typeparam>
        /// <param name="propertyExpression">Property selector.</param>
        /// <param name="valueExpression">Expression to compute the new value.</param>
        /// <returns>This instance for method chaining.</returns>
        public SetPropertyCalls<TEntity> SetProperty<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyExpression,
            Expression<Func<TEntity, TProperty>> valueExpression)
        {
            _setters.Add(new SetPropertyCall
            {
                Property = propertyExpression,
                Value = null,
                ValueExpression = valueExpression
            });
            return this;
        }
    }
}


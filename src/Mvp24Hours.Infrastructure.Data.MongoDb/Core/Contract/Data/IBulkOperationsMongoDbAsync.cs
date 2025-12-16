//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Core.Contract.Data
{
    /// <summary>
    /// Interface for high-performance bulk database operations specific to MongoDB.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface extends the base bulk operations interface with MongoDB-specific features:
    /// <list type="bullet">
    ///   <item><see cref="BulkInsertAsync(IList{TEntity}, MongoDbBulkOperationOptions, CancellationToken)"/> - Optimized InsertMany with ordered/unordered support</item>
    ///   <item><see cref="BulkUpdateAsync(IList{TEntity}, MongoDbBulkOperationOptions, CancellationToken)"/> - BulkWrite with UpdateOne operations</item>
    ///   <item><see cref="BulkDeleteAsync(IList{TEntity}, MongoDbBulkOperationOptions, CancellationToken)"/> - BulkWrite with DeleteOne operations</item>
    ///   <item><see cref="BulkWriteAsync"/> - Full BulkWrite access for mixed operations</item>
    ///   <item><see cref="UpdateManyAsync"/> - Update all documents matching a filter</item>
    ///   <item><see cref="DeleteManyAsync"/> - Delete all documents matching a filter</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // High-throughput bulk insert
    /// var options = new MongoDbBulkOperationOptions
    /// {
    ///     IsOrdered = false,
    ///     BatchSize = 5000,
    ///     ProgressCallback = (processed, total) => Console.WriteLine($"{processed}/{total}")
    /// };
    /// 
    /// var result = await repository.BulkInsertAsync(documents, options);
    /// Console.WriteLine($"Inserted {result.RowsAffected} in {result.ElapsedTime.TotalSeconds}s");
    /// 
    /// // Update many with filter
    /// var updated = await repository.UpdateManyAsync(
    ///     d => d.Status == "pending",
    ///     Builders&lt;Document&gt;.Update.Set(d => d.Status, "processed")
    /// );
    /// </code>
    /// </example>
    public interface IBulkOperationsMongoDbAsync<TEntity> : IBulkOperationsAsync<TEntity>
        where TEntity : class, IEntityBase
    {
        #region Bulk Insert with MongoDB Options

        /// <summary>
        /// Inserts a large collection of entities using MongoDB's InsertMany with MongoDB-specific options.
        /// </summary>
        /// <param name="entities">Collection of entities to insert.</param>
        /// <param name="options">MongoDB-specific bulk operation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of inserted rows and execution statistics.</returns>
        /// <remarks>
        /// <para>
        /// Uses MongoDB's native InsertMany for optimal performance.
        /// <list type="bullet">
        ///   <item>Set <see cref="MongoDbBulkOperationOptions.IsOrdered"/> to false for parallel inserts</item>
        ///   <item>Use <see cref="MongoDbBulkOperationOptions.BypassDocumentValidation"/> to skip validation</item>
        ///   <item>Configure <see cref="MongoDbBulkOperationOptions.WriteConcern"/> for durability requirements</item>
        /// </list>
        /// </para>
        /// </remarks>
        Task<BulkOperationResult> BulkInsertAsync(
            IList<TEntity> entities,
            MongoDbBulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion

        #region Bulk Update with MongoDB Options

        /// <summary>
        /// Updates a large collection of entities using MongoDB's BulkWrite with ReplaceOne operations.
        /// </summary>
        /// <param name="entities">Collection of entities to update (identified by _id).</param>
        /// <param name="options">MongoDB-specific bulk operation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of updated rows and execution statistics.</returns>
        /// <remarks>
        /// <para>
        /// Entities are matched by their _id field and completely replaced.
        /// For partial updates, use <see cref="UpdateManyAsync"/> or <see cref="BulkWriteAsync"/>.
        /// </para>
        /// </remarks>
        Task<BulkOperationResult> BulkUpdateAsync(
            IList<TEntity> entities,
            MongoDbBulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion

        #region Bulk Delete with MongoDB Options

        /// <summary>
        /// Deletes a large collection of entities using MongoDB's BulkWrite with DeleteOne operations.
        /// </summary>
        /// <param name="entities">Collection of entities to delete (identified by _id).</param>
        /// <param name="options">MongoDB-specific bulk operation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of deleted rows and execution statistics.</returns>
        Task<BulkOperationResult> BulkDeleteAsync(
            IList<TEntity> entities,
            MongoDbBulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion

        #region BulkWrite (Advanced)

        /// <summary>
        /// Executes a bulk write operation with mixed write models (inserts, updates, deletes).
        /// </summary>
        /// <param name="requests">Collection of write models to execute.</param>
        /// <param name="options">MongoDB-specific bulk operation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing statistics about all operations.</returns>
        /// <remarks>
        /// <para>
        /// This method provides direct access to MongoDB's BulkWrite for advanced scenarios:
        /// <list type="bullet">
        ///   <item>Mixed operations in a single call</item>
        ///   <item>Custom update definitions</item>
        ///   <item>Upsert operations</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var requests = new List&lt;WriteModel&lt;Document&gt;&gt;
        /// {
        ///     new InsertOneModel&lt;Document&gt;(newDoc),
        ///     new UpdateOneModel&lt;Document&gt;(
        ///         Builders&lt;Document&gt;.Filter.Eq(d => d.Id, id),
        ///         Builders&lt;Document&gt;.Update.Set(d => d.Status, "active")
        ///     ),
        ///     new DeleteOneModel&lt;Document&gt;(
        ///         Builders&lt;Document&gt;.Filter.Eq(d => d.Id, oldId)
        ///     )
        /// };
        /// 
        /// var result = await repository.BulkWriteAsync(requests, options);
        /// </code>
        /// </example>
        Task<MongoDbBulkOperationResult> BulkWriteAsync(
            IEnumerable<WriteModel<TEntity>> requests,
            MongoDbBulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion

        #region UpdateMany (Filter-based)

        /// <summary>
        /// Updates all documents matching the specified filter.
        /// </summary>
        /// <param name="filter">Filter to match documents to update.</param>
        /// <param name="update">Update definition specifying modifications.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of documents modified.</returns>
        /// <remarks>
        /// <para>
        /// Executes directly on the database without loading documents into memory.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Set status to "archived" for all inactive documents
        /// var count = await repository.UpdateManyAsync(
        ///     d => d.IsActive == false,
        ///     Builders&lt;Document&gt;.Update
        ///         .Set(d => d.Status, "archived")
        ///         .Set(d => d.ArchivedAt, DateTime.UtcNow)
        /// );
        /// </code>
        /// </example>
        Task<long> UpdateManyAsync(
            Expression<Func<TEntity, bool>> filter,
            UpdateDefinition<TEntity> update,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates all documents matching the specified filter using FilterDefinition.
        /// </summary>
        /// <param name="filter">Filter definition to match documents.</param>
        /// <param name="update">Update definition specifying modifications.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of documents modified.</returns>
        Task<long> UpdateManyAsync(
            FilterDefinition<TEntity> filter,
            UpdateDefinition<TEntity> update,
            CancellationToken cancellationToken = default);

        #endregion

        #region DeleteMany (Filter-based)

        /// <summary>
        /// Deletes all documents matching the specified filter.
        /// </summary>
        /// <param name="filter">Filter to match documents to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of documents deleted.</returns>
        /// <remarks>
        /// <para>
        /// This performs a hard delete. For soft delete, use UpdateManyAsync to set a deleted flag.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Delete all documents older than 5 years
        /// var count = await repository.DeleteManyAsync(
        ///     d => d.CreatedAt &lt; DateTime.UtcNow.AddYears(-5)
        /// );
        /// </code>
        /// </example>
        Task<long> DeleteManyAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all documents matching the specified filter using FilterDefinition.
        /// </summary>
        /// <param name="filter">Filter definition to match documents.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of documents deleted.</returns>
        Task<long> DeleteManyAsync(
            FilterDefinition<TEntity> filter,
            CancellationToken cancellationToken = default);

        #endregion
    }

    /// <summary>
    /// Extended result for MongoDB bulk operations with detailed statistics.
    /// </summary>
    public sealed class MongoDbBulkOperationResult
    {
        /// <summary>
        /// Number of documents inserted.
        /// </summary>
        public long InsertedCount { get; init; }

        /// <summary>
        /// Number of documents matched by update operations.
        /// </summary>
        public long MatchedCount { get; init; }

        /// <summary>
        /// Number of documents modified by update operations.
        /// </summary>
        public long ModifiedCount { get; init; }

        /// <summary>
        /// Number of documents deleted.
        /// </summary>
        public long DeletedCount { get; init; }

        /// <summary>
        /// Number of documents upserted.
        /// </summary>
        public long UpsertedCount { get; init; }

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
        /// Number of write errors that occurred (for unordered operations).
        /// </summary>
        public int WriteErrorCount { get; init; }

        /// <summary>
        /// Creates a successful result from MongoDB BulkWriteResult.
        /// </summary>
        public static MongoDbBulkOperationResult FromBulkWriteResult(BulkWriteResult<object> result, TimeSpan elapsed)
            => new()
            {
                InsertedCount = result.InsertedCount,
                MatchedCount = result.MatchedCount,
                ModifiedCount = result.ModifiedCount,
                DeletedCount = result.DeletedCount,
                UpsertedCount = result.Upserts?.Count ?? 0,
                ElapsedTime = elapsed,
                IsSuccess = true
            };

        /// <summary>
        /// Creates a successful result with counts.
        /// </summary>
        public static MongoDbBulkOperationResult Success(
            long insertedCount = 0,
            long matchedCount = 0,
            long modifiedCount = 0,
            long deletedCount = 0,
            TimeSpan elapsed = default)
            => new()
            {
                InsertedCount = insertedCount,
                MatchedCount = matchedCount,
                ModifiedCount = modifiedCount,
                DeletedCount = deletedCount,
                ElapsedTime = elapsed,
                IsSuccess = true
            };

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        public static MongoDbBulkOperationResult Failure(string errorMessage, TimeSpan elapsed, int writeErrorCount = 0)
            => new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ElapsedTime = elapsed,
                WriteErrorCount = writeErrorCount
            };

        /// <summary>
        /// Converts to the base BulkOperationResult.
        /// </summary>
        public BulkOperationResult ToBulkOperationResult()
            => IsSuccess
                ? BulkOperationResult.Success((int)(InsertedCount + ModifiedCount + DeletedCount), ElapsedTime)
                : BulkOperationResult.Failure(ErrorMessage, ElapsedTime);
    }
}


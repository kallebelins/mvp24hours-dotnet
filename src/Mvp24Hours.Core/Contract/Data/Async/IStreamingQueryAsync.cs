//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Interface for streaming query operations using IAsyncEnumerable.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <remarks>
    /// <para>
    /// Streaming queries use <see cref="IAsyncEnumerable{T}"/> to process results
    /// one at a time without loading everything into memory. This is ideal for:
    /// <list type="bullet">
    /// <item>Large result sets that would consume too much memory</item>
    /// <item>Data export operations</item>
    /// <item>ETL processes</item>
    /// <item>Background processing of records</item>
    /// </list>
    /// </para>
    /// <para>
    /// Unlike <see cref="IQueryAsync{TEntity}"/> which returns complete lists,
    /// streaming queries allow processing each item as it's retrieved from the database.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// await foreach (var customer in repository.StreamAllAsync())
    /// {
    ///     await ProcessCustomerAsync(customer);
    /// }
    /// </code>
    /// </example>
    public interface IStreamingQueryAsync<TEntity>
        where TEntity : IEntityBase
    {
        /// <summary>
        /// Streams all entities of the typed entity.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of entities.</returns>
        /// <remarks>
        /// <para>
        /// Results are returned one at a time as they're retrieved from the database.
        /// Use this for large datasets or when processing records individually.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// await foreach (var customer in repository.StreamAllAsync())
        /// {
        ///     await ProcessCustomerAsync(customer);
        /// }
        /// </code>
        /// </example>
        IAsyncEnumerable<TEntity> StreamAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams all entities with pagination criteria applied.
        /// </summary>
        /// <param name="criteria">Paging criteria (ordering, navigation).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of entities.</returns>
        /// <remarks>
        /// <para>
        /// Unlike batch queries, streaming with criteria still processes one record at a time.
        /// Useful for ordered streaming or including related data.
        /// </para>
        /// </remarks>
        IAsyncEnumerable<TEntity> StreamAllAsync(IPagingCriteria criteria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams entities matching the specified filter.
        /// </summary>
        /// <param name="clause">Filter expression.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of filtered entities.</returns>
        /// <example>
        /// <code>
        /// await foreach (var customer in repository.StreamByAsync(c => c.IsActive))
        /// {
        ///     await ProcessActiveCustomerAsync(customer);
        /// }
        /// </code>
        /// </example>
        IAsyncEnumerable<TEntity> StreamByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams entities matching the specified filter with pagination criteria.
        /// </summary>
        /// <param name="clause">Filter expression.</param>
        /// <param name="criteria">Paging criteria (ordering, navigation).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of filtered entities.</returns>
        IAsyncEnumerable<TEntity> StreamByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams entities in batches for efficient processing.
        /// </summary>
        /// <param name="batchSize">Number of entities per batch.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of entity batches.</returns>
        /// <remarks>
        /// <para>
        /// Batch streaming is useful when you need to process records in groups,
        /// such as batch database operations or parallel processing.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// await foreach (var batch in repository.StreamBatchesAsync(batchSize: 100))
        /// {
        ///     await BulkProcessAsync(batch);
        /// }
        /// </code>
        /// </example>
        IAsyncEnumerable<IList<TEntity>> StreamBatchesAsync(int batchSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams filtered entities in batches for efficient processing.
        /// </summary>
        /// <param name="clause">Filter expression.</param>
        /// <param name="batchSize">Number of entities per batch.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of entity batches.</returns>
        IAsyncEnumerable<IList<TEntity>> StreamBatchesAsync(Expression<Func<TEntity, bool>> clause, int batchSize, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for streaming repository operations, combining streaming queries with standard repository features.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <remarks>
    /// <para>
    /// Use this interface when you need both standard repository operations
    /// and streaming capabilities for large data processing.
    /// </para>
    /// </remarks>
    public interface IStreamingRepositoryAsync<T> : IRepositoryAsync<T>, IStreamingQueryAsync<T>
        where T : IEntityBase
    {
    }
}


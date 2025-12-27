//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Streaming
{
    /// <summary>
    /// Provides IAsyncEnumerable streaming capabilities for MongoDB queries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Streaming allows processing large result sets without loading all documents into memory.
    /// Documents are fetched and processed one at a time (or in batches).
    /// </para>
    /// <para>
    /// Benefits:
    /// <list type="bullet">
    ///   <item>Reduced memory usage for large result sets</item>
    ///   <item>Faster time-to-first-result</item>
    ///   <item>Better support for cancellation</item>
    ///   <item>Enables processing of results that don't fit in memory</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var streamer = new MongoDbAsyncStreaming&lt;Order&gt;(collection, logger);
    /// 
    /// // Stream all orders
    /// await foreach (var order in streamer.StreamAllAsync())
    /// {
    ///     ProcessOrder(order);
    /// }
    /// 
    /// // Stream with filter
    /// await foreach (var order in streamer.StreamAsync(o => o.Status == "Pending"))
    /// {
    ///     ProcessOrder(order);
    /// }
    /// 
    /// // Stream in batches
    /// await foreach (var batch in streamer.StreamBatchesAsync(batchSize: 100))
    /// {
    ///     ProcessBatch(batch);
    /// }
    /// </code>
    /// </example>
    public class MongoDbAsyncStreaming<T>
    {
        private readonly IMongoCollection<T> _collection;
        private readonly ILogger<MongoDbAsyncStreaming<T>> _logger;

        /// <summary>
        /// Initializes a new async streaming provider for the specified collection.
        /// </summary>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="logger">Optional logger for structured logging.</param>
        public MongoDbAsyncStreaming(IMongoCollection<T> collection, ILogger<MongoDbAsyncStreaming<T>> logger = null)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _logger = logger;
        }

        /// <summary>
        /// Streams all documents from the collection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of documents.</returns>
        public IAsyncEnumerable<T> StreamAllAsync(CancellationToken cancellationToken = default)
        {
            return StreamAsync(Builders<T>.Filter.Empty, null, null, cancellationToken);
        }

        /// <summary>
        /// Streams documents matching the specified filter.
        /// </summary>
        /// <param name="filter">The filter expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of documents.</returns>
        public IAsyncEnumerable<T> StreamAsync(
            Expression<Func<T, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            var filterDef = Builders<T>.Filter.Where(filter);
            return StreamAsync(filterDef, null, null, cancellationToken);
        }

        /// <summary>
        /// Streams documents with filter, sort, and projection.
        /// </summary>
        /// <param name="filter">The filter definition.</param>
        /// <param name="sort">The sort definition (optional).</param>
        /// <param name="projection">The projection definition (optional).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of documents.</returns>
        public async IAsyncEnumerable<T> StreamAsync(
            FilterDefinition<T> filter,
            SortDefinition<T> sort = null,
            ProjectionDefinition<T> projection = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Starting streaming for collection {CollectionName}", typeof(T).Name);

            var options = new FindOptions<T>
            {
                Sort = sort,
                Projection = projection,
                BatchSize = 1000 // MongoDB cursor batch size
            };

            using var cursor = await _collection.FindAsync(filter, options, cancellationToken);

            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (var document in cursor.Current)
                {
                    yield return document;
                }
            }

            _logger?.LogDebug("Streaming completed for collection {CollectionName}", typeof(T).Name);
        }

        /// <summary>
        /// Streams documents in batches of specified size.
        /// </summary>
        /// <param name="batchSize">The batch size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of document batches.</returns>
        public IAsyncEnumerable<IReadOnlyList<T>> StreamBatchesAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            return StreamBatchesAsync(Builders<T>.Filter.Empty, batchSize, null, null, cancellationToken);
        }

        /// <summary>
        /// Streams documents in batches with filter.
        /// </summary>
        /// <param name="filter">The filter expression.</param>
        /// <param name="batchSize">The batch size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of document batches.</returns>
        public IAsyncEnumerable<IReadOnlyList<T>> StreamBatchesAsync(
            Expression<Func<T, bool>> filter,
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            var filterDef = Builders<T>.Filter.Where(filter);
            return StreamBatchesAsync(filterDef, batchSize, null, null, cancellationToken);
        }

        /// <summary>
        /// Streams documents in batches with full options.
        /// </summary>
        /// <param name="filter">The filter definition.</param>
        /// <param name="batchSize">The batch size.</param>
        /// <param name="sort">The sort definition (optional).</param>
        /// <param name="projection">The projection definition (optional).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of document batches.</returns>
        public async IAsyncEnumerable<IReadOnlyList<T>> StreamBatchesAsync(
            FilterDefinition<T> filter,
            int batchSize,
            SortDefinition<T> sort = null,
            ProjectionDefinition<T> projection = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Starting batch streaming for collection {CollectionName} with batch size {BatchSize}", typeof(T).Name, batchSize);

            var options = new FindOptions<T>
            {
                Sort = sort,
                Projection = projection,
                BatchSize = batchSize
            };

            using var cursor = await _collection.FindAsync(filter, options, cancellationToken);
            var batch = new List<T>(batchSize);

            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (var document in cursor.Current)
                {
                    batch.Add(document);

                    if (batch.Count >= batchSize)
                    {
                        yield return batch.AsReadOnly();
                        batch = new List<T>(batchSize);
                    }
                }
            }

            // Return any remaining documents
            if (batch.Count > 0)
            {
                yield return batch.AsReadOnly();
            }

            _logger?.LogDebug("Batch streaming completed for collection {CollectionName}", typeof(T).Name);
        }

        /// <summary>
        /// Streams projected documents.
        /// </summary>
        /// <typeparam name="TProjection">The projection result type.</typeparam>
        /// <param name="filter">The filter definition.</param>
        /// <param name="projection">The projection definition.</param>
        /// <param name="sort">The sort definition (optional).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of projected documents.</returns>
        public async IAsyncEnumerable<TProjection> StreamProjectedAsync<TProjection>(
            FilterDefinition<T> filter,
            ProjectionDefinition<T, TProjection> projection,
            SortDefinition<T> sort = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Starting projected streaming for collection {CollectionName}", typeof(T).Name);

            var options = new FindOptions<T, TProjection>
            {
                Sort = sort,
                Projection = projection,
                BatchSize = 1000
            };

            using var cursor = await _collection.FindAsync(filter, options, cancellationToken);

            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (var document in cursor.Current)
                {
                    yield return document;
                }
            }

            _logger?.LogDebug("Projected streaming completed for collection {CollectionName}", typeof(T).Name);
        }

        /// <summary>
        /// Streams aggregation pipeline results.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="pipeline">The aggregation pipeline.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of aggregation results.</returns>
        public async IAsyncEnumerable<TResult> StreamAggregationAsync<TResult>(
            PipelineDefinition<T, TResult> pipeline,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Starting aggregation streaming for collection {CollectionName}", typeof(T).Name);

            var options = new AggregateOptions
            {
                BatchSize = 1000
            };

            using var cursor = await _collection.AggregateAsync(pipeline, options, cancellationToken);

            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (var document in cursor.Current)
                {
                    yield return document;
                }
            }

            _logger?.LogDebug("Aggregation streaming completed for collection {CollectionName}", typeof(T).Name);
        }

        /// <summary>
        /// Processes documents in parallel using streaming.
        /// </summary>
        /// <param name="filter">The filter definition.</param>
        /// <param name="processor">The async processor function.</param>
        /// <param name="maxDegreeOfParallelism">Maximum parallel operations.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task ProcessParallelAsync(
            FilterDefinition<T> filter,
            Func<T, CancellationToken, Task> processor,
            int maxDegreeOfParallelism = 4,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Starting parallel processing for collection {CollectionName} with max parallelism {MaxDegreeOfParallelism}",
                typeof(T).Name, maxDegreeOfParallelism);

            var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            var tasks = new List<Task>();

            await foreach (var document in StreamAsync(filter, null, null, cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await processor(document, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            _logger?.LogDebug("Parallel processing completed for collection {CollectionName}: {ProcessedCount} documents processed",
                typeof(T).Name, tasks.Count);
        }

        /// <summary>
        /// Counts documents while streaming (with progress callback).
        /// </summary>
        /// <param name="filter">The filter definition.</param>
        /// <param name="progressCallback">Progress callback (called every N documents).</param>
        /// <param name="progressInterval">How often to call progress callback.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The total count.</returns>
        public async Task<long> CountWithProgressAsync(
            FilterDefinition<T> filter,
            Action<long> progressCallback,
            int progressInterval = 1000,
            CancellationToken cancellationToken = default)
        {
            long count = 0;

            await foreach (var _ in StreamAsync(filter, null, null, cancellationToken))
            {
                count++;
                if (count % progressInterval == 0)
                {
                    progressCallback?.Invoke(count);
                }
            }

            progressCallback?.Invoke(count); // Final count
            return count;
        }
    }
}


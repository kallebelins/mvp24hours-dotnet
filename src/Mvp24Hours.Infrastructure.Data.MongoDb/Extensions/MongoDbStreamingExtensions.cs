//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Streaming;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for MongoDB streaming operations.
    /// </summary>
    public static class MongoDbStreamingExtensions
    {
        /// <summary>
        /// Creates an async streaming provider for the collection.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <returns>An async streaming provider.</returns>
        /// <example>
        /// <code>
        /// await foreach (var order in collection.AsAsyncStreaming().StreamAllAsync())
        /// {
        ///     ProcessOrder(order);
        /// }
        /// </code>
        /// </example>
        public static MongoDbAsyncStreaming<T> AsAsyncStreaming<T>(this IMongoCollection<T> collection)
        {
            return new MongoDbAsyncStreaming<T>(collection);
        }

        /// <summary>
        /// Streams all documents from the collection.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of documents.</returns>
        public static IAsyncEnumerable<T> StreamAllAsync<T>(
            this IMongoCollection<T> collection,
            CancellationToken cancellationToken = default)
        {
            return new MongoDbAsyncStreaming<T>(collection).StreamAllAsync(cancellationToken);
        }

        /// <summary>
        /// Streams documents matching the specified filter.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="filter">The filter expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of documents.</returns>
        public static IAsyncEnumerable<T> StreamAsync<T>(
            this IMongoCollection<T> collection,
            Expression<Func<T, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            return new MongoDbAsyncStreaming<T>(collection).StreamAsync(filter, cancellationToken);
        }

        /// <summary>
        /// Streams documents with filter definition.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="filter">The filter definition.</param>
        /// <param name="sort">The sort definition (optional).</param>
        /// <param name="projection">The projection definition (optional).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of documents.</returns>
        public static IAsyncEnumerable<T> StreamAsync<T>(
            this IMongoCollection<T> collection,
            FilterDefinition<T> filter,
            SortDefinition<T> sort = null,
            ProjectionDefinition<T> projection = null,
            CancellationToken cancellationToken = default)
        {
            return new MongoDbAsyncStreaming<T>(collection).StreamAsync(filter, sort, projection, cancellationToken);
        }

        /// <summary>
        /// Streams documents in batches.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="batchSize">The batch size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of document batches.</returns>
        public static IAsyncEnumerable<IReadOnlyList<T>> StreamBatchesAsync<T>(
            this IMongoCollection<T> collection,
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            return new MongoDbAsyncStreaming<T>(collection).StreamBatchesAsync(batchSize, cancellationToken);
        }

        /// <summary>
        /// Streams documents in batches with filter.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="filter">The filter expression.</param>
        /// <param name="batchSize">The batch size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of document batches.</returns>
        public static IAsyncEnumerable<IReadOnlyList<T>> StreamBatchesAsync<T>(
            this IMongoCollection<T> collection,
            Expression<Func<T, bool>> filter,
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            return new MongoDbAsyncStreaming<T>(collection).StreamBatchesAsync(filter, batchSize, cancellationToken);
        }
    }
}


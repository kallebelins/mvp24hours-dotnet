//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Profiling;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for MongoDB query profiling.
    /// </summary>
    public static class MongoDbProfilingExtensions
    {
        /// <summary>
        /// Creates a query profiler for the collection.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <returns>A query profiler.</returns>
        public static MongoDbQueryProfiler<T> AsProfiler<T>(this IMongoCollection<T> collection)
        {
            return new MongoDbQueryProfiler<T>(collection);
        }

        /// <summary>
        /// Gets explain output for a query.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="filter">The filter expression.</param>
        /// <param name="verbosity">The explain verbosity level.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The query explain result.</returns>
        public static Task<QueryExplainResult> ExplainAsync<T>(
            this IMongoCollection<T> collection,
            Expression<Func<T, bool>> filter,
            ExplainVerbosity verbosity = ExplainVerbosity.ExecutionStats,
            CancellationToken cancellationToken = default)
        {
            return new MongoDbQueryProfiler<T>(collection).ExplainAsync(filter, verbosity, cancellationToken);
        }

        /// <summary>
        /// Gets explain output for a query with filter and sort.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="filter">The filter definition.</param>
        /// <param name="sort">The sort definition.</param>
        /// <param name="verbosity">The explain verbosity level.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The query explain result.</returns>
        public static Task<QueryExplainResult> ExplainAsync<T>(
            this IMongoCollection<T> collection,
            FilterDefinition<T> filter,
            SortDefinition<T> sort = null,
            ExplainVerbosity verbosity = ExplainVerbosity.ExecutionStats,
            CancellationToken cancellationToken = default)
        {
            return new MongoDbQueryProfiler<T>(collection).ExplainAsync(filter, sort, verbosity, cancellationToken);
        }

        /// <summary>
        /// Executes a find with a specific index hint.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="filter">The filter definition.</param>
        /// <param name="indexName">The index name to hint.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The query results.</returns>
        public static Task<List<T>> FindWithHintAsync<T>(
            this IMongoCollection<T> collection,
            FilterDefinition<T> filter,
            string indexName,
            CancellationToken cancellationToken = default)
        {
            return new MongoDbQueryProfiler<T>(collection).FindWithHintAsync(filter, indexName, cancellationToken);
        }

        /// <summary>
        /// Gets all indexes for the collection.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of index information.</returns>
        public static Task<List<IndexInfo>> GetIndexInfoAsync<T>(
            this IMongoCollection<T> collection,
            CancellationToken cancellationToken = default)
        {
            return new MongoDbQueryProfiler<T>(collection).GetIndexesAsync(cancellationToken);
        }

        /// <summary>
        /// Gets collection statistics.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection statistics.</returns>
        public static Task<CollectionStats> GetCollectionStatsAsync<T>(
            this IMongoCollection<T> collection,
            CancellationToken cancellationToken = default)
        {
            return new MongoDbQueryProfiler<T>(collection).GetCollectionStatsAsync(cancellationToken);
        }
    }
}


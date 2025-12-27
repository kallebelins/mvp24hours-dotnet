//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Profiling
{
    /// <summary>
    /// Provides query profiling and explain capabilities for MongoDB operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this class to analyze query performance, understand index usage,
    /// and identify optimization opportunities.
    /// </para>
    /// <para>
    /// Explain verbosity levels:
    /// <list type="bullet">
    ///   <item><see cref="ExplainVerbosity.QueryPlanner"/> - Shows the winning query plan</item>
    ///   <item><see cref="ExplainVerbosity.ExecutionStats"/> - Includes execution statistics</item>
    ///   <item><see cref="ExplainVerbosity.AllPlansExecution"/> - Shows all considered plans and their execution</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var profiler = new MongoDbQueryProfiler&lt;Order&gt;(collection, logger);
    /// 
    /// // Get explain output
    /// var explain = await profiler.ExplainAsync(
    ///     o => o.Status == "Completed",
    ///     ExplainVerbosity.ExecutionStats);
    /// 
    /// Console.WriteLine($"Index used: {explain.IndexName}");
    /// Console.WriteLine($"Documents examined: {explain.DocumentsExamined}");
    /// Console.WriteLine($"Execution time: {explain.ExecutionTimeMs}ms");
    /// </code>
    /// </example>
    public class MongoDbQueryProfiler<T>
    {
        private readonly IMongoCollection<T> _collection;
        private readonly ILogger<MongoDbQueryProfiler<T>> _logger;

        /// <summary>
        /// Initializes a new query profiler for the specified collection.
        /// </summary>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="logger">Optional logger for structured logging.</param>
        public MongoDbQueryProfiler(IMongoCollection<T> collection, ILogger<MongoDbQueryProfiler<T>> logger = null)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _logger = logger;
        }

        /// <summary>
        /// Gets explain output for a filter query.
        /// </summary>
        /// <param name="filter">The filter expression.</param>
        /// <param name="verbosity">The explain verbosity level.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The query explain result.</returns>
        public async Task<QueryExplainResult> ExplainAsync(
            Expression<Func<T, bool>> filter,
            ExplainVerbosity verbosity = ExplainVerbosity.ExecutionStats,
            CancellationToken cancellationToken = default)
        {
            var filterDef = Builders<T>.Filter.Where(filter);
            return await ExplainAsync(filterDef, null, verbosity, cancellationToken);
        }

        /// <summary>
        /// Gets explain output for a filter query with sorting.
        /// </summary>
        /// <param name="filter">The filter definition.</param>
        /// <param name="sort">The sort definition.</param>
        /// <param name="verbosity">The explain verbosity level.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The query explain result.</returns>
        public async Task<QueryExplainResult> ExplainAsync(
            FilterDefinition<T> filter,
            SortDefinition<T> sort = null,
            ExplainVerbosity verbosity = ExplainVerbosity.ExecutionStats,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Starting query explain for collection {CollectionName}", typeof(T).Name);

            try
            {
                var findFluent = _collection.Find(filter);
                if (sort != null)
                {
                    findFluent = findFluent.Sort(sort);
                }

                // Use explain via command
                var command = new BsonDocument
                {
                    { "explain", new BsonDocument
                        {
                            { "find", _collection.CollectionNamespace.CollectionName },
                            { "filter", filter.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry) }
                        }
                    },
                    { "verbosity", verbosity.ToString().ToLowerInvariant() }
                };

                if (sort != null)
                {
                    var sortDoc = sort.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                    command["explain"]["sort"] = sortDoc;
                }

                var result = await _collection.Database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);
                var explainResult = ParseExplainResult(result);
                _logger?.LogDebug("Query explain completed for collection {CollectionName}: Index={IndexName}, ExecutionTime={ExecutionTimeMs}ms",
                    typeof(T).Name, explainResult.IndexName, explainResult.ExecutionTimeMs);
                return explainResult;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during query explain for collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Gets explain output for an aggregation pipeline.
        /// </summary>
        /// <param name="pipeline">The aggregation pipeline.</param>
        /// <param name="verbosity">The explain verbosity level.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The query explain result.</returns>
        public async Task<QueryExplainResult> ExplainAggregationAsync(
            PipelineDefinition<T, BsonDocument> pipeline,
            ExplainVerbosity verbosity = ExplainVerbosity.ExecutionStats,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Starting aggregation explain for collection {CollectionName}", typeof(T).Name);

            try
            {
                var pipelineDocs = pipeline.Render(
                    _collection.DocumentSerializer,
                    _collection.Settings.SerializerRegistry);

                var command = new BsonDocument
                {
                    { "explain", new BsonDocument
                        {
                            { "aggregate", _collection.CollectionNamespace.CollectionName },
                            { "pipeline", new BsonArray(pipelineDocs.Documents) },
                            { "cursor", new BsonDocument() }
                        }
                    },
                    { "verbosity", verbosity.ToString().ToLowerInvariant() }
                };

                var result = await _collection.Database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);
                var explainResult = ParseExplainResult(result);
                _logger?.LogDebug("Aggregation explain completed for collection {CollectionName}: ExecutionTime={ExecutionTimeMs}ms",
                    typeof(T).Name, explainResult.ExecutionTimeMs);
                return explainResult;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during aggregation explain for collection {CollectionName}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Provides query hint to force index usage.
        /// </summary>
        /// <param name="filter">The filter definition.</param>
        /// <param name="indexName">The index name to hint.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The query results with forced index.</returns>
        public async Task<List<T>> FindWithHintAsync(
            FilterDefinition<T> filter,
            string indexName,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Executing query with index hint {IndexName} for collection {CollectionName}", indexName, typeof(T).Name);

            var options = new FindOptions<T>
            {
                Hint = indexName
            };

            var cursor = await _collection.FindAsync(filter, options, cancellationToken);
            return await cursor.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Provides query hint to force index usage with index keys.
        /// </summary>
        /// <param name="filter">The filter definition.</param>
        /// <param name="indexKeys">The index keys document to hint.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The query results with forced index.</returns>
        public async Task<List<T>> FindWithHintAsync(
            FilterDefinition<T> filter,
            BsonDocument indexKeys,
            CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Executing query with index hint keys for collection {CollectionName}", typeof(T).Name);

            var options = new FindOptions<T>
            {
                Hint = indexKeys
            };

            var cursor = await _collection.FindAsync(filter, options, cancellationToken);
            return await cursor.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets all indexes for the collection with their statistics.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of index information.</returns>
        public async Task<List<IndexInfo>> GetIndexesAsync(CancellationToken cancellationToken = default)
        {
            var indexes = new List<IndexInfo>();
            var cursor = await _collection.Indexes.ListAsync(cancellationToken);

            await cursor.ForEachAsync(doc =>
            {
                indexes.Add(new IndexInfo
                {
                    Name = doc.GetValue("name", "").AsString,
                    Keys = doc.GetValue("key", new BsonDocument()).AsBsonDocument,
                    Unique = doc.GetValue("unique", false).AsBoolean,
                    Sparse = doc.GetValue("sparse", false).AsBoolean,
                    Background = doc.GetValue("background", false).AsBoolean,
                    ExpireAfterSeconds = doc.Contains("expireAfterSeconds")
                        ? (long?)doc["expireAfterSeconds"].AsInt64
                        : null,
                    PartialFilterExpression = doc.Contains("partialFilterExpression")
                        ? doc["partialFilterExpression"].AsBsonDocument
                        : null
                });
            }, cancellationToken);

            return indexes;
        }

        /// <summary>
        /// Gets collection statistics including size and index information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection statistics.</returns>
        public async Task<CollectionStats> GetCollectionStatsAsync(CancellationToken cancellationToken = default)
        {
            var command = new BsonDocument("collStats", _collection.CollectionNamespace.CollectionName);
            var result = await _collection.Database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);

            return new CollectionStats
            {
                Namespace = result.GetValue("ns", "").AsString,
                Count = result.GetValue("count", 0).ToInt64(),
                Size = result.GetValue("size", 0).ToInt64(),
                AvgObjSize = result.Contains("avgObjSize") ? result["avgObjSize"].ToDouble() : 0,
                StorageSize = result.GetValue("storageSize", 0).ToInt64(),
                TotalIndexSize = result.GetValue("totalIndexSize", 0).ToInt64(),
                IndexSizes = result.Contains("indexSizes")
                    ? result["indexSizes"].AsBsonDocument
                    : new BsonDocument(),
                Capped = result.GetValue("capped", false).AsBoolean
            };
        }

        #region Private Methods

        private static QueryExplainResult ParseExplainResult(BsonDocument result)
        {
            var explainResult = new QueryExplainResult
            {
                RawOutput = result
            };

            // Parse query planner
            if (result.Contains("queryPlanner"))
            {
                var queryPlanner = result["queryPlanner"].AsBsonDocument;

                if (queryPlanner.Contains("winningPlan"))
                {
                    var winningPlan = queryPlanner["winningPlan"].AsBsonDocument;
                    explainResult.WinningPlan = winningPlan;
                    explainResult.Stage = winningPlan.GetValue("stage", "").AsString;

                    // Try to get index name from input stage
                    if (winningPlan.Contains("inputStage"))
                    {
                        var inputStage = winningPlan["inputStage"].AsBsonDocument;
                        explainResult.IndexName = inputStage.GetValue("indexName", "").AsString;

                        if (inputStage.Contains("indexBounds"))
                        {
                            explainResult.IndexBounds = inputStage["indexBounds"].AsBsonDocument;
                        }
                    }
                }

                if (queryPlanner.Contains("rejectedPlans"))
                {
                    explainResult.RejectedPlansCount = queryPlanner["rejectedPlans"].AsBsonArray.Count;
                }
            }

            // Parse execution stats
            if (result.Contains("executionStats"))
            {
                var execStats = result["executionStats"].AsBsonDocument;

                explainResult.ExecutionSuccess = execStats.GetValue("executionSuccess", false).AsBoolean;
                explainResult.DocumentsReturned = execStats.GetValue("nReturned", 0).ToInt64();
                explainResult.ExecutionTimeMs = execStats.GetValue("executionTimeMillis", 0).ToInt64();
                explainResult.TotalKeysExamined = execStats.GetValue("totalKeysExamined", 0).ToInt64();
                explainResult.DocumentsExamined = execStats.GetValue("totalDocsExamined", 0).ToInt64();

                // Calculate efficiency
                if (explainResult.DocumentsExamined > 0)
                {
                    explainResult.Efficiency = (double)explainResult.DocumentsReturned / explainResult.DocumentsExamined;
                }
            }

            return explainResult;
        }

        #endregion
    }

    /// <summary>
    /// Specifies the verbosity level for explain output.
    /// </summary>
    public enum ExplainVerbosity
    {
        /// <summary>
        /// Returns information about the query plan without executing it.
        /// </summary>
        QueryPlanner,

        /// <summary>
        /// Executes the query and returns execution statistics.
        /// </summary>
        ExecutionStats,

        /// <summary>
        /// Executes all candidate plans and returns statistics for all.
        /// </summary>
        AllPlansExecution
    }

    /// <summary>
    /// Represents the result of a query explain operation.
    /// </summary>
    public class QueryExplainResult
    {
        /// <summary>
        /// Gets or sets the raw BSON output from explain.
        /// </summary>
        public BsonDocument RawOutput { get; set; }

        /// <summary>
        /// Gets or sets the winning plan document.
        /// </summary>
        public BsonDocument WinningPlan { get; set; }

        /// <summary>
        /// Gets or sets the execution stage (e.g., "COLLSCAN", "IXSCAN").
        /// </summary>
        public string Stage { get; set; }

        /// <summary>
        /// Gets or sets the name of the index used (if any).
        /// </summary>
        public string IndexName { get; set; }

        /// <summary>
        /// Gets or sets the index bounds used.
        /// </summary>
        public BsonDocument IndexBounds { get; set; }

        /// <summary>
        /// Gets or sets whether execution was successful.
        /// </summary>
        public bool ExecutionSuccess { get; set; }

        /// <summary>
        /// Gets or sets the number of documents returned.
        /// </summary>
        public long DocumentsReturned { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the total number of index keys examined.
        /// </summary>
        public long TotalKeysExamined { get; set; }

        /// <summary>
        /// Gets or sets the total number of documents examined.
        /// </summary>
        public long DocumentsExamined { get; set; }

        /// <summary>
        /// Gets or sets the query efficiency ratio (returned/examined).
        /// </summary>
        public double Efficiency { get; set; }

        /// <summary>
        /// Gets or sets the number of rejected query plans.
        /// </summary>
        public int RejectedPlansCount { get; set; }

        /// <summary>
        /// Gets whether the query used an index scan.
        /// </summary>
        public bool UsedIndex => !string.IsNullOrEmpty(IndexName) || Stage == "IXSCAN";

        /// <summary>
        /// Gets whether the query performed a collection scan.
        /// </summary>
        public bool IsCollectionScan => Stage == "COLLSCAN";
    }

    /// <summary>
    /// Represents information about a MongoDB index.
    /// </summary>
    public class IndexInfo
    {
        /// <summary>
        /// Gets or sets the index name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the index keys document.
        /// </summary>
        public BsonDocument Keys { get; set; }

        /// <summary>
        /// Gets or sets whether the index is unique.
        /// </summary>
        public bool Unique { get; set; }

        /// <summary>
        /// Gets or sets whether the index is sparse.
        /// </summary>
        public bool Sparse { get; set; }

        /// <summary>
        /// Gets or sets whether the index was built in background.
        /// </summary>
        public bool Background { get; set; }

        /// <summary>
        /// Gets or sets the TTL in seconds (for TTL indexes).
        /// </summary>
        public long? ExpireAfterSeconds { get; set; }

        /// <summary>
        /// Gets or sets the partial filter expression (for partial indexes).
        /// </summary>
        public BsonDocument PartialFilterExpression { get; set; }
    }

    /// <summary>
    /// Represents collection statistics.
    /// </summary>
    public class CollectionStats
    {
        /// <summary>
        /// Gets or sets the full namespace.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the document count.
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        /// Gets or sets the total data size in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the average object size in bytes.
        /// </summary>
        public double AvgObjSize { get; set; }

        /// <summary>
        /// Gets or sets the storage size in bytes.
        /// </summary>
        public long StorageSize { get; set; }

        /// <summary>
        /// Gets or sets the total index size in bytes.
        /// </summary>
        public long TotalIndexSize { get; set; }

        /// <summary>
        /// Gets or sets the individual index sizes.
        /// </summary>
        public BsonDocument IndexSizes { get; set; }

        /// <summary>
        /// Gets or sets whether the collection is capped.
        /// </summary>
        public bool Capped { get; set; }
    }
}


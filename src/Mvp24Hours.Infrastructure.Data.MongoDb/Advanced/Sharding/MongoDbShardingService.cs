//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Sharding
{
    /// <summary>
    /// Service for MongoDB Sharding operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service provides helpers for managing sharded collections.
    /// Most operations require admin privileges and a sharded cluster.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Enable sharding for a database
    /// await shardingService.EnableShardingAsync("myDatabase");
    /// 
    /// // Shard a collection with hashed key
    /// await shardingService.ShardCollectionAsync("myDatabase", "orders", new MongoDbShardingOptions
    /// {
    ///     ShardKeyFields = { ShardKeyField.Hashed("_id") }
    /// });
    /// 
    /// // Shard with compound key
    /// await shardingService.ShardCollectionAsync("myDatabase", "orders", new MongoDbShardingOptions
    /// {
    ///     ShardKeyFields = 
    ///     { 
    ///         ShardKeyField.Ascending("tenantId"),
    ///         ShardKeyField.Ascending("createdAt")
    ///     }
    /// });
    /// 
    /// // Get shard distribution
    /// var distribution = await shardingService.GetShardDistributionAsync("myDatabase", "orders");
    /// </code>
    /// </example>
    public class MongoDbShardingService : IMongoDbShardingService
    {
        private readonly IMongoClient _client;
        private readonly ILogger<MongoDbShardingService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbShardingService"/> class.
        /// </summary>
        /// <param name="client">The MongoDB client.</param>
        /// <param name="logger">Optional logger.</param>
        public MongoDbShardingService(
            IMongoClient client,
            ILogger<MongoDbShardingService> logger = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task EnableShardingAsync(
            string databaseName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException("Database name is required.", nameof(databaseName));
            }

            var adminDb = _client.GetDatabase("admin");
            var command = new BsonDocument("enableSharding", databaseName);

            await adminDb.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-sharding-enabled",
                new { Database = databaseName });

            _logger?.LogInformation("Sharding enabled for database '{DatabaseName}'.", databaseName);
        }

        /// <inheritdoc/>
        public async Task ShardCollectionAsync(
            string databaseName,
            string collectionName,
            MongoDbShardingOptions options,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException("Database name is required.", nameof(databaseName));
            }

            if (string.IsNullOrWhiteSpace(collectionName))
            {
                throw new ArgumentException("Collection name is required.", nameof(collectionName));
            }

            if (options?.ShardKeyFields == null || options.ShardKeyFields.Count == 0)
            {
                throw new ArgumentException("At least one shard key field is required.", nameof(options));
            }

            var adminDb = _client.GetDatabase("admin");

            // Build shard key
            var shardKey = new BsonDocument();
            foreach (var field in options.ShardKeyFields)
            {
                shardKey.Add(field.FieldName, field.Order);
            }

            var command = new BsonDocument
            {
                { "shardCollection", $"{databaseName}.{collectionName}" },
                { "key", shardKey }
            };

            if (options.UniqueShardKey)
            {
                command.Add("unique", true);
            }

            if (options.NumInitialChunks.HasValue)
            {
                command.Add("numInitialChunks", options.NumInitialChunks.Value);
            }

            await adminDb.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-collection-sharded",
                new { Database = databaseName, Collection = collectionName });

            _logger?.LogInformation("Collection '{DatabaseName}.{CollectionName}' sharded with key: {ShardKey}.",
                databaseName, collectionName, shardKey);
        }

        /// <inheritdoc/>
        public async Task<ShardDistribution> GetShardDistributionAsync(
            string databaseName,
            string collectionName,
            CancellationToken cancellationToken = default)
        {
            var database = _client.GetDatabase(databaseName);
            var command = new BsonDocument("collStats", collectionName);
            var stats = await database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);

            var distribution = new ShardDistribution
            {
                Namespace = $"{databaseName}.{collectionName}",
                TotalDocuments = stats.Contains("count") ? stats["count"].ToInt64() : 0,
                TotalDataSize = stats.Contains("size") ? stats["size"].ToInt64() : 0
            };

            if (stats.Contains("shards"))
            {
                var shards = stats["shards"].AsBsonDocument;
                foreach (var shard in shards.Elements)
                {
                    var shardStats = shard.Value.AsBsonDocument;
                    distribution.ShardStats.Add(new ShardStats
                    {
                        ShardId = shard.Name,
                        DocumentCount = shardStats.Contains("count") ? shardStats["count"].ToInt64() : 0,
                        DataSize = shardStats.Contains("size") ? shardStats["size"].ToInt64() : 0
                    });
                }

                // Calculate percentages
                foreach (var shardStat in distribution.ShardStats)
                {
                    shardStat.PercentageOfTotal = distribution.TotalDocuments > 0
                        ? (double)shardStat.DocumentCount / distribution.TotalDocuments * 100
                        : 0;
                }
            }

            return distribution;
        }

        /// <inheritdoc/>
        public async Task<IList<ShardInfo>> GetShardsAsync(CancellationToken cancellationToken = default)
        {
            var configDb = _client.GetDatabase("config");
            var shardsCollection = configDb.GetCollection<BsonDocument>("shards");

            var shards = await shardsCollection.Find(FilterDefinition<BsonDocument>.Empty)
                .ToListAsync(cancellationToken);

            return shards.Select(s => new ShardInfo
            {
                Id = s["_id"].AsString,
                Host = s["host"].AsString,
                State = s.Contains("state") ? s["state"].ToString() : "unknown",
                Tags = s.Contains("tags") ? s["tags"].AsBsonArray.Select(t => t.AsString).ToList() : new List<string>()
            }).ToList();
        }

        /// <inheritdoc/>
        public async Task<bool> IsDatabaseShardedAsync(
            string databaseName,
            CancellationToken cancellationToken = default)
        {
            var configDb = _client.GetDatabase("config");
            var databasesCollection = configDb.GetCollection<BsonDocument>("databases");

            var filter = Builders<BsonDocument>.Filter.Eq("_id", databaseName);
            var database = await databasesCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            return database != null && database.Contains("partitioned") && database["partitioned"].AsBoolean;
        }

        /// <inheritdoc/>
        public async Task<bool> IsCollectionShardedAsync(
            string databaseName,
            string collectionName,
            CancellationToken cancellationToken = default)
        {
            var shardKey = await GetShardKeyAsync(databaseName, collectionName, cancellationToken);
            return shardKey != null;
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> GetShardKeyAsync(
            string databaseName,
            string collectionName,
            CancellationToken cancellationToken = default)
        {
            var configDb = _client.GetDatabase("config");
            var collectionsConfig = configDb.GetCollection<BsonDocument>("collections");

            var ns = $"{databaseName}.{collectionName}";
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ns);
            var collection = await collectionsConfig.Find(filter).FirstOrDefaultAsync(cancellationToken);

            if (collection != null && collection.Contains("key"))
            {
                return collection["key"].AsBsonDocument;
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task MoveChunkAsync(
            string databaseName,
            string collectionName,
            BsonDocument chunkMin,
            string targetShard,
            CancellationToken cancellationToken = default)
        {
            var adminDb = _client.GetDatabase("admin");

            var command = new BsonDocument
            {
                { "moveChunk", $"{databaseName}.{collectionName}" },
                { "find", chunkMin },
                { "to", targetShard }
            };

            await adminDb.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);

            _logger?.LogInformation("Moved chunk to shard '{TargetShard}' for {Database}.{Collection}.",
                targetShard, databaseName, collectionName);
        }

        /// <inheritdoc/>
        public async Task SplitChunkAsync(
            string databaseName,
            string collectionName,
            BsonDocument splitPoint,
            CancellationToken cancellationToken = default)
        {
            var adminDb = _client.GetDatabase("admin");

            var command = new BsonDocument
            {
                { "split", $"{databaseName}.{collectionName}" },
                { "middle", splitPoint }
            };

            await adminDb.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);

            _logger?.LogInformation("Split chunk at {SplitPoint} for {Database}.{Collection}.",
                splitPoint, databaseName, collectionName);
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> GetClusterStatsAsync(CancellationToken cancellationToken = default)
        {
            var adminDb = _client.GetDatabase("admin");
            var command = new BsonDocument("serverStatus", 1);

            return await adminDb.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);
        }
    }
}


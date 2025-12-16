//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Sharding
{
    /// <summary>
    /// Interface for MongoDB Sharding operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sharding requires:
    /// <list type="bullet">
    ///   <item>A sharded cluster (mongos, config servers, shards)</item>
    ///   <item>Admin privileges to execute sharding commands</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IMongoDbShardingService
    {
        /// <summary>
        /// Enables sharding for a database.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task EnableShardingAsync(
            string databaseName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Shards a collection.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="options">Sharding options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ShardCollectionAsync(
            string databaseName,
            string collectionName,
            MongoDbShardingOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the shard distribution for a collection.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Information about shard distribution.</returns>
        Task<ShardDistribution> GetShardDistributionAsync(
            string databaseName,
            string collectionName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of shards in the cluster.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of shards.</returns>
        Task<IList<ShardInfo>> GetShardsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a database is sharded.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the database is sharded.</returns>
        Task<bool> IsDatabaseShardedAsync(
            string databaseName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a collection is sharded.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the collection is sharded.</returns>
        Task<bool> IsCollectionShardedAsync(
            string databaseName,
            string collectionName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the shard key for a collection.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The shard key, or null if not sharded.</returns>
        Task<BsonDocument> GetShardKeyAsync(
            string databaseName,
            string collectionName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Moves a chunk to a different shard.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="chunkMin">The minimum bound of the chunk.</param>
        /// <param name="targetShard">The target shard name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task MoveChunkAsync(
            string databaseName,
            string collectionName,
            BsonDocument chunkMin,
            string targetShard,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Splits a chunk at a specific point.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="splitPoint">The point at which to split.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SplitChunkAsync(
            string databaseName,
            string collectionName,
            BsonDocument splitPoint,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets cluster statistics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Cluster statistics.</returns>
        Task<BsonDocument> GetClusterStatsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Information about a shard.
    /// </summary>
    public class ShardInfo
    {
        /// <summary>
        /// Gets or sets the shard ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the shard host address.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the shard state.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets additional shard tags.
        /// </summary>
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Information about data distribution across shards.
    /// </summary>
    public class ShardDistribution
    {
        /// <summary>
        /// Gets or sets the collection namespace.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the total document count.
        /// </summary>
        public long TotalDocuments { get; set; }

        /// <summary>
        /// Gets or sets the total data size in bytes.
        /// </summary>
        public long TotalDataSize { get; set; }

        /// <summary>
        /// Gets or sets the number of chunks.
        /// </summary>
        public int TotalChunks { get; set; }

        /// <summary>
        /// Gets or sets distribution per shard.
        /// </summary>
        public List<ShardStats> ShardStats { get; set; } = new();
    }

    /// <summary>
    /// Statistics for a single shard.
    /// </summary>
    public class ShardStats
    {
        /// <summary>
        /// Gets or sets the shard ID.
        /// </summary>
        public string ShardId { get; set; }

        /// <summary>
        /// Gets or sets the document count on this shard.
        /// </summary>
        public long DocumentCount { get; set; }

        /// <summary>
        /// Gets or sets the data size in bytes on this shard.
        /// </summary>
        public long DataSize { get; set; }

        /// <summary>
        /// Gets or sets the number of chunks on this shard.
        /// </summary>
        public int ChunkCount { get; set; }

        /// <summary>
        /// Gets the percentage of total documents.
        /// </summary>
        public double PercentageOfTotal { get; set; }
    }
}


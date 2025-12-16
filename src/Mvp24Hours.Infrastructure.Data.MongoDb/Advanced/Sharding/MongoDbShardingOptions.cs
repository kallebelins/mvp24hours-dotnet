//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Sharding
{
    /// <summary>
    /// Options for MongoDB sharding configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sharding is MongoDB's approach to horizontal scaling. It distributes data
    /// across multiple machines (shards) to support deployments with very large
    /// data sets and high throughput operations.
    /// </para>
    /// <para>
    /// <b>Important considerations:</b>
    /// <list type="bullet">
    ///   <item>Sharding must be enabled at the database level first</item>
    ///   <item>Choose shard key carefully - it cannot be changed after creation</item>
    ///   <item>Shard key should provide good write distribution</item>
    ///   <item>Shard key should support your most common queries</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class MongoDbShardingOptions
    {
        /// <summary>
        /// Gets or sets the shard key fields.
        /// </summary>
        /// <remarks>
        /// The shard key is a field or combination of fields that MongoDB uses
        /// to distribute documents across shards.
        /// </remarks>
        public List<ShardKeyField> ShardKeyFields { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to use a hashed shard key.
        /// </summary>
        /// <remarks>
        /// Hashed shard keys provide more even data distribution but don't
        /// support range queries on the shard key efficiently.
        /// </remarks>
        public bool UseHashedShardKey { get; set; }

        /// <summary>
        /// Gets or sets whether the shard key is unique.
        /// </summary>
        public bool UniqueShardKey { get; set; }

        /// <summary>
        /// Gets or sets the number of initial chunks to create.
        /// </summary>
        /// <remarks>
        /// Pre-splitting can help with initial data distribution.
        /// </remarks>
        public int? NumInitialChunks { get; set; }
    }

    /// <summary>
    /// Represents a field in a shard key.
    /// </summary>
    public class ShardKeyField
    {
        /// <summary>
        /// Gets or sets the field name.
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// Gets or sets the sort order (1 for ascending, -1 for descending, "hashed" for hashed).
        /// </summary>
        public BsonValue Order { get; set; } = 1;

        /// <summary>
        /// Creates an ascending shard key field.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        public static ShardKeyField Ascending(string fieldName) => new() { FieldName = fieldName, Order = 1 };

        /// <summary>
        /// Creates a descending shard key field.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        public static ShardKeyField Descending(string fieldName) => new() { FieldName = fieldName, Order = -1 };

        /// <summary>
        /// Creates a hashed shard key field.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        public static ShardKeyField Hashed(string fieldName) => new() { FieldName = fieldName, Order = "hashed" };
    }
}


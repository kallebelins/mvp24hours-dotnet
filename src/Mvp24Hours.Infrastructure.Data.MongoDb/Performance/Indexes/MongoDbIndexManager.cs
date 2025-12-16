//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Indexes
{
    /// <summary>
    /// Manages automatic index creation for MongoDB collections based on attribute definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The MongoDbIndexManager scans entity types for index attributes and creates the corresponding
    /// indexes in MongoDB. It supports:
    /// <list type="bullet">
    ///   <item>Single-field indexes via <see cref="MongoIndexAttribute"/></item>
    ///   <item>Compound indexes via <see cref="MongoCompoundIndexAttribute"/></item>
    ///   <item>TTL indexes via <see cref="MongoTtlIndexAttribute"/></item>
    /// </list>
    /// </para>
    /// <para>
    /// Indexes are created lazily when collections are first accessed, or can be created
    /// explicitly using the EnsureIndexes methods.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register in DI
    /// services.AddSingleton&lt;IMongoDbIndexManager, MongoDbIndexManager&gt;();
    /// 
    /// // Create indexes on startup
    /// var indexManager = serviceProvider.GetRequiredService&lt;IMongoDbIndexManager&gt;();
    /// await indexManager.EnsureIndexesAsync&lt;Customer&gt;(collection);
    /// 
    /// // Or scan an entire assembly
    /// await indexManager.EnsureAllIndexesAsync(database, typeof(Customer).Assembly);
    /// </code>
    /// </example>
    public class MongoDbIndexManager : IMongoDbIndexManager
    {
        private static readonly ConcurrentDictionary<Type, bool> _indexesCreated = new();
        private readonly object _lock = new();

        /// <inheritdoc/>
        public async Task EnsureIndexesAsync<T>(
            IMongoCollection<T> collection,
            CancellationToken cancellationToken = default)
        {
            var type = typeof(T);

            // Check if indexes were already created for this type
            if (_indexesCreated.ContainsKey(type))
            {
                return;
            }

            lock (_lock)
            {
                if (_indexesCreated.ContainsKey(type))
                {
                    return;
                }

                TelemetryHelper.Execute(TelemetryLevels.Verbose,
                    "mongodb-index-manager-ensure-start",
                    new { Type = type.Name });

                try
                {
                    var indexes = BuildIndexModels<T>();

                    if (indexes.Count > 0)
                    {
                        // Create indexes synchronously within lock to ensure thread safety
                        var task = collection.Indexes.CreateManyAsync(indexes, cancellationToken);
                        task.Wait(cancellationToken);

                        TelemetryHelper.Execute(TelemetryLevels.Verbose,
                            "mongodb-index-manager-created",
                            new { Type = type.Name, Count = indexes.Count });
                    }

                    _indexesCreated.TryAdd(type, true);
                }
                catch (Exception ex)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Warning,
                        "mongodb-index-manager-error",
                        new { Type = type.Name, Error = ex.Message });
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task EnsureAllIndexesAsync(
            IMongoDatabase database,
            Assembly assembly,
            CancellationToken cancellationToken = default)
        {
            var entityTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => HasIndexAttributes(t))
                .ToList();

            TelemetryHelper.Execute(TelemetryLevels.Verbose,
                "mongodb-index-manager-scan-assembly",
                new { Assembly = assembly.GetName().Name, TypesFound = entityTypes.Count });

            foreach (var type in entityTypes)
            {
                var collectionName = GetCollectionName(type);
                var method = typeof(MongoDbIndexManager)
                    .GetMethod(nameof(EnsureIndexesForTypeAsync), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(type);

                if (method != null)
                {
                    var task = (Task)method.Invoke(this, new object[] { database, collectionName, cancellationToken });
                    await task;
                }
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<CreateIndexModel<T>> BuildIndexModels<T>()
        {
            var type = typeof(T);
            var indexes = new List<CreateIndexModel<T>>();

            // Process property-level indexes
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Group properties by compound index group
            var compoundGroups = new Dictionary<string, List<(PropertyInfo Property, MongoIndexAttribute Attr)>>();

            foreach (var property in properties)
            {
                var indexAttr = property.GetCustomAttribute<MongoIndexAttribute>();
                if (indexAttr != null)
                {
                    if (!string.IsNullOrEmpty(indexAttr.CompoundIndexGroup))
                    {
                        if (!compoundGroups.ContainsKey(indexAttr.CompoundIndexGroup))
                        {
                            compoundGroups[indexAttr.CompoundIndexGroup] = new List<(PropertyInfo, MongoIndexAttribute)>();
                        }
                        compoundGroups[indexAttr.CompoundIndexGroup].Add((property, indexAttr));
                    }
                    else
                    {
                        // Single-field index
                        var index = BuildSingleFieldIndex<T>(property, indexAttr);
                        if (index != null)
                        {
                            indexes.Add(index);
                        }
                    }
                }

                // Process TTL indexes
                var ttlAttr = property.GetCustomAttribute<MongoTtlIndexAttribute>();
                if (ttlAttr != null)
                {
                    var ttlIndex = BuildTtlIndex<T>(property, ttlAttr);
                    if (ttlIndex != null)
                    {
                        indexes.Add(ttlIndex);
                    }
                }
            }

            // Create compound indexes from grouped properties
            foreach (var group in compoundGroups)
            {
                var orderedProps = group.Value.OrderBy(x => x.Attr.Order).ToList();
                var index = BuildCompoundIndexFromProperties<T>(group.Key, orderedProps);
                if (index != null)
                {
                    indexes.Add(index);
                }
            }

            // Process class-level compound indexes
            var compoundAttrs = type.GetCustomAttributes<MongoCompoundIndexAttribute>();
            foreach (var attr in compoundAttrs)
            {
                var index = BuildCompoundIndex<T>(attr);
                if (index != null)
                {
                    indexes.Add(index);
                }
            }

            return indexes;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<BsonDocument>> GetExistingIndexesAsync<T>(
            IMongoCollection<T> collection,
            CancellationToken cancellationToken = default)
        {
            var cursor = await collection.Indexes.ListAsync(cancellationToken);
            return await cursor.ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DropIndexAsync<T>(
            IMongoCollection<T> collection,
            string indexName,
            CancellationToken cancellationToken = default)
        {
            await collection.Indexes.DropOneAsync(indexName, cancellationToken);

            TelemetryHelper.Execute(TelemetryLevels.Verbose,
                "mongodb-index-manager-dropped",
                new { Type = typeof(T).Name, IndexName = indexName });
        }

        /// <inheritdoc/>
        public void ResetIndexCache()
        {
            _indexesCreated.Clear();

            TelemetryHelper.Execute(TelemetryLevels.Verbose,
                "mongodb-index-manager-cache-reset");
        }

        #region Private Methods

        private async Task EnsureIndexesForTypeAsync<T>(
            IMongoDatabase database,
            string collectionName,
            CancellationToken cancellationToken)
        {
            var collection = database.GetCollection<T>(collectionName);
            await EnsureIndexesAsync(collection, cancellationToken);
        }

        private static bool HasIndexAttributes(Type type)
        {
            var hasClassAttr = type.GetCustomAttributes<MongoCompoundIndexAttribute>().Any();
            if (hasClassAttr) return true;

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return properties.Any(p =>
                p.GetCustomAttribute<MongoIndexAttribute>() != null ||
                p.GetCustomAttribute<MongoTtlIndexAttribute>() != null);
        }

        private static string GetCollectionName(Type type)
        {
            // Check for BsonCollection attribute or use type name
            var collectionAttr = type.GetCustomAttribute<BsonCollectionAttribute>();
            return collectionAttr?.CollectionName ?? type.Name;
        }

        private static CreateIndexModel<T> BuildSingleFieldIndex<T>(PropertyInfo property, MongoIndexAttribute attr)
        {
            var fieldName = GetBsonFieldName(property);
            var keyDefinition = CreateKeyDefinition<T>(fieldName, attr.IndexType);
            var options = CreateIndexOptions<T>(attr.Name ?? $"idx_{fieldName}", attr.Unique, attr.Sparse, attr.Background);

            if (!string.IsNullOrEmpty(attr.PartialFilterExpression))
            {
                options.PartialFilterExpression = BsonDocument.Parse(attr.PartialFilterExpression);
            }

            if (!string.IsNullOrEmpty(attr.CollationLocale))
            {
                options.Collation = new Collation(attr.CollationLocale,
                    strength: attr.CollationCaseInsensitive ? CollationStrength.Secondary : CollationStrength.Tertiary);
            }

            return new CreateIndexModel<T>(keyDefinition, options);
        }

        private static CreateIndexModel<T> BuildTtlIndex<T>(PropertyInfo property, MongoTtlIndexAttribute attr)
        {
            var fieldName = GetBsonFieldName(property);
            var keyDefinition = Builders<T>.IndexKeys.Ascending(fieldName);

            var options = new CreateIndexOptions<T>
            {
                Name = attr.Name ?? $"idx_ttl_{fieldName}",
                Background = attr.Background,
                ExpireAfter = TimeSpan.FromSeconds(attr.ExpireAfterSeconds)
            };

            return new CreateIndexModel<T>(keyDefinition, options);
        }

        private static CreateIndexModel<T> BuildCompoundIndexFromProperties<T>(
            string groupName,
            List<(PropertyInfo Property, MongoIndexAttribute Attr)> properties)
        {
            var keyBuilder = Builders<T>.IndexKeys;
            IndexKeysDefinition<T> keys = null;

            foreach (var (property, attr) in properties)
            {
                var fieldName = GetBsonFieldName(property);
                var keyDef = CreateKeyDefinition<T>(fieldName, attr.IndexType);

                keys = keys == null ? keyDef : Builders<T>.IndexKeys.Combine(keys, keyDef);
            }

            if (keys == null) return null;

            var firstAttr = properties.First().Attr;
            var options = CreateIndexOptions<T>(
                firstAttr.Name ?? $"idx_compound_{groupName}",
                firstAttr.Unique,
                firstAttr.Sparse,
                firstAttr.Background);

            return new CreateIndexModel<T>(keys, options);
        }

        private static CreateIndexModel<T> BuildCompoundIndex<T>(MongoCompoundIndexAttribute attr)
        {
            if (string.IsNullOrEmpty(attr.Fields))
            {
                return null;
            }

            var fieldDefinitions = attr.Fields.Split(',', StringSplitOptions.RemoveEmptyEntries);
            IndexKeysDefinition<T> keys = null;

            foreach (var fieldDef in fieldDefinitions)
            {
                var parts = fieldDef.Trim().Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                var fieldName = parts[0].Trim();
                var direction = parts[1].Trim().ToLower();

                IndexKeysDefinition<T> keyDef = direction switch
                {
                    "1" or "asc" or "ascending" => Builders<T>.IndexKeys.Ascending(fieldName),
                    "-1" or "desc" or "descending" => Builders<T>.IndexKeys.Descending(fieldName),
                    "text" => Builders<T>.IndexKeys.Text(fieldName),
                    "hashed" => Builders<T>.IndexKeys.Hashed(fieldName),
                    "2d" => Builders<T>.IndexKeys.Geo2D(fieldName),
                    "2dsphere" => Builders<T>.IndexKeys.Geo2DSphere(fieldName),
                    _ => Builders<T>.IndexKeys.Ascending(fieldName)
                };

                keys = keys == null ? keyDef : Builders<T>.IndexKeys.Combine(keys, keyDef);
            }

            if (keys == null) return null;

            var options = CreateIndexOptions<T>(attr.Name, attr.Unique, attr.Sparse, attr.Background);

            if (!string.IsNullOrEmpty(attr.PartialFilterExpression))
            {
                options.PartialFilterExpression = BsonDocument.Parse(attr.PartialFilterExpression);
            }

            if (!string.IsNullOrEmpty(attr.CollationLocale))
            {
                options.Collation = new Collation(attr.CollationLocale,
                    strength: attr.CollationCaseInsensitive ? CollationStrength.Secondary : CollationStrength.Tertiary);
            }

            return new CreateIndexModel<T>(keys, options);
        }

        private static IndexKeysDefinition<T> CreateKeyDefinition<T>(string fieldName, MongoIndexType indexType)
        {
            return indexType switch
            {
                MongoIndexType.Ascending => Builders<T>.IndexKeys.Ascending(fieldName),
                MongoIndexType.Descending => Builders<T>.IndexKeys.Descending(fieldName),
                MongoIndexType.Hashed => Builders<T>.IndexKeys.Hashed(fieldName),
                MongoIndexType.Text => Builders<T>.IndexKeys.Text(fieldName),
                MongoIndexType.Geo2d => Builders<T>.IndexKeys.Geo2D(fieldName),
                MongoIndexType.Geo2dSphere => Builders<T>.IndexKeys.Geo2DSphere(fieldName),
                MongoIndexType.Wildcard => Builders<T>.IndexKeys.Wildcard(fieldName),
                _ => Builders<T>.IndexKeys.Ascending(fieldName)
            };
        }

        private static CreateIndexOptions<T> CreateIndexOptions<T>(string name, bool unique, bool sparse, bool background)
        {
            return new CreateIndexOptions<T>
            {
                Name = name,
                Unique = unique,
                Sparse = sparse,
                Background = background
            };
        }

        private static string GetBsonFieldName(PropertyInfo property)
        {
            var bsonElement = property.GetCustomAttribute<BsonElementAttribute>();
            return bsonElement?.ElementName ?? property.Name;
        }

        #endregion
    }

    /// <summary>
    /// Attribute to specify a custom collection name for BSON mapping.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class BsonCollectionAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the collection name.
        /// </summary>
        public string CollectionName { get; }

        /// <summary>
        /// Initializes a new instance with the specified collection name.
        /// </summary>
        /// <param name="collectionName">The MongoDB collection name.</param>
        public BsonCollectionAttribute(string collectionName)
        {
            CollectionName = collectionName;
        }
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Attributes;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Indexes;

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
/// <remarks>
/// Initializes a new instance of the <see cref="MongoDbIndexManager"/> class.
/// </remarks>
/// <param name="logger">Optional logger for structured logging.</param>
public class MongoDbIndexManager(ILogger<MongoDbIndexManager>? logger = null) : IMongoDbIndexManager
{
    private static readonly ConcurrentDictionary<Type, bool> _indexesCreated = new();
    private readonly object _lock = new();
    private readonly ILogger<MongoDbIndexManager>? _logger = logger;

    /// <inheritdoc/>
    public async Task EnsureIndexesAsync<T>(
        IMongoCollection<T> collection,
        CancellationToken cancellationToken = default)
    {
        Type type = typeof(T);

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

            _logger?.LogDebug("Ensuring indexes for type {TypeName}", type.Name);

            try
            {
                IReadOnlyList<CreateIndexModel<T>> indexes = BuildIndexModels<T>();

                if (indexes.Count > 0)
                {
                    // Create indexes synchronously within lock to ensure thread safety
                    Task<IEnumerable<string>> task = collection.Indexes.CreateManyAsync(indexes, cancellationToken);
                    task.Wait(cancellationToken);

                    _logger?.LogInformation("Created {IndexCount} indexes for type {TypeName}", indexes.Count, type.Name);
                }

                _indexesCreated.TryAdd(type, true);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error creating indexes for type {TypeName}: {ErrorMessage}", type.Name, ex.Message);
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

        _logger?.LogDebug("Scanning assembly {AssemblyName} for index attributes: found {TypeCount} types",
            assembly.GetName().Name, entityTypes.Count);

        foreach (Type? type in entityTypes)
        {
            string collectionName = GetCollectionName(type);
            MethodInfo? method = typeof(MongoDbIndexManager)
                .GetMethod(nameof(EnsureIndexesForTypeAsync), BindingFlags.NonPublic | BindingFlags.Instance)
                ?.MakeGenericMethod(type);

            if (method != null)
            {
                if (method.Invoke(this, [database, collectionName, cancellationToken]) is Task task)
                {
                    await task;
                }
            }
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<CreateIndexModel<T>> BuildIndexModels<T>()
    {
        Type type = typeof(T);
        var indexes = new List<CreateIndexModel<T>>();

        // Process property-level indexes
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Group properties by compound index group
        var compoundGroups = new Dictionary<string, List<(PropertyInfo Property, MongoIndexAttribute Attr)>>();

        foreach (PropertyInfo property in properties)
        {
            MongoIndexAttribute? indexAttr = property.GetCustomAttribute<MongoIndexAttribute>();
            if (indexAttr != null)
            {
                if (!string.IsNullOrEmpty(indexAttr.CompoundIndexGroup))
                {
                    if (!compoundGroups.ContainsKey(indexAttr.CompoundIndexGroup))
                    {
                        compoundGroups[indexAttr.CompoundIndexGroup] = [];
                    }
                    compoundGroups[indexAttr.CompoundIndexGroup].Add((property, indexAttr));
                }
                else
                {
                    // Single-field index
                    CreateIndexModel<T> index = BuildSingleFieldIndex<T>(property, indexAttr);
                    if (index != null)
                    {
                        indexes.Add(index);
                    }
                }
            }

            // Process TTL indexes
            MongoTtlIndexAttribute? ttlAttr = property.GetCustomAttribute<MongoTtlIndexAttribute>();
            if (ttlAttr != null)
            {
                CreateIndexModel<T> ttlIndex = BuildTtlIndex<T>(property, ttlAttr);
                if (ttlIndex != null)
                {
                    indexes.Add(ttlIndex);
                }
            }
        }

        // Create compound indexes from grouped properties
        foreach (KeyValuePair<string, List<(PropertyInfo Property, MongoIndexAttribute Attr)>> group in compoundGroups)
        {
            var orderedProps = group.Value.OrderBy(x => x.Attr.Order).ToList();
            CreateIndexModel<T>? index = BuildCompoundIndexFromProperties<T>(group.Key, orderedProps);
            if (index != null)
            {
                indexes.Add(index);
            }
        }

        // Process class-level compound indexes
        IEnumerable<MongoCompoundIndexAttribute> compoundAttrs = type.GetCustomAttributes<MongoCompoundIndexAttribute>();
        foreach (MongoCompoundIndexAttribute attr in compoundAttrs)
        {
            CreateIndexModel<T>? index = BuildCompoundIndex<T>(attr);
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
        IAsyncCursor<BsonDocument> cursor = await collection.Indexes.ListAsync(cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DropIndexAsync<T>(
        IMongoCollection<T> collection,
        string indexName,
        CancellationToken cancellationToken = default)
    {
        await collection.Indexes.DropOneAsync(indexName, cancellationToken);

        _logger?.LogInformation("Dropped index {IndexName} for type {TypeName}", indexName, typeof(T).Name);
    }

    /// <inheritdoc/>
    public void ResetIndexCache()
    {
        _indexesCreated.Clear();

        _logger?.LogDebug("Index cache reset");
    }

    #region Private Methods

    private async Task EnsureIndexesForTypeAsync<T>(
        IMongoDatabase database,
        string collectionName,
        CancellationToken cancellationToken)
    {
        IMongoCollection<T> collection = database.GetCollection<T>(collectionName);
        await EnsureIndexesAsync(collection, cancellationToken);
    }

    private static bool HasIndexAttributes(Type type)
    {
        bool hasClassAttr = type.GetCustomAttributes<MongoCompoundIndexAttribute>().Any();
        if (hasClassAttr)
        {
            return true;
        }

        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties.Any(p =>
            p.GetCustomAttribute<MongoIndexAttribute>() != null ||
            p.GetCustomAttribute<MongoTtlIndexAttribute>() != null);
    }

    private static string GetCollectionName(Type type)
    {
        // Check for BsonCollection attribute or use type name
        BsonCollectionAttribute? collectionAttr = type.GetCustomAttribute<BsonCollectionAttribute>();
        return collectionAttr?.CollectionName ?? type.Name;
    }

    private static CreateIndexModel<T> BuildSingleFieldIndex<T>(PropertyInfo property, MongoIndexAttribute attr)
    {
        string fieldName = GetBsonFieldName(property);
        IndexKeysDefinition<T> keyDefinition = CreateKeyDefinition<T>(fieldName, attr.IndexType);
        CreateIndexOptions<T> options = CreateIndexOptions<T>(attr.Name ?? $"idx_{fieldName}", attr.Unique, attr.Sparse, attr.Background);

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
        string fieldName = GetBsonFieldName(property);
        IndexKeysDefinition<T> keyDefinition = Builders<T>.IndexKeys.Ascending(fieldName);

        var options = new CreateIndexOptions<T>
        {
            Name = attr.Name ?? $"idx_ttl_{fieldName}",
            Background = attr.Background,
            ExpireAfter = TimeSpan.FromSeconds(attr.ExpireAfterSeconds)
        };

        return new CreateIndexModel<T>(keyDefinition, options);
    }

    private static CreateIndexModel<T>? BuildCompoundIndexFromProperties<T>(
        string groupName,
        List<(PropertyInfo Property, MongoIndexAttribute Attr)> properties)
    {
        _ = Builders<T>.IndexKeys;
        IndexKeysDefinition<T>? keys = null;

        foreach ((PropertyInfo? property, MongoIndexAttribute? attr) in properties)
        {
            string fieldName = GetBsonFieldName(property);
            IndexKeysDefinition<T> keyDef = CreateKeyDefinition<T>(fieldName, attr.IndexType);

            keys = keys == null ? keyDef : Builders<T>.IndexKeys.Combine(keys, keyDef);
        }

        if (keys == null)
        {
            return null;
        }

        MongoIndexAttribute firstAttr = properties.First().Attr;
        CreateIndexOptions<T> options = CreateIndexOptions<T>(
            firstAttr.Name ?? $"idx_compound_{groupName}",
            firstAttr.Unique,
            firstAttr.Sparse,
            firstAttr.Background);

        return new CreateIndexModel<T>(keys, options);
    }

    private static CreateIndexModel<T>? BuildCompoundIndex<T>(MongoCompoundIndexAttribute attr)
    {
        if (string.IsNullOrEmpty(attr.Fields))
        {
            return null;
        }

        string[] fieldDefinitions = attr.Fields.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IndexKeysDefinition<T>? keys = null;

        foreach (string fieldDef in fieldDefinitions)
        {
            string[] parts = fieldDef.Trim().Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            string fieldName = parts[0].Trim();
            string direction = parts[1].Trim().ToLower();

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

        if (keys == null)
        {
            return null;
        }

        CreateIndexOptions<T> options = CreateIndexOptions<T>(attr.Name, attr.Unique, attr.Sparse, attr.Background);

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

    private static CreateIndexOptions<T> CreateIndexOptions<T>(string? name, bool unique, bool sparse, bool background)
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
        BsonElementAttribute? bsonElement = property.GetCustomAttribute<BsonElementAttribute>();
        return bsonElement?.ElementName ?? property.Name;
    }

    #endregion
}

/// <summary>
/// Attribute to specify a custom collection name for BSON mapping.
/// </summary>
/// <remarks>
/// Initializes a new instance with the specified collection name.
/// </remarks>
/// <param name="collectionName">The MongoDB collection name.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class BsonCollectionAttribute(string collectionName) : Attribute
{
    /// <summary>
    /// Gets or sets the collection name.
    /// </summary>
    public string CollectionName { get; } = collectionName;
}


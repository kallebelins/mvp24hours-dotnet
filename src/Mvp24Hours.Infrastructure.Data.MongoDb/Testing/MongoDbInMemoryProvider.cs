//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Testing;

/// <summary>
/// In-memory provider for MongoDB testing using an in-memory data store.
/// </summary>
/// <remarks>
/// <para>
/// This provider simulates MongoDB operations using in-memory collections,
/// eliminating the need for a real MongoDB instance during unit testing.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item>No MongoDB instance required</item>
/// <item>Fast test execution</item>
/// <item>Complete isolation between tests</item>
/// <item>Supports basic CRUD operations</item>
/// </list>
/// </para>
/// <para>
/// <strong>Limitations:</strong>
/// <list type="bullet">
/// <item>No aggregation pipeline support</item>
/// <item>No indexing behavior</item>
/// <item>No transaction support</item>
/// <item>Limited query operator support</item>
/// </list>
/// </para>
/// <para>
/// For more realistic testing, consider using <see cref="MongoDbTestcontainersHelper"/>
/// or Mongo2Go which provide actual MongoDB instances.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create provider
/// var provider = new MongoDbInMemoryProvider();
/// 
/// // Get a collection
/// var collection = provider.GetCollection&lt;Customer&gt;("customers");
/// 
/// // Add test data
/// await collection.InsertOneAsync(new Customer { Name = "Test" });
/// 
/// // Query data
/// var customer = await collection.Find(c => c.Name == "Test").FirstOrDefaultAsync();
/// 
/// // Cleanup
/// provider.Dispose();
/// </code>
/// </example>
public class MongoDbInMemoryProvider : IDisposable
{
    private readonly MongoDbInMemoryOptions _options;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _databases;
    private readonly string _currentDatabaseName;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance with default options.
    /// </summary>
    public MongoDbInMemoryProvider()
        : this(new MongoDbInMemoryOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified options.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    public MongoDbInMemoryProvider(MongoDbInMemoryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _databases = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>();
        _currentDatabaseName = options.GetEffectiveDatabaseName();

        TelemetryHelper.Execute(TelemetryLevels.Verbose,
            "mongodbinmemoryprovider-created",
            new { DatabaseName = _currentDatabaseName });
    }

    /// <summary>
    /// Gets the current database name.
    /// </summary>
    public string DatabaseName => _currentDatabaseName;

    /// <summary>
    /// Gets an in-memory collection for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>An in-memory collection wrapper.</returns>
    public InMemoryMongoCollection<TEntity> GetCollection<TEntity>()
        where TEntity : class
    {
        return GetCollection<TEntity>(typeof(TEntity).Name);
    }

    /// <summary>
    /// Gets an in-memory collection with the specified name.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="collectionName">The collection name.</param>
    /// <returns>An in-memory collection wrapper.</returns>
    public InMemoryMongoCollection<TEntity> GetCollection<TEntity>(string collectionName)
        where TEntity : class
    {
        var database = _databases.GetOrAdd(_currentDatabaseName,
            _ => new ConcurrentDictionary<string, object>());

        var collection = database.GetOrAdd(collectionName,
            _ => new InMemoryMongoCollection<TEntity>(collectionName));

        return (InMemoryMongoCollection<TEntity>)collection;
    }

    /// <summary>
    /// Drops a collection from the in-memory database.
    /// </summary>
    /// <param name="collectionName">The collection name to drop.</param>
    public void DropCollection(string collectionName)
    {
        if (_databases.TryGetValue(_currentDatabaseName, out var database))
        {
            database.TryRemove(collectionName, out _);
        }
    }

    /// <summary>
    /// Drops all collections from the current database.
    /// </summary>
    public void DropDatabase()
    {
        _databases.TryRemove(_currentDatabaseName, out _);
    }

    /// <summary>
    /// Gets all collection names in the current database.
    /// </summary>
    /// <returns>A list of collection names.</returns>
    public IEnumerable<string> GetCollectionNames()
    {
        if (_databases.TryGetValue(_currentDatabaseName, out var database))
        {
            return database.Keys;
        }
        return [];
    }

    /// <summary>
    /// Creates a Mvp24HoursContext configured to use this in-memory provider.
    /// </summary>
    /// <returns>A new MongoDB context.</returns>
    public Mvp24HoursContext CreateContext()
    {
        // For in-memory testing, we create a context with a mock connection
        // This requires a real MongoDB instance to be available
        // For true in-memory testing, use MongoDbRepositoryFake instead
        var options = new MongoDbOptions
        {
            DatabaseName = _currentDatabaseName,
            ConnectionString = _options.ConnectionString ?? "mongodb://localhost:27017",
            EnableTransaction = _options.EnableTransaction,
            EnableMultiTenancy = _options.EnableMultiTenancy
        };

        _options.ConfigureOptions?.Invoke(options);

        return new Mvp24HoursContext(options);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _databases.Clear();
            TelemetryHelper.Execute(TelemetryLevels.Verbose,
                "mongodbinmemoryprovider-disposed",
                new { DatabaseName = _currentDatabaseName });
        }

        _disposed = true;
    }
}

/// <summary>
/// In-memory collection that simulates MongoDB collection behavior.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This class provides a simple in-memory implementation of MongoDB collection
/// operations for unit testing without a real database.
/// </para>
/// </remarks>
public class InMemoryMongoCollection<TEntity>
    where TEntity : class
{
    private readonly ConcurrentDictionary<object, TEntity> _documents;
    private readonly string _collectionName;
    private readonly Func<TEntity, object> _keySelector;

    /// <summary>
    /// Initializes a new instance with the specified collection name.
    /// </summary>
    /// <param name="collectionName">The collection name.</param>
    public InMemoryMongoCollection(string collectionName)
        : this(collectionName, GetDefaultKeySelector())
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified collection name and key selector.
    /// </summary>
    /// <param name="collectionName">The collection name.</param>
    /// <param name="keySelector">Function to extract the document key.</param>
    public InMemoryMongoCollection(string collectionName, Func<TEntity, object> keySelector)
    {
        _collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        _documents = new ConcurrentDictionary<object, TEntity>();
    }

    /// <summary>
    /// Gets the collection name.
    /// </summary>
    public string CollectionName => _collectionName;

    /// <summary>
    /// Gets the count of documents in the collection.
    /// </summary>
    public long Count => _documents.Count;

    /// <summary>
    /// Gets all documents in the collection.
    /// </summary>
    public IEnumerable<TEntity> Documents => _documents.Values;

    /// <summary>
    /// Inserts a single document.
    /// </summary>
    /// <param name="document">The document to insert.</param>
    public void InsertOne(TEntity document)
    {
        ArgumentNullException.ThrowIfNull(document);

        EnsureIdSet(document);
        var key = _keySelector(document);
        if (!_documents.TryAdd(key, document))
        {
            throw new InvalidOperationException($"Document with key '{key}' already exists.");
        }
    }

    /// <summary>
    /// Inserts a single document asynchronously.
    /// </summary>
    /// <param name="document">The document to insert.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task InsertOneAsync(TEntity document, CancellationToken cancellationToken = default)
    {
        InsertOne(document);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Inserts multiple documents.
    /// </summary>
    /// <param name="documents">The documents to insert.</param>
    public void InsertMany(IEnumerable<TEntity> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        foreach (var document in documents)
        {
            InsertOne(document);
        }
    }

    /// <summary>
    /// Inserts multiple documents asynchronously.
    /// </summary>
    /// <param name="documents">The documents to insert.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task InsertManyAsync(IEnumerable<TEntity> documents, CancellationToken cancellationToken = default)
    {
        InsertMany(documents);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Replaces a single document.
    /// </summary>
    /// <param name="document">The replacement document.</param>
    /// <returns>True if the document was replaced; otherwise, false.</returns>
    public bool ReplaceOne(TEntity document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var key = _keySelector(document);
        if (_documents.ContainsKey(key))
        {
            _documents[key] = document;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Replaces a single document asynchronously.
    /// </summary>
    /// <param name="document">The replacement document.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the document was replaced; otherwise, false.</returns>
    public Task<bool> ReplaceOneAsync(TEntity document, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ReplaceOne(document));
    }

    /// <summary>
    /// Deletes a document by key.
    /// </summary>
    /// <param name="key">The document key.</param>
    /// <returns>True if the document was deleted; otherwise, false.</returns>
    public bool DeleteOne(object key)
    {
        return _documents.TryRemove(key, out _);
    }

    /// <summary>
    /// Deletes a document by key asynchronously.
    /// </summary>
    /// <param name="key">The document key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the document was deleted; otherwise, false.</returns>
    public Task<bool> DeleteOneAsync(object key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DeleteOne(key));
    }

    /// <summary>
    /// Deletes a document.
    /// </summary>
    /// <param name="document">The document to delete.</param>
    /// <returns>True if the document was deleted; otherwise, false.</returns>
    public bool DeleteOne(TEntity document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var key = _keySelector(document);
        return DeleteOne(key);
    }

    /// <summary>
    /// Finds a document by key.
    /// </summary>
    /// <param name="key">The document key.</param>
    /// <returns>The document if found; otherwise, null.</returns>
    public TEntity? FindById(object key)
    {
        _documents.TryGetValue(key, out var document);
        return document;
    }

    /// <summary>
    /// Finds a document by key asynchronously.
    /// </summary>
    /// <param name="key">The document key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The document if found; otherwise, null.</returns>
    public Task<TEntity?> FindByIdAsync(object key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(FindById(key));
    }

    /// <summary>
    /// Finds documents matching a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>A list of matching documents.</returns>
    public IList<TEntity> Find(Func<TEntity, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var results = new List<TEntity>();
        foreach (var document in _documents.Values)
        {
            if (predicate(document))
            {
                results.Add(document);
            }
        }
        return results;
    }

    /// <summary>
    /// Finds documents matching a predicate asynchronously.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of matching documents.</returns>
    public Task<IList<TEntity>> FindAsync(Func<TEntity, bool> predicate, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Find(predicate));
    }

    /// <summary>
    /// Gets all documents in the collection.
    /// </summary>
    /// <returns>A list of all documents.</returns>
    public IList<TEntity> FindAll()
    {
        return new List<TEntity>(_documents.Values);
    }

    /// <summary>
    /// Gets all documents in the collection asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of all documents.</returns>
    public Task<IList<TEntity>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(FindAll());
    }

    /// <summary>
    /// Checks if any documents match a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>True if any documents match; otherwise, false.</returns>
    public bool Any(Func<TEntity, bool>? predicate = null)
    {
        if (predicate == null)
        {
            return _documents.Count > 0;
        }

        foreach (var document in _documents.Values)
        {
            if (predicate(document))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Counts documents matching a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match. If null, counts all documents.</param>
    /// <returns>The count of matching documents.</returns>
    public long CountDocuments(Func<TEntity, bool>? predicate = null)
    {
        if (predicate == null)
        {
            return _documents.Count;
        }

        long count = 0;
        foreach (var document in _documents.Values)
        {
            if (predicate(document))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Clears all documents from the collection.
    /// </summary>
    public void Clear()
    {
        _documents.Clear();
    }

    /// <summary>
    /// Gets the default key selector for the entity type.
    /// </summary>
    private static Func<TEntity, object> GetDefaultKeySelector()
    {
        var idProperty = typeof(TEntity).GetProperty("Id")
            ?? typeof(TEntity).GetProperty("_id")
            ?? typeof(TEntity).GetProperty("EntityKey");

        if (idProperty != null)
        {
            return entity => idProperty.GetValue(entity) ?? throw new InvalidOperationException("Entity key cannot be null.");
        }

        throw new InvalidOperationException(
            $"Cannot determine key property for type {typeof(TEntity).Name}. " +
            "Either add an 'Id' or '_id' property, or provide a custom key selector.");
    }

    /// <summary>
    /// Ensures the ID property is set for the document.
    /// </summary>
    private static void EnsureIdSet(TEntity document)
    {
        var idProperty = typeof(TEntity).GetProperty("Id")
            ?? typeof(TEntity).GetProperty("_id");

        if (idProperty == null) return;

        var currentValue = idProperty.GetValue(document);

        // Auto-generate ObjectId if not set
        if (idProperty.PropertyType == typeof(ObjectId))
        {
            if (currentValue is ObjectId objectId && objectId == ObjectId.Empty)
            {
                idProperty.SetValue(document, ObjectId.GenerateNewId());
            }
        }
        else if (idProperty.PropertyType == typeof(Guid))
        {
            if (currentValue is Guid guid && guid == Guid.Empty)
            {
                idProperty.SetValue(document, Guid.NewGuid());
            }
        }
        else if (idProperty.PropertyType == typeof(string))
        {
            if (string.IsNullOrEmpty(currentValue as string))
            {
                idProperty.SetValue(document, ObjectId.GenerateNewId().ToString());
            }
        }
    }
}


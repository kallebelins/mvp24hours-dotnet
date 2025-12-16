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
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.CappedCollections
{
    /// <summary>
    /// Service for MongoDB Capped Collection operations.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <example>
    /// <code>
    /// // Create a capped collection for logs
    /// await cappedService.CreateCappedCollectionAsync("logs", new CappedCollectionOptions
    /// {
    ///     MaxSizeBytes = 100 * 1024 * 1024, // 100 MB
    ///     MaxDocuments = 1000000 // Max 1 million documents
    /// });
    /// 
    /// // Insert log entries
    /// await cappedService.InsertAsync(new LogEntry
    /// {
    ///     Timestamp = DateTime.UtcNow,
    ///     Level = "INFO",
    ///     Message = "Application started"
    /// });
    /// 
    /// // Get latest logs
    /// var recentLogs = await cappedService.GetLatestAsync(100);
    /// 
    /// // Tail for real-time logs
    /// await cappedService.TailAsync(async log =>
    /// {
    ///     Console.WriteLine($"[{log.Level}] {log.Message}");
    /// }, cancellationToken);
    /// </code>
    /// </example>
    public class MongoDbCappedCollectionService<TDocument> : IMongoDbCappedCollectionService<TDocument>
    {
        private readonly IMongoDatabase _database;
        private readonly string _collectionName;
        private readonly ILogger<MongoDbCappedCollectionService<TDocument>> _logger;
        private IMongoCollection<TDocument> _collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbCappedCollectionService{TDocument}"/> class.
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="logger">Optional logger.</param>
        public MongoDbCappedCollectionService(
            IMongoDatabase database,
            string collectionName,
            ILogger<MongoDbCappedCollectionService<TDocument>> logger = null)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
            _logger = logger;
            _collection = database.GetCollection<TDocument>(collectionName);
        }

        /// <inheritdoc/>
        public IMongoCollection<TDocument> Collection => _collection;

        /// <inheritdoc/>
        public async Task CreateCappedCollectionAsync(
            string collectionName,
            CappedCollectionOptions options,
            CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.MaxSizeBytes <= 0)
            {
                throw new ArgumentException("MaxSizeBytes must be greater than 0.", nameof(options));
            }

            var createOptions = new CreateCollectionOptions
            {
                Capped = true,
                MaxSize = options.MaxSizeBytes,
                MaxDocuments = options.MaxDocuments,
                AutoIndexId = options.AutoIndexId
            };

            await _database.CreateCollectionAsync(collectionName, createOptions, cancellationToken);

            _collection = _database.GetCollection<TDocument>(collectionName);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-capped-created",
                new { Collection = collectionName, MaxSize = options.MaxSizeBytes, MaxDocs = options.MaxDocuments });

            _logger?.LogInformation(
                "Capped collection '{CollectionName}' created with max size {MaxSize} bytes and max {MaxDocs} documents.",
                collectionName, options.MaxSizeBytes, options.MaxDocuments);
        }

        /// <inheritdoc/>
        public async Task ConvertToCappedCollectionAsync(
            string collectionName,
            CappedCollectionOptions options,
            CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var command = new BsonDocument
            {
                { "convertToCapped", collectionName },
                { "size", options.MaxSizeBytes }
            };

            if (options.MaxDocuments.HasValue)
            {
                command.Add("max", options.MaxDocuments.Value);
            }

            await _database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);

            _collection = _database.GetCollection<TDocument>(collectionName);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-capped-converted",
                new { Collection = collectionName, MaxSize = options.MaxSizeBytes });

            _logger?.LogInformation("Collection '{CollectionName}' converted to capped.", collectionName);
        }

        /// <inheritdoc/>
        public async Task InsertAsync(
            TDocument document,
            CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task InsertManyAsync(
            IEnumerable<TDocument> documents,
            CancellationToken cancellationToken = default)
        {
            await _collection.InsertManyAsync(documents, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<TDocument>> GetLatestAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            // Use natural order descending to get most recent documents
            var result = await _collection
                .Find(FilterDefinition<TDocument>.Empty)
                .Sort(new BsonDocument("$natural", -1))
                .Limit(count)
                .ToListAsync(cancellationToken);

            return result;
        }

        /// <inheritdoc/>
        public async Task<IList<TDocument>> GetOldestAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            // Use natural order ascending to get oldest documents
            var result = await _collection
                .Find(FilterDefinition<TDocument>.Empty)
                .Sort(new BsonDocument("$natural", 1))
                .Limit(count)
                .ToListAsync(cancellationToken);

            return result;
        }

        /// <inheritdoc/>
        public async Task TailAsync(
            Func<TDocument, Task> handler,
            CancellationToken cancellationToken = default)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var options = new FindOptions<TDocument>
            {
                CursorType = CursorType.TailableAwait,
                NoCursorTimeout = true
            };

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-capped-tail-started",
                new { Collection = _collectionName });

            _logger?.LogInformation("Started tailing capped collection '{CollectionName}'.", _collectionName);

            using var cursor = await _collection.FindAsync(
                FilterDefinition<TDocument>.Empty,
                options,
                cancellationToken);

            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (var document in cursor.Current)
                {
                    try
                    {
                        await handler(document);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error processing document from tailable cursor.");
                        TelemetryHelper.Execute(TelemetryLevels.Error, "mongodb-capped-tail-error",
                            new { Error = ex.Message });
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task TailFromAsync(
            BsonValue lastId,
            Func<TDocument, Task> handler,
            CancellationToken cancellationToken = default)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var filter = Builders<TDocument>.Filter.Gt("_id", lastId);

            var options = new FindOptions<TDocument>
            {
                CursorType = CursorType.TailableAwait,
                NoCursorTimeout = true
            };

            _logger?.LogInformation("Started tailing capped collection '{CollectionName}' from ID {LastId}.",
                _collectionName, lastId);

            using var cursor = await _collection.FindAsync(filter, options, cancellationToken);

            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (var document in cursor.Current)
                {
                    try
                    {
                        await handler(document);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error processing document from tailable cursor.");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsCappedAsync(CancellationToken cancellationToken = default)
        {
            var stats = await GetStatsAsync(cancellationToken);
            return stats.IsCapped;
        }

        /// <inheritdoc/>
        public async Task<CappedCollectionStats> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            var command = new BsonDocument("collStats", _collectionName);
            var result = await _database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);

            return new CappedCollectionStats
            {
                IsCapped = result.Contains("capped") && result["capped"].AsBoolean,
                MaxSizeBytes = result.Contains("maxSize") ? result["maxSize"].ToInt64() : 0,
                MaxDocuments = result.Contains("max") ? result["max"].ToInt64() : null,
                CurrentSizeBytes = result.Contains("size") ? result["size"].ToInt64() : 0,
                DocumentCount = result.Contains("count") ? result["count"].ToInt64() : 0,
                StorageSizeBytes = result.Contains("storageSize") ? result["storageSize"].ToInt64() : 0
            };
        }

        /// <inheritdoc/>
        public async Task<IList<TDocument>> GetAllInNaturalOrderAsync(
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(FilterDefinition<TDocument>.Empty)
                .Sort(new BsonDocument("$natural", 1))
                .ToListAsync(cancellationToken);
        }
    }
}


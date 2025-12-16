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

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.ChangeStreams
{
    /// <summary>
    /// Service for MongoDB Change Streams to receive real-time notifications of data changes.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <remarks>
    /// <para>
    /// Change Streams allow applications to access real-time data changes. Example use cases:
    /// <list type="bullet">
    ///   <item>Real-time notifications to users</item>
    ///   <item>Triggering workflows on data changes</item>
    ///   <item>Keeping caches in sync</item>
    ///   <item>Event-driven microservices</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Watch all changes
    /// await changeStreamService.WatchCollectionAsync(async change =>
    /// {
    ///     Console.WriteLine($"Operation: {change.OperationType}");
    ///     if (change.FullDocument != null)
    ///     {
    ///         Console.WriteLine($"Document: {change.FullDocument}");
    ///     }
    /// });
    /// 
    /// // Watch only inserts
    /// await changeStreamService.WatchInsertsAsync(async document =>
    /// {
    ///     Console.WriteLine($"New document inserted: {document}");
    /// });
    /// </code>
    /// </example>
    public class MongoDbChangeStreamService<TDocument> : IMongoDbChangeStreamService<TDocument>
    {
        private readonly IMongoCollection<TDocument> _collection;
        private readonly ILogger<MongoDbChangeStreamService<TDocument>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbChangeStreamService{TDocument}"/> class.
        /// </summary>
        /// <param name="collection">The MongoDB collection to watch.</param>
        /// <param name="logger">Optional logger.</param>
        public MongoDbChangeStreamService(
            IMongoCollection<TDocument> collection,
            ILogger<MongoDbChangeStreamService<TDocument>> logger = null)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task WatchCollectionAsync(
            Func<ChangeStreamDocument<TDocument>, Task> handler,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            options ??= CreateDefaultOptions();

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-changestream-started",
                new { Collection = _collection.CollectionNamespace.CollectionName });

            _logger?.LogInformation("Started watching collection '{CollectionName}' for changes.",
                _collection.CollectionNamespace.CollectionName);

            using var cursor = await _collection.WatchAsync(options, cancellationToken);

            await ProcessCursorAsync(cursor, handler, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task WatchCollectionAsync(
            Func<ChangeStreamDocument<TDocument>, Task> handler,
            PipelineDefinition<ChangeStreamDocument<TDocument>, ChangeStreamDocument<TDocument>> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (pipeline == null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

            options ??= CreateDefaultOptions();

            using var cursor = await _collection.WatchAsync(pipeline, options, cancellationToken);

            await ProcessCursorAsync(cursor, handler, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task WatchCollectionAsync(
            Func<ChangeStreamDocument<TDocument>, Task> handler,
            IEnumerable<ChangeStreamOperationType> operationTypes,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (operationTypes == null || !operationTypes.Any())
            {
                throw new ArgumentException("At least one operation type must be specified.", nameof(operationTypes));
            }

            var operationTypeStrings = operationTypes.Select(GetOperationTypeString).ToList();
            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<TDocument>>()
                .Match(change => operationTypeStrings.Contains(change.OperationType.ToString().ToLower()));

            await WatchCollectionAsync(handler, pipeline, options, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IChangeStreamCursor<ChangeStreamDocument<TDocument>>> GetChangeStreamCursorAsync(
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= CreateDefaultOptions();
            return await _collection.WatchAsync(options, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IChangeStreamCursor<ChangeStreamDocument<TDocument>>> GetChangeStreamCursorAsync(
            PipelineDefinition<ChangeStreamDocument<TDocument>, ChangeStreamDocument<TDocument>> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= CreateDefaultOptions();
            return await _collection.WatchAsync(pipeline, options, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task ResumeWatchingAsync(
            BsonDocument resumeToken,
            Func<ChangeStreamDocument<TDocument>, Task> handler,
            CancellationToken cancellationToken = default)
        {
            if (resumeToken == null)
            {
                throw new ArgumentNullException(nameof(resumeToken));
            }

            var options = new ChangeStreamOptions
            {
                ResumeAfter = resumeToken,
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
            };

            _logger?.LogInformation("Resuming change stream from token for collection '{CollectionName}'.",
                _collection.CollectionNamespace.CollectionName);

            await WatchCollectionAsync(handler, options, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task WatchInsertsAsync(
            Func<TDocument, Task> handler,
            CancellationToken cancellationToken = default)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<TDocument>>()
                .Match(change => change.OperationType == ChangeStreamOperationType.Insert);

            await WatchCollectionAsync(async change =>
            {
                if (change.FullDocument != null)
                {
                    await handler(change.FullDocument);
                }
            }, pipeline, null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task WatchUpdatesAsync(
            Func<ChangeStreamDocument<TDocument>, Task> handler,
            bool includeFullDocument = true,
            CancellationToken cancellationToken = default)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var options = new ChangeStreamOptions
            {
                FullDocument = includeFullDocument
                    ? ChangeStreamFullDocumentOption.UpdateLookup
                    : ChangeStreamFullDocumentOption.Default
            };

            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<TDocument>>()
                .Match(change =>
                    change.OperationType == ChangeStreamOperationType.Update ||
                    change.OperationType == ChangeStreamOperationType.Replace);

            await WatchCollectionAsync(handler, pipeline, options, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task WatchDeletesAsync(
            Func<BsonValue, Task> handler,
            CancellationToken cancellationToken = default)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<TDocument>>()
                .Match(change => change.OperationType == ChangeStreamOperationType.Delete);

            await WatchCollectionAsync(async change =>
            {
                await handler(change.DocumentKey["_id"]);
            }, pipeline, null, cancellationToken);
        }

        private async Task ProcessCursorAsync(
            IChangeStreamCursor<ChangeStreamDocument<TDocument>> cursor,
            Func<ChangeStreamDocument<TDocument>, Task> handler,
            CancellationToken cancellationToken)
        {
            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (var change in cursor.Current)
                {
                    try
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-changestream-event",
                            new
                            {
                                Operation = change.OperationType.ToString(),
                                Collection = _collection.CollectionNamespace.CollectionName
                            });

                        await handler(change);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error processing change stream event. Operation: {OperationType}",
                            change.OperationType);

                        TelemetryHelper.Execute(TelemetryLevels.Error, "mongodb-changestream-error",
                            new { Operation = change.OperationType.ToString(), Error = ex.Message });
                    }
                }
            }
        }

        private static ChangeStreamOptions CreateDefaultOptions()
        {
            return new ChangeStreamOptions
            {
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                MaxAwaitTime = TimeSpan.FromSeconds(30)
            };
        }

        private static string GetOperationTypeString(ChangeStreamOperationType operationType)
        {
            return operationType switch
            {
                ChangeStreamOperationType.Insert => "insert",
                ChangeStreamOperationType.Update => "update",
                ChangeStreamOperationType.Replace => "replace",
                ChangeStreamOperationType.Delete => "delete",
                ChangeStreamOperationType.Invalidate => "invalidate",
                ChangeStreamOperationType.Rename => "rename",
                ChangeStreamOperationType.Drop => "drop",
                ChangeStreamOperationType.DropDatabase => "dropDatabase",
                _ => operationType.ToString().ToLower()
            };
        }
    }
}


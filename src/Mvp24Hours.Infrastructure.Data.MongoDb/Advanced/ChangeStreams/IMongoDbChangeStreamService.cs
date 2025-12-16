//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.ChangeStreams
{
    /// <summary>
    /// Interface for MongoDB Change Streams to receive real-time notifications of data changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Change Streams allow applications to access real-time data changes without the complexity
    /// and risk of tailing the oplog. This service provides:
    /// <list type="bullet">
    ///   <item>Real-time change notifications at collection, database, or deployment level</item>
    ///   <item>Filtering changes by operation type (insert, update, delete, replace)</item>
    ///   <item>Resume token support for resuming from last processed change</item>
    ///   <item>Full document lookup for update operations</item>
    /// </list>
    /// </para>
    /// <para>
    /// Change Streams require a replica set or sharded cluster. They are not available on standalone deployments.
    /// </para>
    /// </remarks>
    public interface IMongoDbChangeStreamService<TDocument>
    {
        /// <summary>
        /// Watches a collection for changes and invokes a handler for each change.
        /// </summary>
        /// <param name="handler">The handler to invoke for each change.</param>
        /// <param name="options">Optional change stream options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the watch is cancelled or an error occurs.</returns>
        Task WatchCollectionAsync(
            Func<ChangeStreamDocument<TDocument>, Task> handler,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Watches a collection for changes with a pipeline filter.
        /// </summary>
        /// <param name="handler">The handler to invoke for each change.</param>
        /// <param name="pipeline">The aggregation pipeline to filter changes.</param>
        /// <param name="options">Optional change stream options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task WatchCollectionAsync(
            Func<ChangeStreamDocument<TDocument>, Task> handler,
            PipelineDefinition<ChangeStreamDocument<TDocument>, ChangeStreamDocument<TDocument>> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Watches a collection for specific operation types.
        /// </summary>
        /// <param name="handler">The handler to invoke for each change.</param>
        /// <param name="operationTypes">The operation types to watch.</param>
        /// <param name="options">Optional change stream options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task WatchCollectionAsync(
            Func<ChangeStreamDocument<TDocument>, Task> handler,
            IEnumerable<ChangeStreamOperationType> operationTypes,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a change stream cursor for manual iteration.
        /// </summary>
        /// <param name="options">Optional change stream options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A change stream cursor.</returns>
        Task<IChangeStreamCursor<ChangeStreamDocument<TDocument>>> GetChangeStreamCursorAsync(
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a change stream cursor with a pipeline filter.
        /// </summary>
        /// <param name="pipeline">The aggregation pipeline to filter changes.</param>
        /// <param name="options">Optional change stream options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A change stream cursor.</returns>
        Task<IChangeStreamCursor<ChangeStreamDocument<TDocument>>> GetChangeStreamCursorAsync(
            PipelineDefinition<ChangeStreamDocument<TDocument>, ChangeStreamDocument<TDocument>> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resumes watching a collection from a specific resume token.
        /// </summary>
        /// <param name="resumeToken">The resume token from a previous change stream.</param>
        /// <param name="handler">The handler to invoke for each change.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ResumeWatchingAsync(
            BsonDocument resumeToken,
            Func<ChangeStreamDocument<TDocument>, Task> handler,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Watches for insert operations only.
        /// </summary>
        /// <param name="handler">The handler to invoke for each insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task WatchInsertsAsync(
            Func<TDocument, Task> handler,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Watches for update operations only.
        /// </summary>
        /// <param name="handler">The handler to invoke for each update.</param>
        /// <param name="includeFullDocument">Whether to include the full document in updates.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task WatchUpdatesAsync(
            Func<ChangeStreamDocument<TDocument>, Task> handler,
            bool includeFullDocument = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Watches for delete operations only.
        /// </summary>
        /// <param name="handler">The handler to invoke for each delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task WatchDeletesAsync(
            Func<BsonValue, Task> handler,
            CancellationToken cancellationToken = default);
    }
}


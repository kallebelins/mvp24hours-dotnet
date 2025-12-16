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

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.CappedCollections
{
    /// <summary>
    /// Interface for MongoDB Capped Collection operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Capped collections provide:
    /// <list type="bullet">
    ///   <item>Fixed size storage with automatic oldest document removal</item>
    ///   <item>High-throughput sequential inserts</item>
    ///   <item>Tailable cursors for real-time streaming</item>
    ///   <item>Natural ordering by insertion time</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IMongoDbCappedCollectionService<TDocument>
    {
        /// <summary>
        /// Gets the capped collection.
        /// </summary>
        IMongoCollection<TDocument> Collection { get; }

        /// <summary>
        /// Creates a capped collection.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="options">Capped collection options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task CreateCappedCollectionAsync(
            string collectionName,
            CappedCollectionOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts an existing collection to a capped collection.
        /// </summary>
        /// <param name="collectionName">The name of the collection to convert.</param>
        /// <param name="options">Capped collection options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ConvertToCappedCollectionAsync(
            string collectionName,
            CappedCollectionOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts a document into the capped collection.
        /// </summary>
        /// <param name="document">The document to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InsertAsync(
            TDocument document,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts multiple documents into the capped collection.
        /// </summary>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InsertManyAsync(
            IEnumerable<TDocument> documents,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the most recent documents.
        /// </summary>
        /// <param name="count">Number of documents to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The most recent documents in reverse insertion order.</returns>
        Task<IList<TDocument>> GetLatestAsync(
            int count,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the oldest documents.
        /// </summary>
        /// <param name="count">Number of documents to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The oldest documents in insertion order.</returns>
        Task<IList<TDocument>> GetOldestAsync(
            int count,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a tailable cursor for streaming new documents.
        /// </summary>
        /// <param name="handler">Handler for each new document.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the cursor is closed.</returns>
        /// <remarks>
        /// Tailable cursors remain open after returning the last document,
        /// waiting for new documents to be inserted. This is ideal for
        /// real-time streaming applications.
        /// </remarks>
        Task TailAsync(
            Func<TDocument, Task> handler,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a tailable cursor starting after a specific document.
        /// </summary>
        /// <param name="lastId">The ID of the last processed document.</param>
        /// <param name="handler">Handler for each new document.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task TailFromAsync(
            BsonValue lastId,
            Func<TDocument, Task> handler,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the collection is capped.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the collection is capped.</returns>
        Task<bool> IsCappedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets collection statistics including capped collection info.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection statistics.</returns>
        Task<CappedCollectionStats> GetStatsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all documents in natural (insertion) order.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>All documents in insertion order.</returns>
        Task<IList<TDocument>> GetAllInNaturalOrderAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Statistics for a capped collection.
    /// </summary>
    public class CappedCollectionStats
    {
        /// <summary>
        /// Gets or sets whether the collection is capped.
        /// </summary>
        public bool IsCapped { get; set; }

        /// <summary>
        /// Gets or sets the maximum size in bytes.
        /// </summary>
        public long MaxSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of documents.
        /// </summary>
        public long? MaxDocuments { get; set; }

        /// <summary>
        /// Gets or sets the current size in bytes.
        /// </summary>
        public long CurrentSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the current document count.
        /// </summary>
        public long DocumentCount { get; set; }

        /// <summary>
        /// Gets or sets the storage size in bytes.
        /// </summary>
        public long StorageSizeBytes { get; set; }

        /// <summary>
        /// Gets the percentage of capacity used.
        /// </summary>
        public double CapacityUsedPercent => MaxSizeBytes > 0
            ? (double)CurrentSizeBytes / MaxSizeBytes * 100
            : 0;
    }
}


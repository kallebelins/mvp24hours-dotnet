//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Indexes
{
    /// <summary>
    /// Interface for managing MongoDB indexes based on attribute definitions.
    /// </summary>
    public interface IMongoDbIndexManager
    {
        /// <summary>
        /// Ensures that all indexes defined via attributes are created for the specified collection.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EnsureIndexesAsync<T>(
            IMongoCollection<T> collection,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Scans an assembly for entity types with index attributes and creates indexes for all of them.
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        /// <param name="assembly">The assembly to scan for entity types.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EnsureAllIndexesAsync(
            IMongoDatabase database,
            Assembly assembly,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Builds index models from attribute definitions without creating them.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>A read-only list of index models.</returns>
        IReadOnlyList<CreateIndexModel<T>> BuildIndexModels<T>();

        /// <summary>
        /// Gets the existing indexes on a collection.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of BSON documents describing existing indexes.</returns>
        Task<IEnumerable<BsonDocument>> GetExistingIndexesAsync<T>(
            IMongoCollection<T> collection,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Drops a specific index from a collection.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="indexName">The name of the index to drop.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DropIndexAsync<T>(
            IMongoCollection<T> collection,
            string indexName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets the internal cache of created indexes, allowing indexes to be recreated.
        /// </summary>
        void ResetIndexCache();
    }
}


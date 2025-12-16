//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Concerns
{
    /// <summary>
    /// Extension methods for MongoDB operations with custom read/write concerns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use these extensions when you need fine-grained control over consistency
    /// and durability for specific operations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // High durability write
    /// await collection.InsertWithConcernAsync(document, ConcernPresets.MaxDurability);
    /// 
    /// // Analytics query from secondaries
    /// var reports = await collection.FindWithConcernAsync(
    ///     filter,
    ///     ConcernPresets.Analytics);
    /// 
    /// // Fire-and-forget for non-critical data
    /// await collection.InsertWithConcernAsync(logEntry, ConcernPresets.FireAndForget);
    /// </code>
    /// </example>
    public static class MongoDbConcernExtensions
    {
        /// <summary>
        /// Inserts a document with custom write concern.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="document">The document to insert.</param>
        /// <param name="options">The concern options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task InsertWithConcernAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            TDocument document,
            MongoDbConcernOptions options,
            CancellationToken cancellationToken = default)
        {
            var collectionWithConcern = collection.WithWriteConcern(options.ToWriteConcern());
            await collectionWithConcern.InsertOneAsync(document, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Inserts multiple documents with custom write concern.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="options">The concern options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task InsertManyWithConcernAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IEnumerable<TDocument> documents,
            MongoDbConcernOptions options,
            CancellationToken cancellationToken = default)
        {
            var collectionWithConcern = collection.WithWriteConcern(options.ToWriteConcern());
            await collectionWithConcern.InsertManyAsync(documents, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Updates a document with custom write concern.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="update">The update definition.</param>
        /// <param name="options">The concern options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The update result.</returns>
        public static async Task<UpdateResult> UpdateWithConcernAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            FilterDefinition<TDocument> filter,
            UpdateDefinition<TDocument> update,
            MongoDbConcernOptions options,
            CancellationToken cancellationToken = default)
        {
            var collectionWithConcern = collection.WithWriteConcern(options.ToWriteConcern());
            return await collectionWithConcern.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes documents with custom write concern.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The concern options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The delete result.</returns>
        public static async Task<DeleteResult> DeleteWithConcernAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            FilterDefinition<TDocument> filter,
            MongoDbConcernOptions options,
            CancellationToken cancellationToken = default)
        {
            var collectionWithConcern = collection.WithWriteConcern(options.ToWriteConcern());
            return await collectionWithConcern.DeleteManyAsync(filter, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Finds documents with custom read concern and preference.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The concern options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of matching documents.</returns>
        public static async Task<IList<TDocument>> FindWithConcernAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            FilterDefinition<TDocument> filter,
            MongoDbConcernOptions options,
            CancellationToken cancellationToken = default)
        {
            var collectionWithConcern = collection
                .WithReadConcern(options.ToReadConcern())
                .WithReadPreference(options.ToReadPreference());

            return await collectionWithConcern.Find(filter).ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets a collection configured with the specified concerns.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="options">The concern options.</param>
        /// <returns>The collection with configured concerns.</returns>
        public static IMongoCollection<TDocument> WithConcerns<TDocument>(
            this IMongoCollection<TDocument> collection,
            MongoDbConcernOptions options)
        {
            return collection
                .WithReadConcern(options.ToReadConcern())
                .WithWriteConcern(options.ToWriteConcern())
                .WithReadPreference(options.ToReadPreference());
        }

        /// <summary>
        /// Counts documents with custom read concern and preference.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The concern options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The count of matching documents.</returns>
        public static async Task<long> CountWithConcernAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            FilterDefinition<TDocument> filter,
            MongoDbConcernOptions options,
            CancellationToken cancellationToken = default)
        {
            var collectionWithConcern = collection
                .WithReadConcern(options.ToReadConcern())
                .WithReadPreference(options.ToReadPreference());

            return await collectionWithConcern.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Aggregates documents with custom read concern and preference.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="pipeline">The aggregation pipeline.</param>
        /// <param name="options">The concern options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The aggregation results.</returns>
        public static async Task<IList<TResult>> AggregateWithConcernAsync<TDocument, TResult>(
            this IMongoCollection<TDocument> collection,
            PipelineDefinition<TDocument, TResult> pipeline,
            MongoDbConcernOptions options,
            CancellationToken cancellationToken = default)
        {
            var collectionWithConcern = collection
                .WithReadConcern(options.ToReadConcern())
                .WithReadPreference(options.ToReadPreference());

            return await collectionWithConcern.Aggregate(pipeline).ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Replaces a document with custom write concern.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="replacement">The replacement document.</param>
        /// <param name="options">The concern options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The replace result.</returns>
        public static async Task<ReplaceOneResult> ReplaceWithConcernAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            FilterDefinition<TDocument> filter,
            TDocument replacement,
            MongoDbConcernOptions options,
            CancellationToken cancellationToken = default)
        {
            var collectionWithConcern = collection.WithWriteConcern(options.ToWriteConcern());
            return await collectionWithConcern.ReplaceOneAsync(filter, replacement, cancellationToken: cancellationToken);
        }
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Collation
{
    /// <summary>
    /// Extension methods for MongoDB operations with collation support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Collation allows for language-specific string comparison. Use these extensions
    /// when you need locale-aware sorting or case-insensitive operations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Case-insensitive search
    /// var results = await collection.FindWithCollationAsync(
    ///     x => x.Name,
    ///     "john",
    ///     CollationPresets.EnglishCaseInsensitive);
    /// 
    /// // Sort with numeric ordering
    /// var sorted = await collection.SortWithCollationAsync(
    ///     "version",
    ///     ascending: true,
    ///     CollationPresets.NumericOrdered);
    /// 
    /// // Portuguese locale sorting
    /// var names = await collection.SortWithCollationAsync(
    ///     "nome",
    ///     ascending: true,
    ///     CollationPresets.PortugueseCaseInsensitive);
    /// </code>
    /// </example>
    public static class MongoDbCollationExtensions
    {
        /// <summary>
        /// Finds documents using a collation-aware filter.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter definition.</param>
        /// <param name="collationOptions">The collation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of matching documents.</returns>
        public static async Task<IList<TDocument>> FindWithCollationAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            FilterDefinition<TDocument> filter,
            MongoDbCollationOptions collationOptions,
            CancellationToken cancellationToken = default)
        {
            var findOptions = new FindOptions
            {
                Collation = collationOptions.ToCollation()
            };

            return await collection.Find(filter, findOptions).ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Finds documents with a case-insensitive text filter.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="fieldName">The field name to search.</param>
        /// <param name="value">The value to search for.</param>
        /// <param name="collationOptions">The collation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of matching documents.</returns>
        public static async Task<IList<TDocument>> FindWithCollationAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string fieldName,
            string value,
            MongoDbCollationOptions collationOptions,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<TDocument>.Filter.Eq(fieldName, value);
            return await FindWithCollationAsync(collection, filter, collationOptions, cancellationToken);
        }

        /// <summary>
        /// Sorts documents using collation rules.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="sortField">The field to sort by.</param>
        /// <param name="ascending">Sort direction.</param>
        /// <param name="collationOptions">The collation options.</param>
        /// <param name="limit">Maximum number of results.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A sorted list of documents.</returns>
        public static async Task<IList<TDocument>> SortWithCollationAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string sortField,
            bool ascending,
            MongoDbCollationOptions collationOptions,
            int? limit = null,
            CancellationToken cancellationToken = default)
        {
            var sort = ascending
                ? Builders<TDocument>.Sort.Ascending(sortField)
                : Builders<TDocument>.Sort.Descending(sortField);

            var findOptions = new FindOptions
            {
                Collation = collationOptions.ToCollation()
            };

            var findFluent = collection.Find(FilterDefinition<TDocument>.Empty, findOptions)
                .Sort(sort);

            if (limit.HasValue)
            {
                findFluent = findFluent.Limit(limit.Value);
            }

            return await findFluent.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Counts documents with collation-aware filter.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter definition.</param>
        /// <param name="collationOptions">The collation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The count of matching documents.</returns>
        public static async Task<long> CountWithCollationAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            FilterDefinition<TDocument> filter,
            MongoDbCollationOptions collationOptions,
            CancellationToken cancellationToken = default)
        {
            var options = new CountOptions
            {
                Collation = collationOptions.ToCollation()
            };

            return await collection.CountDocumentsAsync(filter, options, cancellationToken);
        }

        /// <summary>
        /// Updates documents with collation-aware filter.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter definition.</param>
        /// <param name="update">The update definition.</param>
        /// <param name="collationOptions">The collation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The update result.</returns>
        public static async Task<UpdateResult> UpdateWithCollationAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            FilterDefinition<TDocument> filter,
            UpdateDefinition<TDocument> update,
            MongoDbCollationOptions collationOptions,
            CancellationToken cancellationToken = default)
        {
            var options = new UpdateOptions
            {
                Collation = collationOptions.ToCollation()
            };

            return await collection.UpdateManyAsync(filter, update, options, cancellationToken);
        }

        /// <summary>
        /// Deletes documents with collation-aware filter.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter definition.</param>
        /// <param name="collationOptions">The collation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The delete result.</returns>
        public static async Task<DeleteResult> DeleteWithCollationAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            FilterDefinition<TDocument> filter,
            MongoDbCollationOptions collationOptions,
            CancellationToken cancellationToken = default)
        {
            var options = new DeleteOptions
            {
                Collation = collationOptions.ToCollation()
            };

            return await collection.DeleteManyAsync(filter, options, cancellationToken);
        }

        /// <summary>
        /// Creates an index with collation.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="field">The field to index.</param>
        /// <param name="collationOptions">The collation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The name of the created index.</returns>
        public static async Task<string> CreateIndexWithCollationAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string field,
            MongoDbCollationOptions collationOptions,
            CancellationToken cancellationToken = default)
        {
            var indexKeys = Builders<TDocument>.IndexKeys.Ascending(field);
            var indexOptions = new CreateIndexOptions
            {
                Collation = collationOptions.ToCollation()
            };

            return await collection.Indexes.CreateOneAsync(
                new CreateIndexModel<TDocument>(indexKeys, indexOptions),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Aggregates documents with collation support.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="pipeline">The aggregation pipeline.</param>
        /// <param name="collationOptions">The collation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The aggregation results.</returns>
        public static async Task<IList<TResult>> AggregateWithCollationAsync<TDocument, TResult>(
            this IMongoCollection<TDocument> collection,
            PipelineDefinition<TDocument, TResult> pipeline,
            MongoDbCollationOptions collationOptions,
            CancellationToken cancellationToken = default)
        {
            var options = new AggregateOptions
            {
                Collation = collationOptions.ToCollation()
            };

            return await collection.Aggregate(pipeline, options).ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets distinct values with collation support.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <typeparam name="TField">The field type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="field">The field to get distinct values from.</param>
        /// <param name="collationOptions">The collation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of distinct values.</returns>
        public static async Task<IList<TField>> DistinctWithCollationAsync<TDocument, TField>(
            this IMongoCollection<TDocument> collection,
            FieldDefinition<TDocument, TField> field,
            MongoDbCollationOptions collationOptions,
            CancellationToken cancellationToken = default)
        {
            var options = new DistinctOptions
            {
                Collation = collationOptions.ToCollation()
            };

            return await collection.Distinct(field, FilterDefinition<TDocument>.Empty, options)
                .ToListAsync(cancellationToken);
        }
    }
}


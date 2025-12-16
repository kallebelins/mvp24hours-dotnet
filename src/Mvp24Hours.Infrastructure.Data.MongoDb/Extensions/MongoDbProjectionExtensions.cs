//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Projections;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for MongoDB projection operations.
    /// </summary>
    public static class MongoDbProjectionExtensions
    {
        /// <summary>
        /// Creates an include projection for the specified fields.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="fields">The fields to include.</param>
        /// <returns>A projection definition.</returns>
        /// <example>
        /// <code>
        /// var projection = collection.ProjectInclude(c => c.Name, c => c.Email);
        /// var results = await collection.Find(filter).Project(projection).ToListAsync();
        /// </code>
        /// </example>
        public static ProjectionDefinition<T> ProjectInclude<T>(
            this IMongoCollection<T> collection,
            params Expression<Func<T, object>>[] fields)
        {
            return MongoDbProjection<T>.Include(fields);
        }

        /// <summary>
        /// Creates an exclude projection for the specified fields.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="fields">The fields to exclude.</param>
        /// <returns>A projection definition.</returns>
        public static ProjectionDefinition<T> ProjectExclude<T>(
            this IMongoCollection<T> collection,
            params Expression<Func<T, object>>[] fields)
        {
            return MongoDbProjection<T>.Exclude(fields);
        }

        /// <summary>
        /// Finds documents and projects them to the specified type.
        /// </summary>
        /// <typeparam name="T">The source document type.</typeparam>
        /// <typeparam name="TProjection">The projection result type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="filter">The filter expression.</param>
        /// <param name="projection">The projection expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of projected documents.</returns>
        /// <example>
        /// <code>
        /// var dtos = await collection.FindProjectedAsync(
        ///     c => c.IsActive,
        ///     c => new CustomerDto { Name = c.Name, Email = c.Email });
        /// </code>
        /// </example>
        public static async Task<List<TProjection>> FindProjectedAsync<T, TProjection>(
            this IMongoCollection<T> collection,
            Expression<Func<T, bool>> filter,
            Expression<Func<T, TProjection>> projection,
            CancellationToken cancellationToken = default)
        {
            var filterDef = Builders<T>.Filter.Where(filter);
            var projectionDef = Builders<T>.Projection.Expression(projection);

            var cursor = await collection.FindAsync(filterDef, new FindOptions<T, TProjection>
            {
                Projection = projectionDef
            }, cancellationToken);

            return await cursor.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Finds a single document and projects it.
        /// </summary>
        /// <typeparam name="T">The source document type.</typeparam>
        /// <typeparam name="TProjection">The projection result type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="filter">The filter expression.</param>
        /// <param name="projection">The projection expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The projected document or default.</returns>
        public static async Task<TProjection> FindOneProjectedAsync<T, TProjection>(
            this IMongoCollection<T> collection,
            Expression<Func<T, bool>> filter,
            Expression<Func<T, TProjection>> projection,
            CancellationToken cancellationToken = default)
        {
            var filterDef = Builders<T>.Filter.Where(filter);
            var projectionDef = Builders<T>.Projection.Expression(projection);

            var cursor = await collection.FindAsync(filterDef, new FindOptions<T, TProjection>
            {
                Projection = projectionDef,
                Limit = 1
            }, cancellationToken);

            return await cursor.FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Creates a projection options builder for auto-mapping.
        /// </summary>
        /// <typeparam name="TSource">The source document type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <returns>A projection options builder.</returns>
        public static MongoDbProjectionOptions<TSource, TDestination> CreateProjectionOptions<TSource, TDestination>(
            this IMongoCollection<TSource> collection)
        {
            return new MongoDbProjectionOptions<TSource, TDestination>();
        }

        /// <summary>
        /// Finds documents with auto-mapped projection.
        /// </summary>
        /// <typeparam name="TSource">The source document type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="filter">The filter expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of auto-mapped documents.</returns>
        /// <remarks>
        /// Auto-mapping includes only fields that exist in both source and destination types.
        /// </remarks>
        public static async Task<List<TDestination>> FindAutoMappedAsync<TSource, TDestination>(
            this IMongoCollection<TSource> collection,
            Expression<Func<TSource, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            var filterDef = Builders<TSource>.Filter.Where(filter);
            var projectionOptions = new MongoDbProjectionOptions<TSource, TDestination>().AutoMap();
            var projection = projectionOptions.Build();

            var cursor = await collection.FindAsync(filterDef, new FindOptions<TSource, TDestination>
            {
                Projection = projection
            }, cancellationToken);

            return await cursor.ToListAsync(cancellationToken);
        }
    }
}


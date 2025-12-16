//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Aggregation;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for MongoDB aggregation pipelines.
    /// </summary>
    public static class MongoDbAggregationExtensions
    {
        /// <summary>
        /// Creates an aggregation pipeline builder for the collection.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <returns>An aggregation pipeline builder.</returns>
        /// <example>
        /// <code>
        /// var result = await collection.AsAggregation()
        ///     .Match(o => o.Status == "Completed")
        ///     .Group("$customerId", new BsonDocument
        ///     {
        ///         { "totalAmount", new BsonDocument("$sum", "$amount") }
        ///     })
        ///     .Sort("totalAmount", descending: true)
        ///     .Limit(10)
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static MongoDbAggregationPipeline<T> AsAggregation<T>(this IMongoCollection<T> collection)
        {
            return MongoDbAggregationPipeline<T>.Create(collection);
        }

        /// <summary>
        /// Creates an aggregation pipeline builder from a fluent interface.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="find">The fluent find interface.</param>
        /// <returns>An aggregation pipeline builder.</returns>
        public static MongoDbAggregationPipeline<T> ToAggregation<T>(this IFindFluent<T, T> find)
        {
            // Note: This is a simplified conversion - actual implementation would need
            // to extract filter and other options from the find fluent
            throw new System.NotImplementedException(
                "Use collection.AsAggregation() instead for full aggregation pipeline support.");
        }
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Pagination;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for MongoDB keyset pagination.
    /// </summary>
    public static class MongoDbPaginationExtensions
    {
        /// <summary>
        /// Creates a keyset pagination builder for the collection.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <returns>A keyset pagination builder.</returns>
        /// <example>
        /// <code>
        /// var paginator = collection.AsKeysetPagination()
        ///     .Where(o => o.Status == "Active")
        ///     .OrderByDescending(o => o.CreatedAt)
        ///     .ThenBy(o => o.Id);
        /// 
        /// var firstPage = await paginator.GetPageAsync(pageSize: 20);
        /// var nextPage = await paginator.GetNextPageAsync(firstPage.LastCursor, pageSize: 20);
        /// </code>
        /// </example>
        public static MongoDbKeysetPagination<T> AsKeysetPagination<T>(this IMongoCollection<T> collection)
        {
            return MongoDbKeysetPagination<T>.Create(collection);
        }
    }
}


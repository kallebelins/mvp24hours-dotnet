//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Application.Contract.Cache
{
    /// <summary>
    /// Marker interface for queries that can be cached.
    /// Implement this interface to enable second-level caching for query results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface defines the caching behavior for a query, including:
    /// <list type="bullet">
    /// <item>Cache duration (how long results are stored)</item>
    /// <item>Cache key generation (how to uniquely identify cached results)</item>
    /// <item>Sliding expiration (whether to extend cache lifetime on access)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// public class GetProductsByCategory : ICacheableQuery
    /// {
    ///     public int CategoryId { get; set; }
    ///     
    ///     public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
    ///     public bool UseSlidingExpiration => true;
    ///     
    ///     public string GetCacheKey() => $"products_category_{CategoryId}";
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public interface ICacheableQuery
    {
        /// <summary>
        /// Gets the duration for which the query result should be cached.
        /// </summary>
        /// <value>The cache duration. Default implementation returns 5 minutes.</value>
        TimeSpan CacheDuration => TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets a value indicating whether to use sliding expiration.
        /// When true, the cache lifetime is extended each time the cached result is accessed.
        /// </summary>
        /// <value>True to use sliding expiration; false for absolute expiration.</value>
        bool UseSlidingExpiration => false;

        /// <summary>
        /// Gets the unique cache key for this query.
        /// The cache key should uniquely identify the query parameters.
        /// </summary>
        /// <returns>A unique string that identifies this query and its parameters.</returns>
        string GetCacheKey();

        /// <summary>
        /// Gets the cache region/group for this query.
        /// Queries in the same region can be invalidated together.
        /// </summary>
        /// <value>The cache region name. Default returns the query type name.</value>
        string CacheRegion => GetType().Name;
    }

    /// <summary>
    /// Interface for queries that can be cached with a specific response type.
    /// </summary>
    /// <typeparam name="TResponse">The type of the cached response.</typeparam>
    public interface ICacheableQuery<TResponse> : ICacheableQuery
    {
    }
}


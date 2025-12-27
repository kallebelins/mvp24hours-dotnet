//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Caching
{
    /// <summary>
    /// Interface for implementing the Read-Through caching pattern.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <remarks>
    /// <para>
    /// The Read-Through pattern automatically loads data from the data source when a cache miss occurs.
    /// The cache acts as a transparent layer between the application and the data source.
    /// </para>
    /// <para>
    /// <strong>How it works:</strong>
    /// <list type="number">
    /// <item>Application requests data from cache</item>
    /// <item>Cache checks if data exists</item>
    /// <item>If cache miss, cache automatically loads from data source</item>
    /// <item>Cache stores the data and returns it to application</item>
    /// <item>Subsequent requests are served from cache</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// <list type="bullet">
    /// <item>Application doesn't need to handle cache misses</item>
    /// <item>Centralized cache loading logic</item>
    /// <item>Consistent behavior across the application</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Use cases:</strong>
    /// <list type="bullet">
    /// <item>When you want to abstract cache logic from application code</item>
    /// <item>When cache loading logic is complex or shared</item>
    /// <item>When you want consistent cache behavior</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Implementation example
    /// public class ProductReadThroughCache : IReadThroughCache&lt;Product&gt;
    /// {
    ///     private readonly ICacheProvider _cache;
    ///     private readonly IProductRepository _repository;
    ///     
    ///     public ProductReadThroughCache(ICacheProvider cache, IProductRepository repository)
    ///     {
    ///         _cache = cache;
    ///         _repository = repository;
    ///     }
    ///     
    ///     public async Task&lt;Product&gt; GetAsync(string key, CancellationToken cancellationToken = default)
    ///     {
    ///         var cached = await _cache.GetAsync&lt;Product&gt;(key, cancellationToken);
    ///         if (cached != null) return cached;
    ///         
    ///         // Extract ID from key (e.g., "product:123" -> 123)
    ///         var id = ExtractIdFromKey(key);
    ///         var product = await _repository.GetByIdAsync(id, cancellationToken);
    ///         
    ///         if (product != null)
    ///         {
    ///             await _cache.SetAsync(key, product, 
    ///                 CacheEntryOptions.FromDuration(TimeSpan.FromMinutes(5)), 
    ///                 cancellationToken);
    ///         }
    ///         
    ///         return product;
    ///     }
    ///     
    ///     private int ExtractIdFromKey(string key) { /* ... */ }
    /// }
    /// </code>
    /// </example>
    public interface IReadThroughCache<T> where T : class
    {
        /// <summary>
        /// Gets a value from the cache, automatically loading from the data source if not found.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or loaded value, or null if not found in data source.</returns>
        Task<T?> GetAsync(string key, CancellationToken cancellationToken = default);
    }
}


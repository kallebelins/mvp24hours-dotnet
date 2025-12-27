//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Caching
{
    /// <summary>
    /// Interface for implementing the Write-Through caching pattern.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <remarks>
    /// <para>
    /// The Write-Through pattern ensures that data is written to both the cache and the data source
    /// synchronously. This guarantees consistency between cache and data source.
    /// </para>
    /// <para>
    /// <strong>How it works:</strong>
    /// <list type="number">
    /// <item>Application writes data through the cache</item>
    /// <item>Cache writes to data source first</item>
    /// <item>If successful, cache updates its own storage</item>
    /// <item>Operation completes only after both writes succeed</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// <list type="bullet">
    /// <item>Guaranteed consistency between cache and data source</item>
    /// <item>No risk of data loss if cache fails</item>
    /// <item>Simplified application logic</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Considerations:</strong>
    /// <list type="bullet">
    /// <item>Higher latency due to synchronous writes</item>
    /// <item>Data source becomes a bottleneck</item>
    /// <item>Cache write failure can cause data source write to fail</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Use cases:</strong>
    /// <list type="bullet">
    /// <item>When data consistency is critical</item>
    /// <item>When write operations are infrequent</item>
    /// <item>When you can tolerate higher write latency</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Implementation example
    /// public class ProductWriteThroughCache : IWriteThroughCache&lt;Product&gt;
    /// {
    ///     private readonly ICacheProvider _cache;
    ///     private readonly IProductRepository _repository;
    ///     
    ///     public async Task SetAsync(string key, Product value, CancellationToken cancellationToken = default)
    ///     {
    ///         // Write to data source first
    ///         await _repository.SaveAsync(value, cancellationToken);
    ///         
    ///         // Then write to cache
    ///         await _cache.SetAsync(key, value, 
    ///             CacheEntryOptions.FromDuration(TimeSpan.FromMinutes(5)), 
    ///             cancellationToken);
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IWriteThroughCache<T> where T : class
    {
        /// <summary>
        /// Sets a value in both the cache and the data source synchronously.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache and persist.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SetAsync(string key, T value, CancellationToken cancellationToken = default);
    }
}


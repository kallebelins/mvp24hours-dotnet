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
    /// Interface for implementing the Write-Behind (Write-Back) caching pattern.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <remarks>
    /// <para>
    /// The Write-Behind pattern writes data to the cache immediately and queues writes to the data source
    /// to be processed asynchronously in the background. This provides low-latency writes while ensuring
    /// eventual consistency.
    /// </para>
    /// <para>
    /// <strong>How it works:</strong>
    /// <list type="number">
    /// <item>Application writes data to cache</item>
    /// <item>Cache immediately updates its storage</item>
    /// <item>Write operation is queued for background processing</item>
    /// <item>Background service processes queued writes to data source</item>
    /// <item>Operation completes quickly (only cache write)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// <list type="bullet">
    /// <item>Very low write latency</item>
    /// <item>High write throughput</item>
    /// <item>Reduced load on data source</item>
    /// <item>Batch writes for efficiency</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Considerations:</strong>
    /// <list type="bullet">
    /// <item>Risk of data loss if application crashes before flush</item>
    /// <item>Eventual consistency (not immediate)</item>
    /// <item>More complex implementation</item>
    /// <item>Requires background processing infrastructure</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Use cases:</strong>
    /// <list type="bullet">
    /// <item>High-frequency write operations</item>
    /// <item>When write latency is critical</item>
    /// <item>When eventual consistency is acceptable</item>
    /// <item>When you can tolerate potential data loss</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Implementation example
    /// public class ProductWriteBehindCache : IWriteBehindCache&lt;Product&gt;
    /// {
    ///     public async Task SetAsync(string key, Product value, CancellationToken cancellationToken = default)
    ///     {
    ///         // Write to cache immediately
    ///         await _cache.SetAsync(key, value, options, cancellationToken);
    ///         
    ///         // Queue for background write
    ///         await _writeQueue.EnqueueAsync(new WriteOperation(key, value));
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IWriteBehindCache<T> where T : class
    {
        /// <summary>
        /// Sets a value in the cache immediately and queues it for background write to the data source.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache and persist.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SetAsync(string key, T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Forces an immediate flush of all pending writes to the data source.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task FlushAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the number of pending writes in the queue.
        /// </summary>
        int PendingWritesCount { get; }
    }
}


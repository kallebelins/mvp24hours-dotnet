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
    /// Prevents cache stampede (thundering herd) by coordinating concurrent cache misses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cache stampede occurs when multiple requests simultaneously miss the cache and
    /// all attempt to recompute the same value, causing unnecessary load on the data source.
    /// </para>
    /// <para>
    /// This interface provides a mechanism to serialize concurrent cache misses for the same key,
    /// ensuring only one request recomputes the value while others wait for the result.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In GetOrSetAsync pattern
    /// var value = await cache.GetAsync&lt;T&gt;(key);
    /// if (value != null) return value;
    /// 
    /// // Use stampede prevention
    /// return await stampedePrevention.ExecuteAsync(key, async () =>
    /// {
    ///     // Only one request executes this, others wait
    ///     var computed = await ComputeValueAsync();
    ///     await cache.SetAsync(key, computed, options);
    ///     return computed;
    /// });
    /// </code>
    /// </example>
    public interface ICacheStampedePrevention
    {
        /// <summary>
        /// Executes a function with stampede prevention for a specific cache key.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="key">The cache key to protect.</param>
        /// <param name="factory">The factory function to execute if no concurrent execution exists.</param>
        /// <param name="timeout">Maximum time to wait for concurrent execution to complete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result from the factory function or from a concurrent execution.</returns>
        Task<T> ExecuteAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);
    }
}


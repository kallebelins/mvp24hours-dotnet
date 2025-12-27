//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Threading;

namespace Mvp24Hours.Infrastructure.Caching.Observability;

/// <summary>
/// Provides cache metrics tracking including hit/miss ratio, latency, and cache size.
/// </summary>
/// <remarks>
/// <para>
/// This class tracks comprehensive cache metrics:
/// <list type="bullet">
/// <item><strong>Hit/Miss Ratio:</strong> Percentage of cache hits vs misses</item>
/// <item><strong>Latency:</strong> Operation duration tracking</item>
/// <item><strong>Size:</strong> Cache size in bytes and number of keys</item>
/// <item><strong>Throughput:</strong> Operations per second</item>
/// </list>
/// </para>
/// <para>
/// Metrics are automatically exported via OpenTelemetry when configured.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register metrics service
/// services.AddSingleton&lt;ICacheMetrics&gt;(sp => new CacheMetrics());
/// 
/// // Use in cache provider
/// public class MyCacheProvider : ICacheProvider
/// {
///     private readonly ICacheMetrics _metrics;
///     
///     public async Task&lt;T&gt; GetAsync&lt;T&gt;(string key)
///     {
///         var stopwatch = Stopwatch.StartNew();
///         try
///         {
///             var value = await _innerProvider.GetAsync&lt;T&gt;(key);
///             _metrics.RecordGet(key, stopwatch.ElapsedMilliseconds, value != null);
///             return value;
///         }
///         catch (Exception ex)
///         {
///             _metrics.RecordError("get", ex);
///             throw;
///         }
///     }
/// }
/// </code>
/// </example>
public interface ICacheMetrics
{
    /// <summary>
    /// Records a cache get operation.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="durationMs">Operation duration in milliseconds.</param>
    /// <param name="isHit">Whether it was a cache hit.</param>
    /// <param name="provider">Cache provider type.</param>
    /// <param name="level">Cache level (L1, L2 for multi-level cache).</param>
    void RecordGet(string key, double durationMs, bool isHit, string? provider = null, string? level = null);

    /// <summary>
    /// Records a cache set operation.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="durationMs">Operation duration in milliseconds.</param>
    /// <param name="valueSizeBytes">Value size in bytes.</param>
    /// <param name="provider">Cache provider type.</param>
    /// <param name="level">Cache level (L1, L2 for multi-level cache).</param>
    void RecordSet(string key, double durationMs, long? valueSizeBytes = null, string? provider = null, string? level = null);

    /// <summary>
    /// Records a cache remove operation.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="durationMs">Operation duration in milliseconds.</param>
    /// <param name="provider">Cache provider type.</param>
    /// <param name="level">Cache level (L1, L2 for multi-level cache).</param>
    void RecordRemove(string key, double durationMs, string? provider = null, string? level = null);

    /// <summary>
    /// Records a cache error.
    /// </summary>
    /// <param name="operation">Operation type (get, set, remove, etc.).</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="provider">Cache provider type.</param>
    void RecordError(string operation, Exception exception, string? provider = null);

    /// <summary>
    /// Records a cache eviction.
    /// </summary>
    /// <param name="provider">Cache provider type.</param>
    /// <param name="level">Cache level (L1, L2 for multi-level cache).</param>
    void RecordEviction(string? provider = null, string? level = null);

    /// <summary>
    /// Records a cache invalidation.
    /// </summary>
    /// <param name="keyPattern">Key pattern that was invalidated.</param>
    /// <param name="provider">Cache provider type.</param>
    /// <param name="level">Cache level (L1, L2 for multi-level cache).</param>
    void RecordInvalidation(string? keyPattern = null, string? provider = null, string? level = null);

    /// <summary>
    /// Gets the current hit ratio (hits / (hits + misses)).
    /// </summary>
    /// <param name="provider">Optional provider filter.</param>
    /// <returns>Hit ratio between 0.0 and 1.0, or null if no operations recorded.</returns>
    double? GetHitRatio(string? provider = null);

    /// <summary>
    /// Gets the total number of operations.
    /// </summary>
    /// <param name="provider">Optional provider filter.</param>
    /// <returns>Total operations count.</returns>
    long GetTotalOperations(string? provider = null);

    /// <summary>
    /// Gets the total number of hits.
    /// </summary>
    /// <param name="provider">Optional provider filter.</param>
    /// <returns>Total hits count.</returns>
    long GetTotalHits(string? provider = null);

    /// <summary>
    /// Gets the total number of misses.
    /// </summary>
    /// <param name="provider">Optional provider filter.</param>
    /// <returns>Total misses count.</returns>
    long GetTotalMisses(string? provider = null);
}

/// <summary>
/// Default implementation of ICacheMetrics using OpenTelemetry metrics.
/// </summary>
public class CacheMetrics : ICacheMetrics
{
    private readonly ConcurrentDictionary<string, CacheProviderStats> _providerStats = new();

    /// <summary>
    /// Creates a new instance of CacheMetrics.
    /// </summary>
    public CacheMetrics()
    {
    }

    /// <inheritdoc />
    public void RecordGet(string key, double durationMs, bool isHit, string? provider = null, string? level = null)
    {
        provider ??= "unknown";
        var stats = _providerStats.GetOrAdd(provider, _ => new CacheProviderStats());

        Interlocked.Increment(ref stats.TotalOperations);
        CacheActivitySource.RecordOperation("get", provider, durationMs, isHit, true, null, level);

        if (isHit)
        {
            Interlocked.Increment(ref stats.Hits);
        }
        else
        {
            Interlocked.Increment(ref stats.Misses);
        }
    }

    /// <inheritdoc />
    public void RecordSet(string key, double durationMs, long? valueSizeBytes = null, string? provider = null, string? level = null)
    {
        provider ??= "unknown";
        var stats = _providerStats.GetOrAdd(provider, _ => new CacheProviderStats());

        Interlocked.Increment(ref stats.TotalOperations);
        CacheActivitySource.RecordOperation("set", provider, durationMs, null, true, valueSizeBytes, level);
    }

    /// <inheritdoc />
    public void RecordRemove(string key, double durationMs, string? provider = null, string? level = null)
    {
        provider ??= "unknown";
        var stats = _providerStats.GetOrAdd(provider, _ => new CacheProviderStats());

        Interlocked.Increment(ref stats.TotalOperations);
        CacheActivitySource.RecordOperation("remove", provider, durationMs, null, true, null, level);
    }

    /// <inheritdoc />
    public void RecordError(string operation, Exception exception, string? provider = null)
    {
        provider ??= "unknown";
        var stats = _providerStats.GetOrAdd(provider, _ => new CacheProviderStats());

        Interlocked.Increment(ref stats.Errors);
        CacheActivitySource.RecordOperation(operation, provider, 0, null, false);
    }

    /// <inheritdoc />
    public void RecordEviction(string? provider = null, string? level = null)
    {
        provider ??= "unknown";
        CacheActivitySource.RecordEviction(provider, level);
    }

    /// <inheritdoc />
    public void RecordInvalidation(string? keyPattern = null, string? provider = null, string? level = null)
    {
        provider ??= "unknown";
        CacheActivitySource.RecordInvalidation(provider, keyPattern, level);
    }

    /// <inheritdoc />
    public double? GetHitRatio(string? provider = null)
    {
        if (provider != null)
        {
            if (!_providerStats.TryGetValue(provider, out var stats))
                return null;

            var hits = stats.Hits;
            var misses = stats.Misses;
            var total = hits + misses;

            return total > 0 ? (double)hits / total : null;
        }

        // Aggregate across all providers
        long totalHits = 0;
        long totalMisses = 0;

        foreach (var kvp in _providerStats)
        {
            totalHits += kvp.Value.Hits;
            totalMisses += kvp.Value.Misses;
        }

        var grandTotal = totalHits + totalMisses;
        return grandTotal > 0 ? (double)totalHits / grandTotal : null;
    }

    /// <inheritdoc />
    public long GetTotalOperations(string? provider = null)
    {
        if (provider != null)
        {
            if (!_providerStats.TryGetValue(provider, out var stats))
                return 0;

            return stats.TotalOperations;
        }

        // Aggregate across all providers
        long total = 0;
        foreach (var kvp in _providerStats)
        {
            total += kvp.Value.TotalOperations;
        }

        return total;
    }

    /// <inheritdoc />
    public long GetTotalHits(string? provider = null)
    {
        if (provider != null)
        {
            if (!_providerStats.TryGetValue(provider, out var stats))
                return 0;

            return stats.Hits;
        }

        // Aggregate across all providers
        long total = 0;
        foreach (var kvp in _providerStats)
        {
            total += kvp.Value.Hits;
        }

        return total;
    }

    /// <inheritdoc />
    public long GetTotalMisses(string? provider = null)
    {
        if (provider != null)
        {
            if (!_providerStats.TryGetValue(provider, out var stats))
                return 0;

            return stats.Misses;
        }

        // Aggregate across all providers
        long total = 0;
        foreach (var kvp in _providerStats)
        {
            total += kvp.Value.Misses;
        }

        return total;
    }

    private class CacheProviderStats
    {
        public long TotalOperations;
        public long Hits;
        public long Misses;
        public long Errors;
    }
}


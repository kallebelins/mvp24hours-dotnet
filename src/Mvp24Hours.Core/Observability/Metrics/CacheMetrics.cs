//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mvp24Hours.Core.Observability.Metrics;

/// <summary>
/// Provides metrics instrumentation for caching operations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides counters, histograms, and gauges for monitoring
/// cache hit/miss ratios, operation durations, and cache health.
/// </para>
/// <para>
/// <strong>Metrics provided:</strong>
/// <list type="bullet">
/// <item><c>gets_total</c> - Counter for cache get operations</item>
/// <item><c>hits_total</c> / <c>misses_total</c> - Cache hit/miss counters</item>
/// <item><c>sets_total</c> - Counter for cache set operations</item>
/// <item><c>removes_total</c> - Counter for cache remove operations</item>
/// <item><c>invalidations_total</c> - Counter for cache invalidations</item>
/// <item><c>operation_duration_ms</c> - Histogram for operation duration</item>
/// <item><c>item_size_bytes</c> - Histogram for cached item sizes</item>
/// <item><c>items_count</c> - Gauge for items in cache</item>
/// <item><c>hit_ratio</c> - Gauge for cache hit ratio</item>
/// </list>
/// </para>
/// </remarks>
public sealed class CacheMetrics
{
    private readonly Counter<long> _getsTotal;
    private readonly Counter<long> _hitsTotal;
    private readonly Counter<long> _missesTotal;
    private readonly Counter<long> _setsTotal;
    private readonly Counter<long> _removesTotal;
    private readonly Counter<long> _invalidationsTotal;
    private readonly Histogram<double> _operationDuration;
    private readonly Histogram<int> _itemSizeBytes;
    private readonly UpDownCounter<int> _itemsCount;

    // For hit ratio calculation
    private long _totalGets;
    private long _totalHits;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheMetrics"/> class.
    /// </summary>
    public CacheMetrics()
    {
        var meter = Mvp24HoursMeters.Caching.Meter;

        _getsTotal = meter.CreateCounter<long>(
            MetricNames.CacheGetsTotal,
            unit: "{operations}",
            description: "Total number of cache get operations");

        _hitsTotal = meter.CreateCounter<long>(
            MetricNames.CacheHitsTotal,
            unit: "{hits}",
            description: "Total number of cache hits");

        _missesTotal = meter.CreateCounter<long>(
            MetricNames.CacheMissesTotal,
            unit: "{misses}",
            description: "Total number of cache misses");

        _setsTotal = meter.CreateCounter<long>(
            MetricNames.CacheSetsTotal,
            unit: "{operations}",
            description: "Total number of cache set operations");

        _removesTotal = meter.CreateCounter<long>(
            MetricNames.CacheRemovesTotal,
            unit: "{operations}",
            description: "Total number of cache remove operations");

        _invalidationsTotal = meter.CreateCounter<long>(
            MetricNames.CacheInvalidationsTotal,
            unit: "{operations}",
            description: "Total number of cache invalidations");

        _operationDuration = meter.CreateHistogram<double>(
            MetricNames.CacheOperationDuration,
            unit: "ms",
            description: "Duration of cache operations in milliseconds");

        _itemSizeBytes = meter.CreateHistogram<int>(
            MetricNames.CacheItemSizeBytes,
            unit: "By",
            description: "Size of cached items in bytes");

        _itemsCount = meter.CreateUpDownCounter<int>(
            MetricNames.CacheItemsCount,
            unit: "{items}",
            description: "Number of items currently in cache");

        // Create observable gauge for hit ratio
        meter.CreateObservableGauge(
            MetricNames.CacheHitRatio,
            () => CalculateHitRatio(),
            unit: "%",
            description: "Cache hit ratio percentage");
    }

    private double CalculateHitRatio()
    {
        var gets = _totalGets;
        return gets > 0 ? ((double)_totalHits / gets) * 100 : 0;
    }

    #region Get Operations

    /// <summary>
    /// Begins tracking a cache get operation.
    /// </summary>
    /// <param name="cacheName">Name of the cache.</param>
    /// <returns>A scope that should be disposed when operation completes.</returns>
    public CacheOperationScope BeginGet(string cacheName)
    {
        return new CacheOperationScope(this, cacheName, "get");
    }

    /// <summary>
    /// Records a cache get operation with hit result.
    /// </summary>
    /// <param name="cacheName">Name of the cache.</param>
    /// <param name="hit">Whether the cache hit.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    public void RecordGet(string cacheName, bool hit, double durationMs)
    {
        var tags = new TagList
        {
            { MetricTags.CacheName, cacheName },
            { MetricTags.CacheOperation, "get" }
        };

        System.Threading.Interlocked.Increment(ref _totalGets);
        _getsTotal.Add(1, tags);

        if (hit)
        {
            System.Threading.Interlocked.Increment(ref _totalHits);
            _hitsTotal.Add(1, tags);
        }
        else
        {
            _missesTotal.Add(1, tags);
        }

        _operationDuration.Record(durationMs, tags);
    }

    #endregion

    #region Set Operations

    /// <summary>
    /// Begins tracking a cache set operation.
    /// </summary>
    /// <param name="cacheName">Name of the cache.</param>
    /// <returns>A scope that should be disposed when operation completes.</returns>
    public CacheOperationScope BeginSet(string cacheName)
    {
        return new CacheOperationScope(this, cacheName, "set");
    }

    /// <summary>
    /// Records a cache set operation.
    /// </summary>
    /// <param name="cacheName">Name of the cache.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="itemSizeBytes">Size of the cached item in bytes (optional).</param>
    public void RecordSet(string cacheName, double durationMs, int itemSizeBytes = 0)
    {
        var tags = new TagList
        {
            { MetricTags.CacheName, cacheName },
            { MetricTags.CacheOperation, "set" }
        };

        _setsTotal.Add(1, tags);
        _operationDuration.Record(durationMs, tags);
        _itemsCount.Add(1, new KeyValuePair<string, object?>(MetricTags.CacheName, cacheName));

        if (itemSizeBytes > 0)
        {
            _itemSizeBytes.Record(itemSizeBytes, tags);
        }
    }

    #endregion

    #region Remove Operations

    /// <summary>
    /// Begins tracking a cache remove operation.
    /// </summary>
    /// <param name="cacheName">Name of the cache.</param>
    /// <returns>A scope that should be disposed when operation completes.</returns>
    public CacheOperationScope BeginRemove(string cacheName)
    {
        return new CacheOperationScope(this, cacheName, "remove");
    }

    /// <summary>
    /// Records a cache remove operation.
    /// </summary>
    /// <param name="cacheName">Name of the cache.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    public void RecordRemove(string cacheName, double durationMs)
    {
        var tags = new TagList
        {
            { MetricTags.CacheName, cacheName },
            { MetricTags.CacheOperation, "remove" }
        };

        _removesTotal.Add(1, tags);
        _operationDuration.Record(durationMs, tags);
        _itemsCount.Add(-1, new KeyValuePair<string, object?>(MetricTags.CacheName, cacheName));
    }

    #endregion

    #region Invalidation Operations

    /// <summary>
    /// Records a cache invalidation.
    /// </summary>
    /// <param name="cacheName">Name of the cache.</param>
    /// <param name="itemsInvalidated">Number of items invalidated.</param>
    public void RecordInvalidation(string cacheName, int itemsInvalidated = 1)
    {
        var tags = new TagList { { MetricTags.CacheName, cacheName } };
        _invalidationsTotal.Add(1, tags);
        _itemsCount.Add(-itemsInvalidated, new KeyValuePair<string, object?>(MetricTags.CacheName, cacheName));
    }

    #endregion

    #region Scope Struct

    /// <summary>
    /// Represents a scope for tracking cache operation duration.
    /// </summary>
    public readonly struct CacheOperationScope : IDisposable
    {
        private readonly CacheMetrics _metrics;
        private readonly string _cacheName;
        private readonly string _operation;
        private readonly long _startTimestamp;

        /// <summary>
        /// Gets or sets whether the cache hit (for get operations).
        /// </summary>
        public bool Hit { get; private set; }

        /// <summary>
        /// Gets or sets the item size in bytes (for set operations).
        /// </summary>
        public int ItemSizeBytes { get; private set; }

        internal CacheOperationScope(CacheMetrics metrics, string cacheName, string operation)
        {
            _metrics = metrics;
            _cacheName = cacheName;
            _operation = operation;
            _startTimestamp = Stopwatch.GetTimestamp();
            Hit = false;
            ItemSizeBytes = 0;
        }

        /// <summary>
        /// Marks a get operation as a cache hit.
        /// </summary>
        public void SetHit() => Hit = true;

        /// <summary>
        /// Marks a get operation as a cache miss.
        /// </summary>
        public void SetMiss() => Hit = false;

        /// <summary>
        /// Sets the size of the cached item.
        /// </summary>
        /// <param name="sizeBytes">Size in bytes.</param>
        public void SetItemSize(int sizeBytes) => ItemSizeBytes = sizeBytes;

        /// <inheritdoc />
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            var durationMs = elapsed.TotalMilliseconds;

            switch (_operation)
            {
                case "get":
                    _metrics.RecordGet(_cacheName, Hit, durationMs);
                    break;
                case "set":
                    _metrics.RecordSet(_cacheName, durationMs, ItemSizeBytes);
                    break;
                case "remove":
                    _metrics.RecordRemove(_cacheName, durationMs);
                    break;
            }
        }
    }

    #endregion
}


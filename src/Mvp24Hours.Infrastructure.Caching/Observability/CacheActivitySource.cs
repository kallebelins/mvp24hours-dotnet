//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mvp24Hours.Infrastructure.Caching.Observability;

/// <summary>
/// ActivitySource and Meter for Cache operations in OpenTelemetry-compatible tracing and metrics.
/// </summary>
/// <remarks>
/// <para>
/// This class provides integration with the .NET Activity API and Metrics API
/// which are automatically exported by OpenTelemetry when configured.
/// </para>
/// <para>
/// <strong>Metric Names:</strong>
/// <list type="bullet">
/// <item>mvp24hours_cache_operations_total - Counter of total cache operations</item>
/// <item>mvp24hours_cache_hits_total - Counter of cache hits</item>
/// <item>mvp24hours_cache_misses_total - Counter of cache misses</item>
/// <item>mvp24hours_cache_operation_duration_ms - Histogram of operation durations</item>
/// <item>mvp24hours_cache_size_bytes - Gauge of cache size in bytes</item>
/// <item>mvp24hours_cache_keys_count - Gauge of number of keys in cache</item>
/// <item>mvp24hours_cache_errors_total - Counter of cache errors</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure OpenTelemetry to include Mvp24Hours Cache activities and metrics
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(builder =>
///     {
///         builder
///             .AddSource(CacheActivitySource.SourceName)
///             .AddAspNetCoreInstrumentation()
///             .AddJaegerExporter();
///     })
///     .WithMetrics(builder =>
///     {
///         builder
///             .AddMeter(CacheActivitySource.MeterName)
///             .AddAspNetCoreInstrumentation()
///             .AddPrometheusExporter();
///     });
/// </code>
/// </example>
public static class CacheActivitySource
{
    /// <summary>
    /// The name of the ActivitySource for Mvp24Hours Cache operations.
    /// </summary>
    public const string SourceName = "Mvp24Hours.Cache";

    /// <summary>
    /// The name of the Meter for Mvp24Hours Cache metrics.
    /// </summary>
    public const string MeterName = "Mvp24Hours.Cache";

    /// <summary>
    /// The version of the instrumentation.
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// The ActivitySource instance used for creating activities.
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName, Version);

    /// <summary>
    /// The Meter instance used for creating metrics.
    /// </summary>
    public static readonly Meter Meter = new(MeterName, Version);

    #region Metrics

    /// <summary>
    /// Counter for total cache operations.
    /// </summary>
    public static readonly Counter<long> OperationsTotal = Meter.CreateCounter<long>(
        "mvp24hours_cache_operations_total",
        unit: "{operation}",
        description: "Total number of cache operations");

    /// <summary>
    /// Counter for cache hits.
    /// </summary>
    public static readonly Counter<long> HitsTotal = Meter.CreateCounter<long>(
        "mvp24hours_cache_hits_total",
        unit: "{hit}",
        description: "Total number of cache hits");

    /// <summary>
    /// Counter for cache misses.
    /// </summary>
    public static readonly Counter<long> MissesTotal = Meter.CreateCounter<long>(
        "mvp24hours_cache_misses_total",
        unit: "{miss}",
        description: "Total number of cache misses");

    /// <summary>
    /// Histogram for cache operation duration in milliseconds.
    /// </summary>
    public static readonly Histogram<double> OperationDuration = Meter.CreateHistogram<double>(
        "mvp24hours_cache_operation_duration_ms",
        unit: "ms",
        description: "Cache operation duration in milliseconds");

    /// <summary>
    /// Histogram for cache value size in bytes.
    /// </summary>
    public static readonly Histogram<long> ValueSize = Meter.CreateHistogram<long>(
        "mvp24hours_cache_value_size_bytes",
        unit: "By",
        description: "Cache value size in bytes");

    /// <summary>
    /// Counter for cache errors.
    /// </summary>
    public static readonly Counter<long> ErrorsTotal = Meter.CreateCounter<long>(
        "mvp24hours_cache_errors_total",
        unit: "{error}",
        description: "Total number of cache errors");

    /// <summary>
    /// Counter for cache evictions.
    /// </summary>
    public static readonly Counter<long> EvictionsTotal = Meter.CreateCounter<long>(
        "mvp24hours_cache_evictions_total",
        unit: "{eviction}",
        description: "Total number of cache evictions");

    /// <summary>
    /// Counter for cache invalidations.
    /// </summary>
    public static readonly Counter<long> InvalidationsTotal = Meter.CreateCounter<long>(
        "mvp24hours_cache_invalidations_total",
        unit: "{invalidation}",
        description: "Total number of cache invalidations");

    #endregion

    #region Activity Names

    /// <summary>
    /// Activity names for different cache operations.
    /// </summary>
    public static class ActivityNames
    {
        /// <summary>Activity name for cache get operations.</summary>
        public const string Get = "Mvp24Hours.Cache.Get";

        /// <summary>Activity name for cache set operations.</summary>
        public const string Set = "Mvp24Hours.Cache.Set";

        /// <summary>Activity name for cache remove operations.</summary>
        public const string Remove = "Mvp24Hours.Cache.Remove";

        /// <summary>Activity name for cache exists operations.</summary>
        public const string Exists = "Mvp24Hours.Cache.Exists";

        /// <summary>Activity name for cache refresh operations.</summary>
        public const string Refresh = "Mvp24Hours.Cache.Refresh";

        /// <summary>Activity name for cache batch operations.</summary>
        public const string Batch = "Mvp24Hours.Cache.Batch";
    }

    #endregion

    #region Tag Names

    /// <summary>
    /// Tag names for activity attributes following OpenTelemetry semantic conventions.
    /// </summary>
    public static class TagNames
    {
        /// <summary>Cache operation type (get, set, remove, etc.).</summary>
        public const string Operation = "cache.operation";

        /// <summary>Cache key.</summary>
        public const string Key = "cache.key";

        /// <summary>Cache key pattern (for batch operations).</summary>
        public const string KeyPattern = "cache.key_pattern";

        /// <summary>Whether the operation was a hit.</summary>
        public const string IsHit = "cache.is_hit";

        /// <summary>Whether the operation was a miss.</summary>
        public const string IsMiss = "cache.is_miss";

        /// <summary>Cache provider type (memory, distributed, multi-level).</summary>
        public const string Provider = "cache.provider";

        /// <summary>Cache level (L1, L2 for multi-level cache).</summary>
        public const string Level = "cache.level";

        /// <summary>Operation duration in milliseconds.</summary>
        public const string DurationMs = "cache.duration_ms";

        /// <summary>Value size in bytes.</summary>
        public const string ValueSizeBytes = "cache.value_size_bytes";

        /// <summary>Error type name.</summary>
        public const string ErrorType = "error.type";

        /// <summary>Error message.</summary>
        public const string ErrorMessage = "error.message";

        /// <summary>Whether the operation was successful.</summary>
        public const string IsSuccess = "cache.is_success";

        /// <summary>Number of keys in batch operation.</summary>
        public const string BatchSize = "cache.batch_size";
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Records a cache operation with metrics.
    /// </summary>
    /// <param name="operation">Operation type (get, set, remove, etc.).</param>
    /// <param name="provider">Cache provider type.</param>
    /// <param name="durationMs">Operation duration in milliseconds.</param>
    /// <param name="isHit">Whether it was a hit (for get operations).</param>
    /// <param name="isSuccess">Whether the operation was successful.</param>
    /// <param name="valueSizeBytes">Value size in bytes (for set operations).</param>
    /// <param name="level">Cache level (L1, L2 for multi-level cache).</param>
    public static void RecordOperation(
        string operation,
        string provider,
        double durationMs,
        bool? isHit = null,
        bool isSuccess = true,
        long? valueSizeBytes = null,
        string? level = null)
    {
        var tags = new TagList
        {
            { TagNames.Operation, operation },
            { TagNames.Provider, provider },
            { TagNames.IsSuccess, isSuccess }
        };

        if (!string.IsNullOrEmpty(level))
        {
            tags.Add(TagNames.Level, level);
        }

        OperationsTotal.Add(1, tags);
        OperationDuration.Record(durationMs, tags);

        if (isHit.HasValue)
        {
            if (isHit.Value)
            {
                HitsTotal.Add(1, tags);
            }
            else
            {
                MissesTotal.Add(1, tags);
            }
        }

        if (valueSizeBytes.HasValue && valueSizeBytes.Value > 0)
        {
            ValueSize.Record(valueSizeBytes.Value, tags);
        }

        if (!isSuccess)
        {
            ErrorsTotal.Add(1, tags);
        }
    }

    /// <summary>
    /// Records a cache eviction.
    /// </summary>
    /// <param name="provider">Cache provider type.</param>
    /// <param name="level">Cache level (L1, L2 for multi-level cache).</param>
    public static void RecordEviction(string provider, string? level = null)
    {
        var tags = new TagList
        {
            { TagNames.Provider, provider }
        };

        if (!string.IsNullOrEmpty(level))
        {
            tags.Add(TagNames.Level, level);
        }

        EvictionsTotal.Add(1, tags);
    }

    /// <summary>
    /// Records a cache invalidation.
    /// </summary>
    /// <param name="provider">Cache provider type.</param>
    /// <param name="keyPattern">Key pattern that was invalidated.</param>
    /// <param name="level">Cache level (L1, L2 for multi-level cache).</param>
    public static void RecordInvalidation(string provider, string? keyPattern = null, string? level = null)
    {
        var tags = new TagList
        {
            { TagNames.Provider, provider }
        };

        if (!string.IsNullOrEmpty(keyPattern))
        {
            tags.Add(TagNames.KeyPattern, keyPattern);
        }

        if (!string.IsNullOrEmpty(level))
        {
            tags.Add(TagNames.Level, level);
        }

        InvalidationsTotal.Add(1, tags);
    }

    /// <summary>
    /// Starts an activity for a cache operation.
    /// </summary>
    /// <param name="operationName">Activity name (from ActivityNames).</param>
    /// <param name="operation">Operation type (get, set, remove, etc.).</param>
    /// <param name="key">Cache key.</param>
    /// <param name="provider">Cache provider type.</param>
    /// <param name="level">Cache level (L1, L2 for multi-level cache).</param>
    /// <returns>An Activity if listeners are registered, null otherwise.</returns>
    public static Activity? StartCacheActivity(
        string operationName,
        string operation,
        string? key = null,
        string? provider = null,
        string? level = null)
    {
        var activity = Source.StartActivity(operationName, ActivityKind.Internal);

        if (activity == null)
            return null;

        activity.SetTag(TagNames.Operation, operation);

        if (!string.IsNullOrEmpty(key))
        {
            activity.SetTag(TagNames.Key, key);
        }

        if (!string.IsNullOrEmpty(provider))
        {
            activity.SetTag(TagNames.Provider, provider);
        }

        if (!string.IsNullOrEmpty(level))
        {
            activity.SetTag(TagNames.Level, level);
        }

        return activity;
    }

    /// <summary>
    /// Sets success status on an activity.
    /// </summary>
    public static void SetSuccess(Activity? activity, bool isHit = false)
    {
        if (activity == null)
            return;

        activity.SetTag(TagNames.IsSuccess, true);
        activity.SetTag(TagNames.IsHit, isHit);
        activity.SetTag(TagNames.IsMiss, !isHit);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Sets error status on an activity.
    /// </summary>
    public static void SetError(Activity? activity, Exception exception)
    {
        if (activity == null)
            return;

        activity.SetTag(TagNames.IsSuccess, false);
        activity.SetTag(TagNames.ErrorType, exception.GetType().FullName);
        activity.SetTag(TagNames.ErrorMessage, exception.Message);
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);

        // Record exception event
        activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace }
        }));
    }

    /// <summary>
    /// Adds operation details to an activity.
    /// </summary>
    public static void EnrichActivity(
        Activity? activity,
        double durationMs,
        long? valueSizeBytes = null,
        int? batchSize = null)
    {
        if (activity == null)
            return;

        activity.SetTag(TagNames.DurationMs, durationMs);

        if (valueSizeBytes.HasValue)
        {
            activity.SetTag(TagNames.ValueSizeBytes, valueSizeBytes.Value);
        }

        if (batchSize.HasValue)
        {
            activity.SetTag(TagNames.BatchSize, batchSize.Value);
        }
    }

    #endregion
}


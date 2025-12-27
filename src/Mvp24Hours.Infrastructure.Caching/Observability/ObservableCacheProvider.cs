//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Observability;

/// <summary>
/// Wrapper around ICacheProvider that adds observability (tracing, metrics, structured logging).
/// </summary>
/// <remarks>
/// <para>
/// This wrapper automatically:
/// <list type="bullet">
/// <item>Creates OpenTelemetry activities for cache operations</item>
/// <item>Records metrics (hits, misses, latency, errors)</item>
/// <item>Logs structured events with context</item>
/// </list>
/// </para>
/// </remarks>
public class ObservableCacheProvider : ICacheProvider
{
    private readonly ICacheProvider _innerProvider;
    private readonly ICacheMetrics? _metrics;
    private readonly ILogger<ObservableCacheProvider>? _logger;
    private readonly string _providerName;

    /// <summary>
    /// Creates a new instance of ObservableCacheProvider.
    /// </summary>
    /// <param name="innerProvider">The underlying cache provider.</param>
    /// <param name="metrics">Optional metrics service.</param>
    /// <param name="logger">Optional logger.</param>
    public ObservableCacheProvider(
        ICacheProvider innerProvider,
        ICacheMetrics? metrics = null,
        ILogger<ObservableCacheProvider>? logger = null)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
        _metrics = metrics;
        _logger = logger;
        _providerName = innerProvider.GetType().Name;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        Activity? activity = null;

        try
        {
            activity = CacheActivitySource.StartCacheActivity(
                CacheActivitySource.ActivityNames.Get,
                "get",
                key,
                _providerName);

            _logger?.LogDebug(
                "Cache GET operation started. Key: {Key}, Provider: {Provider}",
                key,
                _providerName);

            var value = await _innerProvider.GetAsync<T>(key, cancellationToken);
            var isHit = value != null;
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            CacheActivitySource.SetSuccess(activity, isHit);
            CacheActivitySource.EnrichActivity(activity, durationMs);
            CacheActivitySource.RecordOperation("get", _providerName, durationMs, isHit, true);
            _metrics?.RecordGet(key, durationMs, isHit, _providerName);

            if (isHit)
            {
                _logger?.LogDebug(
                    "Cache HIT. Key: {Key}, Provider: {Provider}, Duration: {DurationMs}ms",
                    key,
                    _providerName,
                    durationMs);
            }
            else
            {
                _logger?.LogDebug(
                    "Cache MISS. Key: {Key}, Provider: {Provider}, Duration: {DurationMs}ms",
                    key,
                    _providerName,
                    durationMs);
            }

            return value;
        }
        catch (Exception ex)
        {
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;
            CacheActivitySource.SetError(activity, ex);
            CacheActivitySource.RecordOperation("get", _providerName, durationMs, null, false);
            _metrics?.RecordError("get", ex, _providerName);

            _logger?.LogError(
                ex,
                "Cache GET operation failed. Key: {Key}, Provider: {Provider}, Duration: {DurationMs}ms",
                key,
                _providerName,
                durationMs);

            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Activity? activity = null;

        try
        {
            activity = CacheActivitySource.StartCacheActivity(
                CacheActivitySource.ActivityNames.Get,
                "get",
                key,
                _providerName);

            _logger?.LogDebug(
                "Cache GET operation started. Key: {Key}, Provider: {Provider}",
                key,
                _providerName);

            var value = await _innerProvider.GetStringAsync(key, cancellationToken);
            var isHit = value != null;
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            CacheActivitySource.SetSuccess(activity, isHit);
            CacheActivitySource.EnrichActivity(activity, durationMs);
            CacheActivitySource.RecordOperation("get", _providerName, durationMs, isHit, true);
            _metrics?.RecordGet(key, durationMs, isHit, _providerName);

            if (isHit)
            {
                _logger?.LogDebug(
                    "Cache HIT. Key: {Key}, Provider: {Provider}, Duration: {DurationMs}ms",
                    key,
                    _providerName,
                    durationMs);
            }
            else
            {
                _logger?.LogDebug(
                    "Cache MISS. Key: {Key}, Provider: {Provider}, Duration: {DurationMs}ms",
                    key,
                    _providerName,
                    durationMs);
            }

            return value;
        }
        catch (Exception ex)
        {
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;
            CacheActivitySource.SetError(activity, ex);
            CacheActivitySource.RecordOperation("get", _providerName, durationMs, null, false);
            _metrics?.RecordError("get", ex, _providerName);

            _logger?.LogError(
                ex,
                "Cache GET operation failed. Key: {Key}, Provider: {Provider}, Duration: {DurationMs}ms",
                key,
                _providerName,
                durationMs);

            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        Activity? activity = null;

        try
        {
            activity = CacheActivitySource.StartCacheActivity(
                CacheActivitySource.ActivityNames.Set,
                "set",
                key,
                _providerName);

            long? valueSizeBytes = null;
            if (value is string str)
            {
                valueSizeBytes = System.Text.Encoding.UTF8.GetByteCount(str);
            }

            _logger?.LogDebug(
                "Cache SET operation started. Key: {Key}, Provider: {Provider}, ValueSize: {ValueSizeBytes}",
                key,
                _providerName,
                valueSizeBytes);

            await _innerProvider.SetAsync(key, value, options, cancellationToken);
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            CacheActivitySource.SetSuccess(activity, false);
            CacheActivitySource.EnrichActivity(activity, durationMs, valueSizeBytes);
            CacheActivitySource.RecordOperation("set", _providerName, durationMs, null, true, valueSizeBytes);
            _metrics?.RecordSet(key, durationMs, valueSizeBytes, _providerName);

            _logger?.LogDebug(
                "Cache SET operation completed. Key: {Key}, Provider: {Provider}, Duration: {DurationMs}ms",
                key,
                _providerName,
                durationMs);
        }
        catch (Exception ex)
        {
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;
            CacheActivitySource.SetError(activity, ex);
            CacheActivitySource.RecordOperation("set", _providerName, durationMs, null, false);
            _metrics?.RecordError("set", ex, _providerName);

            _logger?.LogError(
                ex,
                "Cache SET operation failed. Key: {Key}, Provider: {Provider}, Duration: {DurationMs}ms",
                key,
                _providerName,
                durationMs);

            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task SetStringAsync(string key, string value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Activity? activity = null;

        try
        {
            activity = CacheActivitySource.StartCacheActivity(
                CacheActivitySource.ActivityNames.Set,
                "set",
                key,
                _providerName);

            var valueSizeBytes = System.Text.Encoding.UTF8.GetByteCount(value);

            _logger?.LogDebug(
                "Cache SET operation started. Key: {Key}, Provider: {Provider}, ValueSize: {ValueSizeBytes}",
                key,
                _providerName,
                valueSizeBytes);

            await _innerProvider.SetStringAsync(key, value, options, cancellationToken);
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            CacheActivitySource.SetSuccess(activity, false);
            CacheActivitySource.EnrichActivity(activity, durationMs, valueSizeBytes);
            CacheActivitySource.RecordOperation("set", _providerName, durationMs, null, true, valueSizeBytes);
            _metrics?.RecordSet(key, durationMs, valueSizeBytes, _providerName);

            _logger?.LogDebug(
                "Cache SET operation completed. Key: {Key}, Provider: {Provider}, Duration: {DurationMs}ms",
                key,
                _providerName,
                durationMs);
        }
        catch (Exception ex)
        {
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;
            CacheActivitySource.SetError(activity, ex);
            CacheActivitySource.RecordOperation("set", _providerName, durationMs, null, false);
            _metrics?.RecordError("set", ex, _providerName);

            _logger?.LogError(
                ex,
                "Cache SET operation failed. Key: {Key}, Provider: {Provider}, Duration: {DurationMs}ms",
                key,
                _providerName,
                durationMs);

            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Activity? activity = null;

        try
        {
            activity = CacheActivitySource.StartCacheActivity(
                CacheActivitySource.ActivityNames.Remove,
                "remove",
                key,
                _providerName);

            _logger?.LogDebug(
                "Cache REMOVE operation started. Key: {Key}, Provider: {Provider}",
                key,
                _providerName);

            await _innerProvider.RemoveAsync(key, cancellationToken);
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            CacheActivitySource.SetSuccess(activity, false);
            CacheActivitySource.EnrichActivity(activity, durationMs);
            CacheActivitySource.RecordOperation("remove", _providerName, durationMs, null, true);
            _metrics?.RecordRemove(key, durationMs, _providerName);

            _logger?.LogDebug(
                "Cache REMOVE operation completed. Key: {Key}, Provider: {Provider}, Duration: {DurationMs}ms",
                key,
                _providerName,
                durationMs);
        }
        catch (Exception ex)
        {
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;
            CacheActivitySource.SetError(activity, ex);
            CacheActivitySource.RecordOperation("remove", _providerName, durationMs, null, false);
            _metrics?.RecordError("remove", ex, _providerName);

            _logger?.LogError(
                ex,
                "Cache REMOVE operation failed. Key: {Key}, Provider: {Provider}, Duration: {DurationMs}ms",
                key,
                _providerName,
                durationMs);

            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <inheritdoc />
    public Task RemoveManyAsync(string[] keys, CancellationToken cancellationToken = default)
    {
        return _innerProvider.RemoveManyAsync(keys, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Activity? activity = null;

        try
        {
            activity = CacheActivitySource.StartCacheActivity(
                CacheActivitySource.ActivityNames.Exists,
                "exists",
                key,
                _providerName);

            var exists = await _innerProvider.ExistsAsync(key, cancellationToken);
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            CacheActivitySource.SetSuccess(activity, exists);
            CacheActivitySource.EnrichActivity(activity, durationMs);
            CacheActivitySource.RecordOperation("exists", _providerName, durationMs, exists, true);

            return exists;
        }
        catch (Exception ex)
        {
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;
            CacheActivitySource.SetError(activity, ex);
            CacheActivitySource.RecordOperation("exists", _providerName, durationMs, null, false);
            _metrics?.RecordError("exists", ex, _providerName);

            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <inheritdoc />
    public Task<Dictionary<string, T>> GetManyAsync<T>(string[] keys, CancellationToken cancellationToken = default) where T : class
    {
        return _innerProvider.GetManyAsync<T>(keys, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetManyAsync<T>(Dictionary<string, T> values, CacheEntryOptions? options = null, CancellationToken cancellationToken = default) where T : class
    {
        return _innerProvider.SetManyAsync(values, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Activity? activity = null;

        try
        {
            activity = CacheActivitySource.StartCacheActivity(
                CacheActivitySource.ActivityNames.Refresh,
                "refresh",
                key,
                _providerName);

            await _innerProvider.RefreshAsync(key, cancellationToken);
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            CacheActivitySource.SetSuccess(activity, false);
            CacheActivitySource.EnrichActivity(activity, durationMs);
            CacheActivitySource.RecordOperation("refresh", _providerName, durationMs, null, true);
        }
        catch (Exception ex)
        {
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;
            CacheActivitySource.SetError(activity, ex);
            CacheActivitySource.RecordOperation("refresh", _providerName, durationMs, null, false);
            _metrics?.RecordError("refresh", ex, _providerName);

            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }
}


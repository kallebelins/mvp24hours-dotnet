//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Observability;

/// <summary>
/// Health check for cache providers to verify connectivity and functionality.
/// </summary>
/// <remarks>
/// <para>
/// This health check performs the following validations:
/// <list type="bullet">
/// <item>Test write operation (set a test key)</item>
/// <item>Test read operation (get the test key)</item>
/// <item>Test delete operation (remove the test key)</item>
/// <item>Verify response times are within acceptable limits</item>
/// </list>
/// </para>
/// <para>
/// The health check can be configured with:
/// <list type="bullet">
/// <item>Timeout threshold for operations</item>
/// <item>Test key prefix (default: "health_check_")</item>
/// <item>Whether to include detailed diagnostics</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register health check
/// services.AddHealthChecks()
///     .AddCheck&lt;CacheHealthCheck&gt;(
///         "cache",
///         tags: new[] { "cache", "infrastructure" });
/// 
/// // Use in health endpoint
/// app.MapHealthChecks("/health/cache");
/// </code>
/// </example>
public class CacheHealthCheck : IHealthCheck
{
    private readonly ICacheProvider _cacheProvider;
    private readonly ILogger<CacheHealthCheck>? _logger;
    private readonly CacheHealthCheckOptions _options;

    /// <summary>
    /// Creates a new instance of CacheHealthCheck.
    /// </summary>
    /// <param name="cacheProvider">The cache provider to check.</param>
    /// <param name="options">Health check options (optional).</param>
    /// <param name="logger">Optional logger.</param>
    public CacheHealthCheck(
        ICacheProvider cacheProvider,
        CacheHealthCheckOptions? options = null,
        ILogger<CacheHealthCheck>? logger = null)
    {
        _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
        _options = options ?? new CacheHealthCheckOptions();
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var testKey = $"{_options.TestKeyPrefix}{Guid.NewGuid():N}";
        var testValue = "health_check_test_value";
        var startTime = DateTime.UtcNow;

        try
        {
            // Test 1: Write operation
            var setStartTime = DateTime.UtcNow;
            await _cacheProvider.SetStringAsync(testKey, testValue, cancellationToken: cancellationToken);
            var setDuration = (DateTime.UtcNow - setStartTime).TotalMilliseconds;

            if (setDuration > _options.MaxOperationDurationMs)
            {
                return HealthCheckResult.Degraded(
                    $"Cache set operation took {setDuration:F2}ms (threshold: {_options.MaxOperationDurationMs}ms)",
                    data: CreateHealthData("set_duration_ms", setDuration));
            }

            // Test 2: Read operation
            var getStartTime = DateTime.UtcNow;
            var retrievedValue = await _cacheProvider.GetStringAsync(testKey, cancellationToken);
            var getDuration = (DateTime.UtcNow - getStartTime).TotalMilliseconds;

            if (getDuration > _options.MaxOperationDurationMs)
            {
                await TryCleanup(testKey, cancellationToken);
                return HealthCheckResult.Degraded(
                    $"Cache get operation took {getDuration:F2}ms (threshold: {_options.MaxOperationDurationMs}ms)",
                    data: CreateHealthData("get_duration_ms", getDuration));
            }

            if (retrievedValue != testValue)
            {
                await TryCleanup(testKey, cancellationToken);
                return HealthCheckResult.Unhealthy(
                    "Cache get operation returned incorrect value",
                    data: CreateHealthData("expected", testValue, "actual", retrievedValue ?? "null"));
            }

            // Test 3: Delete operation
            var removeStartTime = DateTime.UtcNow;
            await _cacheProvider.RemoveAsync(testKey, cancellationToken);
            var removeDuration = (DateTime.UtcNow - removeStartTime).TotalMilliseconds;

            if (removeDuration > _options.MaxOperationDurationMs)
            {
                return HealthCheckResult.Degraded(
                    $"Cache remove operation took {removeDuration:F2}ms (threshold: {_options.MaxOperationDurationMs}ms)",
                    data: CreateHealthData("remove_duration_ms", removeDuration));
            }

            // Verify key was removed
            var existsAfterRemove = await _cacheProvider.ExistsAsync(testKey, cancellationToken);
            if (existsAfterRemove)
            {
                return HealthCheckResult.Degraded(
                    "Cache remove operation did not remove the key",
                    data: CreateHealthData("key_exists_after_remove", true));
            }

            var totalDuration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var data = new System.Collections.Generic.Dictionary<string, object>
            {
                { "set_duration_ms", setDuration },
                { "get_duration_ms", getDuration },
                { "remove_duration_ms", removeDuration },
                { "total_duration_ms", totalDuration }
            };

            if (_options.IncludeDetailedDiagnostics)
            {
                data.Add("test_key", testKey);
                data.Add("provider_type", _cacheProvider.GetType().Name);
            }

            return HealthCheckResult.Healthy(
                $"Cache is healthy (total duration: {totalDuration:F2}ms)",
                data);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Cache health check failed for key: {TestKey}", testKey);

            await TryCleanup(testKey, cancellationToken);

            return HealthCheckResult.Unhealthy(
                $"Cache health check failed: {ex.Message}",
                ex,
                CreateHealthData("error_type", ex.GetType().Name, "error_message", ex.Message));
        }
    }

    private async Task TryCleanup(string testKey, CancellationToken cancellationToken)
    {
        try
        {
            await _cacheProvider.RemoveAsync(testKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to cleanup test key: {TestKey}", testKey);
        }
    }

    private static System.Collections.Generic.Dictionary<string, object> CreateHealthData(
        params object[] keyValuePairs)
    {
        var data = new System.Collections.Generic.Dictionary<string, object>();

        for (int i = 0; i < keyValuePairs.Length; i += 2)
        {
            if (i + 1 < keyValuePairs.Length)
            {
                data[keyValuePairs[i].ToString()!] = keyValuePairs[i + 1];
            }
        }

        return data;
    }
}

/// <summary>
/// Options for CacheHealthCheck configuration.
/// </summary>
public class CacheHealthCheckOptions
{
    /// <summary>
    /// Maximum allowed duration for a single operation in milliseconds.
    /// Default: 1000ms (1 second).
    /// </summary>
    public double MaxOperationDurationMs { get; set; } = 1000;

    /// <summary>
    /// Prefix for test keys used during health checks.
    /// Default: "health_check_".
    /// </summary>
    public string TestKeyPrefix { get; set; } = "health_check_";

    /// <summary>
    /// Whether to include detailed diagnostics in health check results.
    /// Default: false.
    /// </summary>
    public bool IncludeDetailedDiagnostics { get; set; } = false;
}


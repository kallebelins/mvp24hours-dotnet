//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.HealthChecks
{
    /// <summary>
    /// Health check for distributed cache (Redis) and memory cache.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This health check verifies:
    /// <list type="bullet">
    /// <item>Distributed cache connectivity (if configured)</item>
    /// <item>Memory cache availability (if configured)</item>
    /// <item>Cache read/write operations</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class CacheHealthCheck : BaseHealthCheck
    {
        private readonly IDistributedCache? _distributedCache;
        private readonly IMemoryCache? _memoryCache;
        private readonly CacheHealthCheckOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheHealthCheck"/> class.
        /// </summary>
        /// <param name="distributedCache">The distributed cache instance (optional).</param>
        /// <param name="memoryCache">The memory cache instance (optional).</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">The health check options (optional).</param>
        public CacheHealthCheck(
            IDistributedCache? distributedCache,
            IMemoryCache? memoryCache,
            ILogger<CacheHealthCheck> logger,
            CacheHealthCheckOptions? options = null)
            : base(logger)
        {
            _distributedCache = distributedCache;
            _memoryCache = memoryCache;
            _options = options ?? new CacheHealthCheckOptions();
        }

        /// <inheritdoc />
        protected override async Task<HealthCheckResult> CheckHealthAsyncCore(
            HealthCheckContext context,
            CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, object>();
            var issues = new List<string>();

            // Check distributed cache (Redis)
            if (_distributedCache != null && _options.CheckDistributedCache)
            {
                try
                {
                    var testKey = $"healthcheck_{Guid.NewGuid()}";
                    var testValue = "healthcheck_value";
                    var testBytes = System.Text.Encoding.UTF8.GetBytes(testValue);

                    // Test write
                    await _distributedCache.SetAsync(
                        testKey,
                        testBytes,
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                        },
                        cancellationToken);

                    // Test read
                    var retrievedBytes = await _distributedCache.GetAsync(testKey, cancellationToken);
                    if (retrievedBytes == null)
                    {
                        issues.Add("Distributed cache: Read operation failed");
                    }
                    else
                    {
                        var retrievedValue = System.Text.Encoding.UTF8.GetString(retrievedBytes);
                        if (retrievedValue != testValue)
                        {
                            issues.Add("Distributed cache: Value mismatch");
                        }
                    }

                    // Cleanup
                    await _distributedCache.RemoveAsync(testKey, cancellationToken);

                    data["distributedCache"] = "healthy";
                }
                catch (Exception ex)
                {
                    issues.Add($"Distributed cache: {ex.Message}");
                    data["distributedCache"] = "unhealthy";
                    data["distributedCacheError"] = ex.Message;
                }
            }
            else if (_options.CheckDistributedCache)
            {
                issues.Add("Distributed cache: Not configured");
                data["distributedCache"] = "not_configured";
            }

            // Check memory cache
            if (_memoryCache != null && _options.CheckMemoryCache)
            {
                try
                {
                    var testKey = $"healthcheck_{Guid.NewGuid()}";
                    var testValue = "healthcheck_value";

                    // Test write
                    _memoryCache.Set(testKey, testValue, TimeSpan.FromSeconds(10));

                    // Test read
                    if (!_memoryCache.TryGetValue(testKey, out string? retrievedValue) ||
                        retrievedValue != testValue)
                    {
                        issues.Add("Memory cache: Read operation failed");
                    }

                    // Cleanup
                    _memoryCache.Remove(testKey);

                    data["memoryCache"] = "healthy";
                }
                catch (Exception ex)
                {
                    issues.Add($"Memory cache: {ex.Message}");
                    data["memoryCache"] = "unhealthy";
                    data["memoryCacheError"] = ex.Message;
                }
            }
            else if (_options.CheckMemoryCache)
            {
                issues.Add("Memory cache: Not configured");
                data["memoryCache"] = "not_configured";
            }

            if (issues.Count > 0)
            {
                return HealthCheckResult.Unhealthy(
                    $"Cache health check failed: {string.Join("; ", issues)}",
                    data: GetData(data));
            }

            return HealthCheckResult.Healthy("Cache is healthy", GetData(data));
        }
    }

    /// <summary>
    /// Options for cache health check.
    /// </summary>
    public class CacheHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets whether to check distributed cache (Redis).
        /// Default is true.
        /// </summary>
        public bool CheckDistributedCache { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to check memory cache.
        /// Default is true.
        /// </summary>
        public bool CheckMemoryCache { get; set; } = true;
    }
}


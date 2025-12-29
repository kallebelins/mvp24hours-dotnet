//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.HealthChecks
{
    /// <summary>
    /// Health check for distributed lock providers (Redis, SQL Server, PostgreSQL).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This health check verifies distributed lock provider connectivity by:
    /// <list type="bullet">
    /// <item>Attempting to acquire a test lock</item>
    /// <item>Verifying lock acquisition succeeds within timeout</item>
    /// <item>Releasing the lock to verify cleanup</item>
    /// <item>Measuring lock acquisition time</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHealthChecks()
    ///     .AddDistributedLockHealthCheck(
    ///         "distributed-lock-redis",
    ///         options =>
    ///         {
    ///             options.ProviderName = "Redis";
    ///             options.LockTimeoutSeconds = 5;
    ///         });
    /// </code>
    /// </example>
    public class DistributedLockHealthCheck : IHealthCheck
    {
        private readonly IDistributedLockFactory _lockFactory;
        private readonly DistributedLockHealthCheckOptions _options;
        private readonly ILogger<DistributedLockHealthCheck> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedLockHealthCheck"/> class.
        /// </summary>
        /// <param name="lockFactory">The distributed lock factory.</param>
        /// <param name="options">Health check configuration options.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public DistributedLockHealthCheck(
            IDistributedLockFactory lockFactory,
            DistributedLockHealthCheckOptions? options,
            ILogger<DistributedLockHealthCheck> logger)
        {
            _lockFactory = lockFactory ?? throw new ArgumentNullException(nameof(lockFactory));
            _options = options ?? new DistributedLockHealthCheckOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Get the distributed lock provider
                IDistributedLock distributedLock;
                try
                {
                    if (!string.IsNullOrWhiteSpace(_options.ProviderName))
                    {
                        distributedLock = _lockFactory.Create(_options.ProviderName);
                        data["providerName"] = _options.ProviderName;
                    }
                    else
                    {
                        distributedLock = _lockFactory.Create();
                        data["providerName"] = "default";
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                    data["error"] = $"Failed to create distributed lock: {ex.Message}";

                    _logger.LogError(ex, "Failed to create distributed lock for health check");

                    return HealthCheckResult.Unhealthy(
                        description: $"Failed to create distributed lock: {ex.Message}",
                        exception: ex,
                        data: data);
                }

                // Generate a unique test lock resource name
                var testResourceName = $"health-check-{Guid.NewGuid():N}";
                data["testResourceName"] = testResourceName;

                // Attempt to acquire lock
                var lockOptions = new DistributedLockOptions
                {
                    AcquisitionTimeout = TimeSpan.FromSeconds(_options.LockTimeoutSeconds),
                    LockDuration = TimeSpan.FromSeconds(_options.LockExpirationSeconds)
                };

                var acquisitionStopwatch = Stopwatch.StartNew();
                var acquisitionResult = await distributedLock.TryAcquireAsync(
                    testResourceName,
                    lockOptions,
                    cancellationToken);

                acquisitionStopwatch.Stop();
                stopwatch.Stop();

                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["acquisitionTimeMs"] = acquisitionStopwatch.ElapsedMilliseconds;
                data["acquired"] = acquisitionResult.IsAcquired;

                if (!acquisitionResult.IsAcquired)
                {
                    data["failureReason"] = acquisitionResult.ErrorMessage ?? "Unknown";

                    _logger.LogWarning(
                        "Distributed lock health check failed: Could not acquire lock. Reason: {Reason}",
                        acquisitionResult.ErrorMessage);

                    return HealthCheckResult.Unhealthy(
                        description: $"Failed to acquire distributed lock: {acquisitionResult.ErrorMessage}",
                        data: data);
                }

                // Verify lock handle
                if (acquisitionResult.LockHandle == null)
                {
                    return HealthCheckResult.Unhealthy(
                        description: "Distributed lock acquired but lock handle is null",
                        data: data);
                }

                // Release the lock to verify cleanup
                try
                {
                    await acquisitionResult.LockHandle.ReleaseAsync(cancellationToken);
                    data["released"] = true;
                }
                catch (Exception ex)
                {
                    data["released"] = false;
                    data["releaseError"] = ex.Message;

                    _logger.LogWarning(ex, "Failed to release distributed lock during health check");

                    // Don't fail health check if release fails, but log it
                    return HealthCheckResult.Degraded(
                        description: $"Lock acquired but release failed: {ex.Message}",
                        data: data);
                }

                // Check response time thresholds
                if (stopwatch.ElapsedMilliseconds >= _options.FailureThresholdMs)
                {
                    return HealthCheckResult.Unhealthy(
                        description: $"Distributed lock response time {stopwatch.ElapsedMilliseconds}ms exceeded threshold",
                        data: data);
                }

                if (stopwatch.ElapsedMilliseconds >= _options.DegradedThresholdMs)
                {
                    return HealthCheckResult.Degraded(
                        description: $"Distributed lock response time {stopwatch.ElapsedMilliseconds}ms is slow",
                        data: data);
                }

                return HealthCheckResult.Healthy(
                    description: $"Distributed lock is healthy (acquisition time: {acquisitionStopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;

                _logger.LogError(ex, "Distributed lock health check failed with unexpected error");

                return HealthCheckResult.Unhealthy(
                    description: $"Distributed lock health check failed: {ex.Message}",
                    exception: ex,
                    data: data);
            }
        }
    }

    /// <summary>
    /// Configuration options for distributed lock health checks.
    /// </summary>
    public sealed class DistributedLockHealthCheckOptions
    {
        /// <summary>
        /// Provider name to use. If null or empty, uses the default provider.
        /// </summary>
        public string? ProviderName { get; set; }

        /// <summary>
        /// Timeout in seconds for lock acquisition.
        /// Default is 5 seconds.
        /// </summary>
        public int LockTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// Lock expiration time in seconds.
        /// Default is 10 seconds.
        /// </summary>
        public int LockExpirationSeconds { get; set; } = 10;

        /// <summary>
        /// Response time threshold in milliseconds for degraded status.
        /// Default is 500ms.
        /// </summary>
        public int DegradedThresholdMs { get; set; } = 500;

        /// <summary>
        /// Response time threshold in milliseconds for unhealthy status.
        /// Default is 2000ms.
        /// </summary>
        public int FailureThresholdMs { get; set; } = 2000;

        /// <summary>
        /// Tags to associate with this health check.
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = new[] { "distributed-lock", "locking", "ready" };
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Resilience
{
    /// <summary>
    /// Background service that monitors DbContext pool statistics and logs diagnostics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This monitor provides visibility into DbContext pool usage:
    /// <list type="bullet">
    /// <item>Number of active contexts</item>
    /// <item>Pool hit/miss ratio</item>
    /// <item>Average checkout time</item>
    /// <item>Pool saturation warnings</item>
    /// </list>
    /// </para>
    /// <para>
    /// Enable by registering the monitor in DI:
    /// <code>
    /// services.AddHostedService&lt;DbContextPoolMonitor&gt;();
    /// </code>
    /// </para>
    /// </remarks>
    public class DbContextPoolMonitor : BackgroundService
    {
        private readonly EFCoreResilienceOptions _options;
        private readonly ILogger<DbContextPoolMonitor>? _logger;
        private readonly DbContextPoolStatistics _statistics;

        /// <summary>
        /// Initializes a new instance of <see cref="DbContextPoolMonitor"/>.
        /// </summary>
        public DbContextPoolMonitor(
            Microsoft.Extensions.Options.IOptions<EFCoreResilienceOptions> options,
            ILogger<DbContextPoolMonitor>? logger = null)
        {
            _options = options?.Value ?? new EFCoreResilienceOptions();
            _logger = logger;
            _statistics = new DbContextPoolStatistics();
        }

        /// <summary>
        /// Gets the current pool statistics.
        /// </summary>
        public DbContextPoolStatistics Statistics => _statistics;

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.LogPoolStatistics)
            {
                return;
            }

            _logger?.LogInformation(
                "DbContext pool monitoring started. Interval: {IntervalSeconds}s",
                _options.PoolStatisticsLogIntervalSeconds);

            var interval = TimeSpan.FromSeconds(_options.PoolStatisticsLogIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, stoppingToken);
                    LogPoolStatistics();
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Expected during shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in DbContext pool monitor");
                }
            }

            _logger?.LogInformation("DbContext pool monitoring stopped");
        }

        /// <summary>
        /// Records a successful context checkout from the pool.
        /// </summary>
        public void RecordPoolHit(TimeSpan checkoutTime)
        {
            _statistics.RecordHit(checkoutTime);
        }

        /// <summary>
        /// Records when a new context was created (pool miss).
        /// </summary>
        public void RecordPoolMiss()
        {
            _statistics.RecordMiss();
        }

        /// <summary>
        /// Records when a context is returned to the pool.
        /// </summary>
        public void RecordReturn()
        {
            _statistics.RecordReturn();
        }

        private void LogPoolStatistics()
        {
            var stats = _statistics.GetSnapshot();
            var hitRatio = stats.TotalRequests > 0
                ? (double)stats.PoolHits / stats.TotalRequests * 100
                : 0;

            _logger?.LogInformation(
                "DbContext Pool Stats - Active: {ActiveContexts}, Hits: {PoolHits}, Misses: {PoolMisses}, " +
                "Hit Ratio: {HitRatio:F1}%, Avg Checkout: {AvgCheckoutMs:F2}ms",
                stats.ActiveContexts,
                stats.PoolHits,
                stats.PoolMisses,
                hitRatio,
                stats.AverageCheckoutTimeMs);

            // Warn if hit ratio is low
            if (stats.TotalRequests > 100 && hitRatio < 50)
            {
                _logger?.LogWarning(
                    "Low DbContext pool hit ratio ({HitRatio:F1}%). Consider increasing pool size from {CurrentSize}.",
                    hitRatio,
                    _options.PoolSize);
            }
        }
    }

    /// <summary>
    /// Statistics about DbContext pool usage.
    /// </summary>
    public class DbContextPoolStatistics
    {
        private long _poolHits;
        private long _poolMisses;
        private long _activeContexts;
        private long _totalCheckoutTimeMs;
        private long _checkoutCount;
        private readonly object _lock = new object();

        /// <summary>
        /// Gets the number of successful pool checkouts.
        /// </summary>
        public long PoolHits => Interlocked.Read(ref _poolHits);

        /// <summary>
        /// Gets the number of pool misses (new context created).
        /// </summary>
        public long PoolMisses => Interlocked.Read(ref _poolMisses);

        /// <summary>
        /// Gets the total number of pool requests.
        /// </summary>
        public long TotalRequests => PoolHits + PoolMisses;

        /// <summary>
        /// Gets the current number of active (checked out) contexts.
        /// </summary>
        public long ActiveContexts => Interlocked.Read(ref _activeContexts);

        /// <summary>
        /// Gets the average checkout time in milliseconds.
        /// </summary>
        public double AverageCheckoutTimeMs
        {
            get
            {
                var count = Interlocked.Read(ref _checkoutCount);
                if (count == 0) return 0;
                return (double)Interlocked.Read(ref _totalCheckoutTimeMs) / count;
            }
        }

        /// <summary>
        /// Records a pool hit.
        /// </summary>
        public void RecordHit(TimeSpan checkoutTime)
        {
            Interlocked.Increment(ref _poolHits);
            Interlocked.Increment(ref _activeContexts);
            Interlocked.Add(ref _totalCheckoutTimeMs, (long)checkoutTime.TotalMilliseconds);
            Interlocked.Increment(ref _checkoutCount);
        }

        /// <summary>
        /// Records a pool miss.
        /// </summary>
        public void RecordMiss()
        {
            Interlocked.Increment(ref _poolMisses);
            Interlocked.Increment(ref _activeContexts);
        }

        /// <summary>
        /// Records a context return to pool.
        /// </summary>
        public void RecordReturn()
        {
            Interlocked.Decrement(ref _activeContexts);
        }

        /// <summary>
        /// Gets a snapshot of current statistics.
        /// </summary>
        public PoolStatisticsSnapshot GetSnapshot()
        {
            return new PoolStatisticsSnapshot
            {
                PoolHits = PoolHits,
                PoolMisses = PoolMisses,
                TotalRequests = TotalRequests,
                ActiveContexts = ActiveContexts,
                AverageCheckoutTimeMs = AverageCheckoutTimeMs
            };
        }

        /// <summary>
        /// Resets all statistics.
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _poolHits = 0;
                _poolMisses = 0;
                _activeContexts = 0;
                _totalCheckoutTimeMs = 0;
                _checkoutCount = 0;
            }
        }
    }

    /// <summary>
    /// Immutable snapshot of pool statistics.
    /// </summary>
    public class PoolStatisticsSnapshot
    {
        /// <summary>
        /// Gets the number of pool hits.
        /// </summary>
        public long PoolHits { get; init; }

        /// <summary>
        /// Gets the number of pool misses.
        /// </summary>
        public long PoolMisses { get; init; }

        /// <summary>
        /// Gets the total number of requests.
        /// </summary>
        public long TotalRequests { get; init; }

        /// <summary>
        /// Gets the number of active contexts.
        /// </summary>
        public long ActiveContexts { get; init; }

        /// <summary>
        /// Gets the average checkout time in milliseconds.
        /// </summary>
        public double AverageCheckoutTimeMs { get; init; }
    }
}


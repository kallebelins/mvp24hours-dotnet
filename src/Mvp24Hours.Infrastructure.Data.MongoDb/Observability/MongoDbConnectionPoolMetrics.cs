//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Observability
{
    /// <summary>
    /// Collects and reports MongoDB connection pool metrics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Monitors connection pool health including:
    /// <list type="bullet">
    ///   <item>Pool size and utilization</item>
    ///   <item>Checkout durations and failures</item>
    ///   <item>Connection creation and closure rates</item>
    ///   <item>Wait queue depth</item>
    ///   <item>Health alerts for high utilization</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var metrics = serviceProvider.GetRequiredService&lt;MongoDbConnectionPoolMetrics&gt;();
    /// 
    /// // Get current stats
    /// var stats = metrics.GetCurrentStats();
    /// Console.WriteLine($"Pool utilization: {stats.Utilization:P2}");
    /// Console.WriteLine($"Available: {stats.AvailableCount}/{stats.MaxSize}");
    /// 
    /// // Start periodic collection
    /// metrics.StartPeriodicCollection(cancellationToken);
    /// </code>
    /// </example>
    public class MongoDbConnectionPoolMetrics : IDisposable
    {
        private readonly MongoDbObservabilityOptions _options;
        private readonly ILogger<MongoDbConnectionPoolMetrics> _logger;
        private readonly IMongoDbMetrics _metrics;

        // Per-server statistics
        private readonly ConcurrentDictionary<string, ServerPoolStats> _serverStats = new();

        // Checkout tracking
        private readonly ConcurrentDictionary<long, Stopwatch> _pendingCheckouts = new();

        // Alerts
        private DateTime _lastAlertTime = DateTime.MinValue;
        private readonly TimeSpan _alertCooldown = TimeSpan.FromMinutes(1);

        private Timer _collectionTimer;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbConnectionPoolMetrics"/> class.
        /// </summary>
        /// <param name="options">The observability options.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="metrics">Optional metrics collector.</param>
        public MongoDbConnectionPoolMetrics(
            IOptions<MongoDbObservabilityOptions> options,
            ILogger<MongoDbConnectionPoolMetrics> logger = null,
            IMongoDbMetrics metrics = null)
        {
            _options = options?.Value ?? new MongoDbObservabilityOptions();
            _logger = logger;
            _metrics = metrics;
        }

        /// <summary>
        /// Configures the MongoDB cluster builder to monitor connection pool events.
        /// </summary>
        /// <param name="settings">The MongoDB client settings.</param>
        public void ConfigureClusterBuilder(MongoClientSettings settings)
        {
            if (!_options.EnableConnectionPoolMetrics)
                return;

            var existingConfigurator = settings.ClusterConfigurator;

            settings.ClusterConfigurator = builder =>
            {
                existingConfigurator?.Invoke(builder);

                // Pool lifecycle events
                builder.Subscribe<ConnectionPoolOpenedEvent>(OnPoolOpened);
                builder.Subscribe<ConnectionPoolClosedEvent>(OnPoolClosed);

                // Connection lifecycle events
                builder.Subscribe<ConnectionCreatedEvent>(OnConnectionCreated);
                builder.Subscribe<ConnectionClosedEvent>(OnConnectionClosed);

                // Checkout events
                builder.Subscribe<ConnectionPoolCheckingOutConnectionEvent>(OnCheckoutStarted);
                builder.Subscribe<ConnectionPoolCheckedOutConnectionEvent>(OnCheckoutSucceeded);
                builder.Subscribe<ConnectionPoolCheckingOutConnectionFailedEvent>(OnCheckoutFailed);
                builder.Subscribe<ConnectionPoolCheckedInConnectionEvent>(OnCheckedIn);

                // Pool state events
                builder.Subscribe<ConnectionPoolReadyEvent>(OnPoolReady);
                builder.Subscribe<ConnectionPoolClearedEvent>(OnPoolCleared);
            };
        }

        #region Event Handlers

        private void OnPoolOpened(ConnectionPoolOpenedEvent e)
        {
            var endpoint = e.ServerId?.EndPoint?.ToString() ?? "unknown";
            var stats = GetOrCreateStats(endpoint);
            stats.MaxSize = e.ConnectionPoolSettings?.MaxConnections ?? 100;
            stats.MinSize = e.ConnectionPoolSettings?.MinConnections ?? 0;
            stats.IsOpen = true;

            LogDebug("Connection pool opened for {Endpoint}, MaxSize: {MaxSize}", endpoint, stats.MaxSize);
        }

        private void OnPoolClosed(ConnectionPoolClosedEvent e)
        {
            var endpoint = e.ServerId?.EndPoint?.ToString() ?? "unknown";
            if (_serverStats.TryGetValue(endpoint, out var stats))
            {
                stats.IsOpen = false;
            }

            LogDebug("Connection pool closed for {Endpoint}", endpoint);
        }

        private void OnConnectionCreated(ConnectionCreatedEvent e)
        {
            var endpoint = e.ServerId?.EndPoint?.ToString() ?? "unknown";
            var stats = GetOrCreateStats(endpoint);
            Interlocked.Increment(ref stats.TotalCreated);
            Interlocked.Increment(ref stats.CurrentSize);

            LogDebug("Connection created for {Endpoint}, Total: {Total}", endpoint, stats.CurrentSize);
        }

        private void OnConnectionClosed(ConnectionClosedEvent e)
        {
            var endpoint = e.ServerId?.EndPoint?.ToString() ?? "unknown";
            var stats = GetOrCreateStats(endpoint);
            Interlocked.Increment(ref stats.TotalClosed);
            Interlocked.Decrement(ref stats.CurrentSize);

            LogDebug("Connection closed for {Endpoint}, Remaining: {Remaining}", endpoint, stats.CurrentSize);
        }

        private void OnCheckoutStarted(ConnectionPoolCheckingOutConnectionEvent e)
        {
            if (e.OperationId.HasValue)
            {
                _pendingCheckouts.TryAdd(e.OperationId.Value, Stopwatch.StartNew());
            }
        }

        private void OnCheckoutSucceeded(ConnectionPoolCheckedOutConnectionEvent e)
        {
            if (e.OperationId.HasValue && _pendingCheckouts.TryRemove(e.OperationId.Value, out var sw))
            {
                sw.Stop();
                _metrics?.RecordConnectionCheckoutDuration(sw.Elapsed);
            }

            var endpoint = e.ServerId?.EndPoint?.ToString() ?? "unknown";
            var stats = GetOrCreateStats(endpoint);
            Interlocked.Increment(ref stats.TotalCheckouts);
            Interlocked.Increment(ref stats.InUseCount);
            Interlocked.Decrement(ref stats.AvailableCount);

            // Check for high utilization
            CheckUtilizationAlert(endpoint, stats);
        }

        private void OnCheckoutFailed(ConnectionPoolCheckingOutConnectionFailedEvent e)
        {
            if (e.OperationId.HasValue)
            {
                _pendingCheckouts.TryRemove(e.OperationId.Value, out _);
            }

            var endpoint = e.ServerId?.EndPoint?.ToString() ?? "unknown";
            var stats = GetOrCreateStats(endpoint);
            Interlocked.Increment(ref stats.TotalCheckoutFailures);

            LogWarning("Connection checkout failed for {Endpoint}, Reason: {Reason}", endpoint, e.Reason);
        }

        private void OnCheckedIn(ConnectionPoolCheckedInConnectionEvent e)
        {
            var endpoint = e.ServerId?.EndPoint?.ToString() ?? "unknown";
            var stats = GetOrCreateStats(endpoint);
            Interlocked.Decrement(ref stats.InUseCount);
            Interlocked.Increment(ref stats.AvailableCount);
        }

        private void OnPoolReady(ConnectionPoolReadyEvent e)
        {
            var endpoint = e.ServerId?.EndPoint?.ToString() ?? "unknown";
            LogDebug("Connection pool ready for {Endpoint}", endpoint);
        }

        private void OnPoolCleared(ConnectionPoolClearedEvent e)
        {
            var endpoint = e.ServerId?.EndPoint?.ToString() ?? "unknown";
            var stats = GetOrCreateStats(endpoint);

            // Reset counters after pool clear
            stats.AvailableCount = 0;
            stats.InUseCount = 0;

            LogWarning("Connection pool cleared for {Endpoint}", endpoint);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets current connection pool statistics for all servers.
        /// </summary>
        /// <returns>A list of connection pool stats.</returns>
        public IReadOnlyList<ConnectionPoolStats> GetAllStats()
        {
            return _serverStats.Select(kvp => CreateStats(kvp.Key, kvp.Value)).ToList();
        }

        /// <summary>
        /// Gets current connection pool statistics for a specific server.
        /// </summary>
        /// <param name="endpoint">The server endpoint.</param>
        /// <returns>The connection pool stats, or null if not found.</returns>
        public ConnectionPoolStats GetStats(string endpoint)
        {
            if (_serverStats.TryGetValue(endpoint, out var stats))
            {
                return CreateStats(endpoint, stats);
            }
            return null;
        }

        /// <summary>
        /// Gets the current aggregate stats (sum across all servers).
        /// </summary>
        /// <returns>The aggregate connection pool stats.</returns>
        public ConnectionPoolStats GetCurrentStats()
        {
            var aggregate = new ConnectionPoolStats
            {
                Endpoint = "aggregate",
                Timestamp = DateTimeOffset.UtcNow
            };

            foreach (var stats in _serverStats.Values)
            {
                aggregate.CurrentSize += stats.CurrentSize;
                aggregate.AvailableCount += stats.AvailableCount;
                aggregate.InUseCount += stats.InUseCount;
                aggregate.MaxSize += stats.MaxSize;
                aggregate.MinSize += stats.MinSize;
                aggregate.TotalConnectionsCreated += stats.TotalCreated;
                aggregate.TotalConnectionsClosed += stats.TotalClosed;
                aggregate.TotalCheckoutFailures += stats.TotalCheckoutFailures;
            }

            // Report to metrics
            _metrics?.RecordConnectionPoolStats(aggregate);

            return aggregate;
        }

        /// <summary>
        /// Starts periodic collection of connection pool metrics.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void StartPeriodicCollection(CancellationToken cancellationToken = default)
        {
            if (_collectionTimer != null)
                return;

            _collectionTimer = new Timer(
                CollectMetrics,
                null,
                TimeSpan.Zero,
                _options.ConnectionPoolMetricsInterval);

            cancellationToken.Register(() =>
            {
                _collectionTimer?.Change(Timeout.Infinite, 0);
                _collectionTimer?.Dispose();
                _collectionTimer = null;
            });
        }

        /// <summary>
        /// Stops periodic collection.
        /// </summary>
        public void StopPeriodicCollection()
        {
            _collectionTimer?.Change(Timeout.Infinite, 0);
            _collectionTimer?.Dispose();
            _collectionTimer = null;
        }

        #endregion

        #region Private Methods

        private ServerPoolStats GetOrCreateStats(string endpoint)
        {
            return _serverStats.GetOrAdd(endpoint, _ => new ServerPoolStats());
        }

        private static ConnectionPoolStats CreateStats(string endpoint, ServerPoolStats stats)
        {
            return new ConnectionPoolStats
            {
                Endpoint = endpoint,
                CurrentSize = stats.CurrentSize,
                AvailableCount = stats.AvailableCount,
                InUseCount = stats.InUseCount,
                MaxSize = stats.MaxSize,
                MinSize = stats.MinSize,
                TotalConnectionsCreated = stats.TotalCreated,
                TotalConnectionsClosed = stats.TotalClosed,
                TotalCheckoutFailures = stats.TotalCheckoutFailures,
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        private void CheckUtilizationAlert(string endpoint, ServerPoolStats stats)
        {
            if (!_options.EnableConnectionPoolAlerts)
                return;

            var utilization = stats.MaxSize > 0 ? (double)stats.InUseCount / stats.MaxSize : 0;

            if (utilization >= _options.ConnectionPoolAlertThreshold)
            {
                var now = DateTime.UtcNow;
                if (now - _lastAlertTime >= _alertCooldown)
                {
                    _lastAlertTime = now;

                    LogWarning(
                        "⚠️ HIGH CONNECTION POOL UTILIZATION - Endpoint: {Endpoint}, " +
                        "InUse: {InUse}/{Max} ({Utilization:P2})",
                        endpoint, stats.InUseCount, stats.MaxSize, utilization);
                }
            }
        }

        private void CollectMetrics(object state)
        {
            try
            {
                var stats = GetCurrentStats();

                LogDebug(
                    "Connection pool metrics - Size: {Size}, InUse: {InUse}, Available: {Available}, " +
                    "Utilization: {Utilization:P2}",
                    stats.CurrentSize, stats.InUseCount, stats.AvailableCount, stats.Utilization);
            }
            catch (Exception ex)
            {
                LogWarning("Error collecting connection pool metrics: {Error}", ex.Message);
            }
        }

        private void LogDebug(string message, params object[] args)
        {
            _logger?.LogDebug(message, args);
        }

        private void LogWarning(string message, params object[] args)
        {
            _logger?.LogWarning(message, args);
        }

        #endregion

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopPeriodicCollection();
            _pendingCheckouts.Clear();
            _serverStats.Clear();
            GC.SuppressFinalize(this);
        }

        #region Internal Classes

        private class ServerPoolStats
        {
            public int CurrentSize;
            public int AvailableCount;
            public int InUseCount;
            public int MaxSize;
            public int MinSize;
            public long TotalCreated;
            public long TotalClosed;
            public long TotalCheckouts;
            public long TotalCheckoutFailures;
            public bool IsOpen;
        }

        #endregion
    }
}


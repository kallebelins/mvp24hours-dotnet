//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency
{
    /// <summary>
    /// Manages MongoDB connections with automatic recovery, health monitoring, and failover support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This manager provides enterprise-grade connection management:
    /// <list type="bullet">
    ///   <item><b>Auto-Recovery</b>: Automatically reconnects after connection loss</item>
    ///   <item><b>Health Monitoring</b>: Tracks connection state and server health</item>
    ///   <item><b>Failover Support</b>: Handles replica set failover gracefully</item>
    ///   <item><b>Event Tracking</b>: Monitors connection lifecycle events</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class MongoDbConnectionManager : IDisposable
    {
        private readonly MongoDbResiliencyOptions _options;
        private readonly Random _random = new();
        private readonly SemaphoreSlim _reconnectLock = new(1, 1);

        private bool _isConnected;
        private int _reconnectAttempts;
        private DateTimeOffset? _lastConnectionTime;
        private DateTimeOffset? _lastDisconnectionTime;
        private bool _isDisposed;

        /// <summary>
        /// Occurs when the connection state changes.
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        /// Occurs when a reconnection attempt is made.
        /// </summary>
        public event EventHandler<ReconnectAttemptEventArgs> ReconnectAttempt;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbConnectionManager"/> class.
        /// </summary>
        /// <param name="options">The resiliency options.</param>
        public MongoDbConnectionManager(MongoDbResiliencyOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Gets whether the connection is currently active.
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Gets the timestamp of the last successful connection.
        /// </summary>
        public DateTimeOffset? LastConnectionTime => _lastConnectionTime;

        /// <summary>
        /// Gets the timestamp of the last disconnection.
        /// </summary>
        public DateTimeOffset? LastDisconnectionTime => _lastDisconnectionTime;

        /// <summary>
        /// Gets the number of reconnection attempts since the last successful connection.
        /// </summary>
        public int ReconnectAttempts => _reconnectAttempts;

        /// <summary>
        /// Configures the MongoDB client settings with connection event handlers.
        /// </summary>
        /// <param name="settings">The MongoDB client settings.</param>
        public void ConfigureClientSettings(MongoClientSettings settings)
        {
            if (settings == null) return;

            // Apply failover settings
            if (_options.EnableAutomaticFailover)
            {
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(_options.ServerSelectionTimeoutSeconds);
                settings.HeartbeatInterval = TimeSpan.FromSeconds(_options.HeartbeatFrequencySeconds);

                // Allow reads from secondaries when no primary
                if (_options.AllowReadsWithoutPrimary)
                {
                    settings.ReadPreference = ReadPreference.SecondaryPreferred;
                }
            }

            // Configure event subscribers for monitoring
            if (_options.EnableServerMonitoring)
            {
                var existingConfigurator = settings.ClusterConfigurator;
                settings.ClusterConfigurator = builder =>
                {
                    // Invoke existing configurator if any
                    existingConfigurator?.Invoke(builder);

                    // Subscribe to connection pool events
                    builder.Subscribe<ConnectionPoolOpenedEvent>(OnConnectionPoolOpened);
                    builder.Subscribe<ConnectionPoolClosedEvent>(OnConnectionPoolClosed);
                    builder.Subscribe<ConnectionOpenedEvent>(OnConnectionOpened);
                    builder.Subscribe<ConnectionClosedEvent>(OnConnectionClosed);
                    builder.Subscribe<ConnectionFailedEvent>(OnConnectionFailed);

                    // Subscribe to server events
                    builder.Subscribe<ServerHeartbeatSucceededEvent>(OnServerHeartbeatSucceeded);
                    builder.Subscribe<ServerHeartbeatFailedEvent>(OnServerHeartbeatFailed);
                    builder.Subscribe<ServerDescriptionChangedEvent>(OnServerDescriptionChanged);
                };
            }
        }

        /// <summary>
        /// Attempts to reconnect to MongoDB using exponential backoff.
        /// </summary>
        /// <param name="testConnectionFunc">Function to test the connection.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if reconnection was successful; otherwise false.</returns>
        public async Task<bool> TryReconnectAsync(
            Func<CancellationToken, Task<bool>> testConnectionFunc,
            CancellationToken cancellationToken = default)
        {
            if (!_options.EnableAutoReconnect)
            {
                return false;
            }

            // Only allow one reconnection attempt at a time
            if (!await _reconnectLock.WaitAsync(0, cancellationToken))
            {
                return false;
            }

            try
            {
                var startTime = DateTimeOffset.UtcNow;

                for (int attempt = 1; attempt <= _options.MaxReconnectAttempts; attempt++)
                {
                    _reconnectAttempts = attempt;

                    OnReconnectAttempt(new ReconnectAttemptEventArgs(
                        attempt,
                        _options.MaxReconnectAttempts,
                        DateTimeOffset.UtcNow - startTime));

                    try
                    {
                        if (await testConnectionFunc(cancellationToken))
                        {
                            OnConnectionEstablished();
                            return true;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        if (_options.LogConnectionEvents)
                        {
                            TelemetryHelper.Execute(TelemetryLevels.Warning,
                                "mongodb-reconnect-attempt-failed",
                                new
                                {
                                    Attempt = attempt,
                                    MaxAttempts = _options.MaxReconnectAttempts,
                                    ExceptionType = ex.GetType().Name,
                                    Message = ex.Message
                                });
                        }
                    }

                    if (attempt < _options.MaxReconnectAttempts)
                    {
                        var delay = CalculateReconnectDelay(attempt);
                        await Task.Delay(delay, cancellationToken);
                    }
                }

                var totalDuration = DateTimeOffset.UtcNow - startTime;

                if (_options.LogConnectionEvents)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Error,
                        "mongodb-reconnect-failed",
                        new
                        {
                            TotalAttempts = _options.MaxReconnectAttempts,
                            TotalDuration = totalDuration.TotalMilliseconds
                        });
                }

                return false;
            }
            finally
            {
                _reconnectLock.Release();
            }
        }

        /// <summary>
        /// Marks the connection as established.
        /// </summary>
        public void OnConnectionEstablished()
        {
            var wasConnected = _isConnected;
            _isConnected = true;
            _lastConnectionTime = DateTimeOffset.UtcNow;
            _reconnectAttempts = 0;

            if (!wasConnected)
            {
                OnConnectionStateChanged(new ConnectionStateChangedEventArgs(
                    isConnected: true,
                    previousState: wasConnected,
                    timestamp: DateTimeOffset.UtcNow));

                if (_options.LogConnectionEvents)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Information,
                        "mongodb-connection-established",
                        new { Timestamp = DateTimeOffset.UtcNow });
                }
            }
        }

        /// <summary>
        /// Marks the connection as lost.
        /// </summary>
        /// <param name="reason">The reason for disconnection.</param>
        public void OnConnectionLost(string reason = null)
        {
            var wasConnected = _isConnected;
            _isConnected = false;
            _lastDisconnectionTime = DateTimeOffset.UtcNow;

            if (wasConnected)
            {
                OnConnectionStateChanged(new ConnectionStateChangedEventArgs(
                    isConnected: false,
                    previousState: wasConnected,
                    timestamp: DateTimeOffset.UtcNow,
                    reason: reason));

                if (_options.LogConnectionEvents)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Warning,
                        "mongodb-connection-lost",
                        new
                        {
                            Reason = reason ?? "Unknown",
                            Timestamp = DateTimeOffset.UtcNow
                        });
                }
            }
        }

        private TimeSpan CalculateReconnectDelay(int attempt)
        {
            double delay = _options.ReconnectDelayMilliseconds;

            if (_options.UseExponentialBackoffForReconnect)
            {
                delay *= Math.Pow(2, attempt - 1);
            }

            // Apply jitter
            if (_options.ReconnectJitterFactor > 0)
            {
                var jitter = delay * _options.ReconnectJitterFactor;
                var randomJitter = (_random.NextDouble() * 2 - 1) * jitter;
                delay += randomJitter;
            }

            // Cap at maximum
            delay = Math.Min(delay, _options.MaxReconnectDelayMilliseconds);

            return TimeSpan.FromMilliseconds(delay);
        }

        #region Event Handlers

        private void OnConnectionPoolOpened(ConnectionPoolOpenedEvent e)
        {
            OnConnectionEstablished();
        }

        private void OnConnectionPoolClosed(ConnectionPoolClosedEvent e)
        {
            OnConnectionLost("Connection pool closed");
        }

        private void OnConnectionOpened(ConnectionOpenedEvent e)
        {
            if (_options.LogConnectionEvents)
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose,
                    "mongodb-connection-opened",
                    new
                    {
                        ConnectionId = e.ConnectionId?.ToString(),
                        Duration = e.Duration.TotalMilliseconds
                    });
            }
        }

        private void OnConnectionClosed(ConnectionClosedEvent e)
        {
            if (_options.LogConnectionEvents)
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose,
                    "mongodb-connection-closed",
                    new
                    {
                        ConnectionId = e.ConnectionId?.ToString(),
                        Duration = e.Duration.TotalMilliseconds
                    });
            }
        }

        private void OnConnectionFailed(ConnectionFailedEvent e)
        {
            OnConnectionLost(e.Exception?.Message ?? "Connection failed");

            if (_options.LogConnectionEvents)
            {
                TelemetryHelper.Execute(TelemetryLevels.Warning,
                    "mongodb-connection-failed",
                    new
                    {
                        ConnectionId = e.ConnectionId?.ToString(),
                        ExceptionType = e.Exception?.GetType().Name,
                        Message = e.Exception?.Message
                    });
            }
        }

        private void OnServerHeartbeatSucceeded(ServerHeartbeatSucceededEvent e)
        {
            // Server is responding, ensure we're marked as connected
            if (!_isConnected)
            {
                OnConnectionEstablished();
            }
        }

        private void OnServerHeartbeatFailed(ServerHeartbeatFailedEvent e)
        {
            if (_options.LogConnectionEvents)
            {
                TelemetryHelper.Execute(TelemetryLevels.Warning,
                    "mongodb-heartbeat-failed",
                    new
                    {
                        Duration = e.Duration.TotalMilliseconds,
                        ExceptionType = e.Exception?.GetType().Name
                    });
            }
        }

        private void OnServerDescriptionChanged(ServerDescriptionChangedEvent e)
        {
            var newDesc = e.NewDescription;
            var oldDesc = e.OldDescription;

            // Check for failover events
            if (oldDesc?.Type == MongoDB.Driver.Core.Servers.ServerType.ReplicaSetPrimary &&
                newDesc?.Type != MongoDB.Driver.Core.Servers.ServerType.ReplicaSetPrimary)
            {
                if (_options.LogConnectionEvents)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Warning,
                        "mongodb-primary-changed",
                        new
                        {
                            OldPrimary = oldDesc?.EndPoint?.ToString(),
                            NewState = newDesc?.Type.ToString()
                        });
                }
            }

            // Log server state changes
            if (oldDesc?.State != newDesc?.State)
            {
                if (_options.LogConnectionEvents)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Information,
                        "mongodb-server-state-changed",
                        new
                        {
                            Server = newDesc?.EndPoint?.ToString(),
                            OldState = oldDesc?.State.ToString(),
                            NewState = newDesc?.State.ToString(),
                            Type = newDesc?.Type.ToString()
                        });
                }
            }
        }

        #endregion

        #region Events

        private void OnConnectionStateChanged(ConnectionStateChangedEventArgs e)
        {
            ConnectionStateChanged?.Invoke(this, e);
        }

        private void OnReconnectAttempt(ReconnectAttemptEventArgs e)
        {
            ReconnectAttempt?.Invoke(this, e);
        }

        #endregion

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _reconnectLock.Dispose();
        }
    }

    /// <summary>
    /// Event arguments for connection state changes.
    /// </summary>
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets whether the connection is now active.
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Gets the previous connection state.
        /// </summary>
        public bool PreviousState { get; }

        /// <summary>
        /// Gets the timestamp of the state change.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the reason for the state change, if applicable.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStateChangedEventArgs"/> class.
        /// </summary>
        public ConnectionStateChangedEventArgs(bool isConnected, bool previousState, DateTimeOffset timestamp, string reason = null)
        {
            IsConnected = isConnected;
            PreviousState = previousState;
            Timestamp = timestamp;
            Reason = reason;
        }
    }

    /// <summary>
    /// Event arguments for reconnection attempts.
    /// </summary>
    public class ReconnectAttemptEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the current attempt number.
        /// </summary>
        public int Attempt { get; }

        /// <summary>
        /// Gets the maximum number of attempts.
        /// </summary>
        public int MaxAttempts { get; }

        /// <summary>
        /// Gets the total duration spent on reconnection attempts.
        /// </summary>
        public TimeSpan TotalDuration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReconnectAttemptEventArgs"/> class.
        /// </summary>
        public ReconnectAttemptEventArgs(int attempt, int maxAttempts, TimeSpan totalDuration)
        {
            Attempt = attempt;
            MaxAttempts = maxAttempts;
            TotalDuration = totalDuration;
        }
    }
}


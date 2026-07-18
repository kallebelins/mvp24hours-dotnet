//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency;

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
/// <remarks>
/// Initializes a new instance of the <see cref="MongoDbConnectionManager"/> class.
/// </remarks>
/// <param name="options">The resiliency options.</param>
public sealed class MongoDbConnectionManager(MongoDbResiliencyOptions options) : IDisposable
{
    private readonly MongoDbResiliencyOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly Random _random = new();
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);
    private bool _isDisposed;

    /// <summary>
    /// Occurs when the connection state changes.
    /// </summary>
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// Occurs when a reconnection attempt is made.
    /// </summary>
    public event EventHandler<ReconnectAttemptEventArgs>? ReconnectAttempt;

    /// <summary>
    /// Gets whether the connection is currently active.
    /// </summary>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last successful connection.
    /// </summary>
    public DateTimeOffset? LastConnectionTime { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last disconnection.
    /// </summary>
    public DateTimeOffset? LastDisconnectionTime { get; private set; }

    /// <summary>
    /// Gets the number of reconnection attempts since the last successful connection.
    /// </summary>
    public int ReconnectAttempts { get; private set; }

    /// <summary>
    /// Configures the MongoDB client settings with connection event handlers.
    /// </summary>
    /// <param name="settings">The MongoDB client settings.</param>
    public void ConfigureClientSettings(MongoClientSettings settings)
    {
        if (settings == null)
        {
            return;
        }

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
            Action<ClusterBuilder> existingConfigurator = settings.ClusterConfigurator;
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
            DateTimeOffset startTime = DateTimeOffset.UtcNow;

            for (int attempt = 1; attempt <= _options.MaxReconnectAttempts; attempt++)
            {
                ReconnectAttempts = attempt;

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
                catch (Exception)
                {
                }

                if (attempt < _options.MaxReconnectAttempts)
                {
                    TimeSpan delay = CalculateReconnectDelay(attempt);
                    await Task.Delay(delay, cancellationToken);
                }
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
        bool wasConnected = IsConnected;
        IsConnected = true;
        LastConnectionTime = DateTimeOffset.UtcNow;
        ReconnectAttempts = 0;

        if (!wasConnected)
        {
            OnConnectionStateChanged(new ConnectionStateChangedEventArgs(
                isConnected: true,
                previousState: wasConnected,
                timestamp: DateTimeOffset.UtcNow));
        }
    }

    /// <summary>
    /// Marks the connection as lost.
    /// </summary>
    /// <param name="reason">The reason for disconnection.</param>
    public void OnConnectionLost(string? reason = null)
    {
        bool wasConnected = IsConnected;
        IsConnected = false;
        LastDisconnectionTime = DateTimeOffset.UtcNow;

        if (wasConnected)
        {
            OnConnectionStateChanged(new ConnectionStateChangedEventArgs(
                isConnected: false,
                previousState: wasConnected,
                timestamp: DateTimeOffset.UtcNow,
                reason: reason));
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
            double jitter = delay * _options.ReconnectJitterFactor;
            double randomJitter = (_random.NextDouble() * 2 - 1) * jitter;
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
    }

    private void OnConnectionClosed(ConnectionClosedEvent e)
    {
    }

    private void OnConnectionFailed(ConnectionFailedEvent e)
    {
        OnConnectionLost(e.Exception?.Message ?? "Connection failed");
    }

    private void OnServerHeartbeatSucceeded(ServerHeartbeatSucceededEvent e)
    {
        // Server is responding, ensure we're marked as connected
        if (!IsConnected)
        {
            OnConnectionEstablished();
        }
    }

    private void OnServerHeartbeatFailed(ServerHeartbeatFailedEvent e)
    {
    }

    private void OnServerDescriptionChanged(ServerDescriptionChangedEvent e)
    {
        ServerDescription? newDesc = e.NewDescription;
        ServerDescription? oldDesc = e.OldDescription;

        // Check for failover events
        if (oldDesc?.Type == MongoDB.Driver.Core.Servers.ServerType.ReplicaSetPrimary &&
            newDesc?.Type != MongoDB.Driver.Core.Servers.ServerType.ReplicaSetPrimary)
        {
        }

        // Log server state changes
        if (oldDesc?.State != newDesc?.State)
        {
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
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _reconnectLock.Dispose();
    }
}

/// <summary>
/// Event arguments for connection state changes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConnectionStateChangedEventArgs"/> class.
/// </remarks>
public class ConnectionStateChangedEventArgs(bool isConnected, bool previousState, DateTimeOffset timestamp, string? reason = null) : EventArgs
{
    /// <summary>
    /// Gets whether the connection is now active.
    /// </summary>
    public bool IsConnected { get; } = isConnected;

    /// <summary>
    /// Gets the previous connection state.
    /// </summary>
    public bool PreviousState { get; } = previousState;

    /// <summary>
    /// Gets the timestamp of the state change.
    /// </summary>
    public DateTimeOffset Timestamp { get; } = timestamp;

    /// <summary>
    /// Gets the reason for the state change, if applicable.
    /// </summary>
    public string? Reason { get; } = reason;
}

/// <summary>
/// Event arguments for reconnection attempts.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReconnectAttemptEventArgs"/> class.
/// </remarks>
public class ReconnectAttemptEventArgs(int attempt, int maxAttempts, TimeSpan totalDuration) : EventArgs
{
    /// <summary>
    /// Gets the current attempt number.
    /// </summary>
    public int Attempt { get; } = attempt;

    /// <summary>
    /// Gets the maximum number of attempts.
    /// </summary>
    public int MaxAttempts { get; } = maxAttempts;

    /// <summary>
    /// Gets the total duration spent on reconnection attempts.
    /// </summary>
    public TimeSpan TotalDuration { get; } = totalDuration;
}


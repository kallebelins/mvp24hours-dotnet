//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mvp24Hours.Infrastructure.RabbitMQ.Metrics;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Observability;

/// <summary>
/// Interface for RabbitMQ diagnostics, providing health status and operational metrics.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a comprehensive view of the RabbitMQ connection and operation status.
/// It can be used for health checks, monitoring dashboards, and operational alerting.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DiagnosticsEndpoint
/// {
///     private readonly IRabbitMQDiagnostics _diagnostics;
///
///     public async Task&lt;IActionResult&gt; GetDiagnostics()
///     {
///         var status = await _diagnostics.GetStatusAsync();
///         return Ok(new
///         {
///             IsHealthy = status.IsHealthy,
///             Connection = status.ConnectionStatus,
///             Metrics = status.Metrics,
///             LastError = status.LastError?.Message
///         });
///     }
/// }
/// </code>
/// </example>
public interface IRabbitMQDiagnostics
{
    /// <summary>
    /// Gets the current overall health status of the RabbitMQ connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The health status.</returns>
    Task<RabbitMQHealthStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current connection information.
    /// </summary>
    /// <returns>Connection information or null if not connected.</returns>
    ConnectionInfo? GetConnectionInfo();

    /// <summary>
    /// Gets the current metrics snapshot.
    /// </summary>
    /// <returns>The metrics snapshot.</returns>
    RabbitMQMetricsSnapshot GetMetrics();

    /// <summary>
    /// Gets the list of active consumers.
    /// </summary>
    /// <returns>List of consumer information.</returns>
    IReadOnlyList<ConsumerInfo> GetActiveConsumers();

    /// <summary>
    /// Gets the error history.
    /// </summary>
    /// <param name="maxCount">Maximum number of errors to return.</param>
    /// <returns>List of recent errors.</returns>
    IReadOnlyList<ErrorInfo> GetErrorHistory(int maxCount = 10);

    /// <summary>
    /// Gets queue statistics if available.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>Queue statistics or null if not available.</returns>
    Task<QueueStats?> GetQueueStatsAsync(string queueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a connectivity test.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection is healthy, false otherwise.</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Overall health status of RabbitMQ.
/// </summary>
public class RabbitMQHealthStatus
{
    /// <summary>
    /// Gets or sets whether the overall system is healthy.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Gets or sets the health status level.
    /// </summary>
    public HealthStatus HealthStatus { get; set; }

    /// <summary>
    /// Gets or sets the connection status.
    /// </summary>
    public ConnectionStatus ConnectionStatus { get; set; }

    /// <summary>
    /// Gets or sets the current metrics snapshot.
    /// </summary>
    public RabbitMQMetricsSnapshot? Metrics { get; set; }

    /// <summary>
    /// Gets or sets the last error that occurred.
    /// </summary>
    public ErrorInfo? LastError { get; set; }

    /// <summary>
    /// Gets or sets the number of active consumers.
    /// </summary>
    public int ActiveConsumerCount { get; set; }

    /// <summary>
    /// Gets or sets the uptime since last connection.
    /// </summary>
    public TimeSpan? Uptime { get; set; }

    /// <summary>
    /// Gets or sets when the status was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets additional status details.
    /// </summary>
    public IDictionary<string, object>? Details { get; set; }

    /// <summary>
    /// Gets or sets the description/reason for the current status.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Connection status enum.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>Unknown connection status.</summary>
    Unknown = 0,

    /// <summary>Not connected to RabbitMQ.</summary>
    Disconnected = 1,

    /// <summary>Currently connecting to RabbitMQ.</summary>
    Connecting = 2,

    /// <summary>Connected to RabbitMQ.</summary>
    Connected = 3,

    /// <summary>Attempting to reconnect to RabbitMQ.</summary>
    Reconnecting = 4,

    /// <summary>Connection is blocked (flow control).</summary>
    Blocked = 5,

    /// <summary>Connection failed with error.</summary>
    Failed = 6
}

/// <summary>
/// Connection information.
/// </summary>
public class ConnectionInfo
{
    /// <summary>
    /// Gets or sets the host name.
    /// </summary>
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port number.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the virtual host.
    /// </summary>
    public string? VirtualHost { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets when the connection was established.
    /// </summary>
    public DateTimeOffset? ConnectedAt { get; set; }

    /// <summary>
    /// Gets or sets the connection status.
    /// </summary>
    public ConnectionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets whether the connection uses SSL.
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// Gets or sets the client-provided connection name.
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the number of open channels.
    /// </summary>
    public int OpenChannelCount { get; set; }

    /// <summary>
    /// Gets or sets the server-provided connection properties.
    /// </summary>
    public IDictionary<string, object>? ServerProperties { get; set; }
}

/// <summary>
/// Consumer information.
/// </summary>
public class ConsumerInfo
{
    /// <summary>
    /// Gets or sets the consumer tag.
    /// </summary>
    public string ConsumerTag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the queue name.
    /// </summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message type being consumed.
    /// </summary>
    public string? MessageType { get; set; }

    /// <summary>
    /// Gets or sets whether the consumer is running.
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Gets or sets the prefetch count.
    /// </summary>
    public ushort PrefetchCount { get; set; }

    /// <summary>
    /// Gets or sets when the consumer was registered.
    /// </summary>
    public DateTimeOffset RegisteredAt { get; set; }

    /// <summary>
    /// Gets or sets the number of messages processed.
    /// </summary>
    public long MessagesProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of messages that failed processing.
    /// </summary>
    public long MessagesFailed { get; set; }
}

/// <summary>
/// Error information.
/// </summary>
public class ErrorInfo
{
    /// <summary>
    /// Gets or sets the error type/exception type.
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stack trace.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Gets or sets when the error occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the operation that was being performed.
    /// </summary>
    public string? Operation { get; set; }

    /// <summary>
    /// Gets or sets the related message ID if applicable.
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Gets or sets the queue name if applicable.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Creates an ErrorInfo from an exception.
    /// </summary>
    public static ErrorInfo FromException(Exception exception, string? operation = null, string? messageId = null)
    {
        return new ErrorInfo
        {
            ErrorType = exception.GetType().FullName ?? exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            Timestamp = DateTimeOffset.UtcNow,
            Operation = operation,
            MessageId = messageId
        };
    }
}

/// <summary>
/// Queue statistics.
/// </summary>
public class QueueStats
{
    /// <summary>
    /// Gets or sets the queue name.
    /// </summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of messages in the queue.
    /// </summary>
    public uint MessageCount { get; set; }

    /// <summary>
    /// Gets or sets the number of consumers on the queue.
    /// </summary>
    public uint ConsumerCount { get; set; }

    /// <summary>
    /// Gets or sets whether the queue is durable.
    /// </summary>
    public bool IsDurable { get; set; }

    /// <summary>
    /// Gets or sets whether the queue is exclusive.
    /// </summary>
    public bool IsExclusive { get; set; }

    /// <summary>
    /// Gets or sets whether the queue auto-deletes.
    /// </summary>
    public bool IsAutoDelete { get; set; }

    /// <summary>
    /// Gets or sets when the stats were captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Default implementation of IRabbitMQDiagnostics.
/// </summary>
public class RabbitMQDiagnostics : IRabbitMQDiagnostics
{
    private readonly IRabbitMQMetrics? _metrics;
    private readonly List<ConsumerInfo> _consumers = new();
    private readonly List<ErrorInfo> _errorHistory = new();
    private readonly object _errorLock = new();
    private readonly int _maxErrorHistory;

    private ConnectionInfo? _connectionInfo;
    private DateTimeOffset? _connectedAt;

    /// <summary>
    /// Creates a new RabbitMQDiagnostics instance.
    /// </summary>
    /// <param name="metrics">Optional metrics instance.</param>
    /// <param name="maxErrorHistory">Maximum number of errors to keep in history.</param>
    public RabbitMQDiagnostics(IRabbitMQMetrics? metrics = null, int maxErrorHistory = 100)
    {
        _metrics = metrics;
        _maxErrorHistory = maxErrorHistory;
    }

    /// <inheritdoc />
    public Task<RabbitMQHealthStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var metrics = _metrics?.GetSnapshot();
        var connectionStatus = _connectionInfo?.Status ?? ConnectionStatus.Unknown;
        var isHealthy = connectionStatus == ConnectionStatus.Connected;

        ErrorInfo? lastError;
        lock (_errorLock)
        {
            lastError = _errorHistory.Count > 0 ? _errorHistory[^1] : null;
        }

        var status = new RabbitMQHealthStatus
        {
            IsHealthy = isHealthy,
            HealthStatus = isHealthy ? HealthStatus.Healthy :
                connectionStatus == ConnectionStatus.Reconnecting ? HealthStatus.Degraded :
                HealthStatus.Unhealthy,
            ConnectionStatus = connectionStatus,
            Metrics = metrics,
            LastError = lastError,
            ActiveConsumerCount = _consumers.Count,
            Uptime = _connectedAt.HasValue ? DateTimeOffset.UtcNow - _connectedAt.Value : null,
            Timestamp = DateTimeOffset.UtcNow,
            Description = GetStatusDescription(connectionStatus, lastError)
        };

        return Task.FromResult(status);
    }

    /// <inheritdoc />
    public ConnectionInfo? GetConnectionInfo() => _connectionInfo;

    /// <inheritdoc />
    public RabbitMQMetricsSnapshot GetMetrics()
    {
        return _metrics?.GetSnapshot() ?? new RabbitMQMetricsSnapshot { Timestamp = DateTimeOffset.UtcNow };
    }

    /// <inheritdoc />
    public IReadOnlyList<ConsumerInfo> GetActiveConsumers() => _consumers.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<ErrorInfo> GetErrorHistory(int maxCount = 10)
    {
        lock (_errorLock)
        {
            var count = Math.Min(maxCount, _errorHistory.Count);
            return _errorHistory.GetRange(_errorHistory.Count - count, count).AsReadOnly();
        }
    }

    /// <inheritdoc />
    public Task<QueueStats?> GetQueueStatsAsync(string queueName, CancellationToken cancellationToken = default)
    {
        // This would typically require access to the RabbitMQ management API
        // or a passive queue declaration. For now, return null.
        return Task.FromResult<QueueStats?>(null);
    }

    /// <inheritdoc />
    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_connectionInfo?.Status == ConnectionStatus.Connected);
    }

    /// <summary>
    /// Updates the connection info.
    /// </summary>
    public void UpdateConnectionInfo(ConnectionInfo? info)
    {
        _connectionInfo = info;
        if (info?.Status == ConnectionStatus.Connected && !_connectedAt.HasValue)
        {
            _connectedAt = DateTimeOffset.UtcNow;
        }
        else if (info?.Status != ConnectionStatus.Connected)
        {
            _connectedAt = null;
        }
    }

    /// <summary>
    /// Registers a consumer.
    /// </summary>
    public void RegisterConsumer(ConsumerInfo consumer)
    {
        _consumers.Add(consumer);
    }

    /// <summary>
    /// Unregisters a consumer.
    /// </summary>
    public void UnregisterConsumer(string consumerTag)
    {
        _consumers.RemoveAll(c => c.ConsumerTag == consumerTag);
    }

    /// <summary>
    /// Records an error.
    /// </summary>
    public void RecordError(ErrorInfo error)
    {
        lock (_errorLock)
        {
            _errorHistory.Add(error);
            while (_errorHistory.Count > _maxErrorHistory)
            {
                _errorHistory.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Records an error from an exception.
    /// </summary>
    public void RecordError(Exception exception, string? operation = null, string? messageId = null)
    {
        RecordError(ErrorInfo.FromException(exception, operation, messageId));
    }

    private static string GetStatusDescription(ConnectionStatus status, ErrorInfo? lastError)
    {
        return status switch
        {
            ConnectionStatus.Connected => "Connected to RabbitMQ and operating normally",
            ConnectionStatus.Disconnected => "Not connected to RabbitMQ",
            ConnectionStatus.Connecting => "Establishing connection to RabbitMQ",
            ConnectionStatus.Reconnecting => "Attempting to reconnect to RabbitMQ",
            ConnectionStatus.Blocked => "Connection is blocked due to flow control",
            ConnectionStatus.Failed => lastError != null
                ? $"Connection failed: {lastError.Message}"
                : "Connection failed with unknown error",
            _ => "Unknown connection status"
        };
    }
}


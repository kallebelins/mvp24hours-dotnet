//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Contract;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;

namespace Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy
{
    /// <summary>
    /// Factory for creating and managing RabbitMQ connections per tenant with virtual host isolation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This factory implements connection pooling per tenant, with automatic cleanup of idle connections.
    /// Each tenant can have their own virtual host, credentials, and connection settings.
    /// </para>
    /// <para>
    /// Connection lifecycle:
    /// <code>
    /// ┌────────────────────────────────────────────────────────────────────────────┐
    /// │ GetOrCreateConnection(tenantId)                                            │
    /// │   ├─ Check pool for existing connection                                    │
    /// │   │   ├─ Connection exists and healthy → Return                            │
    /// │   │   └─ Connection missing or unhealthy → Create new                      │
    /// │   └─ Create new connection                                                 │
    /// │       ├─ Resolve tenant configuration                                      │
    /// │       ├─ Apply virtual host from config                                    │
    /// │       ├─ Create ConnectionFactory                                          │
    /// │       ├─ Apply retry policy                                                │
    /// │       └─ Add to pool with timestamp                                        │
    /// └────────────────────────────────────────────────────────────────────────────┘
    /// </code>
    /// </para>
    /// </remarks>
    public class TenantConnectionFactory : ITenantConnectionFactory, IDisposable
    {
        private readonly ConcurrentDictionary<string, TenantConnectionEntry> _connections = new();
        private readonly TenantRabbitMQOptions _options;
        private readonly RabbitMQConnectionOptions _defaultConnectionOptions;
        private readonly ITenantRabbitMQResolver? _resolver;
        private readonly ILogger<TenantConnectionFactory>? _logger;
        private readonly Timer _cleanupTimer;
        private readonly SemaphoreSlim _createLock = new(1, 1);
        private bool _disposed;

        /// <summary>
        /// Creates a new tenant connection factory.
        /// </summary>
        /// <param name="options">Multi-tenancy options.</param>
        /// <param name="connectionOptions">Default RabbitMQ connection options.</param>
        /// <param name="resolver">Optional tenant configuration resolver.</param>
        /// <param name="logger">Optional logger.</param>
        public TenantConnectionFactory(
            IOptions<TenantRabbitMQOptions> options,
            IOptions<RabbitMQConnectionOptions> connectionOptions,
            ITenantRabbitMQResolver? resolver = null,
            ILogger<TenantConnectionFactory>? logger = null)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _defaultConnectionOptions = connectionOptions?.Value ?? throw new ArgumentNullException(nameof(connectionOptions));
            _resolver = resolver;
            _logger = logger;

            // Start cleanup timer for idle connections
            _cleanupTimer = new Timer(
                CleanupIdleConnections,
                null,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5));
        }

        /// <inheritdoc />
        public IConnection GetOrCreateConnection(string tenantId)
        {
            ArgumentNullException.ThrowIfNull(tenantId);

            // Try to get existing connection
            if (_connections.TryGetValue(tenantId, out var entry) && entry.Connection.IsOpen)
            {
                entry.LastAccessed = DateTimeOffset.UtcNow;
                return entry.Connection;
            }

            // Create new connection
            return CreateConnectionInternal(tenantId);
        }

        /// <inheritdoc />
        public IModel GetOrCreateChannel(string tenantId)
        {
            var connection = GetOrCreateConnection(tenantId);
            return connection.CreateModel();
        }

        /// <inheritdoc />
        public string GetVirtualHost(string tenantId)
        {
            ArgumentNullException.ThrowIfNull(tenantId);

            // Check static configuration first
            if (_options.Tenants.TryGetValue(tenantId, out var tenantConfig) &&
                !string.IsNullOrEmpty(tenantConfig.VirtualHost))
            {
                return tenantConfig.VirtualHost;
            }

            // Apply template
            return _options.GetVirtualHost(tenantId);
        }

        /// <inheritdoc />
        public bool HasConnection(string tenantId)
        {
            return _connections.TryGetValue(tenantId, out var entry) && entry.Connection.IsOpen;
        }

        /// <inheritdoc />
        public void CloseConnection(string tenantId)
        {
            if (_connections.TryRemove(tenantId, out var entry))
            {
                try
                {
                    entry.Connection.Close();
                    entry.Connection.Dispose();

                    LogConnectionClosed(tenantId);
                }
                catch (Exception ex)
                {
                    LogConnectionCloseError(tenantId, ex);
                }
            }
        }

        /// <inheritdoc />
        public void CloseAllConnections()
        {
            foreach (var tenantId in _connections.Keys)
            {
                CloseConnection(tenantId);
            }
        }

        private IConnection CreateConnectionInternal(string tenantId)
        {
            _createLock.Wait();
            try
            {
                // Double-check after acquiring lock
                if (_connections.TryGetValue(tenantId, out var existing) && existing.Connection.IsOpen)
                {
                    existing.LastAccessed = DateTimeOffset.UtcNow;
                    return existing.Connection;
                }

                // Check tenant limit
                if (_connections.Count >= _options.MaxTenantConnections)
                {
                    // Evict oldest idle connection
                    EvictOldestConnection();
                }

                // Get tenant configuration
                var config = GetTenantConfiguration(tenantId);
                
                // Create connection factory
                var factory = CreateConnectionFactory(tenantId, config);

                // Apply retry policy and create connection
                var connection = CreateConnectionWithRetry(factory, tenantId);

                // Add to pool
                var entry = new TenantConnectionEntry
                {
                    TenantId = tenantId,
                    Connection = connection,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastAccessed = DateTimeOffset.UtcNow
                };

                _connections[tenantId] = entry;

                // Subscribe to connection events
                connection.ConnectionShutdown += (sender, args) => OnConnectionShutdown(tenantId, args);
                connection.CallbackException += (sender, args) => OnCallbackException(tenantId, args);

                LogConnectionCreated(tenantId, factory.VirtualHost);

                return connection;
            }
            finally
            {
                _createLock.Release();
            }
        }

        private TenantRabbitMQConfiguration? GetTenantConfiguration(string tenantId)
        {
            // Try async resolver if available
            if (_resolver != null)
            {
                var config = _resolver.ResolveAsync(tenantId).GetAwaiter().GetResult();
                if (config != null)
                    return config;
            }

            // Check static configuration
            if (_options.Tenants.TryGetValue(tenantId, out var staticConfig))
            {
                return new TenantRabbitMQConfiguration
                {
                    TenantId = tenantId,
                    VirtualHost = staticConfig.VirtualHost,
                    ConnectionString = staticConfig.ConnectionString,
                    Username = staticConfig.Username,
                    Password = staticConfig.Password,
                    IsEnabled = staticConfig.IsEnabled
                };
            }

            // Return default (will use templates)
            return null;
        }

        private ConnectionFactory CreateConnectionFactory(string tenantId, TenantRabbitMQConfiguration? config)
        {
            ConnectionFactory factory;

            if (!string.IsNullOrEmpty(config?.ConnectionString))
            {
                // Use tenant-specific connection string
                factory = new ConnectionFactory
                {
                    Uri = new Uri(config.ConnectionString),
                    DispatchConsumersAsync = _defaultConnectionOptions.DispatchConsumersAsync
                };
            }
            else if (!string.IsNullOrEmpty(_defaultConnectionOptions.ConnectionString))
            {
                // Use default connection string
                factory = new ConnectionFactory
                {
                    Uri = new Uri(_defaultConnectionOptions.ConnectionString),
                    DispatchConsumersAsync = _defaultConnectionOptions.DispatchConsumersAsync
                };
            }
            else if (_defaultConnectionOptions.Configuration != null)
            {
                // Use default configuration
                var defaultConfig = _defaultConnectionOptions.Configuration;
                factory = new ConnectionFactory
                {
                    HostName = defaultConfig.HostName,
                    Port = defaultConfig.Port,
                    UserName = config?.Username ?? defaultConfig.UserName,
                    Password = config?.Password ?? defaultConfig.Password,
                    DispatchConsumersAsync = _defaultConnectionOptions.DispatchConsumersAsync
                };
            }
            else
            {
                throw new InvalidOperationException("No RabbitMQ connection configuration available.");
            }

            // Set virtual host based on strategy
            if (_options.IsolationStrategy == TenantIsolationStrategy.VirtualHostPerTenant)
            {
                factory.VirtualHost = config?.VirtualHost ?? _options.GetVirtualHost(tenantId);
            }

            // Override credentials if provided
            if (!string.IsNullOrEmpty(config?.Username))
            {
                factory.UserName = config.Username;
            }
            if (!string.IsNullOrEmpty(config?.Password))
            {
                factory.Password = config.Password;
            }

            return factory;
        }

        private IConnection CreateConnectionWithRetry(ConnectionFactory factory, string tenantId)
        {
            var policy = Policy
                .Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(
                    _defaultConnectionOptions.RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (ex, timeSpan, retryCount, context) =>
                    {
                        LogConnectionRetry(tenantId, retryCount, timeSpan, ex);
                    });

            IConnection? connection = null;
            policy.Execute(() =>
            {
                connection = factory.CreateConnection($"tenant-{tenantId}");
            });

            return connection ?? throw new InvalidOperationException($"Failed to create connection for tenant {tenantId}");
        }

        private void OnConnectionShutdown(string tenantId, ShutdownEventArgs args)
        {
            LogConnectionShutdown(tenantId, args.ReplyText);
            
            // Remove from pool - will be recreated on next access
            _connections.TryRemove(tenantId, out _);
        }

        private void OnCallbackException(string tenantId, global::RabbitMQ.Client.Events.CallbackExceptionEventArgs args)
        {
            LogCallbackException(tenantId, args.Exception);
        }

        private void CleanupIdleConnections(object? state)
        {
            var now = DateTimeOffset.UtcNow;
            var threshold = now - _options.IdleConnectionTimeout;

            foreach (var kvp in _connections)
            {
                if (kvp.Value.LastAccessed < threshold && !kvp.Value.Connection.IsOpen)
                {
                    CloseConnection(kvp.Key);
                }
            }
        }

        private void EvictOldestConnection()
        {
            TenantConnectionEntry? oldest = null;
            string? oldestTenantId = null;

            foreach (var kvp in _connections)
            {
                if (oldest == null || kvp.Value.LastAccessed < oldest.LastAccessed)
                {
                    oldest = kvp.Value;
                    oldestTenantId = kvp.Key;
                }
            }

            if (oldestTenantId != null)
            {
                CloseConnection(oldestTenantId);
                LogConnectionEvicted(oldestTenantId);
            }
        }

        #region Logging

        private void LogConnectionCreated(string tenantId, string virtualHost)
        {
            var message = $"Created RabbitMQ connection for tenant '{tenantId}' on virtual host '{virtualHost}'";
            if (_logger != null)
            {
                _logger.LogInformation(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-tenant-connection-created", message);
            }
        }

        private void LogConnectionClosed(string tenantId)
        {
            var message = $"Closed RabbitMQ connection for tenant '{tenantId}'";
            if (_logger != null)
            {
                _logger.LogInformation(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-tenant-connection-closed", message);
            }
        }

        private void LogConnectionCloseError(string tenantId, Exception ex)
        {
            if (_logger != null)
            {
                _logger.LogWarning(ex, "Error closing RabbitMQ connection for tenant '{TenantId}'", tenantId);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Warning, "rabbitmq-tenant-connection-close-error", 
                    $"Error closing connection for tenant '{tenantId}': {ex.Message}");
            }
        }

        private void LogConnectionShutdown(string tenantId, string reason)
        {
            var message = $"RabbitMQ connection shutdown for tenant '{tenantId}': {reason}";
            if (_logger != null)
            {
                _logger.LogWarning(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Warning, "rabbitmq-tenant-connection-shutdown", message);
            }
        }

        private void LogCallbackException(string tenantId, Exception ex)
        {
            if (_logger != null)
            {
                _logger.LogError(ex, "Callback exception for tenant '{TenantId}'", tenantId);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Error, "rabbitmq-tenant-callback-exception", 
                    $"Callback exception for tenant '{tenantId}': {ex.Message}");
            }
        }

        private void LogConnectionRetry(string tenantId, int retryCount, TimeSpan delay, Exception ex)
        {
            if (_logger != null)
            {
                _logger.LogWarning(ex, 
                    "RabbitMQ connection retry {RetryCount} for tenant '{TenantId}' after {Delay}s",
                    retryCount, tenantId, delay.TotalSeconds);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Warning, "rabbitmq-tenant-connection-retry",
                    $"Retry {retryCount} for tenant '{tenantId}' after {delay.TotalSeconds}s: {ex.Message}");
            }
        }

        private void LogConnectionEvicted(string tenantId)
        {
            var message = $"Evicted RabbitMQ connection for tenant '{tenantId}' due to pool limit";
            if (_logger != null)
            {
                _logger.LogInformation(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-tenant-connection-evicted", message);
            }
        }

        #endregion

        #region IDisposable

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        /// <param name="disposing">True if called from Dispose().</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _cleanupTimer.Dispose();
                CloseAllConnections();
                _createLock.Dispose();
                _disposed = true;
            }
        }

        #endregion

        private class TenantConnectionEntry
        {
            public required string TenantId { get; set; }
            public required IConnection Connection { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset LastAccessed { get; set; }
        }
    }
}


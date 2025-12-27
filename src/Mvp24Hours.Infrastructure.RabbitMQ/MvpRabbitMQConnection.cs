using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.IO;
using System.Net.Sockets;

namespace Mvp24Hours.Infrastructure.RabbitMQ
{
    public sealed class MvpRabbitMQConnection : IMvpRabbitMQConnection, IDisposable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Allows you to implement specialized rules.")]
        private readonly IConnectionFactory _connectionFactory;
        private readonly RabbitMQConnectionOptions _options;
        private readonly ILogger<MvpRabbitMQConnection>? _logger;
        private IConnection _connection;
        private bool _disposed;
        private readonly object sync_root = new();

        public MvpRabbitMQConnection(IOptions<RabbitMQConnectionOptions> _options, ILogger<MvpRabbitMQConnection>? logger = null)
            : this(_options?.Value ?? throw new ArgumentNullException(nameof(_options)), logger)
        {
        }

        public MvpRabbitMQConnection(RabbitMQConnectionOptions _options, ILogger<MvpRabbitMQConnection>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(_options);
            _logger = logger;

            if (_options.ConnectionString.HasValue())
            {
                _connectionFactory = new ConnectionFactory()
                {
                    Uri = new Uri(_options.ConnectionString),
                    DispatchConsumersAsync = _options.DispatchConsumersAsync
                };
            }
            else if (_options.Configuration != null)
            {
                var config = _options.Configuration;
                _connectionFactory = new ConnectionFactory()
                {
                    HostName = config.HostName,
                    Port = config.Port,
                    UserName = config.UserName,
                    Password = config.Password,
                    DispatchConsumersAsync = _options.DispatchConsumersAsync
                };
            }
            else
            {
                throw new ArgumentNullException(nameof(_options), "Connection string/configuration is required.");
            }

            this._options = _options;
        }

        public RabbitMQConnectionOptions Options => _options;

        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsOpen && !_disposed;
            }
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return _connection.CreateModel();
        }

        public bool TryConnect()
        {
            _logger?.LogInformation("RabbitMQ Client is trying to connect");

            lock (sync_root)
            {
                var policy = RetryPolicy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_options.RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger?.LogWarning(ex,
                            "RabbitMQ Client could not connect after {ElapsedSeconds}s",
                            time.TotalSeconds);
                    }
                );

                policy.Execute(() =>
                {
                    try
                    {
                        _connection = _connectionFactory
                                .CreateConnection();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "RabbitMQ Client could not connect");
                    }
                });

                if (IsConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;

                    _logger?.LogInformation(
                        "RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events",
                        _connection.Endpoint.HostName);
                    return true;
                }
                else
                {
                    _logger?.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");
                    return false;
                }
            }
        }

        #region [ Events ]

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            _logger?.LogWarning("A RabbitMQ connection is blocked. Trying to re-connect...");

            TryConnect();
        }

        void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger?.LogWarning(e.Exception, "A RabbitMQ connection threw exception. Trying to re-connect...");

            TryConnect();
        }

        void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;

            _logger?.LogWarning(
                "A RabbitMQ connection is on shutdown. Reason={Reason}, ReplyCode={ReplyCode}, ReplyText={ReplyText}. Trying to re-connect...",
                reason.ReplyText, reason.ReplyCode, reason.ReplyText);

            TryConnect();
        }

        #endregion

        #region [ IDisposable ]

        public void Dispose()
        {
            Dispose(!_disposed);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposed = true;
                try
                {
                    _connection.ConnectionShutdown -= OnConnectionShutdown;
                    _connection.CallbackException -= OnCallbackException;
                    _connection.ConnectionBlocked -= OnConnectionBlocked;
                    _connection.Dispose();
                }
                catch (IOException ex)
                {
                    _logger?.LogCritical(ex, "Error disposing RabbitMQ connection");
                }
            }
        }

        #endregion

    }
}

using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
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
        private IConnection _connection;
        private bool _disposed;
        private readonly object sync_root = new();

        public MvpRabbitMQConnection(IOptions<RabbitMQConnectionOptions> _options)
            : this(_options?.Value ?? throw new ArgumentNullException(nameof(_options)))
        {
        }

        public MvpRabbitMQConnection(RabbitMQConnectionOptions _options)
        {
            ArgumentNullException.ThrowIfNull(_options);

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
            TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-connection-tryconnect", "RabbitMQ Client is trying to connect");

            lock (sync_root)
            {
                var policy = RetryPolicy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_options.RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Warning, "rabbitmq-connection-tryconnect-waitandretry", $"RabbitMQ Client could not connect after {time.TotalSeconds:n1}s ({ex.Message})");
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
                        TelemetryHelper.Execute(TelemetryLevels.Error, "rabbitmq-connection-tryconnect-failure", $"RabbitMQ Client could not connect. ({ex.Message}).");
                    }
                });

                if (IsConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;

                    TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-connection-subscribe", $"RabbitMQ Client acquired a persistent connection to '{_connection.Endpoint.HostName}' and is subscribed to failure events");
                    return true;
                }
                else
                {
                    TelemetryHelper.Execute(TelemetryLevels.Critical, "rabbitmq-connection-tryconnect-error", "FATAL ERROR: RabbitMQ connections could not be created and opened");
                    return false;
                }
            }
        }

        #region [ Events ]

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            TelemetryHelper.Execute(TelemetryLevels.Warning, "rabbitmq-connection-tryconnect-subscribe-blocked", "A RabbitMQ connection is shutdown. Trying to re-connect...");

            TryConnect();
        }

        void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            TelemetryHelper.Execute(TelemetryLevels.Warning, "rabbitmq-connection-tryconnect-subscribe-callback", "A RabbitMQ connection throw exception. Trying to re-connect...");

            TryConnect();
        }

        void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;

            TelemetryHelper.Execute(TelemetryLevels.Warning, "rabbitmq-connection-tryconnect-subscribe-shutdown", "A RabbitMQ connection is on shutdown. Trying to re-connect...");

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
                    TelemetryHelper.Execute(TelemetryLevels.Critical, "rabbitmq-connection-dispose-failure", ex);
                }
            }
        }

        #endregion

    }
}

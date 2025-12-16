//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.HealthChecks
{
    /// <summary>
    /// Health check for RabbitMQ connectivity.
    /// </summary>
    public class RabbitMQHealthCheck : IHealthCheck
    {
        private readonly IMvpRabbitMQConnection _connection;

        /// <summary>
        /// Creates a new instance of RabbitMQHealthCheck.
        /// </summary>
        /// <param name="connection">The RabbitMQ connection.</param>
        public RabbitMQHealthCheck(IMvpRabbitMQConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <inheritdoc />
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (_connection.IsConnected)
                {
                    // Try to create a model to verify the connection is working
                    using var channel = _connection.CreateModel();
                    
                    var data = new Dictionary<string, object>
                    {
                        ["host"] = _connection.Options?.ConnectionString ?? "Unknown",
                        ["channelIsOpen"] = channel.IsOpen
                    };

                    return Task.FromResult(HealthCheckResult.Healthy(
                        "RabbitMQ connection is healthy.",
                        data));
                }
                else
                {
                    // Try to connect
                    var connected = _connection.TryConnect();

                    if (connected)
                    {
                        return Task.FromResult(HealthCheckResult.Healthy(
                            "RabbitMQ connection established after reconnect."));
                    }

                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        "RabbitMQ connection could not be established.",
                        data: new Dictionary<string, object>
                        {
                            ["retryCount"] = _connection.Options?.RetryCount ?? 3
                        }));
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "RabbitMQ health check failed.",
                    ex,
                    new Dictionary<string, object>
                    {
                        ["error"] = ex.Message,
                        ["exceptionType"] = ex.GetType().Name
                    }));
            }
        }
    }
}


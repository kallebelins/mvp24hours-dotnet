//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Contract;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy
{
    /// <summary>
    /// Helper for managing tenant-specific dead letter queues.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each tenant can have their own dead letter queue and exchange, providing:
    /// <list type="bullet">
    /// <item>Isolated error handling per tenant</item>
    /// <item>Separate monitoring and alerting</item>
    /// <item>Tenant-specific retry and recovery strategies</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Dead Letter Queue Architecture:</strong>
    /// <code>
    /// ┌────────────────────────────────────────────────────────────────────────────┐
    /// │ Tenant A                                                                   │
    /// │   Queue: tenant_a_orders                                                   │
    /// │   ├─ x-dead-letter-exchange: tenant_a_dlx                                  │
    /// │   └─ x-dead-letter-routing-key: tenant_a_dlq                               │
    /// │                                                                            │
    /// │   Dead Letter Exchange: tenant_a_dlx (direct)                              │
    /// │   └─ tenant_a_dlq → Dead Letter Queue: tenant_a_dlq                        │
    /// │                                                                            │
    /// │ Tenant B                                                                   │
    /// │   Queue: tenant_b_orders                                                   │
    /// │   ├─ x-dead-letter-exchange: tenant_b_dlx                                  │
    /// │   └─ x-dead-letter-routing-key: tenant_b_dlq                               │
    /// │                                                                            │
    /// │   Dead Letter Exchange: tenant_b_dlx (direct)                              │
    /// │   └─ tenant_b_dlq → Dead Letter Queue: tenant_b_dlq                        │
    /// └────────────────────────────────────────────────────────────────────────────┘
    /// </code>
    /// </para>
    /// </remarks>
    public class TenantDeadLetterQueueHelper : ITenantDeadLetterQueueHelper
    {
        private readonly TenantRabbitMQOptions _options;
        private readonly ITenantConnectionFactory _connectionFactory;
        private readonly ILogger<TenantDeadLetterQueueHelper>? _logger;
        private readonly ConcurrentDictionary<string, bool> _initializedQueues = new();

        /// <summary>
        /// Creates a new tenant dead letter queue helper.
        /// </summary>
        /// <param name="options">Multi-tenancy options.</param>
        /// <param name="connectionFactory">Tenant connection factory.</param>
        /// <param name="logger">Optional logger.</param>
        public TenantDeadLetterQueueHelper(
            IOptions<TenantRabbitMQOptions> options,
            ITenantConnectionFactory connectionFactory,
            ILogger<TenantDeadLetterQueueHelper>? logger = null)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger;
        }

        /// <inheritdoc />
        public string GetDeadLetterQueueName(string tenantId)
        {
            ArgumentNullException.ThrowIfNull(tenantId);
            return _options.GetDeadLetterQueue(tenantId);
        }

        /// <inheritdoc />
        public string GetDeadLetterExchangeName(string tenantId)
        {
            ArgumentNullException.ThrowIfNull(tenantId);
            return _options.GetDeadLetterExchange(tenantId);
        }

        /// <inheritdoc />
        public void EnsureDeadLetterInfrastructure(string tenantId)
        {
            ArgumentNullException.ThrowIfNull(tenantId);

            if (!_options.UseTenantSpecificDeadLetterQueues)
            {
                return;
            }

            // Check if already initialized
            var cacheKey = $"{tenantId}_dlq";
            if (_initializedQueues.ContainsKey(cacheKey))
            {
                return;
            }

            try
            {
                using var channel = _connectionFactory.GetOrCreateChannel(tenantId);
                
                var dlxName = GetDeadLetterExchangeName(tenantId);
                var dlqName = GetDeadLetterQueueName(tenantId);

                // Declare dead letter exchange
                channel.ExchangeDeclare(
                    exchange: dlxName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false,
                    arguments: null);

                // Declare dead letter queue
                channel.QueueDeclare(
                    queue: dlqName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: new Dictionary<string, object>
                    {
                        // Optional: Set message TTL for DLQ messages (e.g., 30 days)
                        // ["x-message-ttl"] = 2592000000  // 30 days in milliseconds
                    });

                // Bind dead letter queue to exchange
                channel.QueueBind(
                    queue: dlqName,
                    exchange: dlxName,
                    routingKey: dlqName,
                    arguments: null);

                _initializedQueues[cacheKey] = true;

                LogDeadLetterInfrastructureCreated(tenantId, dlxName, dlqName);
            }
            catch (Exception ex)
            {
                LogDeadLetterInfrastructureError(tenantId, ex);
                throw;
            }
        }

        /// <inheritdoc />
        public Dictionary<string, object> GetDeadLetterArguments(string tenantId)
        {
            ArgumentNullException.ThrowIfNull(tenantId);

            if (!_options.UseTenantSpecificDeadLetterQueues)
            {
                return new Dictionary<string, object>();
            }

            return new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = GetDeadLetterExchangeName(tenantId),
                ["x-dead-letter-routing-key"] = GetDeadLetterQueueName(tenantId)
            };
        }

        /// <inheritdoc />
        public void SendToDeadLetterQueue(string tenantId, byte[] body, IBasicProperties? properties, string reason)
        {
            ArgumentNullException.ThrowIfNull(tenantId);
            ArgumentNullException.ThrowIfNull(body);

            EnsureDeadLetterInfrastructure(tenantId);

            using var channel = _connectionFactory.GetOrCreateChannel(tenantId);

            var dlxName = GetDeadLetterExchangeName(tenantId);
            var dlqName = GetDeadLetterQueueName(tenantId);

            // Create new properties with dead letter info
            var dlqProperties = channel.CreateBasicProperties();
            dlqProperties.Persistent = true;
            dlqProperties.Headers = new Dictionary<string, object>(properties?.Headers ?? new Dictionary<string, object>())
            {
                ["x-dead-letter-reason"] = reason,
                ["x-dead-letter-timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["x-original-tenant-id"] = tenantId
            };

            if (properties != null)
            {
                dlqProperties.MessageId = properties.MessageId;
                dlqProperties.CorrelationId = properties.CorrelationId;
                dlqProperties.ContentType = properties.ContentType;
                dlqProperties.ContentEncoding = properties.ContentEncoding;
            }

            channel.BasicPublish(
                exchange: dlxName,
                routingKey: dlqName,
                basicProperties: dlqProperties,
                body: body);

            LogMessageSentToDeadLetterQueue(tenantId, dlqName, reason);
        }

        #region Logging

        private void LogDeadLetterInfrastructureCreated(string tenantId, string exchangeName, string queueName)
        {
            _logger?.LogInformation(
                "Created dead letter infrastructure for tenant. TenantId={TenantId}, Exchange={ExchangeName}, Queue={QueueName}",
                tenantId, exchangeName, queueName);
        }

        private void LogDeadLetterInfrastructureError(string tenantId, Exception ex)
        {
            _logger?.LogError(ex,
                "Error creating dead letter infrastructure for tenant. TenantId={TenantId}",
                tenantId);
        }

        private void LogMessageSentToDeadLetterQueue(string tenantId, string queueName, string reason)
        {
            _logger?.LogWarning(
                "Message sent to dead letter queue for tenant. TenantId={TenantId}, Queue={QueueName}, Reason={Reason}",
                tenantId, queueName, reason);
        }

        #endregion
    }

    /// <summary>
    /// Interface for managing tenant-specific dead letter queues.
    /// </summary>
    public interface ITenantDeadLetterQueueHelper
    {
        /// <summary>
        /// Gets the dead letter queue name for the specified tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>The dead letter queue name.</returns>
        string GetDeadLetterQueueName(string tenantId);

        /// <summary>
        /// Gets the dead letter exchange name for the specified tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>The dead letter exchange name.</returns>
        string GetDeadLetterExchangeName(string tenantId);

        /// <summary>
        /// Ensures the dead letter infrastructure (exchange and queue) exists for the specified tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        void EnsureDeadLetterInfrastructure(string tenantId);

        /// <summary>
        /// Gets the queue arguments for dead letter configuration.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>Dictionary of queue arguments for dead letter routing.</returns>
        Dictionary<string, object> GetDeadLetterArguments(string tenantId);

        /// <summary>
        /// Sends a message to the dead letter queue for the specified tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="body">The message body.</param>
        /// <param name="properties">The original message properties.</param>
        /// <param name="reason">The reason for dead-lettering.</param>
        void SendToDeadLetterQueue(string tenantId, byte[] body, IBasicProperties? properties, string reason);
    }
}


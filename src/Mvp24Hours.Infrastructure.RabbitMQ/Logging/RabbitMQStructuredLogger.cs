//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Logging
{
    /// <summary>
    /// Provides structured logging for RabbitMQ operations.
    /// </summary>
    public class RabbitMQStructuredLogger : IRabbitMQStructuredLogger
    {
        private readonly ILogger<RabbitMQStructuredLogger> _logger;

        /// <summary>
        /// Creates a new instance of RabbitMQStructuredLogger.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public RabbitMQStructuredLogger(ILogger<RabbitMQStructuredLogger> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public void LogMessagePublished(
            string messageId,
            string exchange,
            string routingKey,
            int bodySize,
            IDictionary<string, object>? headers = null,
            byte? priority = null,
            TimeSpan? elapsed = null)
        {
            _logger.LogInformation(
                "Message published. MessageId={MessageId}, Exchange={Exchange}, RoutingKey={RoutingKey}, " +
                "BodySize={BodySize}, Priority={Priority}, Headers={Headers}, ElapsedMs={ElapsedMs}",
                messageId,
                exchange,
                routingKey,
                bodySize,
                priority,
                headers != null ? FormatHeaders(headers) : null,
                elapsed?.TotalMilliseconds);
        }

        /// <inheritdoc />
        public void LogMessageReceived(
            string messageId,
            string exchange,
            string routingKey,
            string consumerTag,
            bool redelivered,
            int bodySize,
            IDictionary<string, object>? headers = null)
        {
            _logger.LogInformation(
                "Message received. MessageId={MessageId}, Exchange={Exchange}, RoutingKey={RoutingKey}, " +
                "ConsumerTag={ConsumerTag}, Redelivered={Redelivered}, BodySize={BodySize}, Headers={Headers}",
                messageId,
                exchange,
                routingKey,
                consumerTag,
                redelivered,
                bodySize,
                headers != null ? FormatHeaders(headers) : null);
        }

        /// <inheritdoc />
        public void LogMessageAcked(string messageId, ulong deliveryTag, TimeSpan processingTime)
        {
            _logger.LogInformation(
                "Message acknowledged. MessageId={MessageId}, DeliveryTag={DeliveryTag}, ProcessingTimeMs={ProcessingTimeMs}",
                messageId,
                deliveryTag,
                processingTime.TotalMilliseconds);
        }

        /// <inheritdoc />
        public void LogMessageNacked(string messageId, ulong deliveryTag, bool requeue, string? reason = null)
        {
            _logger.LogWarning(
                "Message negatively acknowledged. MessageId={MessageId}, DeliveryTag={DeliveryTag}, " +
                "Requeue={Requeue}, Reason={Reason}",
                messageId,
                deliveryTag,
                requeue,
                reason);
        }

        /// <inheritdoc />
        public void LogMessageRejected(string messageId, ulong deliveryTag, string? reason = null)
        {
            _logger.LogWarning(
                "Message rejected. MessageId={MessageId}, DeliveryTag={DeliveryTag}, Reason={Reason}",
                messageId,
                deliveryTag,
                reason);
        }

        /// <inheritdoc />
        public void LogMessageRedelivered(string messageId, int redeliveryCount, int maxRedeliveries)
        {
            _logger.LogWarning(
                "Message being redelivered. MessageId={MessageId}, RedeliveryCount={RedeliveryCount}, " +
                "MaxRedeliveries={MaxRedeliveries}",
                messageId,
                redeliveryCount,
                maxRedeliveries);
        }

        /// <inheritdoc />
        public void LogDuplicateMessageSkipped(string messageId)
        {
            _logger.LogInformation(
                "Duplicate message skipped. MessageId={MessageId}",
                messageId);
        }

        /// <inheritdoc />
        public void LogPublisherConfirm(string messageId, ulong deliveryTag)
        {
            _logger.LogDebug(
                "Publisher confirm received. MessageId={MessageId}, DeliveryTag={DeliveryTag}",
                messageId,
                deliveryTag);
        }

        /// <inheritdoc />
        public void LogPublisherNack(string messageId, ulong deliveryTag)
        {
            _logger.LogWarning(
                "Publisher nack received. MessageId={MessageId}, DeliveryTag={DeliveryTag}",
                messageId,
                deliveryTag);
        }

        /// <inheritdoc />
        public void LogConnectionEvent(string eventType, string hostName, int port, string? reason = null)
        {
            _logger.LogInformation(
                "Connection event. Event={EventType}, Host={HostName}, Port={Port}, Reason={Reason}",
                eventType,
                hostName,
                port,
                reason);
        }

        /// <inheritdoc />
        public void LogChannelEvent(string eventType, int channelNumber, string? reason = null)
        {
            _logger.LogInformation(
                "Channel event. Event={EventType}, ChannelNumber={ChannelNumber}, Reason={Reason}",
                eventType,
                channelNumber,
                reason);
        }

        /// <inheritdoc />
        public void LogError(string operation, Exception exception, string? messageId = null)
        {
            _logger.LogError(exception,
                "RabbitMQ operation failed. Operation={Operation}, MessageId={MessageId}, " +
                "ErrorType={ErrorType}, ErrorMessage={ErrorMessage}",
                operation,
                messageId,
                exception.GetType().Name,
                exception.Message);
        }

        /// <inheritdoc />
        public void LogQueueDeclared(string queueName, bool durable, bool exclusive, bool autoDelete, int? messageCount = null)
        {
            _logger.LogDebug(
                "Queue declared. QueueName={QueueName}, Durable={Durable}, Exclusive={Exclusive}, " +
                "AutoDelete={AutoDelete}, MessageCount={MessageCount}",
                queueName,
                durable,
                exclusive,
                autoDelete,
                messageCount);
        }

        /// <inheritdoc />
        public void LogExchangeDeclared(string exchange, string exchangeType, bool durable, bool autoDelete)
        {
            _logger.LogDebug(
                "Exchange declared. Exchange={Exchange}, Type={ExchangeType}, Durable={Durable}, AutoDelete={AutoDelete}",
                exchange,
                exchangeType,
                durable,
                autoDelete);
        }

        private static string FormatHeaders(IDictionary<string, object> headers)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            var first = true;
            foreach (var kvp in headers)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(kvp.Key);
                sb.Append('=');
                sb.Append(kvp.Value);
            }
            sb.Append('}');
            return sb.ToString();
        }
    }
}


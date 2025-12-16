//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Logging
{
    /// <summary>
    /// Interface for structured logging of RabbitMQ operations.
    /// </summary>
    public interface IRabbitMQStructuredLogger
    {
        /// <summary>
        /// Logs a message published event.
        /// </summary>
        void LogMessagePublished(
            string messageId,
            string exchange,
            string routingKey,
            int bodySize,
            IDictionary<string, object>? headers = null,
            byte? priority = null,
            TimeSpan? elapsed = null);

        /// <summary>
        /// Logs a message received event.
        /// </summary>
        void LogMessageReceived(
            string messageId,
            string exchange,
            string routingKey,
            string consumerTag,
            bool redelivered,
            int bodySize,
            IDictionary<string, object>? headers = null);

        /// <summary>
        /// Logs a message acknowledged event.
        /// </summary>
        void LogMessageAcked(string messageId, ulong deliveryTag, TimeSpan processingTime);

        /// <summary>
        /// Logs a message negatively acknowledged event.
        /// </summary>
        void LogMessageNacked(string messageId, ulong deliveryTag, bool requeue, string? reason = null);

        /// <summary>
        /// Logs a message rejected event.
        /// </summary>
        void LogMessageRejected(string messageId, ulong deliveryTag, string? reason = null);

        /// <summary>
        /// Logs a message redelivery event.
        /// </summary>
        void LogMessageRedelivered(string messageId, int redeliveryCount, int maxRedeliveries);

        /// <summary>
        /// Logs a duplicate message skipped event.
        /// </summary>
        void LogDuplicateMessageSkipped(string messageId);

        /// <summary>
        /// Logs a publisher confirm event.
        /// </summary>
        void LogPublisherConfirm(string messageId, ulong deliveryTag);

        /// <summary>
        /// Logs a publisher nack event.
        /// </summary>
        void LogPublisherNack(string messageId, ulong deliveryTag);

        /// <summary>
        /// Logs a connection event (connected, disconnected, reconnecting, etc.).
        /// </summary>
        void LogConnectionEvent(string eventType, string hostName, int port, string? reason = null);

        /// <summary>
        /// Logs a channel event (created, closed, etc.).
        /// </summary>
        void LogChannelEvent(string eventType, int channelNumber, string? reason = null);

        /// <summary>
        /// Logs an error during RabbitMQ operations.
        /// </summary>
        void LogError(string operation, Exception exception, string? messageId = null);

        /// <summary>
        /// Logs a queue declaration event.
        /// </summary>
        void LogQueueDeclared(string queueName, bool durable, bool exclusive, bool autoDelete, int? messageCount = null);

        /// <summary>
        /// Logs an exchange declaration event.
        /// </summary>
        void LogExchangeDeclared(string exchange, string exchangeType, bool durable, bool autoDelete);
    }
}


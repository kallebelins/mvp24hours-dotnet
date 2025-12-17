//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Topology.Contract
{
    /// <summary>
    /// Interface for formatting endpoint names (queues, exchanges, routing keys).
    /// Provides conventions for consistent naming across the messaging infrastructure.
    /// </summary>
    public interface IEndpointNameFormatter
    {
        /// <summary>
        /// Gets the separator used between name components.
        /// Default is typically "." or "-".
        /// </summary>
        string Separator { get; }

        /// <summary>
        /// Formats a queue name for a consumer.
        /// </summary>
        /// <typeparam name="T">The consumer type.</typeparam>
        /// <returns>The formatted queue name.</returns>
        string FormatQueueName<T>();

        /// <summary>
        /// Formats a queue name for a consumer type.
        /// </summary>
        /// <param name="consumerType">The consumer type.</param>
        /// <returns>The formatted queue name.</returns>
        string FormatQueueName(Type consumerType);

        /// <summary>
        /// Formats a queue name for a message type.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <returns>The formatted queue name.</returns>
        string FormatQueueNameFromMessage(Type messageType);

        /// <summary>
        /// Formats an exchange name for a message type.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>The formatted exchange name.</returns>
        string FormatExchangeName<T>();

        /// <summary>
        /// Formats an exchange name for a message type.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <returns>The formatted exchange name.</returns>
        string FormatExchangeName(Type messageType);

        /// <summary>
        /// Formats a routing key for a message type.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>The formatted routing key.</returns>
        string FormatRoutingKey<T>();

        /// <summary>
        /// Formats a routing key for a message type.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <returns>The formatted routing key.</returns>
        string FormatRoutingKey(Type messageType);

        /// <summary>
        /// Formats a dead letter queue name based on the original queue name.
        /// </summary>
        /// <param name="originalQueueName">The original queue name.</param>
        /// <returns>The formatted dead letter queue name.</returns>
        string FormatDeadLetterQueueName(string originalQueueName);

        /// <summary>
        /// Formats a dead letter exchange name based on the original exchange name.
        /// </summary>
        /// <param name="originalExchangeName">The original exchange name.</param>
        /// <returns>The formatted dead letter exchange name.</returns>
        string FormatDeadLetterExchangeName(string originalExchangeName);

        /// <summary>
        /// Formats a retry queue name based on the original queue name and retry level.
        /// </summary>
        /// <param name="originalQueueName">The original queue name.</param>
        /// <param name="retryLevel">The retry level (1, 2, 3, etc.).</param>
        /// <returns>The formatted retry queue name.</returns>
        string FormatRetryQueueName(string originalQueueName, int retryLevel);

        /// <summary>
        /// Formats a temporary/exclusive queue name.
        /// </summary>
        /// <returns>The formatted temporary queue name.</returns>
        string FormatTemporaryQueueName();

        /// <summary>
        /// Sanitizes a name component by removing invalid characters.
        /// </summary>
        /// <param name="name">The name to sanitize.</param>
        /// <returns>The sanitized name.</returns>
        string SanitizeName(string name);
    }
}


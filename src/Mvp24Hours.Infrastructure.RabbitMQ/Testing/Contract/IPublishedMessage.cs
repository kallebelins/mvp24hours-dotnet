//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing.Contract
{
    /// <summary>
    /// Represents a published message for tracking in tests.
    /// </summary>
    public interface IPublishedMessage
    {
        /// <summary>
        /// Gets the message object.
        /// </summary>
        object Message { get; }

        /// <summary>
        /// Gets the message type.
        /// </summary>
        Type MessageType { get; }

        /// <summary>
        /// Gets the message ID/token.
        /// </summary>
        string MessageId { get; }

        /// <summary>
        /// Gets the routing key used.
        /// </summary>
        string RoutingKey { get; }

        /// <summary>
        /// Gets the exchange name.
        /// </summary>
        string? Exchange { get; }

        /// <summary>
        /// Gets the message headers.
        /// </summary>
        IReadOnlyDictionary<string, object>? Headers { get; }

        /// <summary>
        /// Gets the message priority.
        /// </summary>
        byte? Priority { get; }

        /// <summary>
        /// Gets the message TTL in milliseconds.
        /// </summary>
        int? TtlMilliseconds { get; }

        /// <summary>
        /// Gets the timestamp when the message was published.
        /// </summary>
        DateTimeOffset PublishedAt { get; }

        /// <summary>
        /// Gets whether the message was published as part of a batch.
        /// </summary>
        bool IsBatch { get; }

        /// <summary>
        /// Gets the correlation ID.
        /// </summary>
        string? CorrelationId { get; }
    }

    /// <summary>
    /// Represents a published message of a specific type for tracking in tests.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    public interface IPublishedMessage<out TMessage> : IPublishedMessage where TMessage : class
    {
        /// <summary>
        /// Gets the typed message.
        /// </summary>
        new TMessage Message { get; }
    }
}


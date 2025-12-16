//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Base interface for messages without a typed payload.
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// Gets the unique message identifier.
        /// </summary>
        string MessageId { get; }

        /// <summary>
        /// Gets the correlation identifier for tracing related messages.
        /// </summary>
        string? CorrelationId { get; }

        /// <summary>
        /// Gets the causation identifier (ID of the message that caused this one).
        /// </summary>
        string? CausationId { get; }

        /// <summary>
        /// Gets the message type name for serialization/deserialization.
        /// </summary>
        string MessageType { get; }

        /// <summary>
        /// Gets the timestamp when the message was created.
        /// </summary>
        DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the message headers.
        /// </summary>
        IDictionary<string, object> Headers { get; }

        /// <summary>
        /// Gets the source application or service name.
        /// </summary>
        string? SourceApplication { get; }

        /// <summary>
        /// Gets the content type (e.g., "application/json").
        /// </summary>
        string ContentType { get; }
    }

    /// <summary>
    /// Generic interface for messages with a typed payload.
    /// </summary>
    /// <typeparam name="TPayload">The type of the message payload.</typeparam>
    public interface IMessage<out TPayload> : IMessage
    {
        /// <summary>
        /// Gets the message payload.
        /// </summary>
        TPayload Payload { get; }
    }
}


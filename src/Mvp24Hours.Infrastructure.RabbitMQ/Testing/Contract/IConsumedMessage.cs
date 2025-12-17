//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing.Contract
{
    /// <summary>
    /// Represents a consumed message for verification in tests.
    /// </summary>
    public interface IConsumedMessage
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
        /// Gets the message ID.
        /// </summary>
        string MessageId { get; }

        /// <summary>
        /// Gets the correlation ID.
        /// </summary>
        string? CorrelationId { get; }

        /// <summary>
        /// Gets the causation ID.
        /// </summary>
        string? CausationId { get; }

        /// <summary>
        /// Gets the consumer type that processed the message.
        /// </summary>
        Type ConsumerType { get; }

        /// <summary>
        /// Gets the queue name the message was consumed from.
        /// </summary>
        string QueueName { get; }

        /// <summary>
        /// Gets the exchange name.
        /// </summary>
        string Exchange { get; }

        /// <summary>
        /// Gets the routing key.
        /// </summary>
        string RoutingKey { get; }

        /// <summary>
        /// Gets the message headers.
        /// </summary>
        IReadOnlyDictionary<string, object> Headers { get; }

        /// <summary>
        /// Gets the timestamp when the message was consumed.
        /// </summary>
        DateTimeOffset ConsumedAt { get; }

        /// <summary>
        /// Gets the duration of the consume operation.
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        /// Gets whether the consume operation was successful.
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// Gets the exception if the consume operation failed.
        /// </summary>
        Exception? Exception { get; }

        /// <summary>
        /// Gets the redelivery count.
        /// </summary>
        int RedeliveryCount { get; }

        /// <summary>
        /// Gets whether this was a redelivered message.
        /// </summary>
        bool Redelivered { get; }
    }

    /// <summary>
    /// Represents a consumed message of a specific type for verification in tests.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    public interface IConsumedMessage<out TMessage> : IConsumedMessage where TMessage : class
    {
        /// <summary>
        /// Gets the typed message.
        /// </summary>
        new TMessage Message { get; }

        /// <summary>
        /// Gets the consume context used.
        /// </summary>
        IConsumeContext<TMessage> Context { get; }
    }
}


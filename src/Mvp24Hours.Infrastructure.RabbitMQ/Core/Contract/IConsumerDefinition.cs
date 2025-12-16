//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Base interface for consumer definitions.
    /// </summary>
    public interface IConsumerDefinition
    {
        /// <summary>
        /// Gets the consumer type.
        /// </summary>
        Type ConsumerType { get; }

        /// <summary>
        /// Gets the message type.
        /// </summary>
        Type MessageType { get; }

        /// <summary>
        /// Gets the queue name for this consumer.
        /// </summary>
        string? QueueName { get; }

        /// <summary>
        /// Gets the exchange name for this consumer.
        /// </summary>
        string? Exchange { get; }

        /// <summary>
        /// Gets the routing key for this consumer.
        /// </summary>
        string? RoutingKey { get; }

        /// <summary>
        /// Gets the prefetch count for this consumer.
        /// </summary>
        ushort? PrefetchCount { get; }

        /// <summary>
        /// Gets the number of concurrent consumers.
        /// </summary>
        int? ConcurrentConsumers { get; }

        /// <summary>
        /// Gets the maximum retry count for this consumer.
        /// </summary>
        int? MaxRetryCount { get; }

        /// <summary>
        /// Gets whether this consumer should use a dead letter queue.
        /// </summary>
        bool UseDeadLetterQueue { get; }
    }

    /// <summary>
    /// Generic consumer definition for typed consumers.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    public interface IConsumerDefinition<TConsumer> : IConsumerDefinition
        where TConsumer : class
    {
    }
}


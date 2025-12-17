//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Context for batch message consumption, providing access to multiple messages and batch metadata.
    /// </summary>
    /// <typeparam name="TMessage">The type of the consumed messages.</typeparam>
    public interface IBatchConsumeContext<out TMessage> where TMessage : class
    {
        /// <summary>
        /// Gets the batch of consumed messages with their individual metadata.
        /// </summary>
        IReadOnlyList<IBatchMessageItem<TMessage>> Messages { get; }

        /// <summary>
        /// Gets the total count of messages in this batch.
        /// </summary>
        int BatchSize { get; }

        /// <summary>
        /// Gets the unique batch identifier.
        /// </summary>
        string BatchId { get; }

        /// <summary>
        /// Gets the correlation identifier for the batch.
        /// </summary>
        string? CorrelationId { get; }

        /// <summary>
        /// Gets the exchange name the messages were received from.
        /// </summary>
        string Exchange { get; }

        /// <summary>
        /// Gets the queue name the messages were consumed from.
        /// </summary>
        string QueueName { get; }

        /// <summary>
        /// Gets the consumer tag.
        /// </summary>
        string ConsumerTag { get; }

        /// <summary>
        /// Gets the timestamp when the batch was created.
        /// </summary>
        DateTimeOffset BatchCreatedAt { get; }

        /// <summary>
        /// Gets the timestamp when the batch was completed (all messages received).
        /// </summary>
        DateTimeOffset BatchCompletedAt { get; }

        /// <summary>
        /// Gets the time elapsed since batch creation.
        /// </summary>
        TimeSpan BatchAge { get; }

        /// <summary>
        /// Gets the service provider for resolving dependencies.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the cancellation token.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Creates a new scope for dependency injection.
        /// </summary>
        /// <returns>A new service scope.</returns>
        IServiceScope CreateScope();

        /// <summary>
        /// Publishes a message to an exchange.
        /// </summary>
        /// <typeparam name="T">The type of message to publish.</typeparam>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Publishes multiple messages to an exchange in a batch.
        /// </summary>
        /// <typeparam name="T">The type of messages to publish.</typeparam>
        /// <param name="messages">The messages to publish.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PublishBatchAsync<T>(IEnumerable<T> messages, string? routingKey = null, CancellationToken cancellationToken = default) where T : class;
    }

    /// <summary>
    /// Represents a single message item within a batch.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public interface IBatchMessageItem<out TMessage> where TMessage : class
    {
        /// <summary>
        /// Gets the message payload.
        /// </summary>
        TMessage Message { get; }

        /// <summary>
        /// Gets the unique message identifier.
        /// </summary>
        string MessageId { get; }

        /// <summary>
        /// Gets the correlation identifier.
        /// </summary>
        string? CorrelationId { get; }

        /// <summary>
        /// Gets the causation identifier.
        /// </summary>
        string? CausationId { get; }

        /// <summary>
        /// Gets the message headers.
        /// </summary>
        IReadOnlyDictionary<string, object> Headers { get; }

        /// <summary>
        /// Gets the routing key used.
        /// </summary>
        string RoutingKey { get; }

        /// <summary>
        /// Gets the delivery tag.
        /// </summary>
        ulong DeliveryTag { get; }

        /// <summary>
        /// Gets whether this message is a redelivery.
        /// </summary>
        bool Redelivered { get; }

        /// <summary>
        /// Gets the redelivery count.
        /// </summary>
        int RedeliveryCount { get; }

        /// <summary>
        /// Gets the timestamp when the message was originally sent.
        /// </summary>
        DateTimeOffset? SentAt { get; }

        /// <summary>
        /// Gets the timestamp when the message was received.
        /// </summary>
        DateTimeOffset ReceivedAt { get; }

        /// <summary>
        /// Gets a header value by key.
        /// </summary>
        /// <typeparam name="T">The expected type of the header value.</typeparam>
        /// <param name="key">The header key.</param>
        /// <returns>The header value or default if not found.</returns>
        T? GetHeader<T>(string key);
    }
}


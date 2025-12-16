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
    /// Context for message consumption, providing access to message metadata and operations.
    /// </summary>
    /// <typeparam name="TMessage">The type of the consumed message.</typeparam>
    public interface IConsumeContext<out TMessage> where TMessage : class
    {
        /// <summary>
        /// Gets the consumed message.
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
        /// Gets the exchange name the message was received from.
        /// </summary>
        string Exchange { get; }

        /// <summary>
        /// Gets the routing key used.
        /// </summary>
        string RoutingKey { get; }

        /// <summary>
        /// Gets the queue name the message was consumed from.
        /// </summary>
        string QueueName { get; }

        /// <summary>
        /// Gets the consumer tag.
        /// </summary>
        string ConsumerTag { get; }

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
        /// Gets the service provider for resolving dependencies.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the cancellation token.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets a header value by key.
        /// </summary>
        /// <typeparam name="T">The expected type of the header value.</typeparam>
        /// <param name="key">The header key.</param>
        /// <returns>The header value or default if not found.</returns>
        T? GetHeader<T>(string key);

        /// <summary>
        /// Publishes a message to an exchange.
        /// </summary>
        /// <typeparam name="T">The type of message to publish.</typeparam>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Responds to a request-response message.
        /// </summary>
        /// <typeparam name="T">The type of response.</typeparam>
        /// <param name="response">The response message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RespondAsync<T>(T response, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Creates a new scope for dependency injection.
        /// </summary>
        /// <returns>A new service scope.</returns>
        IServiceScope CreateScope();
    }

    /// <summary>
    /// Service scope interface for scoped DI per message.
    /// </summary>
    public interface IServiceScope : IDisposable
    {
        /// <summary>
        /// Gets the service provider for this scope.
        /// </summary>
        IServiceProvider ServiceProvider { get; }
    }
}


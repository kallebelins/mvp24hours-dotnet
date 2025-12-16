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
    /// Interface for RabbitMQ client operations.
    /// </summary>
    public interface IMvpRabbitMQClient
    {
        /// <summary>
        /// Starts consuming messages from registered consumers.
        /// </summary>
        void Consume();

        /// <summary>
        /// Publishes a message to RabbitMQ.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="tokenDefault">Optional message ID/token.</param>
        /// <returns>The message ID/token.</returns>
        string Publish(object message, string routingKey, string? tokenDefault = null);

        /// <summary>
        /// Publishes a message with priority support.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="priority">Message priority (0-255).</param>
        /// <param name="tokenDefault">Optional message ID/token.</param>
        /// <returns>The message ID/token.</returns>
        string Publish(object message, string routingKey, byte priority, string? tokenDefault = null);

        /// <summary>
        /// Publishes a message with custom headers (for headers exchange).
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="headers">Custom headers to include.</param>
        /// <param name="tokenDefault">Optional message ID/token.</param>
        /// <returns>The message ID/token.</returns>
        string Publish(object message, string routingKey, IDictionary<string, object> headers, string? tokenDefault = null);

        /// <summary>
        /// Publishes a message with TTL (Time-To-Live).
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="ttlMilliseconds">Message TTL in milliseconds.</param>
        /// <param name="tokenDefault">Optional message ID/token.</param>
        /// <returns>The message ID/token.</returns>
        string PublishWithTtl(object message, string routingKey, int ttlMilliseconds, string? tokenDefault = null);

        /// <summary>
        /// Publishes a message asynchronously.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="tokenDefault">Optional message ID/token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The message ID/token.</returns>
        Task<string> PublishAsync(object message, string routingKey, string? tokenDefault = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes multiple messages in a batch.
        /// </summary>
        /// <param name="messages">The messages to publish (with routing keys).</param>
        /// <returns>Collection of message IDs.</returns>
        IEnumerable<string> PublishBatch(IEnumerable<(object Message, string RoutingKey)> messages);

        /// <summary>
        /// Publishes multiple messages in a batch asynchronously.
        /// </summary>
        /// <param name="messages">The messages to publish (with routing keys).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of message IDs.</returns>
        Task<IEnumerable<string>> PublishBatchAsync(IEnumerable<(object Message, string RoutingKey)> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers a consumer type for message consumption.
        /// </summary>
        /// <param name="consumerType">The consumer type to register.</param>
        void Register(Type consumerType);

        /// <summary>
        /// Registers a consumer type for message consumption.
        /// </summary>
        /// <typeparam name="T">The consumer type to register.</typeparam>
        void Register<T>() where T : class, IMvpRabbitMQConsumer;

        /// <summary>
        /// Unregisters a consumer type.
        /// </summary>
        /// <param name="consumerType">The consumer type to unregister.</param>
        void Unregister(Type consumerType);

        /// <summary>
        /// Unregisters a consumer type.
        /// </summary>
        /// <typeparam name="T">The consumer type to unregister.</typeparam>
        void Unregister<T>() where T : class, IMvpRabbitMQConsumer;
    }
}
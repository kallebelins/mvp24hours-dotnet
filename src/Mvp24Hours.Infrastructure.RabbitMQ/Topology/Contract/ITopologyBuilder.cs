//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Topology.Contract
{
    /// <summary>
    /// Interface for building RabbitMQ topology (exchanges, queues, bindings).
    /// </summary>
    public interface ITopologyBuilder
    {
        /// <summary>
        /// Declares an exchange with the specified configuration.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="exchangeType">The exchange type (direct, topic, fanout, headers).</param>
        /// <param name="durable">Whether the exchange is durable.</param>
        /// <param name="autoDelete">Whether the exchange should auto-delete.</param>
        /// <param name="arguments">Additional exchange arguments.</param>
        void DeclareExchange(
            IModel channel,
            string exchangeName,
            string exchangeType,
            bool durable = true,
            bool autoDelete = false,
            IDictionary<string, object>? arguments = null);

        /// <summary>
        /// Declares a queue with the specified configuration.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="durable">Whether the queue is durable.</param>
        /// <param name="exclusive">Whether the queue is exclusive.</param>
        /// <param name="autoDelete">Whether the queue should auto-delete.</param>
        /// <param name="arguments">Additional queue arguments.</param>
        void DeclareQueue(
            IModel channel,
            string queueName,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            IDictionary<string, object>? arguments = null);

        /// <summary>
        /// Binds a queue to an exchange with the specified routing key.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="routingKey">The routing key (supports wildcards for topic exchanges).</param>
        /// <param name="arguments">Additional binding arguments.</param>
        void BindQueue(
            IModel channel,
            string queueName,
            string exchangeName,
            string routingKey,
            IDictionary<string, object>? arguments = null);

        /// <summary>
        /// Binds an exchange to another exchange (exchange-to-exchange binding).
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="destinationExchange">The destination exchange name.</param>
        /// <param name="sourceExchange">The source exchange name.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="arguments">Additional binding arguments.</param>
        void BindExchange(
            IModel channel,
            string destinationExchange,
            string sourceExchange,
            string routingKey,
            IDictionary<string, object>? arguments = null);

        /// <summary>
        /// Unbinds a queue from an exchange.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="arguments">Additional arguments.</param>
        void UnbindQueue(
            IModel channel,
            string queueName,
            string exchangeName,
            string routingKey,
            IDictionary<string, object>? arguments = null);

        /// <summary>
        /// Unbinds an exchange from another exchange.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="destinationExchange">The destination exchange name.</param>
        /// <param name="sourceExchange">The source exchange name.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="arguments">Additional arguments.</param>
        void UnbindExchange(
            IModel channel,
            string destinationExchange,
            string sourceExchange,
            string routingKey,
            IDictionary<string, object>? arguments = null);

        /// <summary>
        /// Deletes an exchange.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="exchangeName">The exchange name to delete.</param>
        /// <param name="ifUnused">Only delete if the exchange has no bindings.</param>
        void DeleteExchange(IModel channel, string exchangeName, bool ifUnused = false);

        /// <summary>
        /// Deletes a queue.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="queueName">The queue name to delete.</param>
        /// <param name="ifUnused">Only delete if the queue has no consumers.</param>
        /// <param name="ifEmpty">Only delete if the queue is empty.</param>
        uint DeleteQueue(IModel channel, string queueName, bool ifUnused = false, bool ifEmpty = false);

        /// <summary>
        /// Purges all messages from a queue.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="queueName">The queue name to purge.</param>
        /// <returns>The number of messages purged.</returns>
        uint PurgeQueue(IModel channel, string queueName);

        /// <summary>
        /// Auto-configures topology for a message type based on conventions.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="messageType">The message type.</param>
        void ConfigureTopologyForMessage(IModel channel, Type messageType);

        /// <summary>
        /// Auto-configures topology for a consumer type based on conventions.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="consumerType">The consumer type.</param>
        void ConfigureTopologyForConsumer(IModel channel, Type consumerType);
    }
}


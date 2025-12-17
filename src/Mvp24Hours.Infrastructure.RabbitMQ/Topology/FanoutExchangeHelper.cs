//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Topology
{
    /// <summary>
    /// Helper class for working with RabbitMQ fanout exchanges.
    /// Fanout exchanges broadcast messages to all bound queues, ignoring routing keys.
    /// </summary>
    public static class FanoutExchangeHelper
    {
        /// <summary>
        /// Creates a fanout exchange.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="durable">Whether the exchange is durable.</param>
        /// <param name="autoDelete">Whether the exchange should auto-delete.</param>
        /// <param name="arguments">Additional exchange arguments.</param>
        public static void DeclareFanoutExchange(
            IModel channel,
            string exchangeName,
            bool durable = true,
            bool autoDelete = false,
            IDictionary<string, object>? arguments = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(exchangeName);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "fanout-exchange-declare",
                $"exchange:{exchangeName}|durable:{durable}|autoDelete:{autoDelete}");

            channel.ExchangeDeclare(
                exchange: exchangeName,
                type: ExchangeType.Fanout,
                durable: durable,
                autoDelete: autoDelete,
                arguments: arguments);
        }

        /// <summary>
        /// Binds a queue to a fanout exchange.
        /// Note: Routing key is ignored for fanout exchanges.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="exchangeName">The exchange name.</param>
        public static void BindQueueToFanout(
            IModel channel,
            string queueName,
            string exchangeName)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(queueName);
            ArgumentNullException.ThrowIfNull(exchangeName);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "fanout-exchange-bind-queue",
                $"queue:{queueName}|exchange:{exchangeName}");

            // Routing key is ignored for fanout exchanges, but we still need to pass it
            channel.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: string.Empty);
        }

        /// <summary>
        /// Binds multiple queues to a fanout exchange.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="queueNames">The queue names to bind.</param>
        /// <param name="exchangeName">The exchange name.</param>
        public static void BindQueuesToFanout(
            IModel channel,
            IEnumerable<string> queueNames,
            string exchangeName)
        {
            ArgumentNullException.ThrowIfNull(queueNames);

            foreach (var queueName in queueNames)
            {
                BindQueueToFanout(channel, queueName, exchangeName);
            }
        }

        /// <summary>
        /// Creates a fanout exchange and binds it to multiple queues.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="queueNames">The queue names to bind.</param>
        /// <param name="durable">Whether the exchange is durable.</param>
        public static void SetupFanoutWithQueues(
            IModel channel,
            string exchangeName,
            IEnumerable<string> queueNames,
            bool durable = true)
        {
            DeclareFanoutExchange(channel, exchangeName, durable);
            BindQueuesToFanout(channel, queueNames, exchangeName);
        }

        /// <summary>
        /// Unbinds a queue from a fanout exchange.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="exchangeName">The exchange name.</param>
        public static void UnbindQueueFromFanout(
            IModel channel,
            string queueName,
            string exchangeName)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(queueName);
            ArgumentNullException.ThrowIfNull(exchangeName);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "fanout-exchange-unbind-queue",
                $"queue:{queueName}|exchange:{exchangeName}");

            channel.QueueUnbind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: string.Empty);
        }

        /// <summary>
        /// Publishes a message to a fanout exchange.
        /// The message will be delivered to all bound queues.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="body">The message body.</param>
        /// <param name="properties">Optional message properties.</param>
        public static void PublishToFanout(
            IModel channel,
            string exchangeName,
            ReadOnlyMemory<byte> body,
            IBasicProperties? properties = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(exchangeName);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "fanout-exchange-publish",
                $"exchange:{exchangeName}|bodySize:{body.Length}");

            // Routing key is ignored for fanout exchanges
            channel.BasicPublish(
                exchange: exchangeName,
                routingKey: string.Empty,
                basicProperties: properties,
                body: body);
        }
    }
}


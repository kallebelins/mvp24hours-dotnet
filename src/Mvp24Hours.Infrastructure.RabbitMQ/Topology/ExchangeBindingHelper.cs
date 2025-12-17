//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Enums;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Topology
{
    /// <summary>
    /// Helper class for creating exchange-to-exchange bindings in RabbitMQ.
    /// Exchange-to-exchange bindings allow routing messages between exchanges,
    /// enabling complex routing topologies.
    /// </summary>
    public static class ExchangeBindingHelper
    {
        /// <summary>
        /// Binds a destination exchange to a source exchange.
        /// Messages published to the source exchange will be routed to the destination
        /// based on the routing key and exchange type.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="destinationExchange">The destination exchange name.</param>
        /// <param name="sourceExchange">The source exchange name.</param>
        /// <param name="routingKey">The routing key for the binding.</param>
        /// <param name="arguments">Additional binding arguments.</param>
        public static void BindExchanges(
            IModel channel,
            string destinationExchange,
            string sourceExchange,
            string routingKey = "",
            IDictionary<string, object>? arguments = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(destinationExchange);
            ArgumentNullException.ThrowIfNull(sourceExchange);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "exchange-binding-create",
                $"destination:{destinationExchange}|source:{sourceExchange}|routingKey:{routingKey}");

            channel.ExchangeBind(
                destination: destinationExchange,
                source: sourceExchange,
                routingKey: routingKey,
                arguments: arguments);
        }

        /// <summary>
        /// Unbinds a destination exchange from a source exchange.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="destinationExchange">The destination exchange name.</param>
        /// <param name="sourceExchange">The source exchange name.</param>
        /// <param name="routingKey">The routing key for the binding.</param>
        /// <param name="arguments">Additional binding arguments.</param>
        public static void UnbindExchanges(
            IModel channel,
            string destinationExchange,
            string sourceExchange,
            string routingKey = "",
            IDictionary<string, object>? arguments = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(destinationExchange);
            ArgumentNullException.ThrowIfNull(sourceExchange);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "exchange-binding-remove",
                $"destination:{destinationExchange}|source:{sourceExchange}|routingKey:{routingKey}");

            channel.ExchangeUnbind(
                destination: destinationExchange,
                source: sourceExchange,
                routingKey: routingKey,
                arguments: arguments);
        }

        /// <summary>
        /// Creates a hierarchical exchange structure where child exchanges receive messages from a parent.
        /// Useful for creating a message routing hierarchy.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="parentExchange">The parent exchange name.</param>
        /// <param name="childExchanges">The child exchange names.</param>
        /// <param name="exchangeType">The exchange type for all exchanges.</param>
        /// <param name="durable">Whether the exchanges are durable.</param>
        public static void CreateExchangeHierarchy(
            IModel channel,
            string parentExchange,
            IEnumerable<string> childExchanges,
            MvpRabbitMQExchangeType exchangeType = MvpRabbitMQExchangeType.topic,
            bool durable = true)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(parentExchange);
            ArgumentNullException.ThrowIfNull(childExchanges);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "exchange-hierarchy-create",
                $"parent:{parentExchange}|type:{exchangeType}");

            // Declare parent exchange
            channel.ExchangeDeclare(
                exchange: parentExchange,
                type: exchangeType.ToString(),
                durable: durable,
                autoDelete: false);

            // Declare and bind child exchanges
            foreach (var childExchange in childExchanges)
            {
                channel.ExchangeDeclare(
                    exchange: childExchange,
                    type: exchangeType.ToString(),
                    durable: durable,
                    autoDelete: false);

                // Bind child to receive all messages from parent
                var routingKey = exchangeType == MvpRabbitMQExchangeType.topic ? "#" : "";
                BindExchanges(channel, childExchange, parentExchange, routingKey);
            }
        }

        /// <summary>
        /// Creates a fan-out topology where multiple exchanges receive from a single source.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="sourceExchange">The source exchange name.</param>
        /// <param name="destinationExchanges">The destination exchange names.</param>
        /// <param name="durable">Whether the exchanges are durable.</param>
        public static void CreateFanOutTopology(
            IModel channel,
            string sourceExchange,
            IEnumerable<string> destinationExchanges,
            bool durable = true)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(sourceExchange);
            ArgumentNullException.ThrowIfNull(destinationExchanges);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "exchange-fanout-topology-create",
                $"source:{sourceExchange}");

            // Declare source as fanout
            channel.ExchangeDeclare(
                exchange: sourceExchange,
                type: ExchangeType.Fanout,
                durable: durable,
                autoDelete: false);

            foreach (var destExchange in destinationExchanges)
            {
                // Declare destination (can be any type)
                channel.ExchangeDeclare(
                    exchange: destExchange,
                    type: ExchangeType.Direct,
                    durable: durable,
                    autoDelete: false);

                // Bind destination to source
                BindExchanges(channel, destExchange, sourceExchange);
            }
        }

        /// <summary>
        /// Creates a message aggregation topology where messages from multiple sources
        /// are routed to a single destination exchange.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="sourceExchanges">The source exchange names.</param>
        /// <param name="destinationExchange">The destination exchange name.</param>
        /// <param name="exchangeType">The exchange type for the destination.</param>
        /// <param name="durable">Whether the exchanges are durable.</param>
        public static void CreateAggregationTopology(
            IModel channel,
            IEnumerable<string> sourceExchanges,
            string destinationExchange,
            MvpRabbitMQExchangeType exchangeType = MvpRabbitMQExchangeType.direct,
            bool durable = true)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(sourceExchanges);
            ArgumentNullException.ThrowIfNull(destinationExchange);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "exchange-aggregation-topology-create",
                $"destination:{destinationExchange}|type:{exchangeType}");

            // Declare destination exchange
            channel.ExchangeDeclare(
                exchange: destinationExchange,
                type: exchangeType.ToString(),
                durable: durable,
                autoDelete: false);

            foreach (var sourceExchange in sourceExchanges)
            {
                // Declare source
                channel.ExchangeDeclare(
                    exchange: sourceExchange,
                    type: exchangeType.ToString(),
                    durable: durable,
                    autoDelete: false);

                // Bind destination to receive from source
                var routingKey = exchangeType == MvpRabbitMQExchangeType.topic ? "#" : "";
                BindExchanges(channel, destinationExchange, sourceExchange, routingKey);
            }
        }

        /// <summary>
        /// Creates a content-based routing topology using topic exchanges.
        /// Routes messages to different exchanges based on routing key patterns.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="sourceExchange">The source exchange name.</param>
        /// <param name="routingRules">Dictionary of destination exchanges and their routing key patterns.</param>
        /// <param name="durable">Whether the exchanges are durable.</param>
        public static void CreateContentBasedRouter(
            IModel channel,
            string sourceExchange,
            IDictionary<string, string> routingRules,
            bool durable = true)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(sourceExchange);
            ArgumentNullException.ThrowIfNull(routingRules);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "exchange-cbr-topology-create",
                $"source:{sourceExchange}|rules:{routingRules.Count}");

            // Declare source as topic exchange
            channel.ExchangeDeclare(
                exchange: sourceExchange,
                type: ExchangeType.Topic,
                durable: durable,
                autoDelete: false);

            foreach (var rule in routingRules)
            {
                var destExchange = rule.Key;
                var routingKeyPattern = rule.Value;

                // Declare destination
                channel.ExchangeDeclare(
                    exchange: destExchange,
                    type: ExchangeType.Direct,
                    durable: durable,
                    autoDelete: false);

                // Bind with routing pattern
                BindExchanges(channel, destExchange, sourceExchange, routingKeyPattern);
            }
        }

        /// <summary>
        /// Creates a dead letter exchange setup for message handling failures.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="mainExchange">The main exchange name.</param>
        /// <param name="deadLetterExchange">The dead letter exchange name.</param>
        /// <param name="deadLetterQueue">The dead letter queue name.</param>
        /// <param name="durable">Whether the exchanges are durable.</param>
        public static void SetupDeadLetterExchange(
            IModel channel,
            string mainExchange,
            string deadLetterExchange,
            string deadLetterQueue,
            bool durable = true)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(mainExchange);
            ArgumentNullException.ThrowIfNull(deadLetterExchange);
            ArgumentNullException.ThrowIfNull(deadLetterQueue);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "exchange-dlx-setup",
                $"main:{mainExchange}|dlx:{deadLetterExchange}|dlq:{deadLetterQueue}");

            // Declare DLX
            channel.ExchangeDeclare(
                exchange: deadLetterExchange,
                type: ExchangeType.Direct,
                durable: durable,
                autoDelete: false);

            // Declare DLQ
            channel.QueueDeclare(
                queue: deadLetterQueue,
                durable: durable,
                exclusive: false,
                autoDelete: false);

            // Bind DLQ to DLX
            channel.QueueBind(
                queue: deadLetterQueue,
                exchange: deadLetterExchange,
                routingKey: deadLetterQueue);
        }
    }
}


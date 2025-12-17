//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Enums;
using Mvp24Hours.Infrastructure.RabbitMQ.Topology.Contract;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Topology
{
    /// <summary>
    /// Default implementation of <see cref="ITopologyBuilder"/> for building
    /// RabbitMQ topology (exchanges, queues, bindings).
    /// </summary>
    public class TopologyBuilder : ITopologyBuilder
    {
        private readonly IEndpointNameFormatter _nameFormatter;
        private readonly IRoutingKeyConvention _routingKeyConvention;
        private readonly TopologyBuilderOptions _options;

        /// <summary>
        /// Creates a new instance of <see cref="TopologyBuilder"/> with default settings.
        /// </summary>
        public TopologyBuilder()
            : this(EndpointNameFormatter.Instance, RoutingKeyConvention.Instance, new TopologyBuilderOptions())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="TopologyBuilder"/> with custom settings.
        /// </summary>
        /// <param name="nameFormatter">The name formatter to use.</param>
        /// <param name="routingKeyConvention">The routing key convention to use.</param>
        /// <param name="options">The topology builder options.</param>
        public TopologyBuilder(
            IEndpointNameFormatter nameFormatter,
            IRoutingKeyConvention routingKeyConvention,
            TopologyBuilderOptions options)
        {
            _nameFormatter = nameFormatter ?? throw new ArgumentNullException(nameof(nameFormatter));
            _routingKeyConvention = routingKeyConvention ?? throw new ArgumentNullException(nameof(routingKeyConvention));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public void DeclareExchange(
            IModel channel,
            string exchangeName,
            string exchangeType,
            bool durable = true,
            bool autoDelete = false,
            IDictionary<string, object>? arguments = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(exchangeName);
            ArgumentNullException.ThrowIfNull(exchangeType);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "topology-builder-declare-exchange",
                $"exchange:{exchangeName}|type:{exchangeType}|durable:{durable}|autoDelete:{autoDelete}");

            channel.ExchangeDeclare(
                exchange: exchangeName,
                type: exchangeType,
                durable: durable,
                autoDelete: autoDelete,
                arguments: arguments);
        }

        /// <inheritdoc />
        public void DeclareQueue(
            IModel channel,
            string queueName,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            IDictionary<string, object>? arguments = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(queueName);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "topology-builder-declare-queue",
                $"queue:{queueName}|durable:{durable}|exclusive:{exclusive}|autoDelete:{autoDelete}");

            channel.QueueDeclare(
                queue: queueName,
                durable: durable,
                exclusive: exclusive,
                autoDelete: autoDelete,
                arguments: arguments);
        }

        /// <inheritdoc />
        public void BindQueue(
            IModel channel,
            string queueName,
            string exchangeName,
            string routingKey,
            IDictionary<string, object>? arguments = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(queueName);
            ArgumentNullException.ThrowIfNull(exchangeName);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "topology-builder-bind-queue",
                $"queue:{queueName}|exchange:{exchangeName}|routingKey:{routingKey}");

            channel.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey ?? string.Empty,
                arguments: arguments);
        }

        /// <inheritdoc />
        public void BindExchange(
            IModel channel,
            string destinationExchange,
            string sourceExchange,
            string routingKey,
            IDictionary<string, object>? arguments = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(destinationExchange);
            ArgumentNullException.ThrowIfNull(sourceExchange);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "topology-builder-bind-exchange",
                $"destination:{destinationExchange}|source:{sourceExchange}|routingKey:{routingKey}");

            channel.ExchangeBind(
                destination: destinationExchange,
                source: sourceExchange,
                routingKey: routingKey ?? string.Empty,
                arguments: arguments);
        }

        /// <inheritdoc />
        public void UnbindQueue(
            IModel channel,
            string queueName,
            string exchangeName,
            string routingKey,
            IDictionary<string, object>? arguments = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(queueName);
            ArgumentNullException.ThrowIfNull(exchangeName);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "topology-builder-unbind-queue",
                $"queue:{queueName}|exchange:{exchangeName}|routingKey:{routingKey}");

            channel.QueueUnbind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey ?? string.Empty,
                arguments: arguments);
        }

        /// <inheritdoc />
        public void UnbindExchange(
            IModel channel,
            string destinationExchange,
            string sourceExchange,
            string routingKey,
            IDictionary<string, object>? arguments = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(destinationExchange);
            ArgumentNullException.ThrowIfNull(sourceExchange);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "topology-builder-unbind-exchange",
                $"destination:{destinationExchange}|source:{sourceExchange}|routingKey:{routingKey}");

            channel.ExchangeUnbind(
                destination: destinationExchange,
                source: sourceExchange,
                routingKey: routingKey ?? string.Empty,
                arguments: arguments);
        }

        /// <inheritdoc />
        public void DeleteExchange(IModel channel, string exchangeName, bool ifUnused = false)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(exchangeName);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "topology-builder-delete-exchange",
                $"exchange:{exchangeName}|ifUnused:{ifUnused}");

            channel.ExchangeDelete(exchangeName, ifUnused);
        }

        /// <inheritdoc />
        public uint DeleteQueue(IModel channel, string queueName, bool ifUnused = false, bool ifEmpty = false)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(queueName);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "topology-builder-delete-queue",
                $"queue:{queueName}|ifUnused:{ifUnused}|ifEmpty:{ifEmpty}");

            return channel.QueueDelete(queueName, ifUnused, ifEmpty);
        }

        /// <inheritdoc />
        public uint PurgeQueue(IModel channel, string queueName)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(queueName);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "topology-builder-purge-queue",
                $"queue:{queueName}");

            return channel.QueuePurge(queueName);
        }

        /// <inheritdoc />
        public void ConfigureTopologyForMessage(IModel channel, Type messageType)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(messageType);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "topology-builder-configure-message",
                $"messageType:{messageType.FullName}");

            // Get topology configuration (registered or default)
            var topology = MessageTopologyRegistry.Instance.GetTopology(messageType);

            // Determine names from topology or conventions
            var exchangeName = topology?.ExchangeName ?? _nameFormatter.FormatExchangeName(messageType);
            var exchangeType = topology?.ExchangeType ?? _options.DefaultExchangeType;
            var durable = topology?.Durable ?? _options.DefaultDurable;
            var autoDelete = topology?.AutoDelete ?? _options.DefaultAutoDelete;
            var arguments = topology?.ExchangeArguments;

            // Declare exchange
            DeclareExchange(channel, exchangeName, exchangeType.ToString(), durable, autoDelete, arguments);

            // If configuring dead letter queue
            if (_options.AutoConfigureDeadLetter)
            {
                var dlxName = _nameFormatter.FormatDeadLetterExchangeName(exchangeName);
                var dlqName = _nameFormatter.FormatDeadLetterQueueName(_nameFormatter.FormatQueueNameFromMessage(messageType));

                DeclareExchange(channel, dlxName, ExchangeType.Direct, durable, false);
                DeclareQueue(channel, dlqName, durable, false, false);
                BindQueue(channel, dlqName, dlxName, dlqName);
            }
        }

        /// <inheritdoc />
        public void ConfigureTopologyForConsumer(IModel channel, Type consumerType)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(consumerType);

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "topology-builder-configure-consumer",
                $"consumerType:{consumerType.FullName}");

            // Get message type from consumer
            var messageType = GetMessageTypeFromConsumer(consumerType);
            if (messageType == null)
            {
                TelemetryHelper.Execute(TelemetryLevels.Warning, "topology-builder-no-message-type",
                    $"Could not determine message type for consumer: {consumerType.FullName}");
                return;
            }

            // Configure topology for the message type
            ConfigureTopologyForMessage(channel, messageType);

            // Get topology and naming
            var topology = MessageTopologyRegistry.Instance.GetTopology(messageType);
            var exchangeName = topology?.ExchangeName ?? _nameFormatter.FormatExchangeName(messageType);
            var queueName = _nameFormatter.FormatQueueName(consumerType);
            var routingKey = _routingKeyConvention.GetSubscriptionPattern(consumerType, messageType);
            var durable = topology?.Durable ?? _options.DefaultDurable;

            // Build queue arguments
            var queueArguments = new Dictionary<string, object>();

            // Add dead letter exchange if configured
            if (_options.AutoConfigureDeadLetter)
            {
                var dlxName = _nameFormatter.FormatDeadLetterExchangeName(exchangeName);
                queueArguments["x-dead-letter-exchange"] = dlxName;
                queueArguments["x-dead-letter-routing-key"] = _nameFormatter.FormatDeadLetterQueueName(queueName);
            }

            // Add message TTL if configured
            if (topology?.MessageTtlMilliseconds > 0)
            {
                queueArguments["x-message-ttl"] = topology.MessageTtlMilliseconds.Value;
            }

            // Declare queue
            DeclareQueue(channel, queueName, durable, false, false, queueArguments.Count > 0 ? queueArguments : null);

            // Bind queue to exchange
            BindQueue(channel, queueName, exchangeName, routingKey);
        }

        private static Type? GetMessageTypeFromConsumer(Type consumerType)
        {
            var consumerInterface = consumerType
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>));

            return consumerInterface?.GetGenericArguments().FirstOrDefault();
        }
    }

    /// <summary>
    /// Options for the topology builder.
    /// </summary>
    public class TopologyBuilderOptions
    {
        /// <summary>
        /// Gets or sets the default exchange type. Default is direct.
        /// </summary>
        public MvpRabbitMQExchangeType DefaultExchangeType { get; set; } = MvpRabbitMQExchangeType.direct;

        /// <summary>
        /// Gets or sets whether exchanges/queues are durable by default. Default is true.
        /// </summary>
        public bool DefaultDurable { get; set; } = true;

        /// <summary>
        /// Gets or sets whether exchanges/queues auto-delete by default. Default is false.
        /// </summary>
        public bool DefaultAutoDelete { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to automatically configure dead letter queues. Default is true.
        /// </summary>
        public bool AutoConfigureDeadLetter { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to automatically configure retry queues. Default is false.
        /// </summary>
        public bool AutoConfigureRetryQueues { get; set; } = false;

        /// <summary>
        /// Gets or sets the number of retry levels to configure. Default is 3.
        /// </summary>
        public int RetryLevels { get; set; } = 3;
    }
}


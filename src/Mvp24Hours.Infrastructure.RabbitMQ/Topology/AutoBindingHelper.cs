//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Enums;
using Mvp24Hours.Infrastructure.RabbitMQ.Topology.Contract;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Topology
{
    /// <summary>
    /// Helper class for automatic binding of consumers to queues based on type conventions.
    /// </summary>
    public class AutoBindingHelper
    {
        private readonly IEndpointNameFormatter _nameFormatter;
        private readonly IRoutingKeyConvention _routingKeyConvention;
        private readonly ITopologyBuilder _topologyBuilder;
        private readonly AutoBindingOptions _options;
        private readonly ILogger<AutoBindingHelper>? _logger;

        /// <summary>
        /// Creates a new instance of <see cref="AutoBindingHelper"/> with default settings.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public AutoBindingHelper(ILogger<AutoBindingHelper>? logger = null)
            : this(
                EndpointNameFormatter.Instance,
                RoutingKeyConvention.Instance,
                new TopologyBuilder(),
                new AutoBindingOptions(),
                logger)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="AutoBindingHelper"/> with custom settings.
        /// </summary>
        /// <param name="nameFormatter">The name formatter to use.</param>
        /// <param name="routingKeyConvention">The routing key convention to use.</param>
        /// <param name="topologyBuilder">The topology builder to use.</param>
        /// <param name="options">The auto-binding options.</param>
        /// <param name="logger">Optional logger instance.</param>
        public AutoBindingHelper(
            IEndpointNameFormatter nameFormatter,
            IRoutingKeyConvention routingKeyConvention,
            ITopologyBuilder topologyBuilder,
            AutoBindingOptions options,
            ILogger<AutoBindingHelper>? logger = null)
        {
            _nameFormatter = nameFormatter ?? throw new ArgumentNullException(nameof(nameFormatter));
            _routingKeyConvention = routingKeyConvention ?? throw new ArgumentNullException(nameof(routingKeyConvention));
            _topologyBuilder = topologyBuilder ?? throw new ArgumentNullException(nameof(topologyBuilder));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <summary>
        /// Auto-configures topology for a consumer type.
        /// Creates exchange, queue, and bindings based on conventions.
        /// </summary>
        /// <typeparam name="TConsumer">The consumer type.</typeparam>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <returns>The binding information.</returns>
        public ConsumerBindingInfo AutoBindConsumer<TConsumer>(IModel channel)
            where TConsumer : class
        {
            return AutoBindConsumer(channel, typeof(TConsumer));
        }

        /// <summary>
        /// Auto-configures topology for a consumer type.
        /// Creates exchange, queue, and bindings based on conventions.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="consumerType">The consumer type.</param>
        /// <returns>The binding information.</returns>
        public ConsumerBindingInfo AutoBindConsumer(IModel channel, Type consumerType)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(consumerType);

            _logger?.LogDebug(
                "Auto-binding consumer. ConsumerType={ConsumerType}",
                consumerType.FullName);

            // Get message type from consumer
            var messageType = GetMessageTypeFromConsumer(consumerType);
            if (messageType == null)
            {
                throw new InvalidOperationException(
                    $"Could not determine message type for consumer: {consumerType.FullName}. " +
                    $"Ensure the consumer implements IMessageConsumer<TMessage>.");
            }

            // Get or generate names
            var queueName = GetQueueName(consumerType, messageType);
            var exchangeName = GetExchangeName(messageType);
            var exchangeType = GetExchangeType(messageType);
            var routingKey = GetRoutingKey(consumerType, messageType, exchangeType);

            // Build queue arguments
            var queueArguments = BuildQueueArguments(messageType, exchangeName);

            // Declare exchange
            _topologyBuilder.DeclareExchange(
                channel,
                exchangeName,
                exchangeType.ToString(),
                _options.Durable,
                _options.AutoDelete);

            // Declare queue
            _topologyBuilder.DeclareQueue(
                channel,
                queueName,
                _options.Durable,
                false,
                _options.AutoDelete,
                queueArguments.Count > 0 ? queueArguments : null);

            // Bind queue to exchange
            _topologyBuilder.BindQueue(
                channel,
                queueName,
                exchangeName,
                routingKey);

            _logger?.LogInformation(
                "Auto-binding complete. Queue={QueueName}, Exchange={ExchangeName}, RoutingKey={RoutingKey}",
                queueName, exchangeName, routingKey);

            return new ConsumerBindingInfo
            {
                ConsumerType = consumerType,
                MessageType = messageType,
                QueueName = queueName,
                ExchangeName = exchangeName,
                ExchangeType = exchangeType,
                RoutingKey = routingKey
            };
        }

        /// <summary>
        /// Auto-configures topology for all consumers in an assembly.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="assembly">The assembly to scan for consumers.</param>
        /// <returns>The binding information for all consumers.</returns>
        public IEnumerable<ConsumerBindingInfo> AutoBindConsumersFromAssembly(IModel channel, Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(assembly);

            var consumerTypes = assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>)));

            var results = new List<ConsumerBindingInfo>();

            foreach (var consumerType in consumerTypes)
            {
                try
                {
                    var bindingInfo = AutoBindConsumer(channel, consumerType);
                    results.Add(bindingInfo);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex,
                        "Auto-binding consumer failed. ConsumerType={ConsumerType}",
                        consumerType.FullName);

                    if (!_options.ContinueOnError)
                        throw;
                }
            }

            return results;
        }

        /// <summary>
        /// Auto-configures topology for a message type.
        /// Creates exchange and default queue based on conventions.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <returns>The binding information.</returns>
        public MessageBindingInfo AutoBindMessage<TMessage>(IModel channel)
            where TMessage : class
        {
            return AutoBindMessage(channel, typeof(TMessage));
        }

        /// <summary>
        /// Auto-configures topology for a message type.
        /// Creates exchange and default queue based on conventions.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="messageType">The message type.</param>
        /// <returns>The binding information.</returns>
        public MessageBindingInfo AutoBindMessage(IModel channel, Type messageType)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(messageType);

            _logger?.LogDebug(
                "Auto-binding message. MessageType={MessageType}",
                messageType.FullName);

            // Get or generate names
            var exchangeName = GetExchangeName(messageType);
            var exchangeType = GetExchangeType(messageType);
            var queueName = _nameFormatter.FormatQueueNameFromMessage(messageType);
            var routingKey = _routingKeyConvention.GetRoutingKey(messageType);

            // Declare exchange
            _topologyBuilder.DeclareExchange(
                channel,
                exchangeName,
                exchangeType.ToString(),
                _options.Durable,
                _options.AutoDelete);

            // Declare default queue if enabled
            if (_options.CreateDefaultQueue)
            {
                var queueArguments = BuildQueueArguments(messageType, exchangeName);

                _topologyBuilder.DeclareQueue(
                    channel,
                    queueName,
                    _options.Durable,
                    false,
                    _options.AutoDelete,
                    queueArguments.Count > 0 ? queueArguments : null);

                _topologyBuilder.BindQueue(
                    channel,
                    queueName,
                    exchangeName,
                    routingKey);
            }

            return new MessageBindingInfo
            {
                MessageType = messageType,
                ExchangeName = exchangeName,
                ExchangeType = exchangeType,
                QueueName = _options.CreateDefaultQueue ? queueName : null,
                RoutingKey = routingKey
            };
        }

        private static Type? GetMessageTypeFromConsumer(Type consumerType)
        {
            var consumerInterface = consumerType
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>));

            return consumerInterface?.GetGenericArguments().FirstOrDefault();
        }

        private string GetQueueName(Type consumerType, Type messageType)
        {
            // Check for custom endpoint mapping
            var mapping = EndpointConvention.GetEndpoint(messageType);
            if (mapping?.QueueName != null)
                return mapping.QueueName;

            // Check for consumer definition
            var definition = GetConsumerDefinition(consumerType);
            if (definition?.QueueName != null)
                return definition.QueueName;

            // Use naming convention
            return _nameFormatter.FormatQueueName(consumerType);
        }

        private string GetExchangeName(Type messageType)
        {
            // Check for custom endpoint mapping
            var mapping = EndpointConvention.GetEndpoint(messageType);
            if (mapping?.ExchangeName != null)
                return mapping.ExchangeName;

            // Check for message topology
            var topology = MessageTopologyRegistry.Instance.GetTopology(messageType);
            if (topology?.ExchangeName != null)
                return topology.ExchangeName;

            // Use naming convention
            return _nameFormatter.FormatExchangeName(messageType);
        }

        private MvpRabbitMQExchangeType GetExchangeType(Type messageType)
        {
            // Check for message topology
            var topology = MessageTopologyRegistry.Instance.GetTopology(messageType);
            if (topology != null)
                return topology.ExchangeType;

            // Use default
            return _options.DefaultExchangeType;
        }

        private string GetRoutingKey(Type consumerType, Type messageType, MvpRabbitMQExchangeType exchangeType)
        {
            // Check for custom endpoint mapping
            var mapping = EndpointConvention.GetEndpoint(messageType);
            if (mapping?.RoutingKey != null)
                return mapping.RoutingKey;

            // Check for message topology
            var topology = MessageTopologyRegistry.Instance.GetTopology(messageType);
            if (topology?.RoutingKey != null)
                return topology.RoutingKey;

            // Fanout exchanges ignore routing key
            if (exchangeType == MvpRabbitMQExchangeType.fanout)
                return string.Empty;

            // Use routing key convention
            return _routingKeyConvention.GetSubscriptionPattern(consumerType, messageType);
        }

        private Dictionary<string, object> BuildQueueArguments(Type messageType, string exchangeName)
        {
            var arguments = new Dictionary<string, object>();

            // Dead letter exchange
            if (_options.ConfigureDeadLetter)
            {
                var dlxName = _nameFormatter.FormatDeadLetterExchangeName(exchangeName);
                arguments["x-dead-letter-exchange"] = dlxName;
            }

            // Message TTL
            var topology = MessageTopologyRegistry.Instance.GetTopology(messageType);
            if (topology?.MessageTtlMilliseconds > 0)
            {
                arguments["x-message-ttl"] = topology.MessageTtlMilliseconds.Value;
            }
            else if (_options.DefaultMessageTtlMilliseconds > 0)
            {
                arguments["x-message-ttl"] = _options.DefaultMessageTtlMilliseconds;
            }

            // Priority queue
            if (_options.EnablePriorityQueue)
            {
                arguments["x-max-priority"] = _options.MaxPriority;
            }

            return arguments;
        }

        private static IConsumerDefinition? GetConsumerDefinition(Type consumerType)
        {
            // Look for ConsumerDefinition<T> in the same assembly
            var definitionType = consumerType.Assembly.GetTypes()
                .FirstOrDefault(t => !t.IsAbstract &&
                    t.BaseType?.IsGenericType == true &&
                    t.BaseType.GetGenericTypeDefinition() == typeof(Consumers.ConsumerDefinition<>) &&
                    t.BaseType.GetGenericArguments().FirstOrDefault() == consumerType);

            if (definitionType != null)
            {
                return Activator.CreateInstance(definitionType) as IConsumerDefinition;
            }

            return null;
        }
    }

    /// <summary>
    /// Options for auto-binding.
    /// </summary>
    public class AutoBindingOptions
    {
        /// <summary>
        /// Gets or sets whether exchanges and queues are durable. Default is true.
        /// </summary>
        public bool Durable { get; set; } = true;

        /// <summary>
        /// Gets or sets whether exchanges and queues auto-delete. Default is false.
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to configure dead letter exchange. Default is true.
        /// </summary>
        public bool ConfigureDeadLetter { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to create a default queue for messages. Default is true.
        /// </summary>
        public bool CreateDefaultQueue { get; set; } = true;

        /// <summary>
        /// Gets or sets the default exchange type. Default is direct.
        /// </summary>
        public MvpRabbitMQExchangeType DefaultExchangeType { get; set; } = MvpRabbitMQExchangeType.direct;

        /// <summary>
        /// Gets or sets the default message TTL in milliseconds. Default is 0 (no TTL).
        /// </summary>
        public int DefaultMessageTtlMilliseconds { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether to enable priority queues. Default is false.
        /// </summary>
        public bool EnablePriorityQueue { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum priority for priority queues. Default is 10.
        /// </summary>
        public byte MaxPriority { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to continue on error when binding multiple consumers. Default is true.
        /// </summary>
        public bool ContinueOnError { get; set; } = true;
    }

    /// <summary>
    /// Information about a consumer binding.
    /// </summary>
    public class ConsumerBindingInfo
    {
        /// <summary>
        /// Gets or sets the consumer type.
        /// </summary>
        public Type ConsumerType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public Type MessageType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the queue name.
        /// </summary>
        public string QueueName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the exchange name.
        /// </summary>
        public string ExchangeName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the exchange type.
        /// </summary>
        public MvpRabbitMQExchangeType ExchangeType { get; set; }

        /// <summary>
        /// Gets or sets the routing key.
        /// </summary>
        public string RoutingKey { get; set; } = null!;
    }

    /// <summary>
    /// Information about a message binding.
    /// </summary>
    public class MessageBindingInfo
    {
        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public Type MessageType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the exchange name.
        /// </summary>
        public string ExchangeName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the exchange type.
        /// </summary>
        public MvpRabbitMQExchangeType ExchangeType { get; set; }

        /// <summary>
        /// Gets or sets the queue name (if created).
        /// </summary>
        public string? QueueName { get; set; }

        /// <summary>
        /// Gets or sets the routing key.
        /// </summary>
        public string RoutingKey { get; set; } = null!;
    }
}


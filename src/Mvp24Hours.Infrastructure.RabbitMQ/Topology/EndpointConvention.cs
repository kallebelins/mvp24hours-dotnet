//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Topology.Contract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Topology
{
    /// <summary>
    /// Global configuration for endpoint conventions.
    /// Provides centralized management of endpoint naming, routing, and topology conventions.
    /// </summary>
    public static class EndpointConvention
    {
        private static readonly ConcurrentDictionary<Type, EndpointInfo> _endpointMappings = new();
        private static IEndpointNameFormatter _nameFormatter = EndpointNameFormatter.Instance;
        private static IRoutingKeyConvention _routingKeyConvention = Mvp24Hours.Infrastructure.RabbitMQ.Topology.RoutingKeyConvention.Instance;
        private static EndpointConventionOptions _options = new();

        /// <summary>
        /// Gets or sets the global name formatter.
        /// </summary>
        public static IEndpointNameFormatter NameFormatter
        {
            get => _nameFormatter;
            set => _nameFormatter = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the global routing key convention.
        /// </summary>
        public static IRoutingKeyConvention RoutingKeyConvention
        {
            get => _routingKeyConvention;
            set => _routingKeyConvention = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the global convention options.
        /// </summary>
        public static EndpointConventionOptions Options
        {
            get => _options;
            set => _options = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Configures the global conventions with fluent API.
        /// </summary>
        /// <param name="configure">Configuration action.</param>
        public static void Configure(Action<EndpointConventionOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            configure(_options);
        }

        /// <summary>
        /// Maps a message type to a specific endpoint configuration.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="configure">Configuration action for the endpoint.</param>
        public static void Map<T>(Action<EndpointInfo> configure) where T : class
        {
            Map(typeof(T), configure);
        }

        /// <summary>
        /// Maps a message type to a specific endpoint configuration.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <param name="configure">Configuration action for the endpoint.</param>
        public static void Map(Type messageType, Action<EndpointInfo> configure)
        {
            ArgumentNullException.ThrowIfNull(messageType);
            ArgumentNullException.ThrowIfNull(configure);

            var info = new EndpointInfo();
            configure(info);
            _endpointMappings[messageType] = info;
        }

        /// <summary>
        /// Maps a message type to a specific exchange.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="routingKey">Optional routing key.</param>
        public static void MapToExchange<T>(string exchangeName, string? routingKey = null) where T : class
        {
            MapToExchange(typeof(T), exchangeName, routingKey);
        }

        /// <summary>
        /// Maps a message type to a specific exchange.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="routingKey">Optional routing key.</param>
        public static void MapToExchange(Type messageType, string exchangeName, string? routingKey = null)
        {
            Map(messageType, info =>
            {
                info.ExchangeName = exchangeName;
                info.RoutingKey = routingKey;
            });
        }

        /// <summary>
        /// Maps a message type to a specific queue.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="queueName">The queue name.</param>
        public static void MapToQueue<T>(string queueName) where T : class
        {
            MapToQueue(typeof(T), queueName);
        }

        /// <summary>
        /// Maps a message type to a specific queue.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <param name="queueName">The queue name.</param>
        public static void MapToQueue(Type messageType, string queueName)
        {
            Map(messageType, info => info.QueueName = queueName);
        }

        /// <summary>
        /// Gets the endpoint info for a message type.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>The endpoint info, or null if not mapped.</returns>
        public static EndpointInfo? GetEndpoint<T>() where T : class
        {
            return GetEndpoint(typeof(T));
        }

        /// <summary>
        /// Gets the endpoint info for a message type.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <returns>The endpoint info, or null if not mapped.</returns>
        public static EndpointInfo? GetEndpoint(Type messageType)
        {
            _endpointMappings.TryGetValue(messageType, out var info);
            return info;
        }

        /// <summary>
        /// Gets the exchange name for a message type, using mapping or convention.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>The exchange name.</returns>
        public static string GetExchangeName<T>() where T : class
        {
            return GetExchangeName(typeof(T));
        }

        /// <summary>
        /// Gets the exchange name for a message type, using mapping or convention.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <returns>The exchange name.</returns>
        public static string GetExchangeName(Type messageType)
        {
            var info = GetEndpoint(messageType);
            return info?.ExchangeName ?? _nameFormatter.FormatExchangeName(messageType);
        }

        /// <summary>
        /// Gets the routing key for a message type, using mapping or convention.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>The routing key.</returns>
        public static string GetRoutingKey<T>() where T : class
        {
            return GetRoutingKey(typeof(T));
        }

        /// <summary>
        /// Gets the routing key for a message type, using mapping or convention.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <returns>The routing key.</returns>
        public static string GetRoutingKey(Type messageType)
        {
            var info = GetEndpoint(messageType);
            return info?.RoutingKey ?? _routingKeyConvention.GetRoutingKey(messageType);
        }

        /// <summary>
        /// Gets the queue name for a message type, using mapping or convention.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>The queue name.</returns>
        public static string GetQueueName<T>() where T : class
        {
            return GetQueueName(typeof(T));
        }

        /// <summary>
        /// Gets the queue name for a message type, using mapping or convention.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <returns>The queue name.</returns>
        public static string GetQueueName(Type messageType)
        {
            var info = GetEndpoint(messageType);
            return info?.QueueName ?? _nameFormatter.FormatQueueNameFromMessage(messageType);
        }

        /// <summary>
        /// Removes an endpoint mapping.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <returns>True if the mapping was removed.</returns>
        public static bool Unmap<T>() where T : class
        {
            return Unmap(typeof(T));
        }

        /// <summary>
        /// Removes an endpoint mapping.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <returns>True if the mapping was removed.</returns>
        public static bool Unmap(Type messageType)
        {
            return _endpointMappings.TryRemove(messageType, out _);
        }

        /// <summary>
        /// Clears all endpoint mappings.
        /// </summary>
        public static void ClearMappings()
        {
            _endpointMappings.Clear();
        }

        /// <summary>
        /// Resets all conventions to defaults.
        /// </summary>
        public static void Reset()
        {
            _endpointMappings.Clear();
            _nameFormatter = EndpointNameFormatter.Instance;
            _routingKeyConvention = Mvp24Hours.Infrastructure.RabbitMQ.Topology.RoutingKeyConvention.Instance;
            _options = new EndpointConventionOptions();
        }

        /// <summary>
        /// Gets all registered endpoint mappings.
        /// </summary>
        /// <returns>All endpoint mappings.</returns>
        public static IEnumerable<KeyValuePair<Type, EndpointInfo>> GetAllMappings()
        {
            return _endpointMappings;
        }
    }

    /// <summary>
    /// Information about an endpoint (exchange, queue, routing key).
    /// </summary>
    public class EndpointInfo
    {
        /// <summary>
        /// Gets or sets the exchange name.
        /// </summary>
        public string? ExchangeName { get; set; }

        /// <summary>
        /// Gets or sets the queue name.
        /// </summary>
        public string? QueueName { get; set; }

        /// <summary>
        /// Gets or sets the routing key.
        /// </summary>
        public string? RoutingKey { get; set; }

        /// <summary>
        /// Gets or sets additional binding arguments.
        /// </summary>
        public IDictionary<string, object>? BindingArguments { get; set; }

        /// <summary>
        /// Gets or sets whether this endpoint is durable.
        /// </summary>
        public bool? Durable { get; set; }

        /// <summary>
        /// Gets or sets whether this endpoint auto-deletes.
        /// </summary>
        public bool? AutoDelete { get; set; }

        /// <summary>
        /// Gets or sets the prefetch count for consumers.
        /// </summary>
        public ushort? PrefetchCount { get; set; }
    }

    /// <summary>
    /// Global options for endpoint conventions.
    /// </summary>
    public class EndpointConventionOptions
    {
        /// <summary>
        /// Gets or sets the default separator for names. Default is ".".
        /// </summary>
        public string Separator { get; set; } = ".";

        /// <summary>
        /// Gets or sets the global prefix for all endpoints. Default is empty.
        /// </summary>
        public string? GlobalPrefix { get; set; }

        /// <summary>
        /// Gets or sets whether endpoints are durable by default. Default is true.
        /// </summary>
        public bool DefaultDurable { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to auto-create missing exchanges. Default is true.
        /// </summary>
        public bool AutoCreateExchanges { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to auto-create missing queues. Default is true.
        /// </summary>
        public bool AutoCreateQueues { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to auto-bind queues to exchanges. Default is true.
        /// </summary>
        public bool AutoBindQueues { get; set; } = true;

        /// <summary>
        /// Gets or sets the naming convention options.
        /// </summary>
        public EndpointNamingConventionOptions NamingOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the routing key convention options.
        /// </summary>
        public RoutingKeyConventionOptions RoutingKeyOptions { get; set; } = new();
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Enums;
using Mvp24Hours.Infrastructure.RabbitMQ.Topology.Contract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Topology
{
    /// <summary>
    /// Default implementation of <see cref="IMessageTopology{TMessage}"/>.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    public class MessageTopology<TMessage> : IMessageTopology<TMessage>
        where TMessage : class
    {
        /// <inheritdoc />
        public string? ExchangeName { get; set; }

        /// <inheritdoc />
        public MvpRabbitMQExchangeType ExchangeType { get; set; } = MvpRabbitMQExchangeType.direct;

        /// <inheritdoc />
        public string? RoutingKey { get; set; }

        /// <inheritdoc />
        public bool Durable { get; set; } = true;

        /// <inheritdoc />
        public bool AutoDelete { get; set; }

        /// <inheritdoc />
        public IDictionary<string, object>? ExchangeArguments { get; set; }

        /// <inheritdoc />
        public byte? DefaultPriority { get; set; }

        /// <inheritdoc />
        public int? MessageTtlMilliseconds { get; set; }

        /// <inheritdoc />
        public bool RequireAcknowledgment { get; set; } = true;

        /// <inheritdoc />
        public IDictionary<string, object>? DefaultHeaders { get; set; }

        /// <inheritdoc />
        public Type MessageType => typeof(TMessage);
    }

    /// <summary>
    /// Registry for managing message topologies.
    /// Thread-safe singleton for storing and retrieving topology configurations.
    /// </summary>
    public class MessageTopologyRegistry
    {
        private static readonly Lazy<MessageTopologyRegistry> _instance =
            new(() => new MessageTopologyRegistry());

        private readonly ConcurrentDictionary<Type, IMessageTopology> _topologies = new();

        /// <summary>
        /// Gets the singleton instance of the topology registry.
        /// </summary>
        public static MessageTopologyRegistry Instance => _instance.Value;

        private MessageTopologyRegistry()
        {
        }

        /// <summary>
        /// Registers a topology for a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="configure">Action to configure the topology.</param>
        public void Register<TMessage>(Action<IMessageTopology<TMessage>> configure)
            where TMessage : class
        {
            var topology = new MessageTopology<TMessage>();
            configure(topology);
            _topologies[typeof(TMessage)] = topology;
        }

        /// <summary>
        /// Registers a topology for a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="topology">The topology configuration.</param>
        public void Register<TMessage>(IMessageTopology<TMessage> topology)
            where TMessage : class
        {
            _topologies[typeof(TMessage)] = topology;
        }

        /// <summary>
        /// Gets the topology for a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <returns>The topology configuration, or a default topology if not registered.</returns>
        public IMessageTopology<TMessage> GetTopology<TMessage>()
            where TMessage : class
        {
            if (_topologies.TryGetValue(typeof(TMessage), out var topology))
            {
                return (IMessageTopology<TMessage>)topology;
            }

            return new MessageTopology<TMessage>();
        }

        /// <summary>
        /// Gets the topology for a message type.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <returns>The topology configuration, or null if not registered.</returns>
        public IMessageTopology? GetTopology(Type messageType)
        {
            _topologies.TryGetValue(messageType, out var topology);
            return topology;
        }

        /// <summary>
        /// Checks if a topology is registered for a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <returns>True if a topology is registered.</returns>
        public bool HasTopology<TMessage>()
            where TMessage : class
        {
            return _topologies.ContainsKey(typeof(TMessage));
        }

        /// <summary>
        /// Checks if a topology is registered for a message type.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <returns>True if a topology is registered.</returns>
        public bool HasTopology(Type messageType)
        {
            return _topologies.ContainsKey(messageType);
        }

        /// <summary>
        /// Removes a topology registration for a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <returns>True if the topology was removed.</returns>
        public bool Unregister<TMessage>()
            where TMessage : class
        {
            return _topologies.TryRemove(typeof(TMessage), out _);
        }

        /// <summary>
        /// Removes a topology registration for a message type.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <returns>True if the topology was removed.</returns>
        public bool Unregister(Type messageType)
        {
            return _topologies.TryRemove(messageType, out _);
        }

        /// <summary>
        /// Clears all registered topologies.
        /// </summary>
        public void Clear()
        {
            _topologies.Clear();
        }

        /// <summary>
        /// Gets all registered topologies.
        /// </summary>
        /// <returns>All registered topologies.</returns>
        public IEnumerable<IMessageTopology> GetAllTopologies()
        {
            return _topologies.Values;
        }
    }
}


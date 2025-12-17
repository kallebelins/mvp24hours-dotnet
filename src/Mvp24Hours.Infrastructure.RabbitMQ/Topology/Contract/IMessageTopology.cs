//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Enums;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Topology.Contract
{
    /// <summary>
    /// Interface for defining message-specific topology configuration.
    /// Allows customizing exchange, queue, and routing key settings per message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    public interface IMessageTopology<TMessage> : IMessageTopology
        where TMessage : class
    {
    }

    /// <summary>
    /// Non-generic base interface for message topology.
    /// </summary>
    public interface IMessageTopology
    {
        /// <summary>
        /// Gets or sets the exchange name for this message type.
        /// If null, the default exchange from configuration will be used.
        /// </summary>
        string? ExchangeName { get; set; }

        /// <summary>
        /// Gets or sets the exchange type.
        /// </summary>
        MvpRabbitMQExchangeType ExchangeType { get; set; }

        /// <summary>
        /// Gets or sets the routing key pattern for this message type.
        /// Supports wildcards for topic exchanges.
        /// </summary>
        string? RoutingKey { get; set; }

        /// <summary>
        /// Gets or sets whether the exchange should be durable.
        /// </summary>
        bool Durable { get; set; }

        /// <summary>
        /// Gets or sets whether the exchange should auto-delete.
        /// </summary>
        bool AutoDelete { get; set; }

        /// <summary>
        /// Gets or sets additional exchange arguments.
        /// </summary>
        IDictionary<string, object>? ExchangeArguments { get; set; }

        /// <summary>
        /// Gets or sets the message priority for priority queues.
        /// </summary>
        byte? DefaultPriority { get; set; }

        /// <summary>
        /// Gets or sets the message TTL in milliseconds.
        /// </summary>
        int? MessageTtlMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets whether message acknowledgment should be required.
        /// </summary>
        bool RequireAcknowledgment { get; set; }

        /// <summary>
        /// Gets or sets custom headers to include with messages of this type.
        /// </summary>
        IDictionary<string, object>? DefaultHeaders { get; set; }

        /// <summary>
        /// Gets the message type that this topology is for.
        /// </summary>
        System.Type MessageType { get; }
    }
}


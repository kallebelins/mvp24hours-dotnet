//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Topology.Contract
{
    /// <summary>
    /// Interface for defining custom routing key conventions.
    /// </summary>
    public interface IRoutingKeyConvention
    {
        /// <summary>
        /// Generates a routing key for a message type.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <returns>The routing key.</returns>
        string GetRoutingKey(Type messageType);

        /// <summary>
        /// Generates a routing key pattern for a consumer to subscribe to.
        /// May include wildcards for topic exchanges.
        /// </summary>
        /// <param name="consumerType">The consumer type.</param>
        /// <param name="messageType">The message type being consumed.</param>
        /// <returns>The routing key pattern.</returns>
        string GetSubscriptionPattern(Type consumerType, Type messageType);

        /// <summary>
        /// Determines if a routing key matches a pattern.
        /// Supports topic exchange wildcards (* and #).
        /// </summary>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="pattern">The pattern to match against.</param>
        /// <returns>True if the routing key matches the pattern.</returns>
        bool Matches(string routingKey, string pattern);
    }
}


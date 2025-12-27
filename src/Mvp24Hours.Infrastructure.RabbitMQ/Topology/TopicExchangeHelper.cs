//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Topology
{
    /// <summary>
    /// Helper class for working with RabbitMQ topic exchanges.
    /// Provides utilities for routing key patterns, wildcards, and bindings.
    /// </summary>
    public static class TopicExchangeHelper
    {
        /// <summary>
        /// Single word wildcard character.
        /// Matches exactly one word between dots.
        /// </summary>
        public const string SingleWordWildcard = "*";

        /// <summary>
        /// Multi-word wildcard character.
        /// Matches zero or more words.
        /// </summary>
        public const string MultiWordWildcard = "#";

        /// <summary>
        /// Creates a topic exchange.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="durable">Whether the exchange is durable.</param>
        /// <param name="autoDelete">Whether the exchange should auto-delete.</param>
        /// <param name="arguments">Additional exchange arguments.</param>
        public static void DeclareTopicExchange(
            IModel channel,
            string exchangeName,
            bool durable = true,
            bool autoDelete = false,
            IDictionary<string, object>? arguments = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(exchangeName);

            channel.ExchangeDeclare(
                exchange: exchangeName,
                type: ExchangeType.Topic,
                durable: durable,
                autoDelete: autoDelete,
                arguments: arguments);
        }

        /// <summary>
        /// Binds a queue to a topic exchange with a routing key pattern.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="routingKeyPattern">The routing key pattern (supports * and # wildcards).</param>
        public static void BindQueueToTopic(
            IModel channel,
            string queueName,
            string exchangeName,
            string routingKeyPattern)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(queueName);
            ArgumentNullException.ThrowIfNull(exchangeName);
            ArgumentNullException.ThrowIfNull(routingKeyPattern);

            channel.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKeyPattern);
        }

        /// <summary>
        /// Binds a queue to receive all messages from a topic exchange.
        /// Uses the '#' wildcard to match all routing keys.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="exchangeName">The exchange name.</param>
        public static void BindQueueToAllTopics(
            IModel channel,
            string queueName,
            string exchangeName)
        {
            BindQueueToTopic(channel, queueName, exchangeName, MultiWordWildcard);
        }

        /// <summary>
        /// Binds a queue to multiple routing key patterns.
        /// </summary>
        /// <param name="channel">The RabbitMQ channel.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="routingKeyPatterns">The routing key patterns to bind.</param>
        public static void BindQueueToMultiplePatterns(
            IModel channel,
            string queueName,
            string exchangeName,
            IEnumerable<string> routingKeyPatterns)
        {
            ArgumentNullException.ThrowIfNull(routingKeyPatterns);

            foreach (var pattern in routingKeyPatterns)
            {
                BindQueueToTopic(channel, queueName, exchangeName, pattern);
            }
        }

        /// <summary>
        /// Creates a routing key pattern that matches messages of a specific category.
        /// Example: "orders.*" matches "orders.created", "orders.updated", etc.
        /// </summary>
        /// <param name="category">The message category.</param>
        /// <returns>The routing key pattern.</returns>
        public static string CreateCategoryPattern(string category)
        {
            return $"{category}.{SingleWordWildcard}";
        }

        /// <summary>
        /// Creates a routing key pattern that matches all messages in a namespace.
        /// Example: "sales.orders.#" matches "sales.orders.created", "sales.orders.items.added", etc.
        /// </summary>
        /// <param name="namespacePrefix">The namespace prefix.</param>
        /// <returns>The routing key pattern.</returns>
        public static string CreateNamespacePattern(string namespacePrefix)
        {
            return $"{namespacePrefix}.{MultiWordWildcard}";
        }

        /// <summary>
        /// Creates a routing key pattern that matches a specific event type from any service.
        /// Example: "*.*.created" matches "orders.v1.created", "products.v2.created", etc.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="levels">Number of wildcard levels before the event type.</param>
        /// <returns>The routing key pattern.</returns>
        public static string CreateEventTypePattern(string eventType, int levels = 2)
        {
            var wildcards = string.Join(".", System.Linq.Enumerable.Repeat(SingleWordWildcard, levels));
            return $"{wildcards}.{eventType}";
        }

        /// <summary>
        /// Validates a routing key pattern for topic exchanges.
        /// </summary>
        /// <param name="pattern">The pattern to validate.</param>
        /// <returns>True if the pattern is valid.</returns>
        public static bool IsValidPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;

            // Pattern should only contain alphanumeric, dots, asterisks, and hashes
            var validPattern = new Regex(@"^[a-zA-Z0-9.*#\-_]+$");
            if (!validPattern.IsMatch(pattern))
                return false;

            // Hash should only appear at the end or as a segment
            var parts = pattern.Split('.');
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                
                // Each part should be either *, #, or a word
                if (part != SingleWordWildcard && part != MultiWordWildcard)
                {
                    // Should not contain wildcards mixed with other characters
                    if (part.Contains(SingleWordWildcard) || part.Contains(MultiWordWildcard))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if a routing key matches a pattern.
        /// </summary>
        /// <param name="routingKey">The routing key to check.</param>
        /// <param name="pattern">The pattern to match against.</param>
        /// <returns>True if the routing key matches the pattern.</returns>
        public static bool Matches(string routingKey, string pattern)
        {
            if (string.IsNullOrEmpty(routingKey) || string.IsNullOrEmpty(pattern))
                return false;

            if (routingKey == pattern)
                return true;

            // Convert topic pattern to regex
            var regexPattern = pattern
                .Replace(".", @"\.")
                .Replace("*", @"[^.]+")
                .Replace("#", @".*");

            regexPattern = $"^{regexPattern}$";

            return Regex.IsMatch(routingKey, regexPattern);
        }

        /// <summary>
        /// Builds a routing key from segments.
        /// </summary>
        /// <param name="segments">The routing key segments.</param>
        /// <returns>The complete routing key.</returns>
        public static string BuildRoutingKey(params string[] segments)
        {
            return string.Join(".", segments);
        }

        /// <summary>
        /// Parses a routing key into segments.
        /// </summary>
        /// <param name="routingKey">The routing key to parse.</param>
        /// <returns>The routing key segments.</returns>
        public static string[] ParseRoutingKey(string routingKey)
        {
            if (string.IsNullOrEmpty(routingKey))
                return [];

            return routingKey.Split('.');
        }
    }
}


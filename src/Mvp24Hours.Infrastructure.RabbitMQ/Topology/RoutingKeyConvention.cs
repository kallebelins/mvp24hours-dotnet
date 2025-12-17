//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Topology.Contract;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Topology
{
    /// <summary>
    /// Default implementation of <see cref="IRoutingKeyConvention"/> providing
    /// routing key generation and pattern matching for RabbitMQ topic exchanges.
    /// </summary>
    public class RoutingKeyConvention : IRoutingKeyConvention
    {
        private readonly IEndpointNameFormatter _nameFormatter;
        private readonly RoutingKeyConventionOptions _options;

        /// <summary>
        /// Gets a singleton instance of the default convention.
        /// </summary>
        public static IRoutingKeyConvention Instance { get; } = new RoutingKeyConvention();

        /// <summary>
        /// Creates a new instance of <see cref="RoutingKeyConvention"/> with default settings.
        /// </summary>
        public RoutingKeyConvention()
            : this(EndpointNameFormatter.Instance, new RoutingKeyConventionOptions())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="RoutingKeyConvention"/> with a custom name formatter.
        /// </summary>
        /// <param name="nameFormatter">The name formatter to use.</param>
        public RoutingKeyConvention(IEndpointNameFormatter nameFormatter)
            : this(nameFormatter, new RoutingKeyConventionOptions())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="RoutingKeyConvention"/> with options.
        /// </summary>
        /// <param name="options">The routing key convention options.</param>
        public RoutingKeyConvention(RoutingKeyConventionOptions options)
            : this(EndpointNameFormatter.Instance, options)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="RoutingKeyConvention"/> with custom settings.
        /// </summary>
        /// <param name="nameFormatter">The name formatter to use.</param>
        /// <param name="options">The routing key convention options.</param>
        public RoutingKeyConvention(IEndpointNameFormatter nameFormatter, RoutingKeyConventionOptions options)
        {
            _nameFormatter = nameFormatter ?? throw new ArgumentNullException(nameof(nameFormatter));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public string GetRoutingKey(Type messageType)
        {
            ArgumentNullException.ThrowIfNull(messageType);

            // Check if there's a custom topology registered
            var topology = MessageTopologyRegistry.Instance.GetTopology(messageType);
            if (topology?.RoutingKey != null)
            {
                return topology.RoutingKey;
            }

            // Build routing key based on options
            var parts = new System.Collections.Generic.List<string>();

            // Add prefix if configured
            if (!string.IsNullOrEmpty(_options.Prefix))
            {
                parts.Add(_options.Prefix);
            }

            // Add namespace segments
            if (_options.IncludeNamespace && messageType.Namespace != null)
            {
                var namespaceParts = messageType.Namespace.Split('.');
                var relevantParts = namespaceParts.TakeLast(_options.NamespaceDepth);
                parts.AddRange(relevantParts);
            }

            // Add type name
            var typeName = GetTypeNameForRouting(messageType);
            parts.Add(typeName);

            // Add message type category if configured
            if (_options.IncludeMessageTypeCategory)
            {
                var category = DetermineMessageCategory(messageType, typeName);
                if (!string.IsNullOrEmpty(category))
                {
                    parts.Add(category);
                }
            }

            // Add suffix if configured
            if (!string.IsNullOrEmpty(_options.Suffix))
            {
                parts.Add(_options.Suffix);
            }

            return string.Join(_options.Separator, parts).ToLowerInvariant();
        }

        /// <inheritdoc />
        public string GetSubscriptionPattern(Type consumerType, Type messageType)
        {
            ArgumentNullException.ThrowIfNull(consumerType);
            ArgumentNullException.ThrowIfNull(messageType);

            // If consumer has explicit binding pattern, use it
            var routingKey = GetRoutingKey(messageType);

            // Check if consumer wants to receive all messages of a certain type hierarchy
            if (_options.UseWildcardsForBaseTypes)
            {
                var baseType = messageType.BaseType;
                while (baseType != null && baseType != typeof(object))
                {
                    if (IsMessageBaseType(baseType))
                    {
                        // Subscribe to all derived types
                        var baseParts = GetRoutingKey(baseType).Split('.');
                        return string.Join(".", baseParts.Take(baseParts.Length - 1)) + ".#";
                    }
                    baseType = baseType.BaseType;
                }
            }

            return routingKey;
        }

        /// <inheritdoc />
        public bool Matches(string routingKey, string pattern)
        {
            if (string.IsNullOrEmpty(routingKey) || string.IsNullOrEmpty(pattern))
                return false;

            // Exact match
            if (routingKey == pattern)
                return true;

            // Convert RabbitMQ pattern to regex
            // * matches exactly one word
            // # matches zero or more words
            var regexPattern = pattern
                .Replace(".", @"\.")
                .Replace("*", @"[^.]+")
                .Replace("#", @".*");

            regexPattern = $"^{regexPattern}$";

            return Regex.IsMatch(routingKey, regexPattern, RegexOptions.IgnoreCase);
        }

        private string GetTypeNameForRouting(Type type)
        {
            var name = type.Name;

            // Handle generic types
            if (type.IsGenericType)
            {
                var index = name.IndexOf('`');
                if (index > 0)
                {
                    name = name[..index];
                }
            }

            // Remove common suffixes
            foreach (var suffix in new[] { "Message", "Event", "Command", "Query", "Request", "Response" })
            {
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name[..^suffix.Length];
                    break;
                }
            }

            return _nameFormatter.SanitizeName(name).ToLowerInvariant();
        }

        private static string DetermineMessageCategory(Type messageType, string typeName)
        {
            // Try to determine category from interfaces or type name
            var interfaces = messageType.GetInterfaces().Select(i => i.Name).ToList();

            if (interfaces.Any(i => i.Contains("Event")))
                return "event";
            if (interfaces.Any(i => i.Contains("Command")))
                return "command";
            if (interfaces.Any(i => i.Contains("Query")))
                return "query";

            // Check type name patterns
            if (typeName.EndsWith("Event", StringComparison.OrdinalIgnoreCase))
                return "event";
            if (typeName.EndsWith("Command", StringComparison.OrdinalIgnoreCase))
                return "command";
            if (typeName.EndsWith("Query", StringComparison.OrdinalIgnoreCase))
                return "query";

            return string.Empty;
        }

        private static bool IsMessageBaseType(Type type)
        {
            // Check if type is a known message base type
            return type.Name.Contains("Event") ||
                   type.Name.Contains("Command") ||
                   type.Name.Contains("Message") ||
                   type.GetInterfaces().Any(i =>
                       i.Name.Contains("Event") ||
                       i.Name.Contains("Command") ||
                       i.Name.Contains("Message"));
        }
    }

    /// <summary>
    /// Options for routing key conventions.
    /// </summary>
    public class RoutingKeyConventionOptions
    {
        /// <summary>
        /// Gets or sets the separator between routing key segments. Default is ".".
        /// </summary>
        public string Separator { get; set; } = ".";

        /// <summary>
        /// Gets or sets the prefix to add to all routing keys. Default is empty.
        /// </summary>
        public string? Prefix { get; set; }

        /// <summary>
        /// Gets or sets the suffix to add to all routing keys. Default is empty.
        /// </summary>
        public string? Suffix { get; set; }

        /// <summary>
        /// Gets or sets whether to include namespace in routing keys. Default is true.
        /// </summary>
        public bool IncludeNamespace { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of namespace segments to include. Default is 2.
        /// </summary>
        public int NamespaceDepth { get; set; } = 2;

        /// <summary>
        /// Gets or sets whether to include message type category (event, command, query). Default is false.
        /// </summary>
        public bool IncludeMessageTypeCategory { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to use wildcards for subscribing to base types. Default is false.
        /// </summary>
        public bool UseWildcardsForBaseTypes { get; set; } = false;
    }
}


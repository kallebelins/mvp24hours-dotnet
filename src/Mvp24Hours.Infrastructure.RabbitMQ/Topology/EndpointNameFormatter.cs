//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Topology.Contract;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Topology
{
    /// <summary>
    /// Default implementation of <see cref="IEndpointNameFormatter"/> providing
    /// consistent naming conventions for RabbitMQ endpoints.
    /// </summary>
    public class EndpointNameFormatter : IEndpointNameFormatter
    {
        private readonly string _prefix;
        private readonly EndpointNamingConventionOptions _options;

        /// <summary>
        /// Gets a singleton instance of the default formatter.
        /// </summary>
        public static IEndpointNameFormatter Instance { get; } = new EndpointNameFormatter();

        /// <summary>
        /// Creates a new instance of <see cref="EndpointNameFormatter"/> with default options.
        /// </summary>
        public EndpointNameFormatter()
            : this(string.Empty, new EndpointNamingConventionOptions())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="EndpointNameFormatter"/> with a prefix.
        /// </summary>
        /// <param name="prefix">The prefix to add to all endpoint names.</param>
        public EndpointNameFormatter(string prefix)
            : this(prefix, new EndpointNamingConventionOptions())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="EndpointNameFormatter"/> with options.
        /// </summary>
        /// <param name="options">The naming convention options.</param>
        public EndpointNameFormatter(EndpointNamingConventionOptions options)
            : this(string.Empty, options)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="EndpointNameFormatter"/> with prefix and options.
        /// </summary>
        /// <param name="prefix">The prefix to add to all endpoint names.</param>
        /// <param name="options">The naming convention options.</param>
        public EndpointNameFormatter(string prefix, EndpointNamingConventionOptions options)
        {
            _prefix = prefix ?? string.Empty;
            _options = options ?? new EndpointNamingConventionOptions();
        }

        /// <inheritdoc />
        public string Separator => _options.Separator;

        /// <inheritdoc />
        public string FormatQueueName<T>()
        {
            return FormatQueueName(typeof(T));
        }

        /// <inheritdoc />
        public string FormatQueueName(Type consumerType)
        {
            ArgumentNullException.ThrowIfNull(consumerType);

            // Extract message type from IMessageConsumer<T> if available
            var messageType = GetMessageTypeFromConsumer(consumerType);
            
            var baseName = messageType != null
                ? FormatTypeName(messageType)
                : FormatTypeName(consumerType);

            var queueName = RemoveSuffix(baseName, "Consumer", "Handler", "Subscriber");
            queueName = $"{queueName}{Separator}queue";

            return AddPrefix(ApplyCasing(queueName));
        }

        /// <inheritdoc />
        public string FormatQueueNameFromMessage(Type messageType)
        {
            ArgumentNullException.ThrowIfNull(messageType);

            var baseName = FormatTypeName(messageType);
            var queueName = RemoveSuffix(baseName, "Message", "Event", "Command", "Query");
            queueName = $"{queueName}{Separator}queue";

            return AddPrefix(ApplyCasing(queueName));
        }

        /// <inheritdoc />
        public string FormatExchangeName<T>()
        {
            return FormatExchangeName(typeof(T));
        }

        /// <inheritdoc />
        public string FormatExchangeName(Type messageType)
        {
            ArgumentNullException.ThrowIfNull(messageType);

            var baseName = FormatTypeName(messageType);
            var exchangeName = RemoveSuffix(baseName, "Message", "Event", "Command", "Query");
            exchangeName = $"{exchangeName}{Separator}exchange";

            return AddPrefix(ApplyCasing(exchangeName));
        }

        /// <inheritdoc />
        public string FormatRoutingKey<T>()
        {
            return FormatRoutingKey(typeof(T));
        }

        /// <inheritdoc />
        public string FormatRoutingKey(Type messageType)
        {
            ArgumentNullException.ThrowIfNull(messageType);

            // Use namespace-based routing key for better organization
            var namespaceParts = messageType.Namespace?.Split('.') ?? [];
            var typeName = FormatTypeName(messageType);
            typeName = RemoveSuffix(typeName, "Message", "Event", "Command", "Query");

            if (_options.IncludeNamespaceInRoutingKey && namespaceParts.Length > 0)
            {
                // Take last 2 namespace parts for context
                var relevantParts = namespaceParts.TakeLast(2);
                return ApplyCasing(string.Join(Separator, relevantParts.Concat(new[] { typeName })));
            }

            return ApplyCasing(typeName);
        }

        /// <inheritdoc />
        public string FormatDeadLetterQueueName(string originalQueueName)
        {
            ArgumentNullException.ThrowIfNull(originalQueueName);
            return $"{originalQueueName}{Separator}dlq";
        }

        /// <inheritdoc />
        public string FormatDeadLetterExchangeName(string originalExchangeName)
        {
            ArgumentNullException.ThrowIfNull(originalExchangeName);
            return $"{originalExchangeName}{Separator}dlx";
        }

        /// <inheritdoc />
        public string FormatRetryQueueName(string originalQueueName, int retryLevel)
        {
            ArgumentNullException.ThrowIfNull(originalQueueName);
            if (retryLevel < 1) throw new ArgumentOutOfRangeException(nameof(retryLevel), "Retry level must be >= 1");
            return $"{originalQueueName}{Separator}retry{Separator}{retryLevel}";
        }

        /// <inheritdoc />
        public string FormatTemporaryQueueName()
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            return AddPrefix($"temp{Separator}{suffix}");
        }

        /// <inheritdoc />
        public string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Remove invalid characters (keep alphanumeric, dots, dashes, underscores)
            var sanitized = Regex.Replace(name, @"[^a-zA-Z0-9._\-]", string.Empty);
            
            // Ensure it doesn't start with a number
            if (char.IsDigit(sanitized.FirstOrDefault()))
            {
                sanitized = "_" + sanitized;
            }

            return sanitized;
        }

        private string FormatTypeName(Type type)
        {
            var name = type.Name;

            // Handle generic types
            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition().Name;
                var index = genericTypeDefinition.IndexOf('`');
                if (index > 0)
                {
                    genericTypeDefinition = genericTypeDefinition[..index];
                }

                var genericArgs = type.GetGenericArguments();
                var argNames = string.Join(Separator, genericArgs.Select(FormatTypeName));
                name = $"{genericTypeDefinition}{Separator}{argNames}";
            }

            return SanitizeName(name);
        }

        private string ApplyCasing(string name)
        {
            return _options.CasingStyle switch
            {
                EndpointCasingStyle.LowerCase => name.ToLowerInvariant(),
                EndpointCasingStyle.UpperCase => name.ToUpperInvariant(),
                EndpointCasingStyle.KebabCase => ToKebabCase(name),
                EndpointCasingStyle.SnakeCase => ToSnakeCase(name),
                EndpointCasingStyle.PascalCase => ToPascalCase(name),
                EndpointCasingStyle.CamelCase => ToCamelCase(name),
                _ => name
            };
        }

        private string AddPrefix(string name)
        {
            if (string.IsNullOrEmpty(_prefix))
                return name;

            return $"{_prefix}{Separator}{name}";
        }

        private static string RemoveSuffix(string name, params string[] suffixes)
        {
            foreach (var suffix in suffixes)
            {
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return name[..^suffix.Length];
                }
            }
            return name;
        }

        private static Type? GetMessageTypeFromConsumer(Type consumerType)
        {
            var consumerInterface = consumerType
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>));

            return consumerInterface?.GetGenericArguments().FirstOrDefault();
        }

        private string ToKebabCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            
            var builder = new StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c) && i > 0 && name[i - 1] != '-' && name[i - 1] != '.')
                {
                    builder.Append('-');
                }
                builder.Append(char.ToLowerInvariant(c));
            }
            return builder.ToString().Replace("_", "-");
        }

        private string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            
            var builder = new StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c) && i > 0 && name[i - 1] != '_' && name[i - 1] != '.')
                {
                    builder.Append('_');
                }
                builder.Append(char.ToLowerInvariant(c));
            }
            return builder.ToString().Replace("-", "_");
        }

        private static string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return char.ToUpperInvariant(name[0]) + name[1..];
        }

        private static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return char.ToLowerInvariant(name[0]) + name[1..];
        }
    }

    /// <summary>
    /// Options for endpoint naming conventions.
    /// </summary>
    public class EndpointNamingConventionOptions
    {
        /// <summary>
        /// Gets or sets the separator between name components. Default is ".".
        /// </summary>
        public string Separator { get; set; } = ".";

        /// <summary>
        /// Gets or sets the casing style for endpoint names. Default is KebabCase.
        /// </summary>
        public EndpointCasingStyle CasingStyle { get; set; } = EndpointCasingStyle.KebabCase;

        /// <summary>
        /// Gets or sets whether to include namespace in routing keys. Default is true.
        /// </summary>
        public bool IncludeNamespaceInRoutingKey { get; set; } = true;
    }

    /// <summary>
    /// Casing styles for endpoint names.
    /// </summary>
    public enum EndpointCasingStyle
    {
        /// <summary>
        /// Preserve original casing.
        /// </summary>
        Preserve,

        /// <summary>
        /// All lowercase (e.g., "ordercreated").
        /// </summary>
        LowerCase,

        /// <summary>
        /// All uppercase (e.g., "ORDERCREATED").
        /// </summary>
        UpperCase,

        /// <summary>
        /// Kebab case (e.g., "order-created").
        /// </summary>
        KebabCase,

        /// <summary>
        /// Snake case (e.g., "order_created").
        /// </summary>
        SnakeCase,

        /// <summary>
        /// Pascal case (e.g., "OrderCreated").
        /// </summary>
        PascalCase,

        /// <summary>
        /// Camel case (e.g., "orderCreated").
        /// </summary>
        CamelCase
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration.Fluent
{
    /// <summary>
    /// Builder for configuring endpoint naming conventions and settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This builder allows configuring how endpoints (queues, exchanges, routing keys)
    /// are named and created automatically based on consumer and message types.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// cfg.ConfigureEndpoints(e =>
    /// {
    ///     e.UseConventionalNaming();
    ///     e.SetPrefix("myapp");
    ///     e.SetSuffix("queue");
    ///     e.UseLowercaseEndpoints();
    /// });
    /// </code>
    /// </example>
    public class EndpointConfigurationBuilder
    {
        private readonly EndpointConfiguration _configuration = new();

        /// <summary>
        /// Enables conventional naming based on consumer/message type names.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// When enabled, queue names are generated from the consumer class name.
        /// For example, <c>OrderCreatedConsumer</c> becomes <c>order-created</c>.
        /// </para>
        /// </remarks>
        public EndpointConfigurationBuilder UseConventionalNaming()
        {
            _configuration.UseConventionalNaming = true;
            return this;
        }

        /// <summary>
        /// Sets a prefix for all endpoint names.
        /// </summary>
        /// <param name="prefix">The prefix to prepend.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// e.SetPrefix("myapp"); // Results in: myapp-order-created
        /// </code>
        /// </example>
        public EndpointConfigurationBuilder SetPrefix(string prefix)
        {
            _configuration.Prefix = prefix;
            return this;
        }

        /// <summary>
        /// Sets a suffix for all endpoint names.
        /// </summary>
        /// <param name="suffix">The suffix to append.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// e.SetSuffix("queue"); // Results in: order-created-queue
        /// </code>
        /// </example>
        public EndpointConfigurationBuilder SetSuffix(string suffix)
        {
            _configuration.Suffix = suffix;
            return this;
        }

        /// <summary>
        /// Sets the separator used between name parts.
        /// Default is "-".
        /// </summary>
        /// <param name="separator">The separator character or string.</param>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder SetSeparator(string separator)
        {
            _configuration.Separator = separator;
            return this;
        }

        /// <summary>
        /// Uses lowercase for all endpoint names.
        /// Default is true.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder UseLowercaseEndpoints()
        {
            _configuration.UseLowercase = true;
            return this;
        }

        /// <summary>
        /// Uses the original casing for endpoint names.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder UseOriginalCasing()
        {
            _configuration.UseLowercase = false;
            return this;
        }

        /// <summary>
        /// Uses kebab-case for endpoint names (e.g., order-created).
        /// This is the default.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder UseKebabCase()
        {
            _configuration.NamingStyle = EndpointNamingStyle.KebabCase;
            return this;
        }

        /// <summary>
        /// Uses snake_case for endpoint names (e.g., order_created).
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder UseSnakeCase()
        {
            _configuration.NamingStyle = EndpointNamingStyle.SnakeCase;
            _configuration.Separator = "_";
            return this;
        }

        /// <summary>
        /// Uses dot notation for endpoint names (e.g., order.created).
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder UseDotNotation()
        {
            _configuration.NamingStyle = EndpointNamingStyle.DotNotation;
            _configuration.Separator = ".";
            return this;
        }

        /// <summary>
        /// Uses PascalCase for endpoint names (e.g., OrderCreated).
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder UsePascalCase()
        {
            _configuration.NamingStyle = EndpointNamingStyle.PascalCase;
            _configuration.UseLowercase = false;
            return this;
        }

        /// <summary>
        /// Sets the default exchange name for all endpoints.
        /// </summary>
        /// <param name="exchangeName">The exchange name.</param>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder SetDefaultExchange(string exchangeName)
        {
            _configuration.DefaultExchangeName = exchangeName;
            return this;
        }

        /// <summary>
        /// Sets the default exchange type for all endpoints.
        /// Default is "direct".
        /// </summary>
        /// <param name="exchangeType">The exchange type.</param>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder SetDefaultExchangeType(string exchangeType)
        {
            _configuration.DefaultExchangeType = exchangeType;
            return this;
        }

        /// <summary>
        /// Includes the namespace in endpoint names.
        /// </summary>
        /// <param name="include">Whether to include the namespace.</param>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder IncludeNamespace(bool include = true)
        {
            _configuration.IncludeNamespace = include;
            return this;
        }

        /// <summary>
        /// Strips common suffixes from type names (e.g., Consumer, Handler).
        /// Default is true.
        /// </summary>
        /// <param name="strip">Whether to strip common suffixes.</param>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder StripCommonSuffixes(bool strip = true)
        {
            _configuration.StripCommonSuffixes = strip;
            return this;
        }

        /// <summary>
        /// Adds a custom suffix to strip from type names.
        /// </summary>
        /// <param name="suffix">The suffix to strip.</param>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder AddSuffixToStrip(string suffix)
        {
            _configuration.SuffixesToStrip.Add(suffix);
            return this;
        }

        /// <summary>
        /// Sets the temporary queue name pattern for request clients.
        /// </summary>
        /// <param name="pattern">The pattern (use {guid} as placeholder).</param>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder SetTemporaryQueuePattern(string pattern)
        {
            _configuration.TemporaryQueuePattern = pattern;
            return this;
        }

        /// <summary>
        /// Sets whether queues are durable by default.
        /// Default is true.
        /// </summary>
        /// <param name="durable">Whether queues are durable.</param>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder SetDurable(bool durable = true)
        {
            _configuration.Durable = durable;
            return this;
        }

        /// <summary>
        /// Sets whether queues are auto-delete by default.
        /// Default is false.
        /// </summary>
        /// <param name="autoDelete">Whether queues are auto-delete.</param>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder SetAutoDelete(bool autoDelete = false)
        {
            _configuration.AutoDelete = autoDelete;
            return this;
        }

        /// <summary>
        /// Sets the default prefetch count for consumers.
        /// Default is 16.
        /// </summary>
        /// <param name="count">The prefetch count.</param>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder SetDefaultPrefetchCount(ushort count)
        {
            _configuration.DefaultPrefetchCount = count;
            return this;
        }

        /// <summary>
        /// Enables automatic queue creation.
        /// Default is true.
        /// </summary>
        /// <param name="enable">Whether to auto-create queues.</param>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder EnableAutoQueueCreation(bool enable = true)
        {
            _configuration.AutoCreateQueues = enable;
            return this;
        }

        /// <summary>
        /// Enables automatic exchange creation.
        /// Default is true.
        /// </summary>
        /// <param name="enable">Whether to auto-create exchanges.</param>
        /// <returns>The builder for chaining.</returns>
        public EndpointConfigurationBuilder EnableAutoExchangeCreation(bool enable = true)
        {
            _configuration.AutoCreateExchanges = enable;
            return this;
        }

        /// <summary>
        /// Builds the endpoint configuration.
        /// </summary>
        /// <returns>The endpoint configuration.</returns>
        internal EndpointConfiguration Build()
        {
            return _configuration;
        }
    }

    /// <summary>
    /// Configuration for endpoint naming and creation.
    /// </summary>
    public class EndpointConfiguration
    {
        /// <summary>
        /// Gets or sets whether to use conventional naming.
        /// </summary>
        public bool UseConventionalNaming { get; set; } = true;

        /// <summary>
        /// Gets or sets the prefix for endpoint names.
        /// </summary>
        public string? Prefix { get; set; }

        /// <summary>
        /// Gets or sets the suffix for endpoint names.
        /// </summary>
        public string? Suffix { get; set; }

        /// <summary>
        /// Gets or sets the separator for name parts.
        /// </summary>
        public string Separator { get; set; } = "-";

        /// <summary>
        /// Gets or sets whether to use lowercase names.
        /// </summary>
        public bool UseLowercase { get; set; } = true;

        /// <summary>
        /// Gets or sets the naming style.
        /// </summary>
        public EndpointNamingStyle NamingStyle { get; set; } = EndpointNamingStyle.KebabCase;

        /// <summary>
        /// Gets or sets the default exchange name.
        /// </summary>
        public string? DefaultExchangeName { get; set; }

        /// <summary>
        /// Gets or sets the default exchange type.
        /// </summary>
        public string DefaultExchangeType { get; set; } = "direct";

        /// <summary>
        /// Gets or sets whether to include namespace in names.
        /// </summary>
        public bool IncludeNamespace { get; set; }

        /// <summary>
        /// Gets or sets whether to strip common suffixes from type names.
        /// </summary>
        public bool StripCommonSuffixes { get; set; } = true;

        /// <summary>
        /// Gets the suffixes to strip from type names.
        /// </summary>
        public List<string> SuffixesToStrip { get; } = new()
        {
            "Consumer",
            "Handler",
            "Command",
            "Query",
            "Event",
            "Message",
            "Request",
            "Response"
        };

        /// <summary>
        /// Gets or sets the temporary queue name pattern.
        /// </summary>
        public string TemporaryQueuePattern { get; set; } = "temp-{guid}";

        /// <summary>
        /// Gets or sets whether queues are durable by default.
        /// </summary>
        public bool Durable { get; set; } = true;

        /// <summary>
        /// Gets or sets whether queues are auto-delete by default.
        /// </summary>
        public bool AutoDelete { get; set; }

        /// <summary>
        /// Gets or sets the default prefetch count.
        /// </summary>
        public ushort DefaultPrefetchCount { get; set; } = 16;

        /// <summary>
        /// Gets or sets whether to auto-create queues.
        /// </summary>
        public bool AutoCreateQueues { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to auto-create exchanges.
        /// </summary>
        public bool AutoCreateExchanges { get; set; } = true;

        /// <summary>
        /// Generates an endpoint name for a given type.
        /// </summary>
        /// <param name="type">The type to generate a name for.</param>
        /// <returns>The generated endpoint name.</returns>
        public string GenerateEndpointName(Type type)
        {
            var name = type.Name;

            // Strip common suffixes
            if (StripCommonSuffixes)
            {
                foreach (var suffix in SuffixesToStrip)
                {
                    if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        name = name[..^suffix.Length];
                        break;
                    }
                }
            }

            // Apply naming style
            name = ApplyNamingStyle(name);

            // Include namespace if configured
            if (IncludeNamespace && type.Namespace != null)
            {
                var ns = ApplyNamingStyle(type.Namespace.Replace(".", Separator));
                name = $"{ns}{Separator}{name}";
            }

            // Apply prefix and suffix
            if (!string.IsNullOrEmpty(Prefix))
            {
                name = $"{Prefix}{Separator}{name}";
            }

            if (!string.IsNullOrEmpty(Suffix))
            {
                name = $"{name}{Separator}{Suffix}";
            }

            // Apply case transformation
            if (UseLowercase)
            {
                name = name.ToLowerInvariant();
            }

            return name;
        }

        /// <summary>
        /// Generates a temporary queue name.
        /// </summary>
        /// <returns>A unique temporary queue name.</returns>
        public string GenerateTemporaryQueueName()
        {
            return TemporaryQueuePattern.Replace("{guid}", Guid.NewGuid().ToString("N"));
        }

        private string ApplyNamingStyle(string name)
        {
            return NamingStyle switch
            {
                EndpointNamingStyle.KebabCase => ToKebabCase(name),
                EndpointNamingStyle.SnakeCase => ToSnakeCase(name),
                EndpointNamingStyle.DotNotation => ToDotNotation(name),
                EndpointNamingStyle.PascalCase => name,
                _ => ToKebabCase(name)
            };
        }

        private string ToKebabCase(string name)
        {
            return ToSeparatedCase(name, "-");
        }

        private string ToSnakeCase(string name)
        {
            return ToSeparatedCase(name, "_");
        }

        private string ToDotNotation(string name)
        {
            return ToSeparatedCase(name, ".");
        }

        private static string ToSeparatedCase(string name, string separator)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var result = new System.Text.StringBuilder();

            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];

                if (char.IsUpper(c) && i > 0)
                {
                    // Don't add separator if previous char is already uppercase
                    // (handles acronyms like "HTTP")
                    if (!char.IsUpper(name[i - 1]) ||
                        (i + 1 < name.Length && !char.IsUpper(name[i + 1])))
                    {
                        result.Append(separator);
                    }
                }

                result.Append(char.ToLowerInvariant(c));
            }

            return result.ToString();
        }
    }

    /// <summary>
    /// Endpoint naming styles.
    /// </summary>
    public enum EndpointNamingStyle
    {
        /// <summary>
        /// kebab-case (e.g., order-created).
        /// </summary>
        KebabCase,

        /// <summary>
        /// snake_case (e.g., order_created).
        /// </summary>
        SnakeCase,

        /// <summary>
        /// dot.notation (e.g., order.created).
        /// </summary>
        DotNotation,

        /// <summary>
        /// PascalCase (e.g., OrderCreated).
        /// </summary>
        PascalCase
    }
}


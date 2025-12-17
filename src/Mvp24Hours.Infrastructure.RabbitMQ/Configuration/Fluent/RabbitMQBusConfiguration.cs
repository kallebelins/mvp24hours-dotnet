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
    /// Internal configuration class that holds all RabbitMQ bus settings.
    /// </summary>
    public class RabbitMQBusConfiguration
    {
        /// <summary>
        /// Gets or sets the connection options.
        /// </summary>
        public RabbitMQConnectionOptions ConnectionOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the client options.
        /// </summary>
        public RabbitMQClientOptions ClientOptions { get; set; } = new();

        /// <summary>
        /// Gets the consumer configurations keyed by consumer type.
        /// </summary>
        public Dictionary<Type, ConsumerConfiguration> ConsumerConfigurations { get; } = new();

        /// <summary>
        /// Gets or sets the retry policy configuration.
        /// </summary>
        public RetryPolicyConfiguration? RetryPolicy { get; set; }

        /// <summary>
        /// Gets or sets the circuit breaker policy configuration.
        /// </summary>
        public CircuitBreakerPolicyConfiguration? CircuitBreakerPolicy { get; set; }

        /// <summary>
        /// Gets or sets whether to use in-memory outbox.
        /// </summary>
        public bool UseInMemoryOutbox { get; set; }

        /// <summary>
        /// Gets or sets whether to use Entity Framework Core outbox.
        /// </summary>
        public bool UseEntityFrameworkOutbox { get; set; }

        /// <summary>
        /// Gets or sets the DbContext type for EF Core outbox.
        /// </summary>
        public Type? OutboxDbContextType { get; set; }

        /// <summary>
        /// Gets or sets the outbox options.
        /// </summary>
        public OutboxOptions? OutboxOptions { get; set; }

        /// <summary>
        /// Gets or sets whether to auto-configure endpoints.
        /// </summary>
        public bool AutoConfigureEndpoints { get; set; }

        /// <summary>
        /// Gets or sets the endpoint configuration.
        /// </summary>
        public EndpointConfiguration? EndpointConfiguration { get; set; }
    }

    /// <summary>
    /// Registration record for consumer configurations.
    /// </summary>
    /// <param name="ConsumerType">The consumer type.</param>
    /// <param name="Configuration">The consumer configuration.</param>
    public record ConsumerConfigurationRegistration(Type ConsumerType, ConsumerConfiguration Configuration);
}


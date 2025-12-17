//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Topology;
using Mvp24Hours.Infrastructure.RabbitMQ.Topology.Contract;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for configuring RabbitMQ topology services.
    /// </summary>
    public static class TopologyServiceExtensions
    {
        /// <summary>
        /// Adds RabbitMQ topology services with default configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQTopology(this IServiceCollection services)
        {
            return services.AddMvp24HoursRabbitMQTopology(_ => { });
        }

        /// <summary>
        /// Adds RabbitMQ topology services with custom configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQTopology(
            this IServiceCollection services,
            Action<TopologyOptions> configure)
        {
            var options = new TopologyOptions();
            configure(options);

            // Configure endpoint convention
            if (options.ConfigureEndpointConvention != null)
            {
                EndpointConvention.Configure(options.ConfigureEndpointConvention);
            }

            // Register name formatter
            if (options.NameFormatter != null)
            {
                services.TryAddSingleton(options.NameFormatter);
            }
            else
            {
                var namingOptions = options.NamingConventionOptions ?? new EndpointNamingConventionOptions();
                var prefix = options.EndpointPrefix ?? string.Empty;
                services.TryAddSingleton<IEndpointNameFormatter>(
                    new EndpointNameFormatter(prefix, namingOptions));
            }

            // Register routing key convention
            if (options.RoutingKeyConvention != null)
            {
                services.TryAddSingleton(options.RoutingKeyConvention);
            }
            else
            {
                var routingOptions = options.RoutingKeyConventionOptions ?? new RoutingKeyConventionOptions();
                services.TryAddSingleton<IRoutingKeyConvention>(sp =>
                {
                    var nameFormatter = sp.GetRequiredService<IEndpointNameFormatter>();
                    return new RoutingKeyConvention(nameFormatter, routingOptions);
                });
            }

            // Register topology builder
            services.TryAddSingleton<ITopologyBuilder>(sp =>
            {
                var nameFormatter = sp.GetRequiredService<IEndpointNameFormatter>();
                var routingKeyConvention = sp.GetRequiredService<IRoutingKeyConvention>();
                var builderOptions = options.TopologyBuilderOptions ?? new TopologyBuilderOptions();
                return new TopologyBuilder(nameFormatter, routingKeyConvention, builderOptions);
            });

            // Register auto-binding helper
            services.TryAddSingleton(sp =>
            {
                var nameFormatter = sp.GetRequiredService<IEndpointNameFormatter>();
                var routingKeyConvention = sp.GetRequiredService<IRoutingKeyConvention>();
                var topologyBuilder = sp.GetRequiredService<ITopologyBuilder>();
                var bindingOptions = options.AutoBindingOptions ?? new AutoBindingOptions();
                return new AutoBindingHelper(nameFormatter, routingKeyConvention, topologyBuilder, bindingOptions);
            });

            return services;
        }

        /// <summary>
        /// Registers message topology for a specific message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action for the topology.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMessageTopology<TMessage>(
            this IServiceCollection services,
            Action<IMessageTopology<TMessage>> configure)
            where TMessage : class
        {
            MessageTopologyRegistry.Instance.Register(configure);
            return services;
        }

        /// <summary>
        /// Maps a message type to a specific exchange.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="exchangeName">The exchange name.</param>
        /// <param name="routingKey">Optional routing key.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection MapToExchange<TMessage>(
            this IServiceCollection services,
            string exchangeName,
            string? routingKey = null)
            where TMessage : class
        {
            EndpointConvention.MapToExchange<TMessage>(exchangeName, routingKey);
            return services;
        }

        /// <summary>
        /// Maps a message type to a specific queue.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="queueName">The queue name.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection MapToQueue<TMessage>(
            this IServiceCollection services,
            string queueName)
            where TMessage : class
        {
            EndpointConvention.MapToQueue<TMessage>(queueName);
            return services;
        }

        /// <summary>
        /// Configures the global endpoint naming convention.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ConfigureEndpointConvention(
            this IServiceCollection services,
            Action<EndpointConventionOptions> configure)
        {
            EndpointConvention.Configure(configure);
            return services;
        }
    }

    /// <summary>
    /// Options for topology configuration.
    /// </summary>
    public class TopologyOptions
    {
        /// <summary>
        /// Gets or sets the custom name formatter.
        /// </summary>
        public IEndpointNameFormatter? NameFormatter { get; set; }

        /// <summary>
        /// Gets or sets the custom routing key convention.
        /// </summary>
        public IRoutingKeyConvention? RoutingKeyConvention { get; set; }

        /// <summary>
        /// Gets or sets the naming convention options (used if NameFormatter is not set).
        /// </summary>
        public EndpointNamingConventionOptions? NamingConventionOptions { get; set; }

        /// <summary>
        /// Gets or sets the routing key convention options (used if RoutingKeyConvention is not set).
        /// </summary>
        public RoutingKeyConventionOptions? RoutingKeyConventionOptions { get; set; }

        /// <summary>
        /// Gets or sets the topology builder options.
        /// </summary>
        public TopologyBuilderOptions? TopologyBuilderOptions { get; set; }

        /// <summary>
        /// Gets or sets the auto-binding options.
        /// </summary>
        public AutoBindingOptions? AutoBindingOptions { get; set; }

        /// <summary>
        /// Gets or sets the global endpoint prefix.
        /// </summary>
        public string? EndpointPrefix { get; set; }

        /// <summary>
        /// Gets or sets the endpoint convention configuration action.
        /// </summary>
        public Action<EndpointConventionOptions>? ConfigureEndpointConvention { get; set; }
    }
}


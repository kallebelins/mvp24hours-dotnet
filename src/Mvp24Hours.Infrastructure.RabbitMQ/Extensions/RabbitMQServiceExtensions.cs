//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Deduplication;
using Mvp24Hours.Infrastructure.RabbitMQ.HealthChecks;
using Mvp24Hours.Infrastructure.RabbitMQ.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for configuring RabbitMQ services.
    /// </summary>
    public static class RabbitMQServiceExtensions
    {
        /// <summary>
        /// Add RabbitMQ services with automatic consumer discovery from assembly.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblyConsumers">The assembly containing consumers to register.</param>
        /// <param name="connectionOptions">Optional connection options configuration.</param>
        /// <param name="clientOptions">Optional client options configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQ(this IServiceCollection services,
            Assembly assemblyConsumers,
            Action<RabbitMQConnectionOptions>? connectionOptions = null,
            Action<RabbitMQClientOptions>? clientOptions = null)
        {
            return services.AddMvp24HoursRabbitMQ(sp =>
            {
                var client = new MvpRabbitMQClient(sp);

                assemblyConsumers.GetExportedTypes()
                    .Where(t => t.InheritsOrImplements(typeof(IMvpRabbitMQConsumer)))
                    .ToList()
                    .ForEach(x => client.Register(x));

                return client;
            }, connectionOptions, clientOptions);
        }

        /// <summary>
        /// Add RabbitMQ services with explicit consumer type list.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="typeConsumers">List of consumer types to register.</param>
        /// <param name="connectionOptions">Optional connection options configuration.</param>
        /// <param name="clientOptions">Optional client options configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQ(this IServiceCollection services,
            IList<Type> typeConsumers,
            Action<RabbitMQConnectionOptions>? connectionOptions = null,
            Action<RabbitMQClientOptions>? clientOptions = null)
        {
            return services.AddMvp24HoursRabbitMQ(sp =>
            {
                var client = new MvpRabbitMQClient(sp);

                if (typeConsumers.AnySafe())
                    foreach (var item in typeConsumers)
                        client.Register(item);

                return client;
            }, connectionOptions, clientOptions);
        }

        /// <summary>
        /// Add RabbitMQ services with custom client factory.
        /// </summary>
        /// <typeparam name="TService">The type of RabbitMQ client.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="implementationFactory">Factory function for creating the client.</param>
        /// <param name="connectionOptions">Optional connection options configuration.</param>
        /// <param name="clientOptions">Optional client options configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQ<TService>(this IServiceCollection services,
            Func<IServiceProvider, TService> implementationFactory,
            Action<RabbitMQConnectionOptions>? connectionOptions = null,
            Action<RabbitMQClientOptions>? clientOptions = null)
            where TService : class, IMvpRabbitMQClient
        {
            ArgumentNullException.ThrowIfNull(implementationFactory);

            if (connectionOptions != null)
            {
                services.Configure(connectionOptions);
            }
            else
            {
                services.Configure<RabbitMQConnectionOptions>(opt => { });
            }

            services.AddSingleton<IMvpRabbitMQConnection, MvpRabbitMQConnection>();

            if (clientOptions != null)
            {
                services.Configure(clientOptions);
            }
            else
            {
                services.Configure<RabbitMQClientOptions>(opt => { });
            }

            services.AddSingleton(typeof(TService), implementationFactory);

            return services;
        }

        /// <summary>
        /// Add RabbitMQ metrics collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQMetrics(this IServiceCollection services)
        {
            services.TryAddSingleton<IRabbitMQMetrics, RabbitMQMetrics>();
            return services;
        }

        /// <summary>
        /// Add RabbitMQ structured logging.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQStructuredLogging(this IServiceCollection services)
        {
            services.TryAddSingleton<IRabbitMQStructuredLogger, RabbitMQStructuredLogger>();
            return services;
        }

        /// <summary>
        /// Add RabbitMQ message deduplication with in-memory store.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="expirationMinutes">Default expiration time in minutes. Default is 60.</param>
        /// <param name="maxEntries">Maximum number of entries to keep. Default is 100,000.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQDeduplication(this IServiceCollection services,
            int expirationMinutes = 60,
            int maxEntries = 100_000)
        {
            services.TryAddSingleton<IMessageDeduplicationStore>(
                _ => new InMemoryMessageDeduplicationStore(expirationMinutes, maxEntries));
            return services;
        }

        /// <summary>
        /// Add RabbitMQ message deduplication with custom store.
        /// </summary>
        /// <typeparam name="TStore">The type of deduplication store.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQDeduplication<TStore>(this IServiceCollection services)
            where TStore : class, IMessageDeduplicationStore
        {
            services.TryAddSingleton<IMessageDeduplicationStore, TStore>();
            return services;
        }

        /// <summary>
        /// Add RabbitMQ health check.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">Optional health check name. Default is "rabbitmq".</param>
        /// <param name="tags">Optional health check tags.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQHealthCheck(this IServiceCollection services,
            string name = "rabbitmq",
            IEnumerable<string>? tags = null)
        {
            services.AddHealthChecks()
                .AddCheck<RabbitMQHealthCheck>(name, tags: tags);
            return services;
        }

        /// <summary>
        /// Add all RabbitMQ advanced features (metrics, structured logging, deduplication, health check).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Optional configuration for advanced features.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQAdvanced(this IServiceCollection services,
            Action<RabbitMQAdvancedOptions>? options = null)
        {
            var advancedOptions = new RabbitMQAdvancedOptions();
            options?.Invoke(advancedOptions);

            if (advancedOptions.EnableMetrics)
            {
                services.AddMvp24HoursRabbitMQMetrics();
            }

            if (advancedOptions.EnableStructuredLogging)
            {
                services.AddMvp24HoursRabbitMQStructuredLogging();
            }

            if (advancedOptions.EnableDeduplication)
            {
                services.AddMvp24HoursRabbitMQDeduplication(
                    advancedOptions.DeduplicationExpirationMinutes,
                    advancedOptions.DeduplicationMaxEntries);
            }

            if (advancedOptions.EnableHealthCheck)
            {
                services.AddMvp24HoursRabbitMQHealthCheck(
                    advancedOptions.HealthCheckName,
                    advancedOptions.HealthCheckTags);
            }

            return services;
        }

        /// <summary>
        /// Add RabbitMQ hosted service for background consumption.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Optional hosted service options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursHostedService(this IServiceCollection services,
            Action<RabbitMQHostedOptions>? options = null)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<RabbitMQHostedOptions>(opt => { });
            }
            services.AddHostedService<MvpRabbitMQHostedService>();
            return services;
        }
    }

    /// <summary>
    /// Options for advanced RabbitMQ features.
    /// </summary>
    public class RabbitMQAdvancedOptions
    {
        /// <summary>
        /// Gets or sets whether metrics collection is enabled. Default is true.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether structured logging is enabled. Default is true.
        /// </summary>
        public bool EnableStructuredLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets whether message deduplication is enabled. Default is false.
        /// </summary>
        public bool EnableDeduplication { get; set; } = false;

        /// <summary>
        /// Gets or sets the deduplication expiration time in minutes. Default is 60.
        /// </summary>
        public int DeduplicationExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// Gets or sets the maximum deduplication entries. Default is 100,000.
        /// </summary>
        public int DeduplicationMaxEntries { get; set; } = 100_000;

        /// <summary>
        /// Gets or sets whether health check is enabled. Default is true.
        /// </summary>
        public bool EnableHealthCheck { get; set; } = true;

        /// <summary>
        /// Gets or sets the health check name. Default is "rabbitmq".
        /// </summary>
        public string HealthCheckName { get; set; } = "rabbitmq";

        /// <summary>
        /// Gets or sets the health check tags.
        /// </summary>
        public IEnumerable<string>? HealthCheckTags { get; set; }
    }
}

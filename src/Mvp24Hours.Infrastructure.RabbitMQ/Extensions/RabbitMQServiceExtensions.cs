//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration.Fluent;
using Mvp24Hours.Infrastructure.RabbitMQ.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Deduplication;
using Mvp24Hours.Infrastructure.RabbitMQ.HealthChecks;
using Mvp24Hours.Infrastructure.RabbitMQ.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Metrics;
using Mvp24Hours.Infrastructure.RabbitMQ.Scheduling;
using Mvp24Hours.Infrastructure.RabbitMQ.Serialization;
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
        #region Fluent API - AddMvpRabbitMQ

        /// <summary>
        /// Adds RabbitMQ services using the fluent configuration builder.
        /// This is the recommended way to configure RabbitMQ in the Mvp24Hours framework.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action for the RabbitMQ builder.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method provides a MassTransit-like API for configuring RabbitMQ:
        /// <list type="bullet">
        /// <item>Host connection settings</item>
        /// <item>Consumer registration with auto-discovery</item>
        /// <item>Request/response clients</item>
        /// <item>Retry and circuit breaker policies</item>
        /// <item>Outbox pattern for transactional messaging</item>
        /// <item>Saga state machines</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvpRabbitMQ(cfg =>
        /// {
        ///     cfg.Host("amqp://guest:guest@localhost:5672", h =>
        ///     {
        ///         h.RetryCount(5);
        ///         h.DispatchConsumersAsync(true);
        ///     });
        ///
        ///     cfg.AddConsumer&lt;OrderCreatedConsumer&gt;(c =>
        ///     {
        ///         c.ConcurrentMessageLimit = 10;
        ///         c.PrefetchCount = 16;
        ///     });
        ///
        ///     cfg.AddConsumersFromAssemblyContaining&lt;OrderCreatedConsumer&gt;();
        ///
        ///     cfg.AddRequestClient&lt;GetOrderRequest, GetOrderResponse&gt;();
        ///
        ///     cfg.UseRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1)));
        ///
        ///     cfg.UseCircuitBreaker(cb =>
        ///     {
        ///         cb.TrackingPeriod(TimeSpan.FromMinutes(1));
        ///         cb.TripThreshold(15);
        ///     });
        ///
        ///     cfg.UseInMemoryOutbox();
        ///
        ///     cfg.ConfigureEndpoints(e =>
        ///     {
        ///         e.UseConventionalNaming();
        ///         e.SetPrefix("myapp");
        ///     });
        ///
        ///     cfg.AddSaga&lt;OrderSaga, OrderSagaData&gt;(s =>
        ///     {
        ///         s.UseRedis();
        ///     });
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvpRabbitMQ(
            this IServiceCollection services,
            Action<RabbitMQConfigurationBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            var builder = new RabbitMQConfigurationBuilder(services);
            configure(builder);
            builder.Build();

            // Register serializer if not already registered
            services.TryAddSingleton<IMessageSerializer, JsonMessageSerializer>();

            return services;
        }

        /// <summary>
        /// Adds RabbitMQ services using the fluent configuration builder with a connection string.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="connectionString">The AMQP connection string.</param>
        /// <param name="configure">Optional configuration action for the RabbitMQ builder.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvpRabbitMQ("amqp://guest:guest@localhost:5672", cfg =>
        /// {
        ///     cfg.AddConsumer&lt;OrderCreatedConsumer&gt;();
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvpRabbitMQ(
            this IServiceCollection services,
            string connectionString,
            Action<RabbitMQConfigurationBuilder>? configure = null)
        {
            return services.AddMvpRabbitMQ(cfg =>
            {
                cfg.Host(connectionString);
                configure?.Invoke(cfg);
            });
        }

        /// <summary>
        /// Adds RabbitMQ services using the fluent configuration builder with host and port.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="host">The host name or IP address.</param>
        /// <param name="port">The port number.</param>
        /// <param name="configure">Optional configuration action for the RabbitMQ builder.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvpRabbitMQ("localhost", 5672, cfg =>
        /// {
        ///     cfg.Host(h =>
        ///     {
        ///         h.Username("guest");
        ///         h.Password("guest");
        ///     });
        ///     cfg.AddConsumer&lt;OrderCreatedConsumer&gt;();
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvpRabbitMQ(
            this IServiceCollection services,
            string host,
            int port,
            Action<RabbitMQConfigurationBuilder>? configure = null)
        {
            return services.AddMvpRabbitMQ(cfg =>
            {
                cfg.Host(host, port);
                configure?.Invoke(cfg);
            });
        }

        #endregion

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

            if (advancedOptions.EnableScheduler)
            {
                services.AddMvp24HoursRabbitMQScheduler(advancedOptions.SchedulerOptions);
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

        /// <summary>
        /// Add RabbitMQ message scheduler with in-memory store.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Optional scheduler options configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQScheduler(this IServiceCollection services,
            Action<MessageSchedulerOptions>? options = null)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<MessageSchedulerOptions>(opt => { });
            }

            services.TryAddSingleton<IScheduledMessageStore, InMemoryScheduledMessageStore>();
            services.TryAddSingleton<MessageScheduler>();
            services.TryAddSingleton<IMessageScheduler>(sp => sp.GetRequiredService<MessageScheduler>());
            services.AddHostedService<ScheduledMessageBackgroundService>();

            return services;
        }

        /// <summary>
        /// Add RabbitMQ message scheduler with Redis store for distributed scenarios.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Optional scheduler options configuration.</param>
        /// <param name="keyPrefix">Optional Redis key prefix. Default is "mvp:scheduled:".</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQSchedulerWithRedis(this IServiceCollection services,
            Action<MessageSchedulerOptions>? options = null,
            string keyPrefix = "mvp:scheduled:")
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<MessageSchedulerOptions>(opt => { });
            }

            services.TryAddSingleton<IScheduledMessageStore>(sp =>
            {
                var cache = sp.GetRequiredService<IDistributedCache>();
                return new RedisScheduledMessageStore(cache, keyPrefix);
            });

            services.TryAddSingleton<MessageScheduler>();
            services.TryAddSingleton<IMessageScheduler>(sp => sp.GetRequiredService<MessageScheduler>());
            services.AddHostedService<ScheduledMessageBackgroundService>();

            return services;
        }

        /// <summary>
        /// Add RabbitMQ message scheduler with custom store.
        /// </summary>
        /// <typeparam name="TStore">The type of scheduled message store.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Optional scheduler options configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQScheduler<TStore>(this IServiceCollection services,
            Action<MessageSchedulerOptions>? options = null)
            where TStore : class, IScheduledMessageStore
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<MessageSchedulerOptions>(opt => { });
            }

            services.TryAddSingleton<IScheduledMessageStore, TStore>();
            services.TryAddSingleton<MessageScheduler>();
            services.TryAddSingleton<IMessageScheduler>(sp => sp.GetRequiredService<MessageScheduler>());
            services.AddHostedService<ScheduledMessageBackgroundService>();

            return services;
        }

        #region Batch Consumers

        /// <summary>
        /// Add a batch consumer for processing messages in batches.
        /// </summary>
        /// <typeparam name="TConsumer">The batch consumer type.</typeparam>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Optional batch consumer options configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQBatchConsumer<TConsumer, TMessage>(
            this IServiceCollection services,
            Action<BatchConsumerOptions>? options = null)
            where TConsumer : class, IBatchConsumer<TMessage>
            where TMessage : class
        {
            // Configure batch options
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<BatchConsumerOptions>(opt => { });
            }

            // Register the consumer
            services.TryAddScoped<IBatchConsumer<TMessage>, TConsumer>();
            services.TryAddScoped<TConsumer>();

            // Register serializer if not already registered
            services.TryAddSingleton<IMessageSerializer, JsonMessageSerializer>();

            // Register the batch processor
            services.TryAddSingleton<BatchConsumerProcessor<TMessage>>(sp =>
            {
                var batchOptions = new BatchConsumerOptions();
                options?.Invoke(batchOptions);

                return new BatchConsumerProcessor<TMessage>(
                    batchOptions,
                    sp,
                    sp.GetRequiredService<IMessageSerializer>(),
                    sp.GetRequiredService<ILogger<BatchConsumerProcessor<TMessage>>>(),
                    sp.GetService<IMvpRabbitMQClient>());
            });

            return services;
        }

        /// <summary>
        /// Add a batch consumer with its definition for processing messages in batches.
        /// </summary>
        /// <typeparam name="TConsumer">The batch consumer type.</typeparam>
        /// <typeparam name="TDefinition">The batch consumer definition type.</typeparam>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQBatchConsumer<TConsumer, TDefinition, TMessage>(
            this IServiceCollection services)
            where TConsumer : class, IBatchConsumer<TMessage>
            where TDefinition : BatchConsumerDefinition<TConsumer>, new()
            where TMessage : class
        {
            var definition = new TDefinition();

            // Register the definition
            services.TryAddSingleton<IBatchConsumerDefinition<TConsumer>>(definition);
            services.TryAddSingleton<IBatchConsumerDefinition>(definition);

            // Register the consumer with options from definition
            return services.AddMvp24HoursRabbitMQBatchConsumer<TConsumer, TMessage>(opts =>
            {
                if (definition.BatchOptions != null)
                {
                    opts.MaxBatchSize = definition.BatchOptions.MaxBatchSize;
                    opts.MinBatchSize = definition.BatchOptions.MinBatchSize;
                    opts.BatchTimeout = definition.BatchOptions.BatchTimeout;
                    opts.MessageWaitTimeout = definition.BatchOptions.MessageWaitTimeout;
                    opts.EnableParallelProcessing = definition.BatchOptions.EnableParallelProcessing;
                    opts.MaxDegreeOfParallelism = definition.BatchOptions.MaxDegreeOfParallelism;
                    opts.UseBatchAcknowledgment = definition.BatchOptions.UseBatchAcknowledgment;
                    opts.RequeueOnFailure = definition.BatchOptions.RequeueOnFailure;
                    opts.MaxRetryAttempts = definition.BatchOptions.MaxRetryAttempts;
                    opts.RetryDelay = definition.BatchOptions.RetryDelay;
                    opts.UseExponentialBackoff = definition.BatchOptions.UseExponentialBackoff;
                    opts.PrefetchCount = definition.BatchOptions.PrefetchCount;
                }
            });
        }

        /// <summary>
        /// Add batch consumers from assembly with automatic discovery.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assembly">The assembly to scan for batch consumers.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQBatchConsumersFromAssembly(
            this IServiceCollection services,
            Assembly assembly)
        {
            // Find all types implementing IBatchConsumer<T>
            var batchConsumerTypes = assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBatchConsumer<>)))
                .ToList();

            foreach (var consumerType in batchConsumerTypes)
            {
                var consumerInterface = consumerType.GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBatchConsumer<>));

                var messageType = consumerInterface.GetGenericArguments()[0];

                // Register the consumer
                services.AddScoped(consumerInterface, consumerType);
                services.AddScoped(consumerType);

                // Register serializer if not already registered
                services.TryAddSingleton<IMessageSerializer, JsonMessageSerializer>();

                // Register the batch processor using reflection
                var processorType = typeof(BatchConsumerProcessor<>).MakeGenericType(messageType);
                services.TryAddSingleton(processorType, sp =>
                {
                    var batchOptions = BatchConsumerOptions.Default;

                    // Try to get options from definition if available
                    var definitionType = typeof(IBatchConsumerDefinition<>).MakeGenericType(consumerType);
                    var definition = sp.GetService(definitionType) as IBatchConsumerDefinition;
                    if (definition?.BatchOptions != null)
                    {
                        batchOptions = definition.BatchOptions;
                    }

                    var loggerType = typeof(ILogger<>).MakeGenericType(processorType);
                    var logger = sp.GetRequiredService(loggerType);

                    return Activator.CreateInstance(
                        processorType,
                        batchOptions,
                        sp,
                        sp.GetRequiredService<IMessageSerializer>(),
                        logger,
                        sp.GetService<IMvpRabbitMQClient>())!;
                });
            }

            // Find and register batch consumer definitions
            var definitionTypes = assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.BaseType?.IsGenericType == true &&
                    t.BaseType.GetGenericTypeDefinition() == typeof(BatchConsumerDefinition<>))
                .ToList();

            foreach (var definitionType in definitionTypes)
            {
                var consumerType = definitionType.BaseType!.GetGenericArguments()[0];
                var definitionInterface = typeof(IBatchConsumerDefinition<>).MakeGenericType(consumerType);

                services.TryAddSingleton(definitionInterface, sp => Activator.CreateInstance(definitionType)!);
                services.TryAddSingleton(typeof(IBatchConsumerDefinition), sp => sp.GetRequiredService(definitionInterface));
            }

            return services;
        }

        #endregion
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

        /// <summary>
        /// Gets or sets whether the message scheduler is enabled. Default is false.
        /// </summary>
        public bool EnableScheduler { get; set; } = false;

        /// <summary>
        /// Gets or sets the scheduler options configuration action.
        /// </summary>
        public Action<MessageSchedulerOptions>? SchedulerOptions { get; set; }
    }
}

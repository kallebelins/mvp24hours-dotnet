//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration.Fluent
{
    /// <summary>
    /// Fluent builder for configuring RabbitMQ services.
    /// Provides a MassTransit-like API for configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This builder allows configuring all aspects of RabbitMQ integration:
    /// <list type="bullet">
    /// <item>Host connection settings</item>
    /// <item>Consumer registration</item>
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
    ///     cfg.Host("amqp://localhost:5672", h =>
    ///     {
    ///         h.Username("guest");
    ///         h.Password("guest");
    ///         h.RetryCount(5);
    ///     });
    ///
    ///     cfg.AddConsumer&lt;OrderCreatedConsumer&gt;(c =>
    ///     {
    ///         c.ConcurrentMessageLimit = 10;
    ///         c.PrefetchCount = 16;
    ///     });
    ///
    ///     cfg.AddRequestClient&lt;GetOrderRequest, GetOrderResponse&gt;();
    ///
    ///     cfg.UseRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1)));
    ///     cfg.UseCircuitBreaker(cb => cb.TrackingPeriod(TimeSpan.FromMinutes(1)));
    ///
    ///     cfg.UseInMemoryOutbox();
    ///
    ///     cfg.AddSaga&lt;OrderSaga, OrderSagaData&gt;(s => s.UseInMemory());
    /// });
    /// </code>
    /// </example>
    public class RabbitMQConfigurationBuilder
    {
        private readonly IServiceCollection _services;
        private readonly RabbitMQBusConfiguration _configuration;
        private readonly List<Type> _consumerTypes;
        private readonly List<(Type RequestType, Type ResponseType, Action<RequestClientOptions>? Configure)> _requestClients;
        private readonly List<Action<IServiceCollection>> _deferredRegistrations;

        /// <summary>
        /// Creates a new instance of the configuration builder.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public RabbitMQConfigurationBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _configuration = new RabbitMQBusConfiguration();
            _consumerTypes = new List<Type>();
            _requestClients = new List<(Type, Type, Action<RequestClientOptions>?)>();
            _deferredRegistrations = new List<Action<IServiceCollection>>();
        }

        /// <summary>
        /// Gets the service collection.
        /// </summary>
        public IServiceCollection Services => _services;

        /// <summary>
        /// Gets the bus configuration.
        /// </summary>
        internal RabbitMQBusConfiguration Configuration => _configuration;

        #region Host Configuration

        /// <summary>
        /// Configures the RabbitMQ host connection using a connection string.
        /// </summary>
        /// <param name="connectionString">The AMQP connection string.</param>
        /// <param name="configure">Optional configuration action for advanced settings.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.Host("amqp://guest:guest@localhost:5672", h =>
        /// {
        ///     h.RetryCount(5);
        ///     h.DispatchConsumersAsync(true);
        /// });
        /// </code>
        /// </example>
        public RabbitMQConfigurationBuilder Host(string connectionString, Action<HostConfigurationBuilder>? configure = null)
        {
            _configuration.ConnectionOptions.ConnectionString = connectionString;

            if (configure != null)
            {
                var builder = new HostConfigurationBuilder(_configuration.ConnectionOptions);
                configure(builder);
            }

            return this;
        }

        /// <summary>
        /// Configures the RabbitMQ host connection with detailed settings.
        /// </summary>
        /// <param name="host">The host name or IP address.</param>
        /// <param name="port">The port number.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder Host(string host, int port, Action<HostConfigurationBuilder>? configure = null)
        {
            _configuration.ConnectionOptions.Configuration = new RabbitMQConnection
            {
                HostName = host,
                Port = port
            };

            if (configure != null)
            {
                var builder = new HostConfigurationBuilder(_configuration.ConnectionOptions);
                configure(builder);
            }

            return this;
        }

        /// <summary>
        /// Configures the RabbitMQ host using an existing RabbitMQConnection configuration.
        /// </summary>
        /// <param name="configure">Configuration action.</param>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder Host(Action<HostConfigurationBuilder> configure)
        {
            var builder = new HostConfigurationBuilder(_configuration.ConnectionOptions);
            configure(builder);
            return this;
        }

        #endregion

        #region Consumer Registration

        /// <summary>
        /// Adds a consumer type for message consumption.
        /// </summary>
        /// <typeparam name="TConsumer">The consumer type.</typeparam>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder AddConsumer<TConsumer>()
            where TConsumer : class, IMvpRabbitMQConsumer
        {
            _consumerTypes.Add(typeof(TConsumer));
            _services.AddScoped<TConsumer>();
            return this;
        }

        /// <summary>
        /// Adds a consumer type with specific configuration.
        /// </summary>
        /// <typeparam name="TConsumer">The consumer type.</typeparam>
        /// <param name="configure">Configuration action for consumer options.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.AddConsumer&lt;OrderCreatedConsumer&gt;(c =>
        /// {
        ///     c.ConcurrentMessageLimit = 10;
        ///     c.PrefetchCount = 16;
        ///     c.RetryAttempts = 3;
        /// });
        /// </code>
        /// </example>
        public RabbitMQConfigurationBuilder AddConsumer<TConsumer>(Action<ConsumerConfiguration> configure)
            where TConsumer : class, IMvpRabbitMQConsumer
        {
            _consumerTypes.Add(typeof(TConsumer));

            var consumerConfig = new ConsumerConfiguration();
            configure(consumerConfig);

            // Store consumer-specific configuration
            _configuration.ConsumerConfigurations[typeof(TConsumer)] = consumerConfig;

            _services.AddScoped<TConsumer>();

            return this;
        }

        /// <summary>
        /// Adds a typed message consumer.
        /// </summary>
        /// <typeparam name="TConsumer">The consumer type.</typeparam>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder AddConsumer<TConsumer, TMessage>()
            where TConsumer : class, IMessageConsumer<TMessage>
            where TMessage : class
        {
            _consumerTypes.Add(typeof(TConsumer));
            _services.AddScoped<TConsumer>();
            _services.AddScoped<IMessageConsumer<TMessage>, TConsumer>();
            return this;
        }

        /// <summary>
        /// Adds a typed message consumer with configuration.
        /// </summary>
        /// <typeparam name="TConsumer">The consumer type.</typeparam>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="configure">Configuration action for consumer options.</param>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder AddConsumer<TConsumer, TMessage>(Action<ConsumerConfiguration> configure)
            where TConsumer : class, IMessageConsumer<TMessage>
            where TMessage : class
        {
            _consumerTypes.Add(typeof(TConsumer));

            var consumerConfig = new ConsumerConfiguration();
            configure(consumerConfig);
            _configuration.ConsumerConfigurations[typeof(TConsumer)] = consumerConfig;

            _services.AddScoped<TConsumer>();
            _services.AddScoped<IMessageConsumer<TMessage>, TConsumer>();

            return this;
        }

        /// <summary>
        /// Scans an assembly and registers all consumers found.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder AddConsumersFromAssembly(Assembly assembly)
        {
            var consumerTypes = assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.GetInterfaces().Any(i =>
                    typeof(IMvpRabbitMQConsumer).IsAssignableFrom(i) ||
                    (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>))));

            foreach (var consumerType in consumerTypes)
            {
                _consumerTypes.Add(consumerType);
                _services.AddScoped(consumerType);

                // Also register as IMessageConsumer<T> if applicable
                var messageConsumerInterface = consumerType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>));

                if (messageConsumerInterface != null)
                {
                    _services.AddScoped(messageConsumerInterface, consumerType);
                }
            }

            return this;
        }

        /// <summary>
        /// Scans an assembly containing a specific marker type and registers all consumers found.
        /// </summary>
        /// <typeparam name="T">A type in the assembly to scan.</typeparam>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder AddConsumersFromAssemblyContaining<T>()
        {
            return AddConsumersFromAssembly(typeof(T).Assembly);
        }

        #endregion

        #region Request Client Registration

        /// <summary>
        /// Adds a request client for request/response messaging.
        /// </summary>
        /// <typeparam name="TRequest">The request message type.</typeparam>
        /// <typeparam name="TResponse">The response message type.</typeparam>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder AddRequestClient<TRequest, TResponse>()
            where TRequest : class
            where TResponse : class
        {
            _requestClients.Add((typeof(TRequest), typeof(TResponse), null));
            return this;
        }

        /// <summary>
        /// Adds a request client with specific configuration.
        /// </summary>
        /// <typeparam name="TRequest">The request message type.</typeparam>
        /// <typeparam name="TResponse">The response message type.</typeparam>
        /// <param name="configure">Configuration action for request client options.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.AddRequestClient&lt;GetOrderRequest, GetOrderResponse&gt;(r =>
        /// {
        ///     r.TimeoutMilliseconds = 60000;
        ///     r.Exchange = "orders";
        ///     r.RoutingKey = "orders.get";
        /// });
        /// </code>
        /// </example>
        public RabbitMQConfigurationBuilder AddRequestClient<TRequest, TResponse>(Action<RequestClientOptions> configure)
            where TRequest : class
            where TResponse : class
        {
            _requestClients.Add((typeof(TRequest), typeof(TResponse), configure));
            return this;
        }

        #endregion

        #region Retry Policy

        /// <summary>
        /// Configures retry policy for message handling.
        /// </summary>
        /// <param name="configure">Configuration action for retry policy.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.UseRetry(r =>
        /// {
        ///     r.Exponential(3, TimeSpan.FromSeconds(1));
        ///     // or
        ///     r.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
        ///     // or
        ///     r.Immediate(5);
        /// });
        /// </code>
        /// </example>
        public RabbitMQConfigurationBuilder UseRetry(Action<RetryPolicyBuilder> configure)
        {
            var builder = new RetryPolicyBuilder();
            configure(builder);
            _configuration.RetryPolicy = builder.Build();
            return this;
        }

        #endregion

        #region Circuit Breaker

        /// <summary>
        /// Configures circuit breaker for message handling.
        /// </summary>
        /// <param name="configure">Configuration action for circuit breaker policy.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.UseCircuitBreaker(cb =>
        /// {
        ///     cb.TrackingPeriod(TimeSpan.FromMinutes(1));
        ///     cb.TripThreshold(15);
        ///     cb.ActiveThreshold(10);
        ///     cb.ResetInterval(TimeSpan.FromMinutes(5));
        /// });
        /// </code>
        /// </example>
        public RabbitMQConfigurationBuilder UseCircuitBreaker(Action<CircuitBreakerPolicyBuilder> configure)
        {
            var builder = new CircuitBreakerPolicyBuilder();
            configure(builder);
            _configuration.CircuitBreakerPolicy = builder.Build();
            return this;
        }

        #endregion

        #region Outbox Pattern

        /// <summary>
        /// Configures in-memory outbox for transactional messaging (development/testing only).
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// The in-memory outbox is not suitable for production as messages will be lost on restart.
        /// Use <see cref="UseEntityFrameworkOutbox{TDbContext}"/> for production scenarios.
        /// </para>
        /// </remarks>
        public RabbitMQConfigurationBuilder UseInMemoryOutbox()
        {
            _configuration.UseInMemoryOutbox = true;
            _deferredRegistrations.Add(services =>
            {
                Transactional.Extensions.TransactionalMessagingExtensions.AddTransactionalMessaging(services);
            });
            return this;
        }

        /// <summary>
        /// Configures in-memory outbox with specific options.
        /// </summary>
        /// <param name="configure">Configuration action for outbox options.</param>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder UseInMemoryOutbox(Action<OutboxOptions> configure)
        {
            _configuration.UseInMemoryOutbox = true;
            var options = new OutboxOptions();
            configure(options);
            _configuration.OutboxOptions = options;

            _deferredRegistrations.Add(services =>
            {
                Transactional.Extensions.TransactionalMessagingExtensions.AddTransactionalMessaging(services);
                
                // Configure OutboxPublisher options
                services.Configure<Transactional.OutboxPublisherOptions>(publisherOpts =>
                {
                    publisherOpts.PollingInterval = options.PublishInterval;
                    publisherOpts.BatchSize = options.BatchSize;
                });
            });
            return this;
        }

        /// <summary>
        /// Configures Entity Framework Core outbox for transactional messaging.
        /// </summary>
        /// <typeparam name="TDbContext">The DbContext type that implements outbox tables.</typeparam>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// The DbContext must have an OutboxMessages DbSet configured.
        /// Recommended for production use.
        /// </para>
        /// </remarks>
        public RabbitMQConfigurationBuilder UseEntityFrameworkOutbox<TDbContext>()
            where TDbContext : class
        {
            _configuration.UseEntityFrameworkOutbox = true;
            _configuration.OutboxDbContextType = typeof(TDbContext);
            return this;
        }

        /// <summary>
        /// Configures Entity Framework Core outbox with specific options.
        /// </summary>
        /// <typeparam name="TDbContext">The DbContext type.</typeparam>
        /// <param name="configure">Configuration action for outbox options.</param>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder UseEntityFrameworkOutbox<TDbContext>(Action<OutboxOptions> configure)
            where TDbContext : class
        {
            _configuration.UseEntityFrameworkOutbox = true;
            _configuration.OutboxDbContextType = typeof(TDbContext);

            var options = new OutboxOptions();
            configure(options);
            _configuration.OutboxOptions = options;

            return this;
        }

        #endregion

        #region Saga Configuration

        /// <summary>
        /// Adds a saga state machine.
        /// </summary>
        /// <typeparam name="TSaga">The saga state machine type.</typeparam>
        /// <typeparam name="TInstance">The saga instance data type.</typeparam>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder AddSaga<TSaga, TInstance>()
            where TSaga : SagaStateMachine<TInstance>
            where TInstance : class, new()
        {
            _deferredRegistrations.Add(services =>
            {
                services.AddSaga<TInstance, TSaga>();
            });
            return this;
        }

        /// <summary>
        /// Adds a saga state machine with specific configuration.
        /// </summary>
        /// <typeparam name="TSaga">The saga state machine type.</typeparam>
        /// <typeparam name="TInstance">The saga instance data type.</typeparam>
        /// <param name="configure">Configuration action for saga options.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.AddSaga&lt;OrderSaga, OrderSagaData&gt;(s =>
        /// {
        ///     s.UseRedis(options =>
        ///     {
        ///         options.DefaultExpiration = TimeSpan.FromHours(24);
        ///     });
        ///     // or
        ///     s.UseEntityFramework();
        ///     // or
        ///     s.UseMongoDB();
        /// });
        /// </code>
        /// </example>
        public RabbitMQConfigurationBuilder AddSaga<TSaga, TInstance>(Action<SagaConfigurationBuilder<TInstance>> configure)
            where TSaga : SagaStateMachine<TInstance>
            where TInstance : class, new()
        {
            var sagaBuilder = new SagaConfigurationBuilder<TInstance>();
            configure(sagaBuilder);

            _deferredRegistrations.Add(services =>
            {
                services.AddSaga<TInstance, TSaga>(opts =>
                {
                    opts.PersistenceType = sagaBuilder.PersistenceType;
                    opts.DefaultExpiration = sagaBuilder.DefaultExpiration;
                    opts.CompletedExpiration = sagaBuilder.CompletedExpiration;
                    opts.EnableTimeouts = sagaBuilder.EnableTimeouts;
                    opts.TimeoutCheckInterval = sagaBuilder.TimeoutCheckInterval;
                });
            });

            return this;
        }

        #endregion

        #region Endpoint Configuration

        /// <summary>
        /// Configures all endpoints automatically based on registered consumers.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder ConfigureEndpoints()
        {
            _configuration.AutoConfigureEndpoints = true;
            return this;
        }

        /// <summary>
        /// Configures endpoints with a custom configuration action.
        /// </summary>
        /// <param name="configure">Configuration action for endpoints.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cfg.ConfigureEndpoints(e =>
        /// {
        ///     e.UseConventionalNaming();
        ///     e.SetPrefix("myapp");
        ///     e.SetSuffix("queue");
        /// });
        /// </code>
        /// </example>
        public RabbitMQConfigurationBuilder ConfigureEndpoints(Action<EndpointConfigurationBuilder> configure)
        {
            _configuration.AutoConfigureEndpoints = true;

            var builder = new EndpointConfigurationBuilder();
            configure(builder);
            _configuration.EndpointConfiguration = builder.Build();

            return this;
        }

        #endregion

        #region Filters

        /// <summary>
        /// Adds a consume filter to the pipeline.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder UseConsumeFilter<TFilter>()
            where TFilter : class, Pipeline.Contract.IConsumeFilter
        {
            _deferredRegistrations.Add(services =>
            {
                FilterPipelineExtensions.AddConsumeFilter<TFilter>(services);
            });
            return this;
        }

        /// <summary>
        /// Adds a publish filter to the pipeline.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder UsePublishFilter<TFilter>()
            where TFilter : class, Pipeline.Contract.IPublishFilter
        {
            _deferredRegistrations.Add(services =>
            {
                FilterPipelineExtensions.AddPublishFilter<TFilter>(services);
            });
            return this;
        }

        /// <summary>
        /// Adds a send filter to the pipeline.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder UseSendFilter<TFilter>()
            where TFilter : class, Pipeline.Contract.ISendFilter
        {
            _deferredRegistrations.Add(services =>
            {
                FilterPipelineExtensions.AddSendFilter<TFilter>(services);
            });
            return this;
        }

        /// <summary>
        /// Adds a typed consume filter for a specific message type.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder UseConsumeFilter<TFilter, TMessage>()
            where TFilter : class, Pipeline.Contract.IConsumeFilter<TMessage>
            where TMessage : class
        {
            _deferredRegistrations.Add(services =>
            {
                FilterPipelineExtensions.AddConsumeFilter<TFilter, TMessage>(services);
            });
            return this;
        }

        #endregion

        #region Client Options

        /// <summary>
        /// Configures the client options for the RabbitMQ client.
        /// </summary>
        /// <param name="configure">Configuration action for client options.</param>
        /// <returns>The builder for chaining.</returns>
        public RabbitMQConfigurationBuilder ConfigureClient(Action<RabbitMQClientOptions> configure)
        {
            configure(_configuration.ClientOptions);
            return this;
        }

        #endregion

        #region Build

        /// <summary>
        /// Builds and registers all configured services.
        /// Called internally by AddMvpRabbitMQ extension.
        /// </summary>
        internal void Build()
        {
            // Register connection options
            _services.Configure<RabbitMQConnectionOptions>(opts =>
            {
                opts.ConnectionString = _configuration.ConnectionOptions.ConnectionString;
                opts.Configuration = _configuration.ConnectionOptions.Configuration;
                opts.RetryCount = _configuration.ConnectionOptions.RetryCount;
                opts.DispatchConsumersAsync = _configuration.ConnectionOptions.DispatchConsumersAsync;
            });

            // Register client options
            _services.Configure<RabbitMQClientOptions>(opts =>
            {
                opts.Exchange = _configuration.ClientOptions.Exchange;
                opts.ExchangeType = _configuration.ClientOptions.ExchangeType;
                opts.RoutingKey = _configuration.ClientOptions.RoutingKey;
                opts.QueueName = _configuration.ClientOptions.QueueName;
                opts.Durable = _configuration.ClientOptions.Durable;
                opts.Exclusive = _configuration.ClientOptions.Exclusive;
                opts.AutoDelete = _configuration.ClientOptions.AutoDelete;
                opts.MaxRedeliveredCount = _configuration.ClientOptions.MaxRedeliveredCount;
                opts.DeadLetter = _configuration.ClientOptions.DeadLetter;
                opts.ConsumerPrefetch = _configuration.ClientOptions.ConsumerPrefetch;
                opts.EnableStructuredLogging = _configuration.ClientOptions.EnableStructuredLogging;
                opts.EnableMetrics = _configuration.ClientOptions.EnableMetrics;
            });

            // Register retry policy if configured
            if (_configuration.RetryPolicy != null)
            {
                _services.AddSingleton(_configuration.RetryPolicy);
            }

            // Register circuit breaker policy if configured
            if (_configuration.CircuitBreakerPolicy != null)
            {
                _services.AddSingleton(_configuration.CircuitBreakerPolicy);
            }

            // Register consumer configurations
            foreach (var (consumerType, config) in _configuration.ConsumerConfigurations)
            {
                _services.AddSingleton(new ConsumerConfigurationRegistration(consumerType, config));
            }

            // Register connection and client
            _services.AddSingleton<IMvpRabbitMQConnection, MvpRabbitMQConnection>();
            _services.AddSingleton<IMvpRabbitMQClient>(sp =>
            {
                var client = new MvpRabbitMQClient(sp);

                // Register all consumer types
                foreach (var consumerType in _consumerTypes)
                {
                    client.Register(consumerType);
                }

                return client;
            });

            // Register request clients
            foreach (var (requestType, responseType, configure) in _requestClients)
            {
                RegisterRequestClient(requestType, responseType, configure);
            }

            // Run deferred registrations (outbox, saga, filters, etc.)
            foreach (var registration in _deferredRegistrations)
            {
                registration(_services);
            }

            // Register endpoint configuration if auto-configured
            if (_configuration.AutoConfigureEndpoints && _configuration.EndpointConfiguration != null)
            {
                _services.AddSingleton(_configuration.EndpointConfiguration);
            }

            // Register the bus configuration for other components to use
            _services.AddSingleton(_configuration);
        }

        private void RegisterRequestClient(Type requestType, Type responseType, Action<RequestClientOptions>? configure)
        {
            var requestClientType = typeof(IRequestClient<,>).MakeGenericType(requestType, responseType);
            var implementationType = typeof(RequestResponse.RequestClient<,>).MakeGenericType(requestType, responseType);

            _services.AddScoped(requestClientType, sp =>
            {
                var connection = sp.GetRequiredService<IMvpRabbitMQConnection>();
                var serializer = sp.GetRequiredService<IMessageSerializer>();

                var options = new RequestClientOptions();
                configure?.Invoke(options);

                return Activator.CreateInstance(
                    implementationType,
                    connection,
                    serializer,
                    Microsoft.Extensions.Options.Options.Create(options))!;
            });
        }

        #endregion
    }
}


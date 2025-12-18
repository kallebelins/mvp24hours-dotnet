//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Application.Contract.Events;
using Mvp24Hours.Application.Logic.Events;
using System;
using System.Linq;
using System.Reflection;

namespace Mvp24Hours.Application.Extensions;

/// <summary>
/// Extension methods for registering application event services.
/// </summary>
public static class ApplicationEventServiceCollectionExtensions
{
    #region [ Basic Registration ]

    /// <summary>
    /// Adds application event services with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationEvents();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationEvents(this IServiceCollection services)
    {
        return services.AddMvp24HoursApplicationEvents(_ => { });
    }

    /// <summary>
    /// Adds application event services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for dispatcher options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationEvents(options =>
    /// {
    ///     options.Strategy = EventDispatchStrategy.Parallel;
    ///     options.ContinueOnError = true;
    ///     options.UseOutbox = false;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationEvents(
        this IServiceCollection services,
        Action<ApplicationEventDispatcherOptions> configure)
    {
        // Register options
        services.Configure(configure);

        // Register dispatcher
        services.TryAddScoped<IApplicationEventDispatcher, ApplicationEventDispatcher>();

        return services;
    }

    #endregion

    #region [ Outbox Pattern ]

    /// <summary>
    /// Adds the in-memory application event outbox.
    /// Useful for development and testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Warning:</strong> Events stored in the in-memory outbox will be lost
    /// if the application restarts. Use a persistent implementation for production.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationEvents(options => options.UseOutbox = true)
    ///         .AddMvp24HoursApplicationEventOutboxInMemory();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationEventOutboxInMemory(this IServiceCollection services)
    {
        services.TryAddSingleton<IApplicationEventOutbox, InMemoryApplicationEventOutbox>();
        return services;
    }

    /// <summary>
    /// Adds a custom application event outbox implementation.
    /// </summary>
    /// <typeparam name="TOutbox">The outbox implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationEventOutbox&lt;SqlServerApplicationEventOutbox&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationEventOutbox<TOutbox>(this IServiceCollection services)
        where TOutbox : class, IApplicationEventOutbox
    {
        services.AddSingleton<IApplicationEventOutbox, TOutbox>();
        return services;
    }

    /// <summary>
    /// Adds the outbox processor background service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationEventOutboxProcessor();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationEventOutboxProcessor(this IServiceCollection services)
    {
        return services.AddMvp24HoursApplicationEventOutboxProcessor(_ => { });
    }

    /// <summary>
    /// Adds the outbox processor background service with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for processor options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationEventOutboxProcessor(options =>
    /// {
    ///     options.PollingIntervalMs = 10000;
    ///     options.BatchSize = 50;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationEventOutboxProcessor(
        this IServiceCollection services,
        Action<ApplicationEventOutboxProcessorOptions> configure)
    {
        services.Configure(configure);
        services.AddHostedService<ApplicationEventOutboxProcessor>();
        return services;
    }

    #endregion

    #region [ Handler Registration ]

    /// <summary>
    /// Registers all application event handlers from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationEventHandlers(typeof(MyHandler).Assembly);
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationEventHandlers(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddMvp24HoursApplicationEventHandlers(ServiceLifetime.Scoped, assemblies);
    }

    /// <summary>
    /// Registers all application event handlers from the specified assemblies with the given lifetime.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursApplicationEventHandlers(
        this IServiceCollection services,
        ServiceLifetime lifetime,
        params Assembly[] assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be specified.", nameof(assemblies));
        }

        var handlerInterfaceType = typeof(IApplicationEventHandler<>);

        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == handlerInterfaceType))
                .ToList();

            foreach (var handlerType in handlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
                    .ToList();

                foreach (var @interface in interfaces)
                {
                    services.Add(new ServiceDescriptor(@interface, handlerType, lifetime));
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Registers all application event handlers from the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="T">A type in the assembly to scan.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime (default: Scoped).</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationEventHandlersFromAssemblyContaining&lt;MyHandler&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationEventHandlersFromAssemblyContaining<T>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        return services.AddMvp24HoursApplicationEventHandlers(lifetime, typeof(T).Assembly);
    }

    /// <summary>
    /// Registers a specific application event handler.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime (default: Scoped).</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationEventHandler&lt;CustomerCreatedHandler, EntityCreatedEvent&lt;Customer&gt;&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationEventHandler<THandler, TEvent>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class, IApplicationEventHandler<TEvent>
        where TEvent : IApplicationEvent
    {
        services.Add(new ServiceDescriptor(
            typeof(IApplicationEventHandler<TEvent>),
            typeof(THandler),
            lifetime));

        return services;
    }

    #endregion

    #region [ All-in-One Registration ]

    /// <summary>
    /// Adds all application event services including dispatcher, handlers, outbox, and processor.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDispatcher">Configuration action for dispatcher options.</param>
    /// <param name="configureProcessor">Configuration action for processor options.</param>
    /// <param name="handlerAssemblies">Assemblies to scan for handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationEventsWithOutbox(
    ///     dispatcherOptions => 
    ///     {
    ///         dispatcherOptions.UseOutbox = true;
    ///         dispatcherOptions.Strategy = EventDispatchStrategy.Parallel;
    ///     },
    ///     processorOptions =>
    ///     {
    ///         processorOptions.PollingIntervalMs = 5000;
    ///         processorOptions.BatchSize = 100;
    ///     },
    ///     typeof(MyHandler).Assembly);
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationEventsWithOutbox(
        this IServiceCollection services,
        Action<ApplicationEventDispatcherOptions>? configureDispatcher = null,
        Action<ApplicationEventOutboxProcessorOptions>? configureProcessor = null,
        params Assembly[] handlerAssemblies)
    {
        // Configure dispatcher with outbox enabled
        services.AddMvp24HoursApplicationEvents(options =>
        {
            options.UseOutbox = true;
            configureDispatcher?.Invoke(options);
        });

        // Add outbox
        services.AddMvp24HoursApplicationEventOutboxInMemory();

        // Add processor
        services.AddMvp24HoursApplicationEventOutboxProcessor(options =>
        {
            configureProcessor?.Invoke(options);
        });

        // Add handlers
        if (handlerAssemblies.Length > 0)
        {
            services.AddMvp24HoursApplicationEventHandlers(handlerAssemblies);
        }

        return services;
    }

    #endregion
}


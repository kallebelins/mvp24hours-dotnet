//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Mvp24Hours.Infrastructure.Cqrs.Saga;

/// <summary>
/// Extension methods for registering saga services.
/// </summary>
public static class SagaExtensions
{
    /// <summary>
    /// Adds saga orchestration support to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvpMediator(options => { ... })
    ///         .AddSagaOrchestration(options =>
    ///         {
    ///             options.UseInMemoryStateStore();
    ///             options.RegisterSagasFromAssemblyContaining&lt;OrderSaga&gt;();
    ///         });
    /// </code>
    /// </example>
    public static IServiceCollection AddSagaOrchestration(
        this IServiceCollection services,
        Action<SagaOrchestrationOptions>? configure = null)
    {
        var options = new SagaOrchestrationOptions();
        configure?.Invoke(options);

        // Register state store
        if (options.StateStoreFactory != null)
        {
            services.TryAddSingleton(options.StateStoreFactory);
        }
        else
        {
            services.TryAddSingleton<ISagaStateStore, InMemorySagaStateStore>();
        }

        // Register orchestrator
        services.TryAddScoped<ISagaOrchestrator, SagaOrchestrator>();

        // Register sagas from assemblies
        foreach (var assembly in options.AssembliesToScan)
        {
            RegisterSagasFromAssembly(services, assembly);
        }

        // Register background service if enabled
        if (options.EnableBackgroundService)
        {
            services.AddSingleton(options.HostedServiceOptions);
            services.AddHostedService<SagaHostedService>();
        }

        return services;
    }

    /// <summary>
    /// Adds saga orchestration with default options (in-memory state store).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSagaOrchestration(this IServiceCollection services)
    {
        return services.AddSagaOrchestration(null);
    }

    private static void RegisterSagasFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var sagaTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.GetInterfaces().Any(i =>
                            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISaga<>)))
            .ToList();

        foreach (var type in sagaTypes)
        {
            // Register the saga as transient (new instance per execution)
            services.AddTransient(type);

            // Also register by interface
            var sagaInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISaga<>));

            if (sagaInterface != null)
            {
                services.AddTransient(sagaInterface, type);
            }
        }
    }
}

/// <summary>
/// Options for configuring saga orchestration.
/// </summary>
public sealed class SagaOrchestrationOptions
{
    internal List<Assembly> AssembliesToScan { get; } = new();
    internal Func<IServiceProvider, ISagaStateStore>? StateStoreFactory { get; private set; }
    internal SagaHostedServiceOptions HostedServiceOptions { get; } = new();

    /// <summary>
    /// Gets or sets whether to enable the background service.
    /// Default is true.
    /// </summary>
    public bool EnableBackgroundService { get; set; } = true;

    /// <summary>
    /// Uses the in-memory saga state store.
    /// </summary>
    /// <returns>The options for chaining.</returns>
    public SagaOrchestrationOptions UseInMemoryStateStore()
    {
        StateStoreFactory = _ => new InMemorySagaStateStore();
        return this;
    }

    /// <summary>
    /// Uses a custom saga state store.
    /// </summary>
    /// <typeparam name="TStore">The state store type.</typeparam>
    /// <returns>The options for chaining.</returns>
    public SagaOrchestrationOptions UseStateStore<TStore>() where TStore : class, ISagaStateStore
    {
        StateStoreFactory = sp => ActivatorUtilities.CreateInstance<TStore>(sp);
        return this;
    }

    /// <summary>
    /// Uses a saga state store factory.
    /// </summary>
    /// <param name="factory">The factory function.</param>
    /// <returns>The options for chaining.</returns>
    public SagaOrchestrationOptions UseStateStore(Func<IServiceProvider, ISagaStateStore> factory)
    {
        StateStoreFactory = factory;
        return this;
    }

    /// <summary>
    /// Registers sagas from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The options for chaining.</returns>
    public SagaOrchestrationOptions RegisterSagasFromAssembly(Assembly assembly)
    {
        AssembliesToScan.Add(assembly);
        return this;
    }

    /// <summary>
    /// Registers sagas from the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="T">A type in the assembly to scan.</typeparam>
    /// <returns>The options for chaining.</returns>
    public SagaOrchestrationOptions RegisterSagasFromAssemblyContaining<T>()
    {
        return RegisterSagasFromAssembly(typeof(T).Assembly);
    }

    /// <summary>
    /// Configures the background service options.
    /// </summary>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The options for chaining.</returns>
    public SagaOrchestrationOptions ConfigureBackgroundService(Action<SagaHostedServiceOptions> configure)
    {
        configure(HostedServiceOptions);
        return this;
    }

    /// <summary>
    /// Disables the background service.
    /// </summary>
    /// <returns>The options for chaining.</returns>
    public SagaOrchestrationOptions DisableBackgroundService()
    {
        EnableBackgroundService = false;
        return this;
    }
}


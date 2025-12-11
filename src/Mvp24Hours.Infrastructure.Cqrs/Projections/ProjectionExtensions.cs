//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

namespace Mvp24Hours.Infrastructure.Cqrs.Projections;

/// <summary>
/// Extension methods for configuring projections in the DI container.
/// </summary>
public static class ProjectionExtensions
{
    /// <summary>
    /// Adds the projection infrastructure with in-memory implementations.
    /// Useful for testing and development.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddProjections(options =>
    /// {
    ///     options.AddProjection&lt;OrderSummaryProjection&gt;("OrderSummary");
    ///     options.RegisterHandlersFromAssemblyContaining&lt;OrderSummaryProjectionHandler&gt;();
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddProjections(
        this IServiceCollection services,
        Action<ProjectionOptions>? configure = null)
    {
        var options = new ProjectionOptions();
        configure?.Invoke(options);

        // Register position store
        services.TryAddSingleton<IProjectionPositionStore, InMemoryProjectionPositionStore>();

        // Register projection manager
        services.AddSingleton<IProjectionManager>(sp =>
        {
            var eventStore = sp.GetRequiredService<IEventStoreWithSubscription>();
            var serializer = sp.GetRequiredService<IEventSerializer>();
            var positionStore = sp.GetRequiredService<IProjectionPositionStore>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ProjectionManager>>();

            var manager = new ProjectionManager(eventStore, serializer, sp, positionStore, logger);

            // Register projections from options
            foreach (var registration in options.Registrations)
            {
                manager.RegisterProjection(registration.Name, registration.HandlerTypes.ToArray());
            }

            return manager;
        });

        // Register rebuild service
        services.TryAddSingleton<IProjectionRebuildService, ProjectionRebuildService>();

        // Register handlers from assemblies
        foreach (var assembly in options.AssembliesToScan)
        {
            services.RegisterProjectionHandlersFromAssembly(assembly);
        }

        // Register specific handlers
        foreach (var handlerType in options.HandlerTypes)
        {
            services.RegisterProjectionHandler(handlerType);
        }

        // Register read model repositories
        foreach (var (modelType, repositoryType) in options.RepositoryRegistrations)
        {
            var repoInterfaceType = typeof(IReadModelRepository<>).MakeGenericType(modelType);
            services.TryAddScoped(repoInterfaceType, repositoryType);
        }

        return services;
    }

    /// <summary>
    /// Adds the projection hosted service for background processing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddProjectionHostedService(this IServiceCollection services)
    {
        services.AddHostedService<ProjectionHostedService>();
        return services;
    }

    /// <summary>
    /// Adds an in-memory read model repository.
    /// </summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryReadModelRepository<T>(this IServiceCollection services)
        where T : class
    {
        services.TryAddSingleton<IReadModelRepository<T>, InMemoryReadModelRepository<T>>();
        services.TryAddSingleton<IAdvancedReadModelRepository<T>>(sp =>
            (IAdvancedReadModelRepository<T>)sp.GetRequiredService<IReadModelRepository<T>>());
        return services;
    }

    /// <summary>
    /// Adds an in-memory read model repository with a custom key selector.
    /// </summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="keySelector">The key selector function.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryReadModelRepository<T>(
        this IServiceCollection services,
        Func<T, object> keySelector)
        where T : class
    {
        services.TryAddSingleton<IReadModelRepository<T>>(
            new InMemoryReadModelRepository<T>(keySelector));
        return services;
    }

    /// <summary>
    /// Registers projection handlers from an assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection RegisterProjectionHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(IsProjectionHandler)
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            services.RegisterProjectionHandler(handlerType);
        }

        return services;
    }

    /// <summary>
    /// Registers a specific projection handler.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection RegisterProjectionHandler<THandler>(this IServiceCollection services)
        where THandler : class, IProjectionHandler
    {
        return services.RegisterProjectionHandler(typeof(THandler));
    }

    /// <summary>
    /// Registers a specific projection handler by type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="handlerType">The handler type.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection RegisterProjectionHandler(
        this IServiceCollection services,
        Type handlerType)
    {
        // Register the concrete type
        services.TryAddTransient(handlerType);

        // Register all implemented projection handler interfaces
        var interfaces = handlerType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjectionHandler<>));

        foreach (var @interface in interfaces)
        {
            services.AddTransient(@interface, handlerType);
        }

        // Register IMultiEventProjectionHandler if applicable
        if (typeof(IMultiEventProjectionHandler).IsAssignableFrom(handlerType))
        {
            services.AddTransient(typeof(IMultiEventProjectionHandler), handlerType);
        }

        return services;
    }

    private static bool IsProjectionHandler(Type type)
    {
        return type.GetInterfaces().Any(i =>
            (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjectionHandler<>)) ||
            i == typeof(IMultiEventProjectionHandler));
    }
}

/// <summary>
/// Configuration options for projections.
/// </summary>
public class ProjectionOptions
{
    internal List<ProjectionRegistration> Registrations { get; } = new();
    internal List<Assembly> AssembliesToScan { get; } = new();
    internal List<Type> HandlerTypes { get; } = new();
    internal List<(Type ModelType, Type RepositoryType)> RepositoryRegistrations { get; } = new();

    /// <summary>
    /// Registers a projection with its handlers.
    /// </summary>
    /// <param name="name">The projection name.</param>
    /// <param name="handlerTypes">The handler types.</param>
    /// <returns>The options for chaining.</returns>
    public ProjectionOptions AddProjection(string name, params Type[] handlerTypes)
    {
        Registrations.Add(new ProjectionRegistration
        {
            Name = name,
            HandlerTypes = handlerTypes.ToList()
        });

        HandlerTypes.AddRange(handlerTypes);
        return this;
    }

    /// <summary>
    /// Registers a projection with a single handler.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <param name="name">The projection name.</param>
    /// <returns>The options for chaining.</returns>
    public ProjectionOptions AddProjection<THandler>(string name)
        where THandler : class, IProjectionHandler
    {
        return AddProjection(name, typeof(THandler));
    }

    /// <summary>
    /// Registers an assembly to scan for projection handlers.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns>The options for chaining.</returns>
    public ProjectionOptions RegisterHandlersFromAssembly(Assembly assembly)
    {
        AssembliesToScan.Add(assembly);
        return this;
    }

    /// <summary>
    /// Registers an assembly to scan for projection handlers.
    /// </summary>
    /// <typeparam name="T">A type in the assembly.</typeparam>
    /// <returns>The options for chaining.</returns>
    public ProjectionOptions RegisterHandlersFromAssemblyContaining<T>()
    {
        return RegisterHandlersFromAssembly(typeof(T).Assembly);
    }

    /// <summary>
    /// Registers a read model repository.
    /// </summary>
    /// <typeparam name="TModel">The read model type.</typeparam>
    /// <typeparam name="TRepository">The repository implementation type.</typeparam>
    /// <returns>The options for chaining.</returns>
    public ProjectionOptions AddRepository<TModel, TRepository>()
        where TModel : class
        where TRepository : class, IReadModelRepository<TModel>
    {
        RepositoryRegistrations.Add((typeof(TModel), typeof(TRepository)));
        return this;
    }

    /// <summary>
    /// Adds an in-memory repository for a read model.
    /// </summary>
    /// <typeparam name="TModel">The read model type.</typeparam>
    /// <returns>The options for chaining.</returns>
    public ProjectionOptions AddInMemoryRepository<TModel>() where TModel : class
    {
        return AddRepository<TModel, InMemoryReadModelRepository<TModel>>();
    }

    internal class ProjectionRegistration
    {
        public string Name { get; set; } = string.Empty;
        public List<Type> HandlerTypes { get; set; } = new();
    }
}



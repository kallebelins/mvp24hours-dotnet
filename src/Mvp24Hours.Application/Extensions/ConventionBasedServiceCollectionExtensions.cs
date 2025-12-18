//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Contract.Infrastructure.DependencyInjection;

namespace Mvp24Hours.Application.Extensions;

/// <summary>
/// Extension methods for convention-based service registration.
/// Services implementing marker interfaces (IScopedService, ISingletonService, ITransientService)
/// are automatically registered with the corresponding lifetime.
/// </summary>
/// <remarks>
/// <para>
/// This follows the convention-over-configuration principle, reducing boilerplate registration code.
/// </para>
/// <para>
/// <strong>Supported marker interfaces:</strong>
/// <list type="bullet">
/// <item><see cref="IScopedService"/> - Registered with Scoped lifetime</item>
/// <item><see cref="ISingletonService"/> - Registered with Singleton lifetime</item>
/// <item><see cref="ITransientService"/> - Registered with Transient lifetime</item>
/// <item><see cref="IKeyedService"/> - Registered as keyed service (requires <see cref="ServiceKeyAttribute"/>)</item>
/// <item><see cref="ISelfRegistering"/> - Also registered by concrete type</item>
/// </list>
/// </para>
/// <para>
/// <strong>Supported attributes:</strong>
/// <list type="bullet">
/// <item><see cref="ServiceKeyAttribute"/> - Specifies the key for keyed services</item>
/// <item><see cref="ServiceOrderAttribute"/> - Controls registration order</item>
/// <item><see cref="ServiceReplaceAttribute"/> - Replaces existing registrations</item>
/// <item><see cref="ServiceTryAddAttribute"/> - Only adds if not already registered</item>
/// <item><see cref="ServiceIgnoreAttribute"/> - Excludes from registration</item>
/// </list>
/// </para>
/// </remarks>
public static class ConventionBasedServiceCollectionExtensions
{
    #region [ Convention-Based Registration ]

    /// <summary>
    /// Registers services from the specified assemblies using convention-based registration.
    /// Services implementing IScopedService, ISingletonService, or ITransientService are automatically registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for services.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursServicesByConvention(
    ///     typeof(CustomerService).Assembly,
    ///     typeof(OrderService).Assembly);
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursServicesByConvention(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (assemblies == null || assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be specified.", nameof(assemblies));
        }

        foreach (var assembly in assemblies)
        {
            services.RegisterServicesByConvention(assembly);
        }

        return services;
    }

    /// <summary>
    /// Registers services from the assembly containing the specified type using convention-based registration.
    /// </summary>
    /// <typeparam name="T">A type in the assembly to scan.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursServicesByConventionFromAssemblyContaining&lt;CustomerService&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursServicesByConventionFromAssemblyContaining<T>(
        this IServiceCollection services)
    {
        return services.AddMvp24HoursServicesByConvention(typeof(T).Assembly);
    }

    /// <summary>
    /// Registers services from specified assemblies with a filter predicate.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="filter">A predicate to filter which types should be registered.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Only register services in specific namespace
    /// services.AddMvp24HoursServicesByConvention(
    ///     new[] { assembly },
    ///     type => type.Namespace?.StartsWith("MyApp.Services") == true);
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursServicesByConvention(
        this IServiceCollection services,
        Assembly[] assemblies,
        Func<Type, bool> filter)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (assemblies == null || assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be specified.", nameof(assemblies));
        }
        if (filter == null) throw new ArgumentNullException(nameof(filter));

        foreach (var assembly in assemblies)
        {
            services.RegisterServicesByConvention(assembly, filter);
        }

        return services;
    }

    /// <summary>
    /// Registers only Scoped services by convention.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursScopedServicesByConvention(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddMvp24HoursServicesByConvention(
            assemblies,
            type => typeof(IScopedService).IsAssignableFrom(type));
    }

    /// <summary>
    /// Registers only Singleton services by convention.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursSingletonServicesByConvention(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddMvp24HoursServicesByConvention(
            assemblies,
            type => typeof(ISingletonService).IsAssignableFrom(type));
    }

    /// <summary>
    /// Registers only Transient services by convention.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursTransientServicesByConvention(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddMvp24HoursServicesByConvention(
            assemblies,
            type => typeof(ITransientService).IsAssignableFrom(type));
    }

    #endregion

    #region [ Internal Registration Logic ]

    private static void RegisterServicesByConvention(
        this IServiceCollection services,
        Assembly assembly,
        Func<Type, bool>? filter = null)
    {
        var markerInterface = typeof(IServiceLifetimeMarker);

        // Get all eligible types
        var eligibleTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .Where(t => markerInterface.IsAssignableFrom(t))
            .Where(t => !t.IsDefined(typeof(ServiceIgnoreAttribute), false))
            .Where(t => filter?.Invoke(t) ?? true)
            .Select(t => new
            {
                Type = t,
                Order = t.GetCustomAttribute<ServiceOrderAttribute>()?.Order ?? int.MaxValue
            })
            .OrderBy(x => x.Order)
            .Select(x => x.Type)
            .ToList();

        foreach (var implementationType in eligibleTypes)
        {
            var lifetime = GetServiceLifetime(implementationType);
            var isKeyedService = typeof(IKeyedService).IsAssignableFrom(implementationType);
            var isSelfRegistering = typeof(ISelfRegistering).IsAssignableFrom(implementationType);
            var shouldReplace = implementationType.IsDefined(typeof(ServiceReplaceAttribute), false);
            var shouldTryAdd = implementationType.IsDefined(typeof(ServiceTryAddAttribute), false);

            // Get all interfaces that are not marker interfaces
            var serviceInterfaces = implementationType.GetInterfaces()
                .Where(i => !markerInterface.IsAssignableFrom(i))
                .Where(i => !i.IsGenericType || i.GetGenericTypeDefinition() != typeof(IEquatable<>))
                .Where(i => !i.IsGenericType || i.GetGenericTypeDefinition() != typeof(IComparable<>))
                .Where(i => i != typeof(IDisposable))
                .Where(i => i != typeof(IAsyncDisposable))
                .ToList();

            // If keyed service, get the key
            string? serviceKey = null;
            if (isKeyedService)
            {
                var keyAttribute = implementationType.GetCustomAttribute<Core.Contract.Infrastructure.DependencyInjection.ServiceKeyAttribute>();
                serviceKey = keyAttribute?.Key;

                if (string.IsNullOrEmpty(serviceKey))
                {
                    // Use type name as default key if no attribute
                    serviceKey = implementationType.Name;
                }
            }

            // Register for each interface
            foreach (var serviceInterface in serviceInterfaces)
            {
                if (isKeyedService && serviceKey != null)
                {
                    // Register as keyed service
                    RegisterKeyedService(services, serviceInterface, implementationType, lifetime, serviceKey, shouldReplace, shouldTryAdd);
                }
                else
                {
                    // Register as regular service
                    RegisterService(services, serviceInterface, implementationType, lifetime, shouldReplace, shouldTryAdd);
                }
            }

            // If no interfaces or self-registering, register by concrete type
            if (!serviceInterfaces.Any() || isSelfRegistering)
            {
                if (isKeyedService && serviceKey != null)
                {
                    RegisterKeyedService(services, implementationType, implementationType, lifetime, serviceKey, shouldReplace, shouldTryAdd);
                }
                else
                {
                    RegisterService(services, implementationType, implementationType, lifetime, shouldReplace, shouldTryAdd);
                }
            }
        }
    }

    private static ServiceLifetime GetServiceLifetime(Type type)
    {
        if (typeof(ISingletonService).IsAssignableFrom(type))
        {
            return ServiceLifetime.Singleton;
        }

        if (typeof(ITransientService).IsAssignableFrom(type))
        {
            return ServiceLifetime.Transient;
        }

        // Default to Scoped (IScopedService or just IServiceLifetimeMarker)
        return ServiceLifetime.Scoped;
    }

    private static void RegisterService(
        IServiceCollection services,
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime,
        bool shouldReplace,
        bool shouldTryAdd)
    {
        var descriptor = new ServiceDescriptor(serviceType, implementationType, lifetime);

        if (shouldReplace)
        {
            services.RemoveAll(serviceType);
            services.Add(descriptor);
        }
        else if (shouldTryAdd)
        {
            services.TryAdd(descriptor);
        }
        else
        {
            services.Add(descriptor);
        }
    }

    private static void RegisterKeyedService(
        IServiceCollection services,
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime,
        string key,
        bool shouldReplace,
        bool shouldTryAdd)
    {
        // Use .NET 8 Keyed Services
        var descriptor = lifetime switch
        {
            ServiceLifetime.Singleton => ServiceDescriptor.KeyedSingleton(serviceType, key, implementationType),
            ServiceLifetime.Transient => ServiceDescriptor.KeyedTransient(serviceType, key, implementationType),
            _ => ServiceDescriptor.KeyedScoped(serviceType, key, implementationType)
        };

        if (shouldReplace)
        {
            // Remove existing keyed service with same key
            var existing = services.FirstOrDefault(d =>
                d.ServiceType == serviceType &&
                d.IsKeyedService &&
                Equals(d.ServiceKey, key));

            if (existing != null)
            {
                services.Remove(existing);
            }

            services.Add(descriptor);
        }
        else if (shouldTryAdd)
        {
            var exists = services.Any(d =>
                d.ServiceType == serviceType &&
                d.IsKeyedService &&
                Equals(d.ServiceKey, key));

            if (!exists)
            {
                services.Add(descriptor);
            }
        }
        else
        {
            services.Add(descriptor);
        }
    }

    #endregion

    #region [ Diagnostic Methods ]

    /// <summary>
    /// Gets information about services that would be registered by convention from the specified assemblies.
    /// Useful for debugging and understanding what services will be registered.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>A collection of service registration information.</returns>
    /// <example>
    /// <code>
    /// var registrations = ConventionBasedServiceCollectionExtensions.GetConventionRegistrations(
    ///     typeof(MyService).Assembly);
    /// 
    /// foreach (var reg in registrations)
    /// {
    ///     Console.WriteLine($"{reg.ServiceType.Name} -> {reg.ImplementationType.Name} ({reg.Lifetime})");
    /// }
    /// </code>
    /// </example>
    public static IEnumerable<ConventionServiceRegistration> GetConventionRegistrations(
        params Assembly[] assemblies)
    {
        var result = new List<ConventionServiceRegistration>();
        var markerInterface = typeof(IServiceLifetimeMarker);

        foreach (var assembly in assemblies)
        {
            var eligibleTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Where(t => markerInterface.IsAssignableFrom(t))
                .Where(t => !t.IsDefined(typeof(ServiceIgnoreAttribute), false))
                .ToList();

            foreach (var implementationType in eligibleTypes)
            {
                var lifetime = GetServiceLifetime(implementationType);
                var isKeyedService = typeof(IKeyedService).IsAssignableFrom(implementationType);
                var isSelfRegistering = typeof(ISelfRegistering).IsAssignableFrom(implementationType);

                string? serviceKey = null;
                if (isKeyedService)
                {
                    var keyAttribute = implementationType.GetCustomAttribute<Core.Contract.Infrastructure.DependencyInjection.ServiceKeyAttribute>();
                    serviceKey = keyAttribute?.Key ?? implementationType.Name;
                }

                var serviceInterfaces = implementationType.GetInterfaces()
                    .Where(i => !markerInterface.IsAssignableFrom(i))
                    .Where(i => !i.IsGenericType || i.GetGenericTypeDefinition() != typeof(IEquatable<>))
                    .Where(i => !i.IsGenericType || i.GetGenericTypeDefinition() != typeof(IComparable<>))
                    .Where(i => i != typeof(IDisposable))
                    .Where(i => i != typeof(IAsyncDisposable))
                    .ToList();

                foreach (var serviceInterface in serviceInterfaces)
                {
                    result.Add(new ConventionServiceRegistration(
                        serviceInterface,
                        implementationType,
                        lifetime,
                        isKeyedService,
                        serviceKey,
                        isSelfRegistering));
                }

                if (!serviceInterfaces.Any() || isSelfRegistering)
                {
                    result.Add(new ConventionServiceRegistration(
                        implementationType,
                        implementationType,
                        lifetime,
                        isKeyedService,
                        serviceKey,
                        isSelfRegistering));
                }
            }
        }

        return result;
    }

    #endregion
}

/// <summary>
/// Information about a service registration discovered by convention.
/// </summary>
/// <param name="ServiceType">The service type (interface or concrete type).</param>
/// <param name="ImplementationType">The implementation type.</param>
/// <param name="Lifetime">The service lifetime.</param>
/// <param name="IsKeyedService">Whether this is a keyed service.</param>
/// <param name="ServiceKey">The service key if keyed, null otherwise.</param>
/// <param name="IsSelfRegistering">Whether this service is also registered by concrete type.</param>
public record ConventionServiceRegistration(
    Type ServiceType,
    Type ImplementationType,
    ServiceLifetime Lifetime,
    bool IsKeyedService,
    string? ServiceKey,
    bool IsSelfRegistering);


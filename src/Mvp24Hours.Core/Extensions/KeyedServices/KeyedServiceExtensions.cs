//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mvp24Hours.Core.Extensions.KeyedServices;

/// <summary>
/// Extension methods for registering and resolving keyed services.
/// </summary>
/// <remarks>
/// <para>
/// Keyed Services are a .NET 8+ feature that allows registering multiple implementations
/// of the same interface with different keys. This is useful for scenarios like:
/// </para>
/// <list type="bullet">
/// <item>Multiple database contexts (Read/Write separation)</item>
/// <item>Multiple cache providers (Memory, Redis, Hybrid)</item>
/// <item>Multiple file storage providers (Local, Azure, AWS)</item>
/// <item>Multi-tenancy (tenant-specific services)</item>
/// <item>Environment-specific implementations</item>
/// </list>
/// <para>
/// <strong>.NET 8+ Feature:</strong> Keyed Services were introduced in .NET 8 and are
/// consolidated in .NET 9. They replace many custom factory patterns with a cleaner,
/// framework-native approach.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register multiple implementations
/// services.AddKeyedServices&lt;IFileStorage&gt;(config =>
/// {
///     config.AddKeyed&lt;LocalFileStorageProvider&gt;(ServiceKeys.FileStorage.Local);
///     config.AddKeyed&lt;AzureBlobStorageProvider&gt;(ServiceKeys.FileStorage.Azure);
///     config.AddKeyed&lt;AwsS3StorageProvider&gt;(ServiceKeys.FileStorage.AwsS3);
///     config.SetDefault(ServiceKeys.FileStorage.Local);
/// });
/// 
/// // Inject specific implementation
/// public class MyService([FromKeyedServices(ServiceKeys.FileStorage.Azure)] IFileStorage storage)
/// {
/// }
/// 
/// // Resolve at runtime
/// var storage = provider.GetRequiredKeyedService&lt;IFileStorage&gt;(ServiceKeys.FileStorage.Azure);
/// </code>
/// </example>
public static class KeyedServiceExtensions
{
    #region Registration Extensions

    /// <summary>
    /// Configures keyed services for a specific interface type.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for keyed services.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddKeyedServices&lt;IFileStorage&gt;(config =>
    /// {
    ///     config.AddKeyed&lt;LocalFileStorageProvider&gt;(ServiceKeys.FileStorage.Local);
    ///     config.AddKeyed&lt;AzureBlobStorageProvider&gt;(ServiceKeys.FileStorage.Azure);
    ///     config.SetDefault(ServiceKeys.FileStorage.Local);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddKeyedServices<TService>(
        this IServiceCollection services,
        Action<KeyedServiceConfiguration<TService>> configure)
        where TService : class
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var config = new KeyedServiceConfiguration<TService>(services);
        configure(config);
        config.Build();

        return services;
    }

    /// <summary>
    /// Adds a keyed singleton service.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKeyedSingletonService<TService, TImplementation>(
        this IServiceCollection services,
        object serviceKey)
        where TService : class
        where TImplementation : class, TService
    {
        services.AddKeyedSingleton<TService, TImplementation>(serviceKey);
        return services;
    }

    /// <summary>
    /// Adds a keyed scoped service.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKeyedScopedService<TService, TImplementation>(
        this IServiceCollection services,
        object serviceKey)
        where TService : class
        where TImplementation : class, TService
    {
        services.AddKeyedScoped<TService, TImplementation>(serviceKey);
        return services;
    }

    /// <summary>
    /// Adds a keyed transient service.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKeyedTransientService<TService, TImplementation>(
        this IServiceCollection services,
        object serviceKey)
        where TService : class
        where TImplementation : class, TService
    {
        services.AddKeyedTransient<TService, TImplementation>(serviceKey);
        return services;
    }

    /// <summary>
    /// Adds a keyed singleton service with factory.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <param name="factory">Factory function to create the service.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKeyedSingletonService<TService>(
        this IServiceCollection services,
        object serviceKey,
        Func<IServiceProvider, object?, TService> factory)
        where TService : class
    {
        services.AddKeyedSingleton(serviceKey, factory);
        return services;
    }

    /// <summary>
    /// Adds a keyed scoped service with factory.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <param name="factory">Factory function to create the service.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKeyedScopedService<TService>(
        this IServiceCollection services,
        object serviceKey,
        Func<IServiceProvider, object?, TService> factory)
        where TService : class
    {
        services.AddKeyedScoped(serviceKey, factory);
        return services;
    }

    /// <summary>
    /// Adds a keyed transient service with factory.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <param name="factory">Factory function to create the service.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKeyedTransientService<TService>(
        this IServiceCollection services,
        object serviceKey,
        Func<IServiceProvider, object?, TService> factory)
        where TService : class
    {
        services.AddKeyedTransient(serviceKey, factory);
        return services;
    }

    /// <summary>
    /// Registers a keyed service as the default (non-keyed) service.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultKey">The key of the service to use as default.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This allows existing code that injects <typeparamref name="TService"/> without
    /// a key to receive the service registered with the specified key.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register keyed services
    /// services.AddKeyedSingleton&lt;IFileStorage, LocalFileStorageProvider&gt;(ServiceKeys.FileStorage.Local);
    /// services.AddKeyedSingleton&lt;IFileStorage, AzureBlobStorageProvider&gt;(ServiceKeys.FileStorage.Azure);
    /// 
    /// // Set default
    /// services.SetDefaultKeyedService&lt;IFileStorage&gt;(ServiceKeys.FileStorage.Local);
    /// 
    /// // Now IFileStorage injections without [FromKeyedServices] will receive LocalFileStorageProvider
    /// </code>
    /// </example>
    public static IServiceCollection SetDefaultKeyedService<TService>(
        this IServiceCollection services,
        object defaultKey)
        where TService : class
    {
        services.AddSingleton<TService>(sp => 
            sp.GetRequiredKeyedService<TService>(defaultKey));
        return services;
    }

    /// <summary>
    /// Tries to add a keyed singleton service if not already registered.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection TryAddKeyedSingletonService<TService, TImplementation>(
        this IServiceCollection services,
        object serviceKey)
        where TService : class
        where TImplementation : class, TService
    {
        ServiceCollectionDescriptorExtensions.TryAddKeyedSingleton<TService, TImplementation>(services, serviceKey);
        return services;
    }

    /// <summary>
    /// Tries to add a keyed scoped service if not already registered.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection TryAddKeyedScopedService<TService, TImplementation>(
        this IServiceCollection services,
        object serviceKey)
        where TService : class
        where TImplementation : class, TService
    {
        ServiceCollectionDescriptorExtensions.TryAddKeyedScoped<TService, TImplementation>(services, serviceKey);
        return services;
    }

    /// <summary>
    /// Tries to add a keyed transient service if not already registered.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection TryAddKeyedTransientService<TService, TImplementation>(
        this IServiceCollection services,
        object serviceKey)
        where TService : class
        where TImplementation : class, TService
    {
        ServiceCollectionDescriptorExtensions.TryAddKeyedTransient<TService, TImplementation>(services, serviceKey);
        return services;
    }

    #endregion

    #region Resolution Extensions

    /// <summary>
    /// Gets a keyed service, returning null if not found.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>The service instance or null.</returns>
    public static TService? GetKeyedServiceOrDefault<TService>(
        this IServiceProvider provider,
        object serviceKey)
        where TService : class
    {
        return provider.GetKeyedService<TService>(serviceKey);
    }

    /// <summary>
    /// Gets a keyed service or a fallback service if not found.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <param name="fallbackFactory">Factory to create fallback service.</param>
    /// <returns>The service instance.</returns>
    public static TService GetKeyedServiceOrFallback<TService>(
        this IServiceProvider provider,
        object serviceKey,
        Func<IServiceProvider, TService> fallbackFactory)
        where TService : class
    {
        return provider.GetKeyedService<TService>(serviceKey) 
               ?? fallbackFactory(provider);
    }

    /// <summary>
    /// Gets a keyed service or the default (non-keyed) service if not found.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>The service instance.</returns>
    public static TService GetKeyedServiceOrDefault<TService>(
        this IServiceProvider provider,
        object serviceKey,
        bool useDefaultIfNotFound)
        where TService : class
    {
        var service = provider.GetKeyedService<TService>(serviceKey);
        if (service != null)
            return service;

        if (useDefaultIfNotFound)
            return provider.GetRequiredService<TService>();

        throw new InvalidOperationException(
            $"Service {typeof(TService).Name} with key '{serviceKey}' is not registered.");
    }

    /// <summary>
    /// Checks if a keyed service is registered.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>True if the service is registered.</returns>
    public static bool HasKeyedService<TService>(
        this IServiceCollection services,
        object serviceKey)
        where TService : class
    {
        return services.Any(d => 
            d.ServiceType == typeof(TService) && 
            d.IsKeyedService && 
            Equals(d.ServiceKey, serviceKey));
    }

    /// <summary>
    /// Gets all registered keys for a service type.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>List of registered keys.</returns>
    public static IEnumerable<object?> GetKeyedServiceKeys<TService>(
        this IServiceCollection services)
        where TService : class
    {
        return services
            .Where(d => d.ServiceType == typeof(TService) && d.IsKeyedService)
            .Select(d => d.ServiceKey);
    }

    #endregion

    #region Multi-Tenant Extensions

    /// <summary>
    /// Adds a tenant-specific keyed service.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="serviceCategory">The service category (e.g., "Database", "Cache").</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTenantKeyedService<TService, TImplementation>(
        this IServiceCollection services,
        string tenantId,
        string serviceCategory,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TService : class
        where TImplementation : class, TService
    {
        var key = ServiceKeys.Tenant.ForTenant(serviceCategory, tenantId);
        var descriptor = new ServiceDescriptor(
            typeof(TService),
            key,
            typeof(TImplementation),
            lifetime);
        services.Add(descriptor);
        return services;
    }

    /// <summary>
    /// Adds a tenant-specific keyed service with factory.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="serviceCategory">The service category.</param>
    /// <param name="factory">Factory function to create the service.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTenantKeyedService<TService>(
        this IServiceCollection services,
        string tenantId,
        string serviceCategory,
        Func<IServiceProvider, object?, TService> factory,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TService : class
    {
        var key = ServiceKeys.Tenant.ForTenant(serviceCategory, tenantId);
        var descriptor = ServiceDescriptor.DescribeKeyed(
            typeof(TService),
            key,
            (sp, k) => factory(sp, k),
            lifetime);
        services.Add(descriptor);
        return services;
    }

    /// <summary>
    /// Gets a tenant-specific service.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="serviceCategory">The service category.</param>
    /// <returns>The tenant-specific service.</returns>
    public static TService GetTenantService<TService>(
        this IServiceProvider provider,
        string tenantId,
        string serviceCategory)
        where TService : class
    {
        var key = ServiceKeys.Tenant.ForTenant(serviceCategory, tenantId);
        return provider.GetRequiredKeyedService<TService>(key);
    }

    /// <summary>
    /// Gets a tenant-specific service, falling back to default if not found.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="serviceCategory">The service category.</param>
    /// <param name="defaultKey">The default service key to fall back to.</param>
    /// <returns>The service instance.</returns>
    public static TService GetTenantServiceOrDefault<TService>(
        this IServiceProvider provider,
        string tenantId,
        string serviceCategory,
        object defaultKey)
        where TService : class
    {
        var tenantKey = ServiceKeys.Tenant.ForTenant(serviceCategory, tenantId);
        var service = provider.GetKeyedService<TService>(tenantKey);
        
        return service ?? provider.GetRequiredKeyedService<TService>(defaultKey);
    }

    #endregion

    #region Assembly Scanning

    /// <summary>
    /// Scans an assembly for types with <see cref="KeyedServiceAttribute"/> and registers them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKeyedServicesFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var typesWithAttribute = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<KeyedServiceAttribute>() != null && !t.IsAbstract);

        foreach (var type in typesWithAttribute)
        {
            var attribute = type.GetCustomAttribute<KeyedServiceAttribute>()!;
            var serviceType = attribute.ServiceType ?? type.GetInterfaces().FirstOrDefault() ?? type;

            var descriptor = new ServiceDescriptor(
                serviceType,
                attribute.Key,
                type,
                attribute.Lifetime);

            services.Add(descriptor);
        }

        return services;
    }

    /// <summary>
    /// Scans the assembly containing the specified type for keyed services.
    /// </summary>
    /// <typeparam name="T">A type in the assembly to scan.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKeyedServicesFromAssemblyContaining<T>(
        this IServiceCollection services)
    {
        return services.AddKeyedServicesFromAssembly(typeof(T).Assembly);
    }

    #endregion

    #region Replace and Remove

    /// <summary>
    /// Replaces a keyed service registration.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The new implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ReplaceKeyedService<TService, TImplementation>(
        this IServiceCollection services,
        object serviceKey,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class
        where TImplementation : class, TService
    {
        // Remove existing
        var existing = services.FirstOrDefault(d =>
            d.ServiceType == typeof(TService) &&
            d.IsKeyedService &&
            Equals(d.ServiceKey, serviceKey));

        if (existing != null)
            services.Remove(existing);

        // Add new
        var descriptor = new ServiceDescriptor(
            typeof(TService),
            serviceKey,
            typeof(TImplementation),
            lifetime);

        services.Add(descriptor);
        return services;
    }

    /// <summary>
    /// Removes a keyed service registration.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection RemoveKeyedService<TService>(
        this IServiceCollection services,
        object serviceKey)
        where TService : class
    {
        var existing = services.FirstOrDefault(d =>
            d.ServiceType == typeof(TService) &&
            d.IsKeyedService &&
            Equals(d.ServiceKey, serviceKey));

        if (existing != null)
            services.Remove(existing);

        return services;
    }

    #endregion
}

/// <summary>
/// Configuration builder for keyed services.
/// </summary>
/// <typeparam name="TService">The service interface type.</typeparam>
public class KeyedServiceConfiguration<TService>
    where TService : class
{
    private readonly IServiceCollection _services;
    private readonly List<(object Key, Type Implementation, ServiceLifetime Lifetime, Func<IServiceProvider, object?, TService>? Factory)> _registrations = new();
    private object? _defaultKey;

    internal KeyedServiceConfiguration(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Adds a keyed service implementation.
    /// </summary>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="key">The service key.</param>
    /// <param name="lifetime">The service lifetime (default: Singleton).</param>
    /// <returns>This configuration for chaining.</returns>
    public KeyedServiceConfiguration<TService> AddKeyed<TImplementation>(
        object key,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TImplementation : class, TService
    {
        _registrations.Add((key, typeof(TImplementation), lifetime, null));
        return this;
    }

    /// <summary>
    /// Adds a keyed service with factory.
    /// </summary>
    /// <param name="key">The service key.</param>
    /// <param name="factory">Factory function to create the service.</param>
    /// <param name="lifetime">The service lifetime (default: Singleton).</param>
    /// <returns>This configuration for chaining.</returns>
    public KeyedServiceConfiguration<TService> AddKeyed(
        object key,
        Func<IServiceProvider, object?, TService> factory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _registrations.Add((key, typeof(TService), lifetime, factory));
        return this;
    }

    /// <summary>
    /// Sets the default key to use when resolving without a key.
    /// </summary>
    /// <param name="defaultKey">The default service key.</param>
    /// <returns>This configuration for chaining.</returns>
    public KeyedServiceConfiguration<TService> SetDefault(object defaultKey)
    {
        _defaultKey = defaultKey;
        return this;
    }

    internal void Build()
    {
        foreach (var (key, implementation, lifetime, factory) in _registrations)
        {
            if (factory != null)
            {
                var descriptor = ServiceDescriptor.DescribeKeyed(
                    typeof(TService),
                    key,
                    (sp, k) => factory(sp, k),
                    lifetime);
                _services.Add(descriptor);
            }
            else
            {
                var descriptor = new ServiceDescriptor(
                    typeof(TService),
                    key,
                    implementation,
                    lifetime);
                _services.Add(descriptor);
            }
        }

        if (_defaultKey != null)
        {
            _services.AddSingleton<TService>(sp =>
                sp.GetRequiredKeyedService<TService>(_defaultKey));
        }
    }
}

/// <summary>
/// Attribute for marking types for automatic keyed service registration.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute on implementation classes to enable automatic registration
/// via <see cref="KeyedServiceExtensions.AddKeyedServicesFromAssembly"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [KeyedService(ServiceKeys.FileStorage.Local, typeof(IFileStorage))]
/// public class LocalFileStorageProvider : IFileStorage
/// {
///     // Implementation
/// }
/// 
/// // Register all keyed services in assembly
/// services.AddKeyedServicesFromAssemblyContaining&lt;LocalFileStorageProvider&gt;();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class KeyedServiceAttribute : Attribute
{
    /// <summary>
    /// Gets the service key.
    /// </summary>
    public object Key { get; }

    /// <summary>
    /// Gets or sets the service interface type.
    /// If not specified, uses the first implemented interface or the class type itself.
    /// </summary>
    public Type? ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the service lifetime. Default is <see cref="ServiceLifetime.Singleton"/>.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedServiceAttribute"/> class.
    /// </summary>
    /// <param name="key">The service key.</param>
    public KeyedServiceAttribute(object key)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedServiceAttribute"/> class.
    /// </summary>
    /// <param name="key">The service key.</param>
    /// <param name="serviceType">The service interface type.</param>
    public KeyedServiceAttribute(object key, Type serviceType)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
    }
}


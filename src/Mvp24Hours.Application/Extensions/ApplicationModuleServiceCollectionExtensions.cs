//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.Contract.Events;
using Mvp24Hours.Application.Logic.Cache;
using Mvp24Hours.Application.Logic.Validation;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Extensions;

/// <summary>
/// Unified extension methods for registering all Mvp24Hours.Application services.
/// Provides a single entry point for configuring the entire Application module.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a simplified API for registering all Application module services:
/// <list type="bullet">
/// <item>Application Services (CRUD, Query, Command services)</item>
/// <item>AutoMapper profiles</item>
/// <item>FluentValidation validators</item>
/// <item>Validation services and pipelines</item>
/// <item>Transaction scope support</item>
/// <item>Resilience services (exception mapping)</item>
/// <item>Observability services (correlation, metrics, audit)</item>
/// <item>Cache services</item>
/// <item>Application events and handlers</item>
/// <item>Pagination services</item>
/// <item>Specification services</item>
/// <item>Bulk operations services</item>
/// <item>Convention-based service registration</item>
/// </list>
/// </para>
/// </remarks>
public static class ApplicationModuleServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Mvp24Hours Application module services with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for services, validators, profiles, and handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers all Application module services with sensible defaults:
    /// <list type="bullet">
    /// <item>AutoMapper profiles from specified assemblies</item>
    /// <item>Application services (IApplicationService implementations)</item>
    /// <item>FluentValidation validators</item>
    /// <item>Validation services</item>
    /// <item>Transaction scope</item>
    /// <item>Resilience services</item>
    /// <item>Observability services</item>
    /// <item>Application events</item>
    /// <item>Pagination services</item>
    /// <item>Specification services</item>
    /// <item>Services by convention (IScopedService, etc.)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register all Application module services
    /// services.AddMvp24HoursApplicationModule(typeof(CustomerService).Assembly);
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationModule(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddMvp24HoursApplicationModule(options => { }, assemblies);
    }

    /// <summary>
    /// Adds all Mvp24Hours Application module services from the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="T">A type in the assembly to scan.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationModuleFromAssemblyContaining&lt;CustomerService&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationModuleFromAssemblyContaining<T>(
        this IServiceCollection services)
    {
        return services.AddMvp24HoursApplicationModule(typeof(T).Assembly);
    }

    /// <summary>
    /// Adds all Mvp24Hours Application module services from the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="T">A type in the assembly to scan.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure module options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationModuleFromAssemblyContaining&lt;CustomerService&gt;(options =>
    /// {
    ///     options.EnableCache = true;
    ///     options.EnableObservability = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationModuleFromAssemblyContaining<T>(
        this IServiceCollection services,
        Action<ApplicationModuleOptions> configure)
    {
        return services.AddMvp24HoursApplicationModule(configure, typeof(T).Assembly);
    }

    /// <summary>
    /// Adds all Mvp24Hours Application module services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure module options.</param>
    /// <param name="assemblies">The assemblies to scan for services, validators, profiles, and handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationModule(
    ///     options =>
    ///     {
    ///         // AutoMapper configuration
    ///         options.ConfigureAutoMapper = cfg => cfg.AllowNullDestinationValues = true;
    ///         
    ///         // Feature toggles
    ///         options.EnableCache = true;
    ///         options.EnableObservability = true;
    ///         options.EnableTransactions = true;
    ///         options.EnableResilience = true;
    ///         options.EnableEvents = true;
    ///         options.EnableValidation = true;
    ///         options.EnableConventionBasedRegistration = true;
    ///         
    ///         // Observability options
    ///         options.ObservabilityOptions.EnableMetrics = true;
    ///         options.ObservabilityOptions.EnableAuditTrail = true;
    ///         
    ///         // Resilience options
    ///         options.ResilienceOptions.IncludeExceptionDetails = true;
    ///     },
    ///     typeof(CustomerService).Assembly);
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationModule(
        this IServiceCollection services,
        Action<ApplicationModuleOptions> configure,
        params Assembly[] assemblies)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        if (assemblies == null || assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be specified.", nameof(assemblies));
        }

        var options = new ApplicationModuleOptions();
        configure(options);

        // 1. AutoMapper
        if (options.EnableAutoMapper)
        {
            services.AddMvp24HoursAutoMapper(options.ConfigureAutoMapper, assemblies);
        }

        // 2. Application Services
        if (options.EnableApplicationServices)
        {
            services.AddMvp24HoursApplicationServices(options.ApplicationServiceLifetime, assemblies);
        }

        // 3. Validators
        if (options.EnableValidation)
        {
            services.AddMvp24HoursValidators(options.ValidatorLifetime, assemblies);
            services.AddValidationServices(assemblies, options.ValidationServiceOptions);
        }

        // 4. Transaction Scope
        if (options.EnableTransactions)
        {
            services.AddTransactionScope(options.TransactionOptions);
        }

        // 5. Resilience
        if (options.EnableResilience)
        {
            services.AddMvpResilience(options.ResilienceOptions);
        }

        // 6. Observability
        if (options.EnableObservability)
        {
            services.AddMvp24HoursApplicationObservability(options.ObservabilityOptions);
        }

        // 7. Cache
        if (options.EnableCache)
        {
            services.AddMvpApplicationQueryCache(options.CacheOptions);
        }

        // 8. Events
        if (options.EnableEvents)
        {
            services.AddMvp24HoursApplicationEvents(options.EventOptions);
            services.AddMvp24HoursApplicationEventHandlers(assemblies);
        }

        // 9. Pagination
        if (options.EnablePagination)
        {
            services.AddMvp24HoursPagination(options.PaginationOptions);
        }

        // 10. Specifications
        if (options.EnableSpecifications)
        {
            services.AddMvp24HoursSpecificationPattern();
        }

        // 11. Bulk Operations (no additional registration needed - services use repositories)

        // 12. Convention-based registration
        if (options.EnableConventionBasedRegistration)
        {
            services.AddMvp24HoursServicesByConvention(assemblies);
        }

        return services;
    }

    /// <summary>
    /// Adds minimal Mvp24Hours Application services (AutoMapper and Application Services only).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this method for lightweight scenarios where you only need:
    /// <list type="bullet">
    /// <item>AutoMapper for DTO mapping</item>
    /// <item>Application Services (IApplicationService implementations)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationMinimal(typeof(CustomerService).Assembly);
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationMinimal(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddMvp24HoursApplicationModule(options =>
        {
            options.EnableAutoMapper = true;
            options.EnableApplicationServices = true;
            options.EnableValidation = false;
            options.EnableTransactions = false;
            options.EnableResilience = false;
            options.EnableObservability = false;
            options.EnableCache = false;
            options.EnableEvents = false;
            options.EnablePagination = false;
            options.EnableSpecifications = false;
            options.EnableBulkOperations = false;
            options.EnableConventionBasedRegistration = false;
        }, assemblies);
    }

    /// <summary>
    /// Adds Mvp24Hours Application services optimized for API scenarios.
    /// Includes validation, resilience, and observability.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationForApi(typeof(CustomerService).Assembly);
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationForApi(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddMvp24HoursApplicationModule(options =>
        {
            options.EnableAutoMapper = true;
            options.EnableApplicationServices = true;
            options.EnableValidation = true;
            options.EnableTransactions = true;
            options.EnableResilience = true;
            options.EnableObservability = true;
            options.EnableCache = false;
            options.EnableEvents = false;
            options.EnablePagination = true;
            options.EnableSpecifications = true;
            options.EnableBulkOperations = false;
            options.EnableConventionBasedRegistration = true;
        }, assemblies);
    }

    /// <summary>
    /// Adds Mvp24Hours Application services with all features enabled.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursApplicationFull(typeof(CustomerService).Assembly);
    /// </code>
    /// </example>
    public static IServiceCollection AddMvp24HoursApplicationFull(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddMvp24HoursApplicationModule(options =>
        {
            options.EnableAutoMapper = true;
            options.EnableApplicationServices = true;
            options.EnableValidation = true;
            options.EnableTransactions = true;
            options.EnableResilience = true;
            options.EnableObservability = true;
            options.EnableCache = true;
            options.EnableEvents = true;
            options.EnablePagination = true;
            options.EnableSpecifications = true;
            options.EnableBulkOperations = true;
            options.EnableConventionBasedRegistration = true;
        }, assemblies);
    }
}

/// <summary>
/// Configuration options for the Application module.
/// </summary>
public sealed class ApplicationModuleOptions
{
    #region [ Feature Toggles ]

    /// <summary>
    /// Gets or sets whether to enable AutoMapper registration.
    /// Default is true.
    /// </summary>
    public bool EnableAutoMapper { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Application Service registration.
    /// Default is true.
    /// </summary>
    public bool EnableApplicationServices { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable FluentValidation and validation services.
    /// Default is true.
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Transaction Scope support.
    /// Default is true.
    /// </summary>
    public bool EnableTransactions { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Resilience services (exception mapping).
    /// Default is true.
    /// </summary>
    public bool EnableResilience { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Observability services (correlation, metrics, audit).
    /// Default is true.
    /// </summary>
    public bool EnableObservability { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Query Cache services.
    /// Default is false. Requires IDistributedCache to be registered.
    /// </summary>
    public bool EnableCache { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable Application Events and handlers.
    /// Default is false.
    /// </summary>
    public bool EnableEvents { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable Pagination services.
    /// Default is true.
    /// </summary>
    public bool EnablePagination { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Specification Pattern services.
    /// Default is true.
    /// </summary>
    public bool EnableSpecifications { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Bulk Operations services.
    /// Default is false.
    /// </summary>
    public bool EnableBulkOperations { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable convention-based service registration.
    /// Services implementing IScopedService, ISingletonService, ITransientService
    /// will be automatically registered.
    /// Default is true.
    /// </summary>
    public bool EnableConventionBasedRegistration { get; set; } = true;

    #endregion

    #region [ Lifetime Configuration ]

    /// <summary>
    /// Gets or sets the lifetime for Application Services.
    /// Default is Scoped.
    /// </summary>
    public ServiceLifetime ApplicationServiceLifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Gets or sets the lifetime for Validators.
    /// Default is Scoped.
    /// </summary>
    public ServiceLifetime ValidatorLifetime { get; set; } = ServiceLifetime.Scoped;

    #endregion

    #region [ AutoMapper Configuration ]

    /// <summary>
    /// Gets or sets the AutoMapper configuration action.
    /// Default is null (no additional configuration).
    /// </summary>
    public Action<IMapperConfigurationExpression>? ConfigureAutoMapper { get; set; }

    #endregion

    #region [ Sub-Options ]

    /// <summary>
    /// Gets or sets the validation service options.
    /// </summary>
    public Action<ValidationServiceOptions>? ValidationServiceOptions { get; set; }

    /// <summary>
    /// Gets or sets the transaction scope options.
    /// </summary>
    public Action<TransactionScopeOptions> TransactionOptions { get; set; } = _ => { };

    /// <summary>
    /// Gets or sets the resilience (exception mapping) options.
    /// </summary>
    public Action<Contract.Resilience.ExceptionMappingOptions> ResilienceOptions { get; set; } = _ => { };

    /// <summary>
    /// Gets or sets the observability options.
    /// </summary>
    public Action<ApplicationObservabilityOptions> ObservabilityOptions { get; set; } = _ => { };

    /// <summary>
    /// Gets or sets the cache options.
    /// </summary>
    public Action<QueryCacheOptions> CacheOptions { get; set; } = _ => { };

    /// <summary>
    /// Gets or sets the application event dispatcher options.
    /// </summary>
    public Action<ApplicationEventDispatcherOptions> EventOptions { get; set; } = _ => { };

    /// <summary>
    /// Gets or sets the pagination options.
    /// </summary>
    public Action<PaginationOptions> PaginationOptions { get; set; } = _ => { };

    #endregion
}


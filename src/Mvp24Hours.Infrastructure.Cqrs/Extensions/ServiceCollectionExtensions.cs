//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Behaviors;
using Mvp24Hours.Infrastructure.Cqrs.Implementations;
using MediatorImpl = Mvp24Hours.Infrastructure.Cqrs.Implementations.Mediator;

namespace Mvp24Hours.Infrastructure.Cqrs.Extensions;

/// <summary>
/// Extension methods for configuring the Mediator in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Mvp24Hours Mediator and registers handlers automatically.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure the mediator.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Basic registration with assembly scanning
    /// services.AddMvpMediator(options =>
    /// {
    ///     options.RegisterHandlersFromAssemblyContaining&lt;CreateOrderCommand&gt;();
    ///     options.WithDefaultBehaviors(); // Adds logging, performance, exception behaviors
    /// });
    /// 
    /// // Simple registration from assembly
    /// services.AddMvpMediator(typeof(Program).Assembly);
    /// 
    /// // With Pipeline compatibility
    /// services.AddMvpMediator(options =>
    /// {
    ///     options.RegisterHandlersFromAssemblyContaining&lt;Program&gt;();
    ///     options.WithPipelineCompatibility(); // Break on fail + Transaction
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMvpMediator(
        this IServiceCollection services,
        Action<MediatorOptions>? configure = null)
    {
        var options = new MediatorOptions();
        configure?.Invoke(options);

        // Register MediatorOptions as a singleton for injection into behaviors
        services.TryAddSingleton(Options.Create(options));
        services.TryAddSingleton(options);

        // Register the Mediator
        services.AddScoped<IMediator, MediatorImpl>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());
        services.AddScoped<IStreamSender>(sp => sp.GetRequiredService<IMediator>());
        
        // Register Domain Event Dispatcher
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Register handlers from specified assemblies
        foreach (var assembly in options.AssembliesToScan)
        {
            services.RegisterHandlersFromAssembly(assembly);
        }

        // Register behaviors in the recommended order (outer to inner):
        // 1. UnhandledException - Catches all errors first
        // 2. Logging - Logs all operations
        // 3. Performance - Monitors timing
        // 4. Authorization - Security check before processing
        // 5. Validation - Validate request data
        // 6. Idempotency - Check for duplicate requests
        // 7. Caching - Return cached response if available
        // 8. Retry - Retry on transient failures
        // 9. Transaction - Wrap in transaction
        // [Handler executes here]
        
        if (options.RegisterUnhandledExceptionBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        }

        if (options.RegisterLoggingBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        }

        if (options.RegisterPerformanceBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        }

        if (options.RegisterAuthorizationBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        }

        if (options.RegisterValidationBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        }

        if (options.RegisterIdempotencyBehavior)
        {
            services.AddSingleton<IIdempotencyKeyGenerator, DefaultIdempotencyKeyGenerator>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
        }

        if (options.RegisterCachingBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        }

        if (options.RegisterRetryBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RetryBehavior<,>));
        }

        if (options.RegisterTransactionBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        }

        return services;
    }

    /// <summary>
    /// Adds the Mediator and registers handlers from a specific assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvpMediator(this IServiceCollection services, Assembly assembly)
    {
        return services.AddMvpMediator(options => options.RegisterHandlersFromAssembly(assembly));
    }

    /// <summary>
    /// Adds the Mediator and registers handlers from multiple assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvpMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        return services.AddMvpMediator(options =>
        {
            foreach (var assembly in assemblies)
            {
                options.RegisterHandlersFromAssembly(assembly);
            }
        });
    }

    /// <summary>
    /// Registers all handlers found in an assembly.
    /// </summary>
    private static void RegisterHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .ToList();

        foreach (var type in handlerTypes)
        {
            // Register IMediatorRequestHandler<,>
            var requestHandlerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMediatorRequestHandler<,>));

            foreach (var @interface in requestHandlerInterfaces)
            {
                services.AddTransient(@interface, type);
            }

            // Register IMediatorNotificationHandler<>
            var notificationHandlerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMediatorNotificationHandler<>));

            foreach (var @interface in notificationHandlerInterfaces)
            {
                services.AddTransient(@interface, type);
            }

            // Register IStreamRequestHandler<,>
            var streamHandlerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequestHandler<,>));

            foreach (var @interface in streamHandlerInterfaces)
            {
                services.AddTransient(@interface, type);
            }

            // Also register semantic interfaces (they inherit from the base interfaces)
            // IMediatorCommandHandler<,> inherits from IMediatorRequestHandler<,>
            // IMediatorQueryHandler<,> inherits from IMediatorRequestHandler<,>
            // So the base registration above already covers them
        }
    }
}

/// <summary>
/// Configuration options for the Mvp24Hours Mediator.
/// </summary>
/// <remarks>
/// <para>
/// This class provides configuration options for the Mediator pattern implementation,
/// including behavior registration, assembly scanning, and pipeline settings.
/// </para>
/// <para>
/// <strong>Integration with PipelineOptions:</strong>
/// The <see cref="IsBreakOnFail"/> and <see cref="ForceRollbackOnFailure"/> properties
/// provide compatibility with the traditional Pipeline pattern from Mvp24Hours.Infrastructure.Pipe.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddMvpMediator(options =>
/// {
///     options.RegisterHandlersFromAssemblyContaining&lt;CreateOrderCommand&gt;();
///     options.WithDefaultBehaviors();
///     options.IsBreakOnFail = true; // Stop pipeline on first failure
///     options.PerformanceThresholdMilliseconds = 1000; // Warn if request takes > 1s
/// });
/// </code>
/// </example>
[Serializable]
public sealed class MediatorOptions
{
    internal List<Assembly> AssembliesToScan { get; } = new();

    #region [ Behavior Registration ]

    /// <summary>
    /// Gets or sets whether to register the LoggingBehavior automatically.
    /// Default is false.
    /// </summary>
    public bool RegisterLoggingBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the PerformanceBehavior automatically.
    /// Default is false.
    /// </summary>
    public bool RegisterPerformanceBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the UnhandledExceptionBehavior automatically.
    /// Default is false.
    /// </summary>
    public bool RegisterUnhandledExceptionBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the ValidationBehavior automatically.
    /// Requires FluentValidation validators to be registered.
    /// Default is false.
    /// </summary>
    public bool RegisterValidationBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the CachingBehavior automatically.
    /// Requires IDistributedCache to be registered.
    /// Default is false.
    /// </summary>
    public bool RegisterCachingBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the TransactionBehavior automatically.
    /// Requires IUnitOfWorkAsync to be registered.
    /// Default is false.
    /// </summary>
    public bool RegisterTransactionBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the AuthorizationBehavior automatically.
    /// Requires IUserContext to be registered.
    /// Default is false.
    /// </summary>
    public bool RegisterAuthorizationBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the RetryBehavior automatically.
    /// Default is false.
    /// </summary>
    public bool RegisterRetryBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the IdempotencyBehavior automatically.
    /// Requires IDistributedCache to be registered.
    /// Default is false.
    /// </summary>
    public bool RegisterIdempotencyBehavior { get; set; }

    #endregion

    #region [ Pipeline Configuration (Compatible with PipelineOptions) ]

    /// <summary>
    /// Gets or sets whether to stop the pipeline on first failure.
    /// Compatible with PipelineOptions.IsBreakOnFail from Mvp24Hours.Infrastructure.Pipe.
    /// Default is false.
    /// </summary>
    /// <remarks>
    /// When enabled, if any behavior or handler throws an exception,
    /// subsequent behaviors in the pipeline will not be executed.
    /// </remarks>
    public bool IsBreakOnFail { get; set; }

    /// <summary>
    /// Gets or sets whether to force rollback on failure.
    /// Compatible with PipelineOptions.ForceRollbackOnFalure from Mvp24Hours.Infrastructure.Pipe.
    /// Default is false.
    /// </summary>
    /// <remarks>
    /// When enabled, if any operation fails, the TransactionBehavior will force a rollback
    /// even if the exception is caught and handled by an outer behavior.
    /// </remarks>
    public bool ForceRollbackOnFailure { get; set; }

    #endregion

    #region [ Performance Configuration ]

    /// <summary>
    /// Gets or sets the threshold in milliseconds for the PerformanceBehavior.
    /// Requests taking longer than this will be logged as warnings.
    /// Default is 500ms.
    /// </summary>
    public int PerformanceThresholdMilliseconds { get; set; } = 500;

    #endregion

    #region [ Retry Configuration ]

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for the RetryBehavior.
    /// Default is 3.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay in milliseconds between retry attempts.
    /// Uses exponential backoff: delay = baseDelay * 2^attemptNumber
    /// Default is 100ms.
    /// </summary>
    public int RetryBaseDelayMilliseconds { get; set; } = 100;

    #endregion

    #region [ Idempotency Configuration ]

    /// <summary>
    /// Gets or sets the default duration in hours for idempotency cache entries.
    /// Default is 24 hours.
    /// </summary>
    public int IdempotencyDurationHours { get; set; } = 24;

    #endregion

    #region [ Notification Configuration ]

    /// <summary>
    /// Gets or sets the default notification publishing strategy.
    /// Default is Sequential.
    /// </summary>
    public Abstractions.NotificationPublishingStrategy DefaultNotificationStrategy { get; set; } 
        = Abstractions.NotificationPublishingStrategy.Sequential;

    #endregion

    #region [ Assembly Registration Methods ]

    /// <summary>
    /// Adds an assembly to scan for handlers automatically.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The options for chaining.</returns>
    public MediatorOptions RegisterHandlersFromAssembly(Assembly assembly)
    {
        AssembliesToScan.Add(assembly);
        return this;
    }

    /// <summary>
    /// Adds the assembly containing the specified type to scan for handlers.
    /// </summary>
    /// <typeparam name="T">A type contained in the assembly to scan.</typeparam>
    /// <returns>The options for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMvpMediator(options =>
    /// {
    ///     options.RegisterHandlersFromAssemblyContaining&lt;CreateOrderCommand&gt;();
    /// });
    /// </code>
    /// </example>
    public MediatorOptions RegisterHandlersFromAssemblyContaining<T>()
    {
        return RegisterHandlersFromAssembly(typeof(T).Assembly);
    }

    #endregion

    #region [ Preset Methods ]

    /// <summary>
    /// Enables all default behaviors (Logging, Performance, UnhandledException).
    /// </summary>
    /// <returns>The options for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The behaviors are registered in the following order (execution order):
    /// <list type="number">
    /// <item>UnhandledExceptionBehavior - Catches and logs all unhandled exceptions</item>
    /// <item>LoggingBehavior - Logs request start/end and timing</item>
    /// <item>PerformanceBehavior - Alerts about slow requests</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvpMediator(options =>
    /// {
    ///     options.RegisterHandlersFromAssemblyContaining&lt;Program&gt;();
    ///     options.WithDefaultBehaviors();
    /// });
    /// </code>
    /// </example>
    public MediatorOptions WithDefaultBehaviors()
    {
        RegisterLoggingBehavior = true;
        RegisterPerformanceBehavior = true;
        RegisterUnhandledExceptionBehavior = true;
        return this;
    }

    /// <summary>
    /// Enables all behaviors including advanced ones (Validation, Caching, Transaction, Authorization, Retry, Idempotency).
    /// </summary>
    /// <returns>The options for chaining.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Warning:</strong> Advanced behaviors have dependencies:
    /// <list type="bullet">
    /// <item>ValidationBehavior - Requires FluentValidation validators</item>
    /// <item>CachingBehavior - Requires IDistributedCache</item>
    /// <item>TransactionBehavior - Requires IUnitOfWorkAsync</item>
    /// <item>AuthorizationBehavior - Requires IUserContext</item>
    /// <item>IdempotencyBehavior - Requires IDistributedCache</item>
    /// </list>
    /// </para>
    /// </remarks>
    public MediatorOptions WithAllBehaviors()
    {
        WithDefaultBehaviors();
        RegisterValidationBehavior = true;
        RegisterCachingBehavior = true;
        RegisterTransactionBehavior = true;
        RegisterAuthorizationBehavior = true;
        RegisterRetryBehavior = true;
        RegisterIdempotencyBehavior = true;
        return this;
    }

    /// <summary>
    /// Enables validation and authorization behaviors for secure operations.
    /// </summary>
    /// <returns>The options for chaining.</returns>
    public MediatorOptions WithSecurityBehaviors()
    {
        RegisterValidationBehavior = true;
        RegisterAuthorizationBehavior = true;
        return this;
    }

    /// <summary>
    /// Enables resiliency behaviors (Retry, Idempotency).
    /// </summary>
    /// <returns>The options for chaining.</returns>
    public MediatorOptions WithResiliencyBehaviors()
    {
        RegisterRetryBehavior = true;
        RegisterIdempotencyBehavior = true;
        return this;
    }

    /// <summary>
    /// Configures the mediator to behave like the traditional Pipeline with break-on-fail enabled.
    /// </summary>
    /// <returns>The options for chaining.</returns>
    /// <remarks>
    /// This method sets <see cref="IsBreakOnFail"/> to true and enables transaction behavior
    /// with <see cref="ForceRollbackOnFailure"/> for consistency with PipelineOptions behavior.
    /// </remarks>
    public MediatorOptions WithPipelineCompatibility()
    {
        IsBreakOnFail = true;
        ForceRollbackOnFailure = true;
        RegisterTransactionBehavior = true;
        return this;
    }

    #endregion
}


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
using Mvp24Hours.Infrastructure.Cqrs.MultiTenancy;
using Mvp24Hours.Infrastructure.Cqrs.Observability;
using MediatorImpl = Mvp24Hours.Infrastructure.Cqrs.Implementations.Mediator;

// Extensibility types for decorator registration
using IExceptionHandlerBase = System.Type;

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
        // 2. RequestContext - Establishes tracing context
        // 3. Tracing - OpenTelemetry integration
        // 4. Telemetry - Mvp24Hours telemetry integration
        // 5. Logging - Logs all operations
        // 6. Performance - Monitors timing
        // 7. Audit - Creates audit trail entries
        // 8. Authorization - Security check before processing
        // 9. Validation - Validate request data
        // 10. Idempotency - Check for duplicate requests
        // 11. Caching - Return cached response if available
        // 12. Retry - Retry on transient failures
        // 13. Transaction - Wrap in transaction
        // [Handler executes here]
        
        if (options.RegisterUnhandledExceptionBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        }

        if (options.RegisterRequestContextBehavior)
        {
            services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
            services.TryAddSingleton<IRequestContextFactory, RequestContextFactory>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContextBehavior<,>));
        }

        if (options.RegisterTracingBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TracingBehavior<,>));
        }

        if (options.RegisterTelemetryBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TelemetryBehavior<,>));
        }

        if (options.RegisterLoggingBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        }

        if (options.RegisterPerformanceBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        }

        if (options.RegisterAuditBehavior)
        {
            services.TryAddScoped<IAuditStore, InMemoryAuditStore>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
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

        // Multi-tenancy behaviors
        if (options.RegisterTenantBehavior)
        {
            services.TryAddScoped<ITenantContextAccessor, TenantContextAccessor>();
            services.TryAddScoped<ITenantFilter, TenantFilter>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantBehavior<,>));
        }

        if (options.RegisterCurrentUserBehavior)
        {
            services.TryAddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CurrentUserBehavior<,>));
        }

        // Resilience behaviors
        if (options.RegisterTimeoutBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TimeoutBehavior<,>));
        }

        if (options.RegisterCircuitBreakerBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CircuitBreakerBehavior<,>));
        }

        // Extensibility behaviors
        if (options.RegisterPipelineHookBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineHookBehavior<,>));
        }

        if (options.RegisterPrePostProcessorBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PrePostProcessorBehavior<,>));
        }

        if (options.RegisterExceptionHandlerBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlerBehavior<,>));
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

    /// <summary>
    /// Gets or sets whether to register the RequestContextBehavior automatically.
    /// This behavior establishes CorrelationId, CausationId, and RequestId for tracing.
    /// Default is false.
    /// </summary>
    public bool RegisterRequestContextBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the TracingBehavior automatically.
    /// This behavior creates OpenTelemetry-compatible traces using the Activity API.
    /// Default is false.
    /// </summary>
    public bool RegisterTracingBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the TelemetryBehavior automatically.
    /// This behavior integrates with ILogger for structured logging and OpenTelemetry for telemetry.
    /// Default is false.
    /// </summary>
    public bool RegisterTelemetryBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the AuditBehavior automatically.
    /// This behavior creates audit trail entries for requests implementing IAuditable.
    /// Requires IAuditStore to be registered (InMemoryAuditStore is registered by default).
    /// Default is false.
    /// </summary>
    public bool RegisterAuditBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to audit all commands regardless of IAuditable.
    /// Only applies when AuditBehavior is registered.
    /// Default is false.
    /// </summary>
    public bool AuditAllCommands { get; set; }

    /// <summary>
    /// Gets or sets whether to register the TenantBehavior automatically.
    /// This behavior resolves and injects tenant context for multi-tenant applications.
    /// Requires ITenantResolver to be registered for tenant resolution.
    /// Default is false.
    /// </summary>
    public bool RegisterTenantBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the CurrentUserBehavior automatically.
    /// This behavior resolves and injects the current user context.
    /// Requires ICurrentUserFactory to be registered.
    /// Default is false.
    /// </summary>
    public bool RegisterCurrentUserBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the TimeoutBehavior automatically.
    /// This behavior enforces a timeout on request execution.
    /// Default is false.
    /// </summary>
    public bool RegisterTimeoutBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the CircuitBreakerBehavior automatically.
    /// This behavior provides circuit breaker protection for requests.
    /// Default is false.
    /// </summary>
    public bool RegisterCircuitBreakerBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the PrePostProcessorBehavior automatically.
    /// This behavior executes pre-processors before and post-processors after handlers.
    /// Default is false.
    /// </summary>
    public bool RegisterPrePostProcessorBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the ExceptionHandlerBehavior automatically.
    /// This behavior enables fine-grained exception handling via IExceptionHandler.
    /// Default is false.
    /// </summary>
    public bool RegisterExceptionHandlerBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether to register the PipelineHookBehavior automatically.
    /// This behavior executes pipeline lifecycle hooks.
    /// Default is false.
    /// </summary>
    public bool RegisterPipelineHookBehavior { get; set; }

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

    /// <summary>
    /// Gets or sets the default timeout in milliseconds for the TimeoutBehavior.
    /// Set to 0 to disable default timeout.
    /// Requests implementing IHasTimeout can override this value.
    /// Default is 0 (no timeout).
    /// </summary>
    public int DefaultTimeoutMilliseconds { get; set; }

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
    /// Enables all behaviors including advanced ones (Validation, Caching, Transaction, Authorization, Retry, Idempotency, Observability).
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
    /// <item>AuditBehavior - Requires IAuditStore (InMemoryAuditStore is registered by default)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public MediatorOptions WithAllBehaviors()
    {
        WithDefaultBehaviors();
        WithObservabilityBehaviors();
        RegisterValidationBehavior = true;
        RegisterCachingBehavior = true;
        RegisterTransactionBehavior = true;
        RegisterAuthorizationBehavior = true;
        RegisterRetryBehavior = true;
        RegisterIdempotencyBehavior = true;
        RegisterAuditBehavior = true;
        return this;
    }

    /// <summary>
    /// Enables observability behaviors (RequestContext, Tracing, Telemetry).
    /// </summary>
    /// <returns>The options for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This enables:
    /// <list type="bullet">
    /// <item>RequestContextBehavior - CorrelationId, CausationId, RequestId propagation</item>
    /// <item>TracingBehavior - OpenTelemetry Activity API integration</item>
    /// <item>TelemetryBehavior - ILogger and OpenTelemetry integration</item>
    /// </list>
    /// </para>
    /// </remarks>
    public MediatorOptions WithObservabilityBehaviors()
    {
        RegisterRequestContextBehavior = true;
        RegisterTracingBehavior = true;
        RegisterTelemetryBehavior = true;
        return this;
    }

    /// <summary>
    /// Enables audit trail behavior for tracking operations.
    /// </summary>
    /// <param name="auditAllCommands">Whether to audit all commands regardless of IAuditable.</param>
    /// <returns>The options for chaining.</returns>
    public MediatorOptions WithAuditBehavior(bool auditAllCommands = false)
    {
        RegisterAuditBehavior = true;
        AuditAllCommands = auditAllCommands;
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
    /// Enables advanced resiliency behaviors (Timeout, Circuit Breaker, Retry, Idempotency).
    /// </summary>
    /// <param name="defaultTimeoutMs">Default timeout in milliseconds (0 = no timeout).</param>
    /// <returns>The options for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This enables:
    /// <list type="bullet">
    /// <item>TimeoutBehavior - Request timeout protection</item>
    /// <item>CircuitBreakerBehavior - Circuit breaker for cascading failures</item>
    /// <item>RetryBehavior - Retry with exponential backoff</item>
    /// <item>IdempotencyBehavior - Duplicate request prevention</item>
    /// </list>
    /// </para>
    /// </remarks>
    public MediatorOptions WithAdvancedResiliency(int defaultTimeoutMs = 30000)
    {
        RegisterTimeoutBehavior = true;
        RegisterCircuitBreakerBehavior = true;
        RegisterRetryBehavior = true;
        RegisterIdempotencyBehavior = true;
        DefaultTimeoutMilliseconds = defaultTimeoutMs;
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

    /// <summary>
    /// Enables multi-tenancy behaviors (Tenant context resolution, CurrentUser).
    /// </summary>
    /// <returns>The options for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This enables:
    /// <list type="bullet">
    /// <item>TenantBehavior - Tenant resolution and context injection</item>
    /// <item>CurrentUserBehavior - Current user context injection</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Required registrations:</strong>
    /// <list type="bullet">
    /// <item>ITenantResolver - Custom tenant resolution strategy</item>
    /// <item>ICurrentUserFactory - Custom user context factory</item>
    /// <item>ITenantStore (optional) - For tenant lookup by ID</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvpMediator(options =>
    /// {
    ///     options.RegisterHandlersFromAssemblyContaining&lt;Program&gt;();
    ///     options.WithMultiTenancy();
    /// });
    /// 
    /// // Register your custom resolvers
    /// services.AddScoped&lt;ITenantResolver, HeaderTenantResolver&gt;();
    /// services.AddScoped&lt;ICurrentUserFactory, JwtCurrentUserFactory&gt;();
    /// </code>
    /// </example>
    public MediatorOptions WithMultiTenancy()
    {
        RegisterTenantBehavior = true;
        RegisterCurrentUserBehavior = true;
        return this;
    }

    /// <summary>
    /// Enables extensibility behaviors (pre-processors, post-processors, exception handlers, hooks).
    /// </summary>
    /// <returns>The options for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This enables:
    /// <list type="bullet">
    /// <item>PrePostProcessorBehavior - Pre/post processing hooks</item>
    /// <item>ExceptionHandlerBehavior - Fine-grained exception handling</item>
    /// <item>PipelineHookBehavior - Lifecycle hooks</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvpMediator(options =>
    /// {
    ///     options.RegisterHandlersFromAssemblyContaining&lt;Program&gt;();
    ///     options.WithExtensibility();
    /// });
    /// 
    /// // Register custom processors
    /// services.AddTransient&lt;IPreProcessor&lt;CreateOrderCommand&gt;, EnrichOrderPreProcessor&gt;();
    /// services.AddTransient&lt;IPostProcessor&lt;CreateOrderCommand, Order&gt;, NotifyOrderPostProcessor&gt;();
    /// services.AddTransient&lt;IPipelineHook, MetricsPipelineHook&gt;();
    /// </code>
    /// </example>
    public MediatorOptions WithExtensibility()
    {
        RegisterPrePostProcessorBehavior = true;
        RegisterExceptionHandlerBehavior = true;
        RegisterPipelineHookBehavior = true;
        return this;
    }

    /// <summary>
    /// Enables pre-processor and post-processor support.
    /// </summary>
    /// <returns>The options for chaining.</returns>
    public MediatorOptions WithPrePostProcessors()
    {
        RegisterPrePostProcessorBehavior = true;
        return this;
    }

    /// <summary>
    /// Enables fine-grained exception handler support.
    /// </summary>
    /// <returns>The options for chaining.</returns>
    public MediatorOptions WithExceptionHandlers()
    {
        RegisterExceptionHandlerBehavior = true;
        return this;
    }

    /// <summary>
    /// Enables pipeline lifecycle hooks.
    /// </summary>
    /// <returns>The options for chaining.</returns>
    public MediatorOptions WithPipelineHooks()
    {
        RegisterPipelineHookBehavior = true;
        return this;
    }

    #endregion
}

/// <summary>
/// Extension methods for registering extensibility components.
/// </summary>
public static class MediatorExtensibilityExtensions
{
    /// <summary>
    /// Registers a mediator decorator that wraps all mediator operations.
    /// </summary>
    /// <typeparam name="TDecorator">The decorator type implementing <see cref="IMediatorDecorator"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Decorators wrap the entire mediator, allowing interception of all operations.
    /// Multiple decorators are applied in the order they are registered (last registered wraps first).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvpMediator(typeof(Program).Assembly);
    /// services.AddMediatorDecorator&lt;MetricsMediatorDecorator&gt;();
    /// services.AddMediatorDecorator&lt;LoggingMediatorDecorator&gt;();
    /// // LoggingMediatorDecorator wraps MetricsMediatorDecorator wraps Mediator
    /// </code>
    /// </example>
    public static IServiceCollection AddMediatorDecorator<TDecorator>(this IServiceCollection services)
        where TDecorator : class, IMediatorDecorator
    {
        services.Decorate<IMediator, TDecorator>();
        return services;
    }

    /// <summary>
    /// Registers a global pre-processor that runs for all requests.
    /// </summary>
    /// <typeparam name="TProcessor">The pre-processor type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGlobalPreProcessor<TProcessor>(this IServiceCollection services)
        where TProcessor : class, IPreProcessorGlobal
    {
        services.AddTransient<IPreProcessorGlobal, TProcessor>();
        return services;
    }

    /// <summary>
    /// Registers a pre-processor for a specific request type.
    /// </summary>
    /// <typeparam name="TRequest">The request type to process.</typeparam>
    /// <typeparam name="TProcessor">The pre-processor type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPreProcessor<TRequest, TProcessor>(this IServiceCollection services)
        where TProcessor : class, IPreProcessor<TRequest>
    {
        services.AddTransient<IPreProcessor<TRequest>, TProcessor>();
        return services;
    }

    /// <summary>
    /// Registers a global post-processor that runs for all requests.
    /// </summary>
    /// <typeparam name="TProcessor">The post-processor type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGlobalPostProcessor<TProcessor>(this IServiceCollection services)
        where TProcessor : class, IPostProcessorGlobal
    {
        services.AddTransient<IPostProcessorGlobal, TProcessor>();
        return services;
    }

    /// <summary>
    /// Registers a post-processor for a specific request/response type.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TProcessor">The post-processor type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostProcessor<TRequest, TResponse, TProcessor>(this IServiceCollection services)
        where TProcessor : class, IPostProcessor<TRequest, TResponse>
    {
        services.AddTransient<IPostProcessor<TRequest, TResponse>, TProcessor>();
        return services;
    }

    /// <summary>
    /// Registers an exception handler for a specific request, response, and exception type.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TException">The exception type to handle.</typeparam>
    /// <typeparam name="THandler">The exception handler type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExceptionHandler<TRequest, TResponse, TException, THandler>(this IServiceCollection services)
        where TException : Exception
        where THandler : class, IExceptionHandler<TRequest, TResponse, TException>
    {
        services.AddTransient<IExceptionHandler<TRequest, TResponse, TException>, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a global exception handler for a specific exception type.
    /// </summary>
    /// <typeparam name="TException">The exception type to handle.</typeparam>
    /// <typeparam name="THandler">The exception handler type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGlobalExceptionHandler<TException, THandler>(this IServiceCollection services)
        where TException : Exception
        where THandler : class, IExceptionHandlerGlobal<TException>
    {
        services.AddTransient<IExceptionHandlerGlobal<TException>, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a global pipeline hook.
    /// </summary>
    /// <typeparam name="THook">The hook type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPipelineHook<THook>(this IServiceCollection services)
        where THook : class, IPipelineHook
    {
        services.AddTransient<IPipelineHook, THook>();
        return services;
    }

    /// <summary>
    /// Registers a typed pipeline hook for a specific request type.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="THook">The hook type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPipelineHook<TRequest, THook>(this IServiceCollection services)
        where THook : class, IPipelineHook<TRequest>
    {
        services.AddTransient<IPipelineHook<TRequest>, THook>();
        return services;
    }

    /// <summary>
    /// Helper method to decorate an existing service registration.
    /// </summary>
    private static void Decorate<TInterface, TDecorator>(this IServiceCollection services)
        where TInterface : class
        where TDecorator : class, TInterface
    {
        var wrappedDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TInterface));
        if (wrappedDescriptor == null)
        {
            throw new InvalidOperationException($"Service {typeof(TInterface).Name} is not registered. Register it before decorating.");
        }

        var objectFactory = ActivatorUtilities.CreateFactory(
            typeof(TDecorator),
            new[] { typeof(TInterface) });

        services.Replace(ServiceDescriptor.Describe(
            typeof(TInterface),
            sp =>
            {
                var inner = wrappedDescriptor.ImplementationInstance
                    ?? (wrappedDescriptor.ImplementationFactory?.Invoke(sp)
                        ?? ActivatorUtilities.GetServiceOrCreateInstance(sp, wrappedDescriptor.ImplementationType!));
                return (TInterface)objectFactory(sp, new[] { inner });
            },
            wrappedDescriptor.Lifetime));
    }
}


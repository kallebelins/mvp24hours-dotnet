//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.MultiTenancy;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Marker interface for requests that require a tenant context.
/// </summary>
/// <remarks>
/// <para>
/// Requests implementing this interface will have tenant resolution and
/// validation performed by <see cref="TenantBehavior{TRequest, TResponse}"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CreateProductCommand : IMediatorCommand&lt;int&gt;, ITenantRequired
/// {
///     public string Name { get; init; }
///     public decimal Price { get; init; }
///     
///     // Tenant will be automatically resolved and validated before execution
/// }
/// </code>
/// </example>
public interface ITenantRequired
{
    /// <summary>
    /// Gets whether to allow execution without a tenant.
    /// Defaults to false (tenant is required).
    /// </summary>
    bool AllowNoTenant => false;
}

/// <summary>
/// Marker interface for requests that can optionally specify a tenant.
/// </summary>
/// <remarks>
/// Use this for cross-tenant operations or admin-level queries.
/// </remarks>
/// <example>
/// <code>
/// public class GetAllProductsQuery : IMediatorQuery&lt;List&lt;Product&gt;&gt;, ITenantAware
/// {
///     public string? OverrideTenantId { get; init; }
/// }
/// </code>
/// </example>
public interface ITenantAware
{
    /// <summary>
    /// Gets an optional tenant ID to override the current tenant context.
    /// </summary>
    string? OverrideTenantId => null;
}

/// <summary>
/// Pipeline behavior that resolves and injects tenant context into requests.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior:
/// <list type="bullet">
/// <item>Resolves the current tenant using <see cref="ITenantResolver"/></item>
/// <item>Sets the tenant context in <see cref="ITenantContextAccessor"/></item>
/// <item>Validates tenant requirements for <see cref="ITenantRequired"/> requests</item>
/// <item>Handles tenant override for <see cref="ITenantAware"/> requests</item>
/// </list>
/// </para>
/// <para>
/// <strong>Execution Order:</strong>
/// <code>
/// ┌──────────────────────────────────────────────────────────────────────────────┐
/// │ 1. Check if request implements ITenantAware with override                    │
/// │ 2. If override: Resolve tenant from store by override ID                     │
/// │ 3. Else: Resolve tenant using ITenantResolver                                │
/// │ 4. Set tenant context in ITenantContextAccessor                              │
/// │ 5. If request implements ITenantRequired and no tenant: throw exception      │
/// │ 6. Execute next behavior/handler                                             │
/// │ 7. Clear tenant context on completion                                        │
/// └──────────────────────────────────────────────────────────────────────────────┘
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(TenantBehavior&lt;,&gt;));
/// services.AddScoped&lt;ITenantResolver, HeaderTenantResolver&gt;();
/// services.AddScoped&lt;ITenantContextAccessor, TenantContextAccessor&gt;();
/// </code>
/// </example>
public sealed class TenantBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ITenantResolver? _tenantResolver;
    private readonly ITenantStore? _tenantStore;
    private readonly ILogger<TenantBehavior<TRequest, TResponse>>? _logger;

    /// <summary>
    /// Creates a new instance of the TenantBehavior.
    /// </summary>
    /// <param name="tenantContextAccessor">The tenant context accessor.</param>
    /// <param name="tenantResolver">Optional tenant resolver.</param>
    /// <param name="tenantStore">Optional tenant store for override lookups.</param>
    /// <param name="logger">Optional logger.</param>
    public TenantBehavior(
        ITenantContextAccessor tenantContextAccessor,
        ITenantResolver? tenantResolver = null,
        ITenantStore? tenantStore = null,
        ILogger<TenantBehavior<TRequest, TResponse>>? logger = null)
    {
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _tenantResolver = tenantResolver;
        _tenantStore = tenantStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        ITenantContext? tenantContext = null;
        var previousContext = _tenantContextAccessor.Context;

        try
        {
            // Check for tenant override
            if (request is ITenantAware tenantAware && !string.IsNullOrEmpty(tenantAware.OverrideTenantId))
            {
                if (_tenantStore != null)
                {
                    tenantContext = await _tenantStore.GetByIdAsync(tenantAware.OverrideTenantId, cancellationToken);
                    
                    if (tenantContext == null)
                    {
                        _logger?.LogWarning(
                            "[Tenant] Override tenant {TenantId} not found for {RequestName}",
                            tenantAware.OverrideTenantId,
                            requestName);
                    }
                    else
                    {
                        _logger?.LogDebug(
                            "[Tenant] Using override tenant {TenantId} for {RequestName}",
                            tenantContext.TenantId,
                            requestName);
                    }
                }
                else
                {
                    // Create a minimal context from the override ID
                    tenantContext = TenantContext.FromId(tenantAware.OverrideTenantId);
                    
                    _logger?.LogDebug(
                        "[Tenant] Using override tenant ID {TenantId} for {RequestName} (no store available)",
                        tenantAware.OverrideTenantId,
                        requestName);
                }
            }
            // Normal tenant resolution
            else if (_tenantResolver != null)
            {
                tenantContext = await _tenantResolver.ResolveAsync(cancellationToken);
                
                if (tenantContext != null)
                {
                    _logger?.LogDebug(
                        "[Tenant] Resolved tenant {TenantId} for {RequestName}",
                        tenantContext.TenantId,
                        requestName);
                }
            }

            // Set the tenant context
            _tenantContextAccessor.Context = tenantContext;

            // Validate tenant requirements
            if (request is ITenantRequired tenantRequired)
            {
                if (!tenantRequired.AllowNoTenant && (tenantContext == null || !tenantContext.HasTenant))
                {
                    _logger?.LogWarning(
                        "[Tenant] No tenant resolved for {RequestName} which requires a tenant",
                        requestName);

                    throw new TenantRequiredException(
                        $"A tenant context is required to execute {requestName}.",
                        requestName);
                }
            }

            // Execute the request
            return await next();
        }
        finally
        {
            // Restore previous context
            _tenantContextAccessor.Context = previousContext;
        }
    }
}

/// <summary>
/// Exception thrown when a tenant is required but not available.
/// </summary>
public sealed class TenantRequiredException : Mvp24Hours.Core.Exceptions.Mvp24HoursException
{
    /// <summary>
    /// Gets the name of the request that required a tenant.
    /// </summary>
    public string RequestName { get; }

    /// <summary>
    /// Creates a new instance of TenantRequiredException.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="requestName">The request name.</param>
    public TenantRequiredException(string message, string requestName)
        : base(message, "TENANT_REQUIRED")
    {
        RequestName = requestName;
    }

    /// <summary>
    /// Creates a new instance of TenantRequiredException.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="requestName">The request name.</param>
    /// <param name="innerException">The inner exception.</param>
    public TenantRequiredException(string message, string requestName, Exception innerException)
        : base(message, "TENANT_REQUIRED", innerException)
    {
        RequestName = requestName;
    }
}

/// <summary>
/// Exception thrown when a tenant is not found.
/// </summary>
public sealed class TenantNotFoundException : Mvp24Hours.Core.Exceptions.Mvp24HoursException
{
    /// <summary>
    /// Gets the tenant identifier that was not found.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Creates a new instance of TenantNotFoundException.
    /// </summary>
    /// <param name="tenantId">The tenant ID that was not found.</param>
    public TenantNotFoundException(string tenantId)
        : base($"Tenant '{tenantId}' was not found.", "TENANT_NOT_FOUND")
    {
        TenantId = tenantId;
    }

    /// <summary>
    /// Creates a new instance of TenantNotFoundException.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="tenantId">The tenant ID that was not found.</param>
    public TenantNotFoundException(string message, string tenantId)
        : base(message, "TENANT_NOT_FOUND")
    {
        TenantId = tenantId;
    }
}


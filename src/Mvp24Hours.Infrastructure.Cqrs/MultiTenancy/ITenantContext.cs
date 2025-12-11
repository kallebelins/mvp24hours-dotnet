//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.MultiTenancy;

/// <summary>
/// Provides context information about the current tenant in a multi-tenant application.
/// </summary>
/// <remarks>
/// <para>
/// The tenant context provides:
/// <list type="bullet">
/// <item><strong>TenantId</strong> - Unique identifier for the current tenant</item>
/// <item><strong>TenantName</strong> - Human-readable name of the tenant</item>
/// <item><strong>ConnectionString</strong> - Optional per-tenant database connection</item>
/// <item><strong>Properties</strong> - Additional tenant-specific configuration</item>
/// </list>
/// </para>
/// <para>
/// <strong>Multi-tenancy Strategies:</strong>
/// <code>
/// ┌─────────────────────────────────────────────────────────────────────────────┐
/// │ Strategy 1: Single Database, Shared Schema (TenantId column)               │
/// │   - All tenants in same database                                            │
/// │   - Discriminator column (TenantId) in each table                          │
/// │   - Automatic filtering via ITenantFilter                                  │
/// │                                                                             │
/// │ Strategy 2: Single Database, Separate Schemas                               │
/// │   - Each tenant has own schema                                              │
/// │   - Schema name from tenant context                                         │
/// │                                                                             │
/// │ Strategy 3: Separate Databases                                              │
/// │   - Each tenant has own database                                            │
/// │   - ConnectionString from tenant context                                    │
/// └─────────────────────────────────────────────────────────────────────────────┘
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyQueryHandler : IMediatorQueryHandler&lt;GetProductsQuery, List&lt;Product&gt;&gt;
/// {
///     private readonly ITenantContext _tenantContext;
///     private readonly IRepository&lt;Product&gt; _repository;
///     
///     public MyQueryHandler(ITenantContext tenantContext, IRepository&lt;Product&gt; repository)
///     {
///         _tenantContext = tenantContext;
///         _repository = repository;
///     }
///     
///     public async Task&lt;List&lt;Product&gt;&gt; Handle(GetProductsQuery request, CancellationToken ct)
///     {
///         // TenantId automatically available
///         var tenantId = _tenantContext.TenantId;
///         
///         // Repository automatically filters by tenant via ITenantFilter
///         return await _repository.ToListAsync(ct);
///     }
/// }
/// </code>
/// </example>
public interface ITenantContext
{
    /// <summary>
    /// Gets the unique identifier for the current tenant.
    /// </summary>
    /// <remarks>
    /// Returns null if no tenant is set (e.g., in a non-tenant context or before resolution).
    /// </remarks>
    string? TenantId { get; }

    /// <summary>
    /// Gets the human-readable name of the current tenant.
    /// </summary>
    string? TenantName { get; }

    /// <summary>
    /// Gets the database connection string for the current tenant.
    /// Used in database-per-tenant strategies.
    /// </summary>
    string? ConnectionString { get; }

    /// <summary>
    /// Gets the database schema for the current tenant.
    /// Used in schema-per-tenant strategies.
    /// </summary>
    string? Schema { get; }

    /// <summary>
    /// Gets whether a tenant has been resolved for the current context.
    /// </summary>
    bool HasTenant => !string.IsNullOrEmpty(TenantId);

    /// <summary>
    /// Gets additional properties specific to the current tenant.
    /// </summary>
    IReadOnlyDictionary<string, object?> Properties { get; }

    /// <summary>
    /// Gets a typed property value from the tenant properties.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">Default value if property not found.</param>
    /// <returns>The property value or default.</returns>
    T? GetProperty<T>(string key, T? defaultValue = default);
}

/// <summary>
/// Mutable interface for setting the current tenant context.
/// </summary>
public interface ITenantContextAccessor
{
    /// <summary>
    /// Gets or sets the current tenant context.
    /// </summary>
    ITenantContext? Context { get; set; }
}

/// <summary>
/// Strategy interface for resolving the current tenant.
/// </summary>
/// <remarks>
/// Implement this interface to provide custom tenant resolution logic.
/// Common strategies include:
/// <list type="bullet">
/// <item>HTTP header (X-Tenant-Id)</item>
/// <item>Subdomain (tenant1.app.com)</item>
/// <item>Route parameter (/api/tenant1/...)</item>
/// <item>JWT claim</item>
/// <item>Query parameter</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class HeaderTenantResolver : ITenantResolver
/// {
///     private readonly IHttpContextAccessor _httpContextAccessor;
///     private readonly ITenantStore _tenantStore;
///     
///     public async Task&lt;ITenantContext?&gt; ResolveAsync(CancellationToken ct = default)
///     {
///         var httpContext = _httpContextAccessor.HttpContext;
///         if (httpContext?.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId) == true)
///         {
///             return await _tenantStore.GetByIdAsync(tenantId!, ct);
///         }
///         return null;
///     }
/// }
/// </code>
/// </example>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves the current tenant based on the current context.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved tenant context, or null if no tenant could be resolved.</returns>
    Task<ITenantContext?> ResolveAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for storing and retrieving tenant information.
/// </summary>
/// <example>
/// <code>
/// public class InMemoryTenantStore : ITenantStore
/// {
///     private readonly Dictionary&lt;string, TenantInfo&gt; _tenants = new();
///     
///     public async Task&lt;ITenantContext?&gt; GetByIdAsync(string tenantId, CancellationToken ct)
///     {
///         return _tenants.TryGetValue(tenantId, out var tenant) ? tenant : null;
///     }
/// }
/// </code>
/// </example>
public interface ITenantStore
{
    /// <summary>
    /// Gets a tenant by its identifier.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant context, or null if not found.</returns>
    Task<ITenantContext?> GetByIdAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant by its name.
    /// </summary>
    /// <param name="tenantName">The tenant name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant context, or null if not found.</returns>
    Task<ITenantContext?> GetByNameAsync(string tenantName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All tenant contexts.</returns>
    Task<IEnumerable<ITenantContext>> GetAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Marker interface for entities that belong to a tenant.
/// </summary>
/// <remarks>
/// Entities implementing this interface will automatically have tenant filtering applied.
/// </remarks>
/// <example>
/// <code>
/// public class Product : IHasTenant
/// {
///     public int Id { get; set; }
///     public string Name { get; set; }
///     public string TenantId { get; set; } // Automatically filtered
/// }
/// </code>
/// </example>
public interface IHasTenant
{
    /// <summary>
    /// Gets or sets the tenant identifier for this entity.
    /// </summary>
    string TenantId { get; set; }
}

/// <summary>
/// Interface for applying tenant filters to queries.
/// </summary>
public interface ITenantFilter
{
    /// <summary>
    /// Gets the current tenant ID for filtering.
    /// </summary>
    string? CurrentTenantId { get; }

    /// <summary>
    /// Gets whether tenant filtering should be applied.
    /// </summary>
    bool ShouldFilter { get; }

    /// <summary>
    /// Temporarily disables tenant filtering for the current scope.
    /// </summary>
    /// <returns>A disposable that re-enables filtering when disposed.</returns>
    IDisposable DisableFilter();
}


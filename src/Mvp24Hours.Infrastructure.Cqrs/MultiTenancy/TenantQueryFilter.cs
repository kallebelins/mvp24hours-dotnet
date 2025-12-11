//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Linq.Expressions;

namespace Mvp24Hours.Infrastructure.Cqrs.MultiTenancy;

/// <summary>
/// Provides query filter capabilities for multi-tenant entities.
/// </summary>
/// <remarks>
/// <para>
/// This class provides extension methods and expression builders for filtering
/// entities by tenant. It works with any entity that implements <see cref="IHasTenant"/>.
/// </para>
/// <para>
/// <strong>Usage Scenarios:</strong>
/// <code>
/// ┌─────────────────────────────────────────────────────────────────────────────┐
/// │ 1. EF Core Global Query Filters (automatic filtering)                       │
/// │    - Apply filter in OnModelCreating                                        │
/// │    - All queries automatically filtered by tenant                           │
/// │                                                                             │
/// │ 2. Repository Pattern (manual filtering)                                    │
/// │    - Apply filter expression in repository queries                          │
/// │    - More control over when filtering is applied                            │
/// │                                                                             │
/// │ 3. LINQ Extension Methods                                                   │
/// │    - Filter IQueryable/IEnumerable by tenant                               │
/// │    - Useful for in-memory filtering                                         │
/// └─────────────────────────────────────────────────────────────────────────────┘
/// </code>
/// </para>
/// </remarks>
public static class TenantQueryFilter
{
    /// <summary>
    /// Creates a tenant filter expression for entities implementing <see cref="IHasTenant"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="tenantId">The tenant ID to filter by.</param>
    /// <returns>An expression that filters by tenant ID.</returns>
    /// <example>
    /// <code>
    /// // In EF Core DbContext
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     var tenantId = _tenantContext.TenantId;
    ///     
    ///     modelBuilder.Entity&lt;Product&gt;()
    ///         .HasQueryFilter(TenantQueryFilter.CreateFilter&lt;Product&gt;(tenantId));
    /// }
    /// </code>
    /// </example>
    public static Expression<Func<T, bool>> CreateFilter<T>(string? tenantId) where T : IHasTenant
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            // Return a filter that matches nothing when no tenant
            return entity => false;
        }

        return entity => entity.TenantId == tenantId;
    }

    /// <summary>
    /// Creates a tenant filter expression using a tenant context accessor.
    /// This is useful for EF Core global query filters where the tenant may change per request.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="tenantContextAccessor">The tenant context accessor.</param>
    /// <returns>An expression that filters by the current tenant ID.</returns>
    /// <example>
    /// <code>
    /// // In EF Core DbContext with ITenantContextAccessor injected
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.Entity&lt;Product&gt;()
    ///         .HasQueryFilter(TenantQueryFilter.CreateDynamicFilter&lt;Product&gt;(_tenantContextAccessor));
    /// }
    /// </code>
    /// </example>
    public static Expression<Func<T, bool>> CreateDynamicFilter<T>(ITenantContextAccessor tenantContextAccessor) 
        where T : IHasTenant
    {
        // Note: This creates a closure over the accessor, so the tenant ID is evaluated at query time
        return entity => tenantContextAccessor.Context != null && 
                         entity.TenantId == tenantContextAccessor.Context.TenantId;
    }

    /// <summary>
    /// Creates a tenant filter expression that allows disabling via ITenantFilter.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="tenantFilter">The tenant filter.</param>
    /// <returns>An expression that filters by tenant when filtering is enabled.</returns>
    public static Expression<Func<T, bool>> CreateFilterWithBypass<T>(ITenantFilter tenantFilter) 
        where T : IHasTenant
    {
        return entity => !tenantFilter.ShouldFilter || entity.TenantId == tenantFilter.CurrentTenantId;
    }
}

/// <summary>
/// Extension methods for filtering queryables and enumerables by tenant.
/// </summary>
public static class TenantQueryExtensions
{
    /// <summary>
    /// Filters an IQueryable to only include entities for the current tenant.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The query to filter.</param>
    /// <param name="tenantId">The tenant ID to filter by.</param>
    /// <returns>The filtered query.</returns>
    /// <example>
    /// <code>
    /// var products = await _dbContext.Products
    ///     .FilterByTenant(_tenantContext.TenantId)
    ///     .ToListAsync();
    /// </code>
    /// </example>
    public static IQueryable<T> FilterByTenant<T>(this IQueryable<T> query, string? tenantId) 
        where T : IHasTenant
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            return query.Where(e => false); // Return empty if no tenant
        }

        return query.Where(e => e.TenantId == tenantId);
    }

    /// <summary>
    /// Filters an IQueryable to only include entities for the current tenant from context.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The query to filter.</param>
    /// <param name="tenantContext">The tenant context.</param>
    /// <returns>The filtered query.</returns>
    public static IQueryable<T> FilterByTenant<T>(this IQueryable<T> query, ITenantContext? tenantContext) 
        where T : IHasTenant
    {
        return query.FilterByTenant(tenantContext?.TenantId);
    }

    /// <summary>
    /// Filters an IQueryable using the tenant filter with bypass support.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The query to filter.</param>
    /// <param name="tenantFilter">The tenant filter.</param>
    /// <returns>The filtered query, or unfiltered if filtering is disabled.</returns>
    public static IQueryable<T> FilterByTenant<T>(this IQueryable<T> query, ITenantFilter tenantFilter) 
        where T : IHasTenant
    {
        if (!tenantFilter.ShouldFilter)
        {
            return query;
        }

        return query.FilterByTenant(tenantFilter.CurrentTenantId);
    }

    /// <summary>
    /// Filters an IEnumerable to only include entities for the specified tenant.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="tenantId">The tenant ID to filter by.</param>
    /// <returns>The filtered collection.</returns>
    public static IEnumerable<T> FilterByTenant<T>(this IEnumerable<T> source, string? tenantId) 
        where T : IHasTenant
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            return Enumerable.Empty<T>();
        }

        return source.Where(e => e.TenantId == tenantId);
    }

    /// <summary>
    /// Filters an IEnumerable using the tenant context.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="tenantContext">The tenant context.</param>
    /// <returns>The filtered collection.</returns>
    public static IEnumerable<T> FilterByTenant<T>(this IEnumerable<T> source, ITenantContext? tenantContext) 
        where T : IHasTenant
    {
        return source.FilterByTenant(tenantContext?.TenantId);
    }

    /// <summary>
    /// Sets the tenant ID on an entity before saving.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to set the tenant on.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>The entity with tenant ID set.</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant ID is null or empty.</exception>
    public static T WithTenant<T>(this T entity, string tenantId) where T : IHasTenant
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new InvalidOperationException("Tenant ID cannot be null or empty.");
        }

        entity.TenantId = tenantId;
        return entity;
    }

    /// <summary>
    /// Sets the tenant ID on an entity from the tenant context.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to set the tenant on.</param>
    /// <param name="tenantContext">The tenant context.</param>
    /// <returns>The entity with tenant ID set.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no tenant is available.</exception>
    public static T WithTenant<T>(this T entity, ITenantContext tenantContext) where T : IHasTenant
    {
        if (tenantContext == null || !tenantContext.HasTenant)
        {
            throw new InvalidOperationException("No tenant context available.");
        }

        return entity.WithTenant(tenantContext.TenantId!);
    }
}

/// <summary>
/// Marker interface for queries that should bypass tenant filtering.
/// </summary>
/// <remarks>
/// Use this for administrative queries that need to see all tenants' data.
/// This should be used sparingly and with proper authorization.
/// </remarks>
/// <example>
/// <code>
/// public class GetAllProductsAdminQuery : IMediatorQuery&lt;List&lt;Product&gt;&gt;, IBypassTenantFilter
/// {
///     // This query will see all products across all tenants
///     // Should be protected with proper authorization (e.g., Admin role)
/// }
/// </code>
/// </example>
public interface IBypassTenantFilter
{
}

/// <summary>
/// Marker interface for entities that are shared across all tenants.
/// </summary>
/// <remarks>
/// Entities implementing this interface are not subject to tenant filtering.
/// Examples: lookup tables, configuration, shared reference data.
/// </remarks>
public interface ISharedEntity
{
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Domain.Entity
{
    /// <summary>
    /// Interface for entities that belong to a specific tenant in a multi-tenant system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In multi-tenant applications, data isolation between tenants is critical.
    /// Entities implementing this interface will be automatically filtered by tenant
    /// in queries, ensuring that each tenant can only access their own data.
    /// </para>
    /// <para>
    /// Common implementation strategies:
    /// - Database-per-tenant: Different database for each tenant
    /// - Schema-per-tenant: Different schema in the same database
    /// - Row-level: Same table with TenantId column (this interface supports this approach)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Invoice : ITenantEntity
    /// {
    ///     public int Id { get; set; }
    ///     public string TenantId { get; set; }
    ///     public decimal Amount { get; set; }
    /// }
    /// 
    /// // In DbContext OnModelCreating:
    /// modelBuilder.Entity&lt;Invoice&gt;().HasQueryFilter(
    ///     i => i.TenantId == _tenantProvider.GetCurrentTenantId()
    /// );
    /// </code>
    /// </example>
    public interface ITenantEntity
    {
        /// <summary>
        /// Gets or sets the identifier of the tenant that owns this entity.
        /// </summary>
        string TenantId { get; set; }
    }

    /// <summary>
    /// Generic interface for tenant entities with a typed tenant identifier.
    /// </summary>
    /// <typeparam name="TTenantId">The type of the tenant identifier (e.g., Guid, int, string).</typeparam>
    public interface ITenantEntity<TTenantId>
    {
        /// <summary>
        /// Gets or sets the identifier of the tenant that owns this entity.
        /// </summary>
        TTenantId TenantId { get; set; }
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Core.Contract.Infrastructure
{
    /// <summary>
    /// Provides access to the current tenant's identity information for multi-tenant applications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides the current tenant identifier for use in infrastructure components
    /// like DbContext query filters, repositories, and interceptors.
    /// </para>
    /// <para>
    /// <strong>Multi-tenancy Strategies:</strong>
    /// <code>
    /// ┌─────────────────────────────────────────────────────────────────────────────┐
    /// │ Strategy 1: Single Database, Shared Schema (TenantId column)               │
    /// │   - All tenants in same database                                            │
    /// │   - Discriminator column (TenantId) in each table                          │
    /// │   - Automatic filtering via ITenantProvider                                 │
    /// │                                                                             │
    /// │ Strategy 2: Single Database, Separate Schemas                               │
    /// │   - Each tenant has own schema                                              │
    /// │   - Schema name from tenant provider                                        │
    /// │                                                                             │
    /// │ Strategy 3: Separate Databases                                              │
    /// │   - Each tenant has own database                                            │
    /// │   - ConnectionString from tenant provider                                   │
    /// └─────────────────────────────────────────────────────────────────────────────┘
    /// </code>
    /// </para>
    /// <para>
    /// Implementations can obtain the tenant from various sources:
    /// <list type="bullet">
    /// <item>HTTP header (X-Tenant-Id)</item>
    /// <item>Subdomain (tenant1.app.com)</item>
    /// <item>Route parameter (/api/{tenantId}/...)</item>
    /// <item>JWT claim</item>
    /// <item>Query parameter</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Implementation using HTTP header:
    /// public class HttpHeaderTenantProvider : ITenantProvider
    /// {
    ///     private readonly IHttpContextAccessor _httpContextAccessor;
    ///     
    ///     public HttpHeaderTenantProvider(IHttpContextAccessor httpContextAccessor)
    ///     {
    ///         _httpContextAccessor = httpContextAccessor;
    ///     }
    ///     
    ///     public string TenantId => _httpContextAccessor.HttpContext?
    ///         .Request.Headers["X-Tenant-Id"].FirstOrDefault();
    ///         
    ///     public bool HasTenant => !string.IsNullOrEmpty(TenantId);
    /// }
    /// 
    /// // Implementation using subdomain:
    /// public class SubdomainTenantProvider : ITenantProvider
    /// {
    ///     private readonly IHttpContextAccessor _httpContextAccessor;
    ///     
    ///     public SubdomainTenantProvider(IHttpContextAccessor httpContextAccessor)
    ///     {
    ///         _httpContextAccessor = httpContextAccessor;
    ///     }
    ///     
    ///     public string TenantId
    ///     {
    ///         get
    ///         {
    ///             var host = _httpContextAccessor.HttpContext?.Request.Host.Host;
    ///             if (string.IsNullOrEmpty(host)) return null;
    ///             var parts = host.Split('.');
    ///             return parts.Length > 2 ? parts[0] : null; // tenant.example.com
    ///         }
    ///     }
    ///     
    ///     public bool HasTenant => !string.IsNullOrEmpty(TenantId);
    /// }
    /// </code>
    /// </example>
    public interface ITenantProvider
    {
        /// <summary>
        /// Gets the unique identifier of the current tenant.
        /// </summary>
        /// <value>
        /// The tenant's unique identifier, or null if no tenant is set.
        /// </value>
        string TenantId { get; }

        /// <summary>
        /// Gets whether a tenant has been resolved for the current context.
        /// </summary>
        bool HasTenant { get; }

        /// <summary>
        /// Gets the database connection string for the current tenant.
        /// Used in database-per-tenant strategies.
        /// </summary>
        /// <value>
        /// The connection string for the current tenant, or null if using default/shared database.
        /// </value>
        string ConnectionString { get; }

        /// <summary>
        /// Gets the database schema for the current tenant.
        /// Used in schema-per-tenant strategies.
        /// </summary>
        /// <value>
        /// The schema name for the current tenant, or null if using default/shared schema.
        /// </value>
        string Schema { get; }
    }

    /// <summary>
    /// Generic interface for tenant providers with a typed tenant identifier.
    /// </summary>
    /// <typeparam name="TTenantId">The type of the tenant identifier (e.g., Guid, int, string).</typeparam>
    public interface ITenantProvider<TTenantId>
    {
        /// <summary>
        /// Gets the unique identifier of the current tenant.
        /// </summary>
        TTenantId TenantId { get; }

        /// <summary>
        /// Gets whether a tenant has been resolved for the current context.
        /// </summary>
        bool HasTenant { get; }

        /// <summary>
        /// Gets the database connection string for the current tenant.
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Gets the database schema for the current tenant.
        /// </summary>
        string Schema { get; }
    }

    /// <summary>
    /// A static/singleton implementation of <see cref="ITenantProvider"/> using AsyncLocal storage.
    /// </summary>
    /// <remarks>
    /// This implementation uses AsyncLocal to store the current tenant, making it
    /// thread-safe and usable in async contexts. Set the tenant at the beginning
    /// of a request/operation and it will be available throughout.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Set at the beginning of request:
    /// AsyncLocalTenantProvider.SetCurrentTenant("tenant123", "conn_string", "tenant_schema");
    /// 
    /// // Get anywhere in the call stack:
    /// var tenantId = AsyncLocalTenantProvider.Instance.TenantId; // "tenant123"
    /// 
    /// // Clear at the end:
    /// AsyncLocalTenantProvider.ClearCurrentTenant();
    /// </code>
    /// </example>
    public class AsyncLocalTenantProvider : ITenantProvider
    {
        private static readonly System.Threading.AsyncLocal<TenantData> _current
            = new System.Threading.AsyncLocal<TenantData>();

        private class TenantData
        {
            public string TenantId { get; set; }
            public string ConnectionString { get; set; }
            public string Schema { get; set; }
        }

        /// <summary>
        /// Gets the singleton instance of the provider.
        /// </summary>
        public static AsyncLocalTenantProvider Instance { get; } = new AsyncLocalTenantProvider();

        /// <inheritdoc />
        public string TenantId => _current.Value?.TenantId;

        /// <inheritdoc />
        public bool HasTenant => !string.IsNullOrEmpty(TenantId);

        /// <inheritdoc />
        public string ConnectionString => _current.Value?.ConnectionString;

        /// <inheritdoc />
        public string Schema => _current.Value?.Schema;

        /// <summary>
        /// Sets the current tenant for the current async context.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="connectionString">Optional connection string for database-per-tenant.</param>
        /// <param name="schema">Optional schema name for schema-per-tenant.</param>
        public static void SetCurrentTenant(string tenantId, string connectionString = null, string schema = null)
        {
            _current.Value = new TenantData
            {
                TenantId = tenantId,
                ConnectionString = connectionString,
                Schema = schema
            };
        }

        /// <summary>
        /// Clears the current tenant from the async context.
        /// </summary>
        public static void ClearCurrentTenant()
        {
            _current.Value = null;
        }
    }

    /// <summary>
    /// A default implementation that returns no tenant.
    /// Use this when multi-tenancy is not needed.
    /// </summary>
    public class NoTenantProvider : ITenantProvider
    {
        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static NoTenantProvider Instance { get; } = new NoTenantProvider();

        /// <inheritdoc />
        public string TenantId => null;

        /// <inheritdoc />
        public bool HasTenant => false;

        /// <inheritdoc />
        public string ConnectionString => null;

        /// <inheritdoc />
        public string Schema => null;
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using RabbitMQ.Client;

namespace Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Contract
{
    /// <summary>
    /// Factory for creating RabbitMQ connections per tenant with virtual host isolation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Multi-tenancy strategies for RabbitMQ:
    /// <code>
    /// ┌─────────────────────────────────────────────────────────────────────────────┐
    /// │ Strategy 1: Virtual Host per Tenant (recommended)                           │
    /// │   - Each tenant gets their own virtual host                                 │
    /// │   - Complete isolation of exchanges, queues, and bindings                   │
    /// │   - Different credentials per tenant possible                               │
    /// │                                                                             │
    /// │ Strategy 2: Prefix/Suffix per Tenant                                        │
    /// │   - Shared virtual host with tenant-prefixed queue names                    │
    /// │   - Simpler setup but less isolation                                        │
    /// │   - Example: tenant1_orders, tenant2_orders                                 │
    /// │                                                                             │
    /// │ Strategy 3: Routing Key per Tenant                                          │
    /// │   - Shared queues with tenant-based routing                                 │
    /// │   - Filter messages by tenant routing key                                   │
    /// │   - Least isolation, most resource efficient                                │
    /// └─────────────────────────────────────────────────────────────────────────────┘
    /// </code>
    /// </para>
    /// </remarks>
    public interface ITenantConnectionFactory
    {
        /// <summary>
        /// Gets or creates a RabbitMQ connection for the specified tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>A RabbitMQ connection for the tenant.</returns>
        IConnection GetOrCreateConnection(string tenantId);

        /// <summary>
        /// Gets or creates a RabbitMQ channel for the specified tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>A RabbitMQ channel for the tenant.</returns>
        IModel GetOrCreateChannel(string tenantId);

        /// <summary>
        /// Gets the virtual host name for the specified tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>The virtual host name.</returns>
        string GetVirtualHost(string tenantId);

        /// <summary>
        /// Gets whether the specified tenant has an active connection.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>True if the tenant has an active connection.</returns>
        bool HasConnection(string tenantId);

        /// <summary>
        /// Closes and removes the connection for the specified tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        void CloseConnection(string tenantId);

        /// <summary>
        /// Closes all tenant connections.
        /// </summary>
        void CloseAllConnections();
    }
}


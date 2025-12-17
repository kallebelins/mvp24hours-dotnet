//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Configuration
{
    /// <summary>
    /// Configuration options for multi-tenant RabbitMQ.
    /// </summary>
    public class TenantRabbitMQOptions
    {
        /// <summary>
        /// Gets or sets the multi-tenancy strategy. Default is VirtualHostPerTenant.
        /// </summary>
        public TenantIsolationStrategy IsolationStrategy { get; set; } = TenantIsolationStrategy.VirtualHostPerTenant;

        /// <summary>
        /// Gets or sets the header name for tenant ID. Default is "x-tenant-id".
        /// </summary>
        public string TenantIdHeader { get; set; } = "x-tenant-id";

        /// <summary>
        /// Gets or sets the header name for tenant name. Default is "x-tenant-name".
        /// </summary>
        public string TenantNameHeader { get; set; } = "x-tenant-name";

        /// <summary>
        /// Gets or sets whether to reject messages without tenant information. Default is false.
        /// </summary>
        public bool RejectMessagesWithoutTenant { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to validate tenant exists before processing. Default is true.
        /// </summary>
        public bool ValidateTenantExists { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to automatically propagate tenant headers in published messages. Default is true.
        /// </summary>
        public bool AutoPropagateTenantHeaders { get; set; } = true;

        /// <summary>
        /// Gets or sets the default virtual host for tenants when not explicitly configured.
        /// Default is null, which means tenant ID will be used as virtual host name.
        /// </summary>
        public string? DefaultVirtualHost { get; set; }

        /// <summary>
        /// Gets or sets the virtual host name template. Use {tenantId} as placeholder.
        /// Default is "{tenantId}".
        /// </summary>
        public string VirtualHostTemplate { get; set; } = "{tenantId}";

        /// <summary>
        /// Gets or sets the queue name prefix template. Use {tenantId} as placeholder.
        /// Default is "{tenantId}_".
        /// </summary>
        public string QueuePrefixTemplate { get; set; } = "{tenantId}_";

        /// <summary>
        /// Gets or sets the exchange prefix template. Use {tenantId} as placeholder.
        /// Default is "{tenantId}_".
        /// </summary>
        public string ExchangePrefixTemplate { get; set; } = "{tenantId}_";

        /// <summary>
        /// Gets or sets the dead letter queue name template. Use {tenantId} as placeholder.
        /// Default is "{tenantId}_dlq".
        /// </summary>
        public string DeadLetterQueueTemplate { get; set; } = "{tenantId}_dlq";

        /// <summary>
        /// Gets or sets the dead letter exchange name template. Use {tenantId} as placeholder.
        /// Default is "{tenantId}_dlx".
        /// </summary>
        public string DeadLetterExchangeTemplate { get; set; } = "{tenantId}_dlx";

        /// <summary>
        /// Gets or sets the connection pool size per tenant. Default is 5.
        /// </summary>
        public int ConnectionPoolSizePerTenant { get; set; } = 5;

        /// <summary>
        /// Gets or sets the maximum number of tenant connections to maintain. Default is 100.
        /// </summary>
        public int MaxTenantConnections { get; set; } = 100;

        /// <summary>
        /// Gets or sets the idle connection timeout. Default is 30 minutes.
        /// </summary>
        public TimeSpan IdleConnectionTimeout { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets static tenant configurations (for scenarios without dynamic resolution).
        /// </summary>
        public Dictionary<string, TenantRabbitMQConnectionConfig> Tenants { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to use separate dead letter queues per tenant. Default is true.
        /// </summary>
        public bool UseTenantSpecificDeadLetterQueues { get; set; } = true;

        /// <summary>
        /// Applies the virtual host template to get the actual virtual host name.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <returns>The virtual host name.</returns>
        public string GetVirtualHost(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
                return DefaultVirtualHost ?? "/";

            return VirtualHostTemplate.Replace("{tenantId}", tenantId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Applies the queue prefix template to get the actual queue prefix.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <returns>The queue prefix.</returns>
        public string GetQueuePrefix(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
                return string.Empty;

            return QueuePrefixTemplate.Replace("{tenantId}", tenantId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Applies the exchange prefix template to get the actual exchange prefix.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <returns>The exchange prefix.</returns>
        public string GetExchangePrefix(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
                return string.Empty;

            return ExchangePrefixTemplate.Replace("{tenantId}", tenantId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the dead letter queue name for a tenant.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <returns>The dead letter queue name.</returns>
        public string GetDeadLetterQueue(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
                return "dlq";

            return DeadLetterQueueTemplate.Replace("{tenantId}", tenantId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the dead letter exchange name for a tenant.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <returns>The dead letter exchange name.</returns>
        public string GetDeadLetterExchange(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
                return "dlx";

            return DeadLetterExchangeTemplate.Replace("{tenantId}", tenantId, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Configuration for a specific tenant's RabbitMQ connection.
    /// </summary>
    public class TenantRabbitMQConnectionConfig
    {
        /// <summary>
        /// Gets or sets the virtual host for this tenant.
        /// </summary>
        public string? VirtualHost { get; set; }

        /// <summary>
        /// Gets or sets the connection string for this tenant.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the username for this tenant.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the password for this tenant.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets whether this tenant is enabled. Default is true.
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Tenant isolation strategies for RabbitMQ.
    /// </summary>
    public enum TenantIsolationStrategy
    {
        /// <summary>
        /// Each tenant gets their own virtual host (strongest isolation).
        /// </summary>
        VirtualHostPerTenant,

        /// <summary>
        /// Shared virtual host with tenant-prefixed queue/exchange names.
        /// </summary>
        PrefixPerTenant,

        /// <summary>
        /// Shared queues with tenant-based routing keys.
        /// </summary>
        RoutingKeyPerTenant,

        /// <summary>
        /// No automatic isolation (manual handling).
        /// </summary>
        None
    }
}


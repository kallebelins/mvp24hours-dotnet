//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Contract
{
    /// <summary>
    /// Specialized consume filter for tenant isolation and context propagation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The tenant consume filter is responsible for:
    /// <list type="bullet">
    /// <item>Extracting tenant information from message headers</item>
    /// <item>Setting the tenant context for the current scope</item>
    /// <item>Validating that the message belongs to the expected tenant</item>
    /// <item>Rejecting messages from unauthorized tenants</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Header Format:</strong>
    /// <code>
    /// ┌────────────────────────────────────────────────────────────────────────────┐
    /// │ Standard Tenant Headers:                                                   │
    /// │   x-tenant-id    : Unique tenant identifier                                │
    /// │   x-tenant-name  : Human-readable tenant name (optional)                   │
    /// │   x-tenant-schema: Database schema for the tenant (optional)               │
    /// └────────────────────────────────────────────────────────────────────────────┘
    /// </code>
    /// </para>
    /// </remarks>
    public interface ITenantConsumeFilter : IConsumeFilter
    {
        /// <summary>
        /// Header name for tenant ID. Default: "x-tenant-id".
        /// </summary>
        string TenantIdHeader { get; }

        /// <summary>
        /// Header name for tenant name. Default: "x-tenant-name".
        /// </summary>
        string TenantNameHeader { get; }

        /// <summary>
        /// Gets or sets whether to reject messages without tenant information.
        /// </summary>
        bool RejectMessagesWithoutTenant { get; }

        /// <summary>
        /// Gets or sets whether to validate tenant exists before processing.
        /// </summary>
        bool ValidateTenantExists { get; }
    }

    /// <summary>
    /// Publish filter for automatic tenant header propagation.
    /// </summary>
    public interface ITenantPublishFilter : IPublishFilter
    {
        /// <summary>
        /// Header name for tenant ID. Default: "x-tenant-id".
        /// </summary>
        string TenantIdHeader { get; }

        /// <summary>
        /// Header name for tenant name. Default: "x-tenant-name".
        /// </summary>
        string TenantNameHeader { get; }
    }

    /// <summary>
    /// Send filter for automatic tenant header propagation.
    /// </summary>
    public interface ITenantSendFilter : ISendFilter
    {
        /// <summary>
        /// Header name for tenant ID. Default: "x-tenant-id".
        /// </summary>
        string TenantIdHeader { get; }

        /// <summary>
        /// Header name for tenant name. Default: "x-tenant-name".
        /// </summary>
        string TenantNameHeader { get; }
    }
}


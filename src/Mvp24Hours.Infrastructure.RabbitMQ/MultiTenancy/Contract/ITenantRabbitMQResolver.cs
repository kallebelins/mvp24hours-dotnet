//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Contract
{
    /// <summary>
    /// Resolves RabbitMQ configuration for a specific tenant.
    /// </summary>
    /// <remarks>
    /// Implement this interface to provide custom tenant-specific RabbitMQ configuration,
    /// such as virtual host names, credentials, or connection strings.
    /// </remarks>
    public interface ITenantRabbitMQResolver
    {
        /// <summary>
        /// Resolves the RabbitMQ configuration for the specified tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The tenant-specific RabbitMQ configuration.</returns>
        Task<TenantRabbitMQConfiguration?> ResolveAsync(string tenantId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Configuration for tenant-specific RabbitMQ settings.
    /// </summary>
    public class TenantRabbitMQConfiguration
    {
        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the virtual host for this tenant.
        /// If null, defaults to tenant ID or configured default.
        /// </summary>
        public string? VirtualHost { get; set; }

        /// <summary>
        /// Gets or sets the connection string for this tenant.
        /// If null, uses the default connection string with tenant virtual host.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the username for this tenant.
        /// If null, uses the default credentials.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the password for this tenant.
        /// If null, uses the default credentials.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the queue name prefix for this tenant.
        /// Used for queue name isolation within shared virtual hosts.
        /// </summary>
        public string? QueuePrefix { get; set; }

        /// <summary>
        /// Gets or sets the exchange name prefix for this tenant.
        /// Used for exchange name isolation within shared virtual hosts.
        /// </summary>
        public string? ExchangePrefix { get; set; }

        /// <summary>
        /// Gets or sets the dead letter queue name for this tenant.
        /// </summary>
        public string? DeadLetterQueue { get; set; }

        /// <summary>
        /// Gets or sets the dead letter exchange for this tenant.
        /// </summary>
        public string? DeadLetterExchange { get; set; }

        /// <summary>
        /// Gets or sets whether this tenant is enabled for messaging.
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}


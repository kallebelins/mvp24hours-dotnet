//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Contract;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy
{
    /// <summary>
    /// In-memory implementation of tenant RabbitMQ configuration resolver.
    /// Useful for development, testing, and simple scenarios.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This resolver uses both static configuration from <see cref="TenantRabbitMQOptions"/>
    /// and dynamic configurations added at runtime.
    /// </para>
    /// <para>
    /// For production scenarios with dynamic tenant configuration, implement
    /// <see cref="ITenantRabbitMQResolver"/> backed by a database or configuration service.
    /// </para>
    /// </remarks>
    public class InMemoryTenantRabbitMQResolver : ITenantRabbitMQResolver
    {
        private readonly TenantRabbitMQOptions _options;
        private readonly ConcurrentDictionary<string, TenantRabbitMQConfiguration> _configurations = new();

        /// <summary>
        /// Creates a new in-memory tenant resolver.
        /// </summary>
        /// <param name="options">Multi-tenancy options.</param>
        public InMemoryTenantRabbitMQResolver(IOptions<TenantRabbitMQOptions> options)
        {
            _options = options?.Value ?? new TenantRabbitMQOptions();
        }

        /// <inheritdoc />
        public Task<TenantRabbitMQConfiguration?> ResolveAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return Task.FromResult<TenantRabbitMQConfiguration?>(null);
            }

            // Check dynamic configurations first
            if (_configurations.TryGetValue(tenantId, out var config))
            {
                return Task.FromResult<TenantRabbitMQConfiguration?>(config);
            }

            // Check static configuration from options
            if (_options.Tenants.TryGetValue(tenantId, out var staticConfig))
            {
                return Task.FromResult<TenantRabbitMQConfiguration?>(new TenantRabbitMQConfiguration
                {
                    TenantId = tenantId,
                    VirtualHost = staticConfig.VirtualHost ?? _options.GetVirtualHost(tenantId),
                    ConnectionString = staticConfig.ConnectionString,
                    Username = staticConfig.Username,
                    Password = staticConfig.Password,
                    QueuePrefix = _options.GetQueuePrefix(tenantId),
                    ExchangePrefix = _options.GetExchangePrefix(tenantId),
                    DeadLetterQueue = _options.GetDeadLetterQueue(tenantId),
                    DeadLetterExchange = _options.GetDeadLetterExchange(tenantId),
                    IsEnabled = staticConfig.IsEnabled
                });
            }

            // Return default configuration based on tenant ID
            return Task.FromResult<TenantRabbitMQConfiguration?>(new TenantRabbitMQConfiguration
            {
                TenantId = tenantId,
                VirtualHost = _options.GetVirtualHost(tenantId),
                QueuePrefix = _options.GetQueuePrefix(tenantId),
                ExchangePrefix = _options.GetExchangePrefix(tenantId),
                DeadLetterQueue = _options.GetDeadLetterQueue(tenantId),
                DeadLetterExchange = _options.GetDeadLetterExchange(tenantId),
                IsEnabled = true
            });
        }

        /// <summary>
        /// Adds or updates a tenant configuration dynamically.
        /// </summary>
        /// <param name="config">The tenant configuration.</param>
        public void AddOrUpdate(TenantRabbitMQConfiguration config)
        {
            if (config == null || string.IsNullOrEmpty(config.TenantId))
            {
                return;
            }

            _configurations[config.TenantId] = config;
        }

        /// <summary>
        /// Removes a tenant configuration.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>True if the configuration was removed, false otherwise.</returns>
        public bool Remove(string tenantId)
        {
            return _configurations.TryRemove(tenantId, out _);
        }

        /// <summary>
        /// Gets all dynamic tenant configurations.
        /// </summary>
        /// <returns>All tenant configurations.</returns>
        public IReadOnlyDictionary<string, TenantRabbitMQConfiguration> GetAll()
        {
            return _configurations;
        }

        /// <summary>
        /// Clears all dynamic tenant configurations.
        /// </summary>
        public void Clear()
        {
            _configurations.Clear();
        }
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Extensions
{
    /// <summary>
    /// Extension methods for configuring RabbitMQ multi-tenancy services.
    /// </summary>
    public static class TenantRabbitMQServiceExtensions
    {
        /// <summary>
        /// Adds RabbitMQ multi-tenancy services with virtual host per tenant isolation.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers:
        /// <list type="bullet">
        /// <item><see cref="ITenantConnectionFactory"/> - Connection pool per tenant</item>
        /// <item><see cref="ITenantRabbitMQResolver"/> - Tenant configuration resolver</item>
        /// <item><see cref="ITenantDeadLetterQueueHelper"/> - Tenant-specific DLQ management</item>
        /// <item><see cref="TenantConsumeFilter"/> - Consume filter for tenant context</item>
        /// <item><see cref="TenantPublishFilter"/> - Publish filter for tenant header propagation</item>
        /// <item><see cref="TenantSendFilter"/> - Send filter for tenant header propagation</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Example Usage:</strong>
        /// <code>
        /// services.AddMvp24HoursRabbitMQ(assembly, connectionOptions)
        ///         .AddMvp24HoursRabbitMQMultiTenancy(options =>
        ///         {
        ///             options.IsolationStrategy = TenantIsolationStrategy.VirtualHostPerTenant;
        ///             options.RejectMessagesWithoutTenant = true;
        ///             options.ValidateTenantExists = true;
        ///             options.Tenants["tenant-a"] = new TenantRabbitMQConnectionConfig
        ///             {
        ///                 VirtualHost = "tenant_a",
        ///                 Username = "tenant_a_user",
        ///                 Password = "tenant_a_pass"
        ///             };
        ///         });
        /// </code>
        /// </para>
        /// </remarks>
        public static IServiceCollection AddMvp24HoursRabbitMQMultiTenancy(
            this IServiceCollection services,
            Action<TenantRabbitMQOptions>? configure = null)
        {
            // Configure options
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<TenantRabbitMQOptions>(opt => { });
            }

            // Register tenant connection factory
            services.TryAddSingleton<ITenantConnectionFactory, TenantConnectionFactory>();

            // Register tenant resolver (in-memory by default)
            services.TryAddSingleton<ITenantRabbitMQResolver, InMemoryTenantRabbitMQResolver>();

            // Register dead letter queue helper
            services.TryAddSingleton<ITenantDeadLetterQueueHelper, TenantDeadLetterQueueHelper>();

            // Register consume filter
            services.TryAddSingleton<TenantConsumeFilter>();
            services.TryAddSingleton<ITenantConsumeFilter>(sp => sp.GetRequiredService<TenantConsumeFilter>());
            services.TryAddSingleton<IConsumeFilter>(sp => sp.GetRequiredService<TenantConsumeFilter>());

            // Register publish filter
            services.TryAddSingleton<TenantPublishFilter>();
            services.TryAddSingleton<ITenantPublishFilter>(sp => sp.GetRequiredService<TenantPublishFilter>());
            services.TryAddSingleton<IPublishFilter>(sp => sp.GetRequiredService<TenantPublishFilter>());

            // Register send filter
            services.TryAddSingleton<TenantSendFilter>();
            services.TryAddSingleton<ITenantSendFilter>(sp => sp.GetRequiredService<TenantSendFilter>());
            services.TryAddSingleton<ISendFilter>(sp => sp.GetRequiredService<TenantSendFilter>());

            return services;
        }

        /// <summary>
        /// Adds a custom tenant RabbitMQ resolver.
        /// </summary>
        /// <typeparam name="TResolver">The resolver implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQTenantResolver<TResolver>(
            this IServiceCollection services)
            where TResolver : class, ITenantRabbitMQResolver
        {
            services.AddSingleton<ITenantRabbitMQResolver, TResolver>();
            return services;
        }

        /// <summary>
        /// Adds RabbitMQ multi-tenancy with prefix per tenant isolation (shared virtual host).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQMultiTenancyWithPrefix(
            this IServiceCollection services,
            Action<TenantRabbitMQOptions>? configure = null)
        {
            return services.AddMvp24HoursRabbitMQMultiTenancy(options =>
            {
                options.IsolationStrategy = TenantIsolationStrategy.PrefixPerTenant;
                configure?.Invoke(options);
            });
        }

        /// <summary>
        /// Adds RabbitMQ multi-tenancy with routing key per tenant isolation.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQMultiTenancyWithRoutingKey(
            this IServiceCollection services,
            Action<TenantRabbitMQOptions>? configure = null)
        {
            return services.AddMvp24HoursRabbitMQMultiTenancy(options =>
            {
                options.IsolationStrategy = TenantIsolationStrategy.RoutingKeyPerTenant;
                configure?.Invoke(options);
            });
        }

        /// <summary>
        /// Configures filter pipeline options to include tenant filters.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional additional configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQTenantFilters(
            this IServiceCollection services,
            Action<FilterPipelineOptions>? configure = null)
        {
            services.Configure<FilterPipelineOptions>(options =>
            {
                // Tenant filter should run early to set context
                options.UseConsumeFilter<TenantConsumeFilter>();
                options.UsePublishFilter<TenantPublishFilter>();
                options.UseSendFilter<TenantSendFilter>();

                configure?.Invoke(options);
            });

            return services;
        }

        /// <summary>
        /// Adds a tenant with static configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="configure">Tenant configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQTenant(
            this IServiceCollection services,
            string tenantId,
            Action<TenantRabbitMQConnectionConfig> configure)
        {
            services.Configure<TenantRabbitMQOptions>(options =>
            {
                var config = new TenantRabbitMQConnectionConfig();
                configure(config);
                options.Tenants[tenantId] = config;
            });

            return services;
        }
    }
}


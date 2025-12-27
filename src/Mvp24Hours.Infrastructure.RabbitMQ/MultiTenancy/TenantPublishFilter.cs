//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy
{
    /// <summary>
    /// Publish filter that automatically propagates tenant information to outgoing messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This filter integrates with both the local <see cref="TenantConsumeFilter"/> context
    /// and the CQRS module's <c>ITenantContextAccessor</c> to propagate tenant headers.
    /// </para>
    /// <para>
    /// <strong>Tenant Resolution Priority:</strong>
    /// <code>
    /// 1. Existing headers (already set) → preserve
    /// 2. Current TenantConsumeFilter context → from consuming message
    /// 3. ITenantContextAccessor (CQRS) → from web request or other source
    /// 4. No tenant → skip header (unless required)
    /// </code>
    /// </para>
    /// </remarks>
    public class TenantPublishFilter : ITenantPublishFilter
    {
        private readonly TenantRabbitMQOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TenantPublishFilter>? _logger;

        /// <summary>
        /// Creates a new tenant publish filter.
        /// </summary>
        /// <param name="options">Multi-tenancy options.</param>
        /// <param name="serviceProvider">Service provider for resolving tenant context.</param>
        /// <param name="logger">Optional logger.</param>
        public TenantPublishFilter(
            IOptions<TenantRabbitMQOptions> options,
            IServiceProvider serviceProvider,
            ILogger<TenantPublishFilter>? logger = null)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
        }

        /// <inheritdoc />
        public string TenantIdHeader => _options.TenantIdHeader;

        /// <inheritdoc />
        public string TenantNameHeader => _options.TenantNameHeader;

        /// <inheritdoc />
        public async Task PublishAsync<TMessage>(
            IPublishFilterContext<TMessage> context,
            PublishFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            if (!_options.AutoPropagateTenantHeaders)
            {
                await next(context, cancellationToken);
                return;
            }

            // Try to get tenant from various sources
            var (tenantId, tenantName) = ResolveTenantContext();

            // Set headers if tenant is available and not already set
            if (!string.IsNullOrEmpty(tenantId))
            {
                // Only set if not already present
                if (!context.Headers.ContainsKey(TenantIdHeader))
                {
                    context.Headers[TenantIdHeader] = tenantId;
                }

                if (!string.IsNullOrEmpty(tenantName) && !context.Headers.ContainsKey(TenantNameHeader))
                {
                    context.Headers[TenantNameHeader] = tenantName;
                }

                LogTenantHeadersPropagated(tenantId, tenantName, context.MessageId);
            }

            await next(context, cancellationToken);
        }

        private (string? tenantId, string? tenantName) ResolveTenantContext()
        {
            // 1. Try TenantConsumeFilter.Current (from consuming message)
            var currentContext = TenantConsumeFilter.Current;
            if (currentContext?.HasTenant == true)
            {
                return (currentContext.TenantId, currentContext.TenantName);
            }

            // 2. Try ITenantContextAccessor from CQRS module
            return TryGetTenantFromCqrsModule();
        }

        private (string? tenantId, string? tenantName) TryGetTenantFromCqrsModule()
        {
            try
            {
                var accessorType = Type.GetType("Mvp24Hours.Infrastructure.Cqrs.MultiTenancy.ITenantContextAccessor, Mvp24Hours.Infrastructure.Cqrs");
                if (accessorType == null)
                    return (null, null);

                var accessor = _serviceProvider.GetService(accessorType);
                if (accessor == null)
                    return (null, null);

                var contextProperty = accessorType.GetProperty("Context");
                var tenantContext = contextProperty?.GetValue(accessor);
                if (tenantContext == null)
                    return (null, null);

                var tenantContextType = tenantContext.GetType();
                var tenantIdProperty = tenantContextType.GetProperty("TenantId");
                var tenantNameProperty = tenantContextType.GetProperty("TenantName");

                var tenantId = tenantIdProperty?.GetValue(tenantContext) as string;
                var tenantName = tenantNameProperty?.GetValue(tenantContext) as string;

                return (tenantId, tenantName);
            }
            catch
            {
                return (null, null);
            }
        }

        private void LogTenantHeadersPropagated(string tenantId, string? tenantName, string messageId)
        {
            _logger?.LogDebug(
                "Tenant headers propagated. TenantId={TenantId}, TenantName={TenantName}, MessageId={MessageId}",
                tenantId, tenantName, messageId);
        }
    }

    /// <summary>
    /// Send filter that automatically propagates tenant information to outgoing messages.
    /// </summary>
    public class TenantSendFilter : ITenantSendFilter
    {
        private readonly TenantRabbitMQOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TenantSendFilter>? _logger;

        /// <summary>
        /// Creates a new tenant send filter.
        /// </summary>
        /// <param name="options">Multi-tenancy options.</param>
        /// <param name="serviceProvider">Service provider for resolving tenant context.</param>
        /// <param name="logger">Optional logger.</param>
        public TenantSendFilter(
            IOptions<TenantRabbitMQOptions> options,
            IServiceProvider serviceProvider,
            ILogger<TenantSendFilter>? logger = null)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
        }

        /// <inheritdoc />
        public string TenantIdHeader => _options.TenantIdHeader;

        /// <inheritdoc />
        public string TenantNameHeader => _options.TenantNameHeader;

        /// <inheritdoc />
        public async Task SendAsync<TMessage>(
            ISendFilterContext<TMessage> context,
            SendFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            if (!_options.AutoPropagateTenantHeaders)
            {
                await next(context, cancellationToken);
                return;
            }

            // Try to get tenant from various sources
            var (tenantId, tenantName) = ResolveTenantContext();

            // Set headers if tenant is available and not already set
            if (!string.IsNullOrEmpty(tenantId))
            {
                // Only set if not already present
                if (!context.Headers.ContainsKey(TenantIdHeader))
                {
                    context.Headers[TenantIdHeader] = tenantId;
                }

                if (!string.IsNullOrEmpty(tenantName) && !context.Headers.ContainsKey(TenantNameHeader))
                {
                    context.Headers[TenantNameHeader] = tenantName;
                }

                LogTenantHeadersPropagated(tenantId, tenantName, context.MessageId);
            }

            await next(context, cancellationToken);
        }

        private (string? tenantId, string? tenantName) ResolveTenantContext()
        {
            // 1. Try TenantConsumeFilter.Current (from consuming message)
            var currentContext = TenantConsumeFilter.Current;
            if (currentContext?.HasTenant == true)
            {
                return (currentContext.TenantId, currentContext.TenantName);
            }

            // 2. Try ITenantContextAccessor from CQRS module
            return TryGetTenantFromCqrsModule();
        }

        private (string? tenantId, string? tenantName) TryGetTenantFromCqrsModule()
        {
            try
            {
                var accessorType = Type.GetType("Mvp24Hours.Infrastructure.Cqrs.MultiTenancy.ITenantContextAccessor, Mvp24Hours.Infrastructure.Cqrs");
                if (accessorType == null)
                    return (null, null);

                var accessor = _serviceProvider.GetService(accessorType);
                if (accessor == null)
                    return (null, null);

                var contextProperty = accessorType.GetProperty("Context");
                var tenantContext = contextProperty?.GetValue(accessor);
                if (tenantContext == null)
                    return (null, null);

                var tenantContextType = tenantContext.GetType();
                var tenantIdProperty = tenantContextType.GetProperty("TenantId");
                var tenantNameProperty = tenantContextType.GetProperty("TenantName");

                var tenantId = tenantIdProperty?.GetValue(tenantContext) as string;
                var tenantName = tenantNameProperty?.GetValue(tenantContext) as string;

                return (tenantId, tenantName);
            }
            catch
            {
                return (null, null);
            }
        }

        private void LogTenantHeadersPropagated(string tenantId, string? tenantName, string messageId)
        {
            _logger?.LogDebug(
                "Tenant headers propagated (send). TenantId={TenantId}, TenantName={TenantName}, MessageId={MessageId}",
                tenantId, tenantName, messageId);
        }
    }
}


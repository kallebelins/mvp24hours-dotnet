//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy
{
    /// <summary>
    /// Consume filter that extracts tenant information from message headers and sets the tenant context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This filter integrates with the CQRS module's <c>ITenantContextAccessor</c> to propagate
    /// tenant information throughout the message handling pipeline.
    /// </para>
    /// <para>
    /// <strong>Header Extraction Flow:</strong>
    /// <code>
    /// ┌────────────────────────────────────────────────────────────────────────────┐
    /// │ Message Received                                                           │
    /// │   ├─ Extract x-tenant-id header                                            │
    /// │   ├─ Extract x-tenant-name header (optional)                               │
    /// │   ├─ Validate tenant exists (if enabled)                                   │
    /// │   ├─ Set ITenantContextAccessor.Context                                    │
    /// │   ├─ Store in filter context Items                                         │
    /// │   └─ Continue to next filter / consumer                                    │
    /// │                                                                            │
    /// │ On Message Without Tenant:                                                 │
    /// │   ├─ RejectMessagesWithoutTenant = true → Send to DLQ                      │
    /// │   └─ RejectMessagesWithoutTenant = false → Continue with null tenant       │
    /// └────────────────────────────────────────────────────────────────────────────┘
    /// </code>
    /// </para>
    /// </remarks>
    public class TenantConsumeFilter : ITenantConsumeFilter
    {
        private readonly TenantRabbitMQOptions _options;
        private readonly ILogger<TenantConsumeFilter>? _logger;
        private static readonly AsyncLocal<TenantMessageContext?> _currentTenantContext = new();

        /// <summary>
        /// Gets the current tenant context from the message being processed.
        /// </summary>
        public static TenantMessageContext? Current => _currentTenantContext.Value;

        /// <summary>
        /// Creates a new tenant consume filter.
        /// </summary>
        /// <param name="options">Multi-tenancy options.</param>
        /// <param name="logger">Optional logger.</param>
        public TenantConsumeFilter(
            IOptions<TenantRabbitMQOptions> options,
            ILogger<TenantConsumeFilter>? logger = null)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <inheritdoc />
        public string TenantIdHeader => _options.TenantIdHeader;

        /// <inheritdoc />
        public string TenantNameHeader => _options.TenantNameHeader;

        /// <inheritdoc />
        public bool RejectMessagesWithoutTenant => _options.RejectMessagesWithoutTenant;

        /// <inheritdoc />
        public bool ValidateTenantExists => _options.ValidateTenantExists;

        /// <inheritdoc />
        public async Task ConsumeAsync<TMessage>(
            IConsumeFilterContext<TMessage> context,
            ConsumeFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            // Extract tenant information from headers
            var tenantId = context.GetHeader<string>(TenantIdHeader);
            var tenantName = context.GetHeader<string>(TenantNameHeader);

            // Check if tenant is required
            if (string.IsNullOrEmpty(tenantId) && RejectMessagesWithoutTenant)
            {
                LogTenantMissing(context.MessageId);
                context.SendToDeadLetter($"Missing required header: {TenantIdHeader}");
                return;
            }

            // Create tenant context
            var tenantContext = new TenantMessageContext(tenantId, tenantName);

            // Validate tenant exists if configured
            if (!string.IsNullOrEmpty(tenantId) && ValidateTenantExists)
            {
                var isValid = await ValidateTenantAsync(tenantId, context.ServiceProvider, cancellationToken);
                if (!isValid)
                {
                    LogTenantInvalid(tenantId, context.MessageId);
                    context.SendToDeadLetter($"Invalid tenant: {tenantId}");
                    return;
                }
            }

            // Store in AsyncLocal for downstream access
            var previousContext = _currentTenantContext.Value;
            _currentTenantContext.Value = tenantContext;

            // Store in filter context Items
            context.Items["TenantId"] = tenantId;
            context.Items["TenantName"] = tenantName;
            context.Items["TenantContext"] = tenantContext;

            // Try to set ITenantContextAccessor if available (from CQRS module)
            TrySetTenantContextAccessor(context.ServiceProvider, tenantId, tenantName);

            LogTenantContextSet(tenantId, tenantName, context.MessageId);

            try
            {
                await next(context, cancellationToken);
            }
            finally
            {
                // Restore previous context
                _currentTenantContext.Value = previousContext;

                // Clear ITenantContextAccessor
                TryClearTenantContextAccessor(context.ServiceProvider);

                LogTenantContextCleared(tenantId, context.MessageId);
            }
        }

        private async Task<bool> ValidateTenantAsync(string tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            // Try to resolve ITenantStore from CQRS module
            var tenantStoreType = Type.GetType("Mvp24Hours.Infrastructure.Cqrs.MultiTenancy.ITenantStore, Mvp24Hours.Infrastructure.Cqrs");
            if (tenantStoreType != null)
            {
                var tenantStore = serviceProvider.GetService(tenantStoreType);
                if (tenantStore != null)
                {
                    // Use reflection to call GetByIdAsync
                    var method = tenantStoreType.GetMethod("GetByIdAsync");
                    if (method != null)
                    {
                        var task = method.Invoke(tenantStore, new object[] { tenantId, cancellationToken }) as Task;
                        if (task != null)
                        {
                            await task;
                            var resultProperty = task.GetType().GetProperty("Result");
                            var result = resultProperty?.GetValue(task);
                            return result != null;
                        }
                    }
                }
            }

            // Check ITenantRabbitMQResolver
            var resolver = serviceProvider.GetService<ITenantRabbitMQResolver>();
            if (resolver != null)
            {
                var config = await resolver.ResolveAsync(tenantId, cancellationToken);
                return config != null && config.IsEnabled;
            }

            // Check static configuration
            if (_options.Tenants.TryGetValue(tenantId, out var staticConfig))
            {
                return staticConfig.IsEnabled;
            }

            // Default: allow all tenants if no validation source is available
            return true;
        }

        private void TrySetTenantContextAccessor(IServiceProvider serviceProvider, string? tenantId, string? tenantName)
        {
            if (string.IsNullOrEmpty(tenantId))
                return;

            try
            {
                // Try to resolve ITenantContextAccessor from CQRS module
                var accessorType = Type.GetType("Mvp24Hours.Infrastructure.Cqrs.MultiTenancy.ITenantContextAccessor, Mvp24Hours.Infrastructure.Cqrs");
                if (accessorType == null)
                    return;

                var accessor = serviceProvider.GetService(accessorType);
                if (accessor == null)
                    return;

                // Create TenantContext
                var contextType = Type.GetType("Mvp24Hours.Infrastructure.Cqrs.MultiTenancy.TenantContext, Mvp24Hours.Infrastructure.Cqrs");
                if (contextType == null)
                    return;

                var tenantContextInstance = Activator.CreateInstance(
                    contextType,
                    tenantId, tenantName, null, null, null);

                // Set Context property
                var contextProperty = accessorType.GetProperty("Context");
                contextProperty?.SetValue(accessor, tenantContextInstance);
            }
            catch (Exception ex)
            {
                // Log but don't fail - CQRS module may not be available
                _logger?.LogDebug(ex, "Could not set ITenantContextAccessor");
            }
        }

        private void TryClearTenantContextAccessor(IServiceProvider serviceProvider)
        {
            try
            {
                var accessorType = Type.GetType("Mvp24Hours.Infrastructure.Cqrs.MultiTenancy.ITenantContextAccessor, Mvp24Hours.Infrastructure.Cqrs");
                if (accessorType == null)
                    return;

                var accessor = serviceProvider.GetService(accessorType);
                if (accessor == null)
                    return;

                var contextProperty = accessorType.GetProperty("Context");
                contextProperty?.SetValue(accessor, null);
            }
            catch
            {
                // Ignore errors when clearing
            }
        }

        #region Logging

        private void LogTenantContextSet(string? tenantId, string? tenantName, string messageId)
        {
            var message = $"Tenant context set: TenantId={tenantId}, TenantName={tenantName}, MessageId={messageId}";
            if (_logger != null)
            {
                _logger.LogDebug(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-tenant-context-set", message);
            }
        }

        private void LogTenantContextCleared(string? tenantId, string messageId)
        {
            var message = $"Tenant context cleared: TenantId={tenantId}, MessageId={messageId}";
            if (_logger != null)
            {
                _logger.LogDebug(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "rabbitmq-tenant-context-cleared", message);
            }
        }

        private void LogTenantMissing(string messageId)
        {
            var message = $"Message rejected: missing tenant header. MessageId={messageId}";
            if (_logger != null)
            {
                _logger.LogWarning(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Warning, "rabbitmq-tenant-missing", message);
            }
        }

        private void LogTenantInvalid(string tenantId, string messageId)
        {
            var message = $"Message rejected: invalid tenant. TenantId={tenantId}, MessageId={messageId}";
            if (_logger != null)
            {
                _logger.LogWarning(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Warning, "rabbitmq-tenant-invalid", message);
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents the tenant context extracted from a message.
    /// </summary>
    public class TenantMessageContext
    {
        /// <summary>
        /// Creates a new tenant message context.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="tenantName">The tenant name.</param>
        public TenantMessageContext(string? tenantId, string? tenantName)
        {
            TenantId = tenantId;
            TenantName = tenantName;
        }

        /// <summary>
        /// Gets the tenant ID.
        /// </summary>
        public string? TenantId { get; }

        /// <summary>
        /// Gets the tenant name.
        /// </summary>
        public string? TenantName { get; }

        /// <summary>
        /// Gets whether a tenant is set.
        /// </summary>
        public bool HasTenant => !string.IsNullOrEmpty(TenantId);
    }
}


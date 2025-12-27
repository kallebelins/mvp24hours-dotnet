//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors
{
    /// <summary>
    /// MongoDB interceptor that automatically sets tenant ID on entities implementing <see cref="ITenantEntity"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interceptor ensures tenant isolation by:
    /// <list type="bullet">
    ///   <item>Automatically setting TenantId on insert for entities implementing <see cref="ITenantEntity"/></item>
    ///   <item>Validating tenant ownership on update/delete operations</item>
    ///   <item>Preventing cross-tenant data access</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Behavior:</strong>
    /// <list type="bullet">
    ///   <item>On insert: Sets TenantId from current tenant provider</item>
    ///   <item>On update: Validates entity belongs to current tenant (throws if not)</item>
    ///   <item>On delete: Validates entity belongs to current tenant (throws if not)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register in DI:
    /// services.AddScoped&lt;ITenantProvider&gt;(sp => new HttpHeaderTenantProvider(sp.GetRequiredService&lt;IHttpContextAccessor&gt;()));
    /// services.AddMongoDbTenantInterceptor();
    /// 
    /// // Or manually:
    /// services.AddScoped&lt;IMongoDbInterceptor, TenantInterceptor&gt;();
    /// 
    /// // Entity example:
    /// public class Invoice : EntityBase&lt;Guid&gt;, ITenantEntity
    /// {
    ///     public string TenantId { get; set; }
    ///     public decimal Amount { get; set; }
    /// }
    /// </code>
    /// </example>
    public class TenantInterceptor : MongoDbInterceptorBase
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly bool _validateOnUpdate;
        private readonly bool _validateOnDelete;
        private readonly bool _throwOnMissingTenant;
        private readonly ILogger<TenantInterceptor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantInterceptor"/> class.
        /// </summary>
        /// <param name="tenantProvider">The tenant provider to get the current tenant ID.</param>
        /// <param name="validateOnUpdate">If true, validates tenant ownership on updates. Default is true.</param>
        /// <param name="validateOnDelete">If true, validates tenant ownership on deletes. Default is true.</param>
        /// <param name="throwOnMissingTenant">If true, throws an exception when no tenant is set. Default is true.</param>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when tenantProvider is null.</exception>
        public TenantInterceptor(
            ITenantProvider tenantProvider,
            bool validateOnUpdate = true,
            bool validateOnDelete = true,
            bool throwOnMissingTenant = true,
            ILogger<TenantInterceptor> logger = null)
        {
            _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
            _validateOnUpdate = validateOnUpdate;
            _validateOnDelete = validateOnDelete;
            _throwOnMissingTenant = throwOnMissingTenant;
            _logger = logger;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Runs early to ensure tenant is set before other interceptors like audit.
        /// </remarks>
        public override int Order => -2000;

        /// <inheritdoc />
        public override Task OnBeforeInsertAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            if (entity is ITenantEntity tenantEntity)
            {
                var currentTenantId = _tenantProvider.TenantId;

                if (_throwOnMissingTenant && string.IsNullOrEmpty(currentTenantId))
                {
                    throw new InvalidOperationException(
                        $"Cannot insert entity of type {typeof(T).Name} without a tenant. " +
                        "Ensure a tenant is set in the current context.");
                }

                // Set the tenant ID
                tenantEntity.TenantId = currentTenantId;

                _logger?.LogDebug(
                    "MongoDB tenant interceptor insert: EntityType={EntityType}, TenantId={TenantId}",
                    typeof(T).Name,
                    currentTenantId);
            }

            // Handle generic ITenantEntity<T>
            SetGenericTenantId(entity);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task OnBeforeUpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            if (_validateOnUpdate && entity is ITenantEntity tenantEntity)
            {
                ValidateTenantOwnership(tenantEntity, typeof(T).Name);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task<DeleteInterceptionResult> OnBeforeDeleteAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            if (_validateOnDelete && entity is ITenantEntity tenantEntity)
            {
                ValidateTenantOwnership(tenantEntity, typeof(T).Name);
            }

            return Task.FromResult(DeleteInterceptionResult.Proceed());
        }

        private void ValidateTenantOwnership(ITenantEntity entity, string entityTypeName)
        {
            var currentTenantId = _tenantProvider.TenantId;

            // If no tenant is set in context and we're not throwing on missing tenant, allow the operation
            if (string.IsNullOrEmpty(currentTenantId) && !_throwOnMissingTenant)
            {
                return;
            }

            if (string.IsNullOrEmpty(currentTenantId))
            {
                throw new InvalidOperationException(
                    $"Cannot modify entity of type {entityTypeName} without a tenant context.");
            }

            // Entity's TenantId must match the current tenant
            if (!string.IsNullOrEmpty(entity.TenantId) && entity.TenantId != currentTenantId)
            {
                _logger?.LogWarning(
                    "MongoDB tenant interceptor access denied: EntityType={EntityType}, EntityTenant={EntityTenant}, CurrentTenant={CurrentTenant}",
                    entityTypeName,
                    entity.TenantId,
                    currentTenantId);

                throw new UnauthorizedAccessException(
                    $"Access denied. Entity belongs to tenant '{entity.TenantId}' but current tenant is '{currentTenantId}'.");
            }
        }

        private void SetGenericTenantId<T>(T entity) where T : class, IEntityBase
        {
            var entityType = entity.GetType();
            var interfaces = entityType.GetInterfaces();

            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ITenantEntity<>))
                {
                    var property = entityType.GetProperty("TenantId");
                    if (property != null && property.CanWrite)
                    {
                        var tenantIdType = property.PropertyType;
                        var currentTenantId = _tenantProvider.TenantId;

                        if (string.IsNullOrEmpty(currentTenantId))
                        {
                            if (_throwOnMissingTenant)
                            {
                                throw new InvalidOperationException(
                                    $"Cannot insert entity of type {typeof(T).Name} without a tenant.");
                            }
                            return;
                        }

                        try
                        {
                            // Convert string tenant ID to the target type
                            object convertedValue;
                            if (tenantIdType == typeof(string))
                            {
                                convertedValue = currentTenantId;
                            }
                            else if (tenantIdType == typeof(Guid))
                            {
                                convertedValue = Guid.Parse(currentTenantId);
                            }
                            else if (tenantIdType == typeof(int))
                            {
                                convertedValue = int.Parse(currentTenantId);
                            }
                            else if (tenantIdType == typeof(long))
                            {
                                convertedValue = long.Parse(currentTenantId);
                            }
                            else
                            {
                                convertedValue = Convert.ChangeType(currentTenantId, tenantIdType);
                            }

                            property.SetValue(entity, convertedValue);

                            _logger?.LogDebug(
                                "MongoDB tenant interceptor generic insert: EntityType={EntityType}, TenantId={TenantId}, TenantIdType={TenantIdType}",
                                typeof(T).Name,
                                currentTenantId,
                                tenantIdType.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex,
                                "MongoDB tenant interceptor conversion error: EntityType={EntityType}, TenantId={TenantId}, TargetType={TargetType}",
                                typeof(T).Name,
                                currentTenantId,
                                tenantIdType.Name);
                        }
                    }
                    break;
                }
            }
        }
    }
}


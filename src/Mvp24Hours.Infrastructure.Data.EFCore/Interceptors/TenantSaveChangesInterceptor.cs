//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Interceptors
{
    /// <summary>
    /// EF Core interceptor that automatically sets the TenantId on new entities implementing ITenantEntity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interceptor ensures data isolation in multi-tenant applications by automatically
    /// setting the TenantId on new entities and validating that modified entities belong to
    /// the current tenant.
    /// </para>
    /// <para>
    /// <strong>Behaviors:</strong>
    /// <list type="bullet">
    /// <item><strong>Added entities:</strong> Sets TenantId to current tenant</item>
    /// <item><strong>Modified entities:</strong> Optionally validates TenantId hasn't changed</item>
    /// <item><strong>Deleted entities:</strong> Optionally validates entity belongs to current tenant</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Important:</strong> This interceptor works in conjunction with global query filters.
    /// You should also configure query filters to restrict queries to the current tenant:
    /// </para>
    /// <code>
    /// // In OnModelCreating:
    /// modelBuilder.ApplyTenantQueryFilters(_tenantProvider);
    /// </code>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register in DbContext configuration:
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     var tenantProvider = sp.GetRequiredService&lt;ITenantProvider&gt;();
    ///     options.UseSqlServer(connectionString)
    ///            .AddInterceptors(new TenantSaveChangesInterceptor(tenantProvider));
    /// });
    /// 
    /// // Or with DI:
    /// services.AddScoped&lt;TenantSaveChangesInterceptor&gt;();
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .AddInterceptors(sp.GetRequiredService&lt;TenantSaveChangesInterceptor&gt;());
    /// });
    /// </code>
    /// </example>
    public class TenantSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly TenantInterceptorOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantSaveChangesInterceptor"/> class.
        /// </summary>
        /// <param name="tenantProvider">The tenant provider for resolving the current tenant.</param>
        /// <param name="options">Optional configuration options.</param>
        /// <exception cref="ArgumentNullException">Thrown when tenantProvider is null.</exception>
        public TenantSaveChangesInterceptor(
            ITenantProvider tenantProvider,
            TenantInterceptorOptions options = null)
        {
            _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
            _options = options ?? new TenantInterceptorOptions();
        }

        /// <inheritdoc />
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            ProcessTenantEntities(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ProcessTenantEntities(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ProcessTenantEntities(DbContext context)
        {
            if (context == null) return;

            var currentTenantId = _tenantProvider.TenantId;

            // If no tenant is set and we require tenant, throw exception
            if (_options.RequireTenant && string.IsNullOrEmpty(currentTenantId))
            {
                var hasTenantEntities = context.ChangeTracker
                    .Entries()
                    .Any(e => e.Entity is ITenantEntity && 
                              (e.State == EntityState.Added || e.State == EntityState.Modified));

                if (hasTenantEntities)
                {
                    throw new InvalidOperationException(
                        "No tenant context is set, but tenant entities are being modified. " +
                        "Ensure ITenantProvider is properly configured.");
                }
                return;
            }

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.Entity is ITenantEntity tenantEntity)
                {
                    ProcessTenantEntity(entry, tenantEntity, currentTenantId);
                }
                else
                {
                    // Try to process generic ITenantEntity<T>
                    TryProcessGenericTenantEntity(entry, currentTenantId);
                }
            }
        }

        private void ProcessTenantEntity(
            EntityEntry entry,
            ITenantEntity entity,
            string currentTenantId)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // Set tenant ID on new entities
                    if (string.IsNullOrEmpty(entity.TenantId))
                    {
                        entity.TenantId = currentTenantId;
                    }
                    else if (_options.ValidateTenantOnAdd && entity.TenantId != currentTenantId)
                    {
                        // Entity has a different tenant ID than current
                        throw new InvalidOperationException(
                            $"Cannot add entity of type {entry.Entity.GetType().Name} with TenantId '{entity.TenantId}' " +
                            $"in context of tenant '{currentTenantId}'.");
                    }
                    break;

                case EntityState.Modified:
                    if (_options.ValidateTenantOnModify)
                    {
                        ValidateTenantId(entry, entity.TenantId, currentTenantId);
                    }
                    
                    // Prevent modification of TenantId
                    if (_options.PreventTenantIdChange)
                    {
                        entry.Property(nameof(ITenantEntity.TenantId)).IsModified = false;
                    }
                    break;

                case EntityState.Deleted:
                    if (_options.ValidateTenantOnDelete)
                    {
                        ValidateTenantId(entry, entity.TenantId, currentTenantId);
                    }
                    break;
            }
        }

        private void TryProcessGenericTenantEntity(EntityEntry entry, string currentTenantId)
        {
            var entityType = entry.Entity.GetType();
            var interfaces = entityType.GetInterfaces();

            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ITenantEntity<>))
                {
                    var tenantIdProperty = entry.Property("TenantId");
                    if (tenantIdProperty == null) continue;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            var currentValue = tenantIdProperty.CurrentValue;
                            if (currentValue == null || IsDefaultValue(currentValue))
                            {
                                // Try to set the tenant ID (string or other types)
                                if (iface.GetGenericArguments()[0] == typeof(string))
                                {
                                    tenantIdProperty.CurrentValue = currentTenantId;
                                }
                                else if (iface.GetGenericArguments()[0] == typeof(Guid) && 
                                         Guid.TryParse(currentTenantId, out var guidTenantId))
                                {
                                    tenantIdProperty.CurrentValue = guidTenantId;
                                }
                                else if (iface.GetGenericArguments()[0] == typeof(int) && 
                                         int.TryParse(currentTenantId, out var intTenantId))
                                {
                                    tenantIdProperty.CurrentValue = intTenantId;
                                }
                            }
                            break;

                        case EntityState.Modified:
                            if (_options.PreventTenantIdChange)
                            {
                                tenantIdProperty.IsModified = false;
                            }
                            break;
                    }
                    break;
                }
            }
        }

        private void ValidateTenantId(EntityEntry entry, string entityTenantId, string currentTenantId)
        {
            if (!string.IsNullOrEmpty(currentTenantId) && 
                !string.IsNullOrEmpty(entityTenantId) && 
                entityTenantId != currentTenantId)
            {
                throw new InvalidOperationException(
                    $"Cannot modify entity of type {entry.Entity.GetType().Name} " +
                    $"with TenantId '{entityTenantId}' in context of tenant '{currentTenantId}'. " +
                    "Cross-tenant data access is not allowed.");
            }
        }

        private static bool IsDefaultValue(object value)
        {
            if (value == null) return true;
            var type = value.GetType();
            if (type.IsValueType)
            {
                return value.Equals(Activator.CreateInstance(type));
            }
            return value is string str && string.IsNullOrEmpty(str);
        }
    }

    /// <summary>
    /// Configuration options for <see cref="TenantSaveChangesInterceptor"/>.
    /// </summary>
    public class TenantInterceptorOptions
    {
        /// <summary>
        /// Gets or sets whether a tenant is required for operations on tenant entities.
        /// Default is true.
        /// </summary>
        public bool RequireTenant { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to validate the tenant ID when adding new entities.
        /// When true, throws if entity has a different TenantId than current tenant.
        /// Default is true.
        /// </summary>
        public bool ValidateTenantOnAdd { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to validate the tenant ID when modifying entities.
        /// When true, throws if entity belongs to a different tenant.
        /// Default is true.
        /// </summary>
        public bool ValidateTenantOnModify { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to validate the tenant ID when deleting entities.
        /// When true, throws if entity belongs to a different tenant.
        /// Default is true.
        /// </summary>
        public bool ValidateTenantOnDelete { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to prevent modification of the TenantId property.
        /// Default is true.
        /// </summary>
        public bool PreventTenantIdChange { get; set; } = true;
    }
}


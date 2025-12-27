//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for configuring multi-tenant global query filters in Entity Framework Core.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions automatically apply query filters to all entities implementing
    /// <see cref="ITenantEntity"/> or <see cref="ITenantEntity{T}"/>, ensuring data isolation
    /// between tenants in a shared database/schema scenario.
    /// </para>
    /// <para>
    /// <strong>Usage in DbContext:</strong>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     base.OnModelCreating(modelBuilder);
    ///     modelBuilder.ApplyTenantQueryFilters(_tenantProvider);
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public static class TenantModelBuilderExtensions
    {
        /// <summary>
        /// Applies global query filters to all entities implementing <see cref="ITenantEntity"/>.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="tenantProvider">The tenant provider to get the current tenant ID.</param>
        /// <returns>The model builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method adds a query filter that ensures queries only return entities
        /// where TenantId matches the current tenant from <paramref name="tenantProvider"/>.
        /// </para>
        /// <para>
        /// <strong>Important:</strong> The tenant provider is captured by reference, so the
        /// filter will always use the current tenant at query execution time.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// public class AppDbContext : DbContext
        /// {
        ///     private readonly ITenantProvider _tenantProvider;
        ///     
        ///     public AppDbContext(DbContextOptions options, ITenantProvider tenantProvider) 
        ///         : base(options)
        ///     {
        ///         _tenantProvider = tenantProvider;
        ///     }
        ///     
        ///     protected override void OnModelCreating(ModelBuilder modelBuilder)
        ///     {
        ///         base.OnModelCreating(modelBuilder);
        ///         modelBuilder.ApplyTenantQueryFilters(_tenantProvider);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static ModelBuilder ApplyTenantQueryFilters(
            this ModelBuilder modelBuilder,
            ITenantProvider tenantProvider)
        {
            if (tenantProvider == null)
            {
                throw new ArgumentNullException(nameof(tenantProvider));
            }

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Check if entity implements ITenantEntity (string TenantId)
                if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                {
                    ApplyTenantFilter(modelBuilder, entityType.ClrType, tenantProvider);
                }
                // Check for generic ITenantEntity<T>
                else if (ImplementsGenericTenantEntity(entityType.ClrType, out var tenantIdType))
                {
                    ApplyGenericTenantFilter(modelBuilder, entityType.ClrType, tenantIdType, tenantProvider);
                }
            }

            return modelBuilder;
        }

        /// <summary>
        /// Applies a global query filter to a specific entity type for tenant filtering.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="tenantProvider">The tenant provider.</param>
        /// <returns>The model builder for chaining.</returns>
        public static ModelBuilder ApplyTenantQueryFilter<TEntity>(
            this ModelBuilder modelBuilder,
            ITenantProvider tenantProvider)
            where TEntity : class, ITenantEntity
        {
            if (tenantProvider == null)
            {
                throw new ArgumentNullException(nameof(tenantProvider));
            }

            modelBuilder.Entity<TEntity>().HasQueryFilter(e => 
                e.TenantId == tenantProvider.TenantId || tenantProvider.TenantId == null);

            return modelBuilder;
        }

        /// <summary>
        /// Applies combined query filters for both tenant isolation and soft delete.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="tenantProvider">The tenant provider.</param>
        /// <returns>The model builder for chaining.</returns>
        /// <remarks>
        /// This method combines tenant filtering with soft delete filtering for entities
        /// that implement both <see cref="ITenantEntity"/> and <see cref="ISoftDeletable"/>.
        /// </remarks>
        public static ModelBuilder ApplyTenantAndSoftDeleteFilters(
            this ModelBuilder modelBuilder,
            ITenantProvider tenantProvider)
        {
            if (tenantProvider == null)
            {
                throw new ArgumentNullException(nameof(tenantProvider));
            }

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;
                var isTenant = typeof(ITenantEntity).IsAssignableFrom(clrType);
                var isSoftDelete = typeof(ISoftDeletable).IsAssignableFrom(clrType);

                if (isTenant && isSoftDelete)
                {
                    ApplyCombinedFilter(modelBuilder, clrType, tenantProvider);
                }
                else if (isTenant)
                {
                    ApplyTenantFilter(modelBuilder, clrType, tenantProvider);
                }
                else if (isSoftDelete)
                {
                    ApplySoftDeleteFilter(modelBuilder, clrType);
                }
            }

            return modelBuilder;
        }

        /// <summary>
        /// Configures the TenantId property for all tenant entities with consistent settings.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="maxLength">Maximum length for the TenantId column. Default is 50.</param>
        /// <param name="isRequired">Whether TenantId is required. Default is true.</param>
        /// <param name="createIndex">Whether to create an index on TenantId. Default is true.</param>
        /// <returns>The model builder for chaining.</returns>
        public static ModelBuilder ConfigureTenantProperties(
            this ModelBuilder modelBuilder,
            int maxLength = 50,
            bool isRequired = true,
            bool createIndex = true)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var entityBuilder = modelBuilder.Entity(entityType.ClrType);
                    
                    entityBuilder.Property(nameof(ITenantEntity.TenantId))
                        .HasMaxLength(maxLength)
                        .IsRequired(isRequired);

                    if (createIndex)
                    {
                        entityBuilder.HasIndex(nameof(ITenantEntity.TenantId))
                            .HasDatabaseName($"IX_{entityType.ClrType.Name}_TenantId");
                    }
                }
            }

            return modelBuilder;
        }

        #region Private Methods

        private static void ApplyTenantFilter(
            ModelBuilder modelBuilder,
            Type entityType,
            ITenantProvider tenantProvider)
        {
            // Build expression: e => e.TenantId == tenantProvider.TenantId || tenantProvider.TenantId == null
            var parameter = Expression.Parameter(entityType, "e");
            var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));

            // Access tenantProvider.TenantId at runtime
            var tenantProviderConstant = Expression.Constant(tenantProvider);
            var currentTenantId = Expression.Property(tenantProviderConstant, nameof(ITenantProvider.TenantId));

            // e.TenantId == tenantProvider.TenantId
            var equalExpression = Expression.Equal(tenantIdProperty, currentTenantId);

            // tenantProvider.TenantId == null (bypass filter when no tenant set)
            var nullConstant = Expression.Constant(null, typeof(string));
            var tenantIsNull = Expression.Equal(currentTenantId, nullConstant);

            // Combine: e.TenantId == tenantProvider.TenantId || tenantProvider.TenantId == null
            var combinedExpression = Expression.OrElse(equalExpression, tenantIsNull);

            var lambda = Expression.Lambda(combinedExpression, parameter);
            modelBuilder.Entity(entityType).HasQueryFilter(lambda);
        }

        private static void ApplyGenericTenantFilter(
            ModelBuilder modelBuilder,
            Type entityType,
            Type tenantIdType,
            ITenantProvider tenantProvider)
        {
            // For generic tenant entities, we create a dynamic filter
            // This is more complex as we need to handle different TenantId types
            
            var parameter = Expression.Parameter(entityType, "e");
            var tenantIdProperty = Expression.Property(parameter, "TenantId");

            if (tenantIdType == typeof(string))
            {
                // Same as non-generic case
                var tenantProviderConstant = Expression.Constant(tenantProvider);
                var currentTenantId = Expression.Property(tenantProviderConstant, nameof(ITenantProvider.TenantId));
                var equalExpression = Expression.Equal(tenantIdProperty, currentTenantId);
                var nullConstant = Expression.Constant(null, typeof(string));
                var tenantIsNull = Expression.Equal(currentTenantId, nullConstant);
                var combinedExpression = Expression.OrElse(equalExpression, tenantIsNull);
                var lambda = Expression.Lambda(combinedExpression, parameter);
                modelBuilder.Entity(entityType).HasQueryFilter(lambda);
            }
            else if (tenantIdType == typeof(Guid))
            {
                // For Guid tenant IDs, use EF.Property for comparison
                var methodCall = CreateEfPropertyCall(entityType, tenantIdType, "TenantId", parameter);
                
                // Create method to convert string to Guid at runtime
                var tenantProviderConstant = Expression.Constant(tenantProvider);
                var currentTenantIdString = Expression.Property(tenantProviderConstant, nameof(ITenantProvider.TenantId));
                
                // Convert string to Guid using Guid.Parse wrapped in a null check
                var parseMethod = typeof(TenantModelBuilderExtensions).GetMethod(
                    nameof(ParseGuidOrDefault), 
                    BindingFlags.NonPublic | BindingFlags.Static);
                var parsedGuid = Expression.Call(parseMethod, currentTenantIdString);
                
                var equalExpression = Expression.Equal(methodCall, parsedGuid);
                
                // Check if tenant string is null
                var nullConstant = Expression.Constant(null, typeof(string));
                var tenantIsNull = Expression.Equal(currentTenantIdString, nullConstant);
                
                var combinedExpression = Expression.OrElse(equalExpression, tenantIsNull);
                var lambda = Expression.Lambda(combinedExpression, parameter);
                modelBuilder.Entity(entityType).HasQueryFilter(lambda);
            }
            else if (tenantIdType == typeof(int))
            {
                // For int tenant IDs
                var methodCall = CreateEfPropertyCall(entityType, tenantIdType, "TenantId", parameter);
                
                var tenantProviderConstant = Expression.Constant(tenantProvider);
                var currentTenantIdString = Expression.Property(tenantProviderConstant, nameof(ITenantProvider.TenantId));
                
                var parseMethod = typeof(TenantModelBuilderExtensions).GetMethod(
                    nameof(ParseIntOrDefault), 
                    BindingFlags.NonPublic | BindingFlags.Static);
                var parsedInt = Expression.Call(parseMethod, currentTenantIdString);
                
                var equalExpression = Expression.Equal(methodCall, parsedInt);
                
                var nullConstant = Expression.Constant(null, typeof(string));
                var tenantIsNull = Expression.Equal(currentTenantIdString, nullConstant);
                
                var combinedExpression = Expression.OrElse(equalExpression, tenantIsNull);
                var lambda = Expression.Lambda(combinedExpression, parameter);
                modelBuilder.Entity(entityType).HasQueryFilter(lambda);
            }
        }

        private static void ApplyCombinedFilter(
            ModelBuilder modelBuilder,
            Type entityType,
            ITenantProvider tenantProvider)
        {
            var parameter = Expression.Parameter(entityType, "e");
            
            // Tenant filter: e.TenantId == tenantProvider.TenantId || tenantProvider.TenantId == null
            var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
            var tenantProviderConstant = Expression.Constant(tenantProvider);
            var currentTenantId = Expression.Property(tenantProviderConstant, nameof(ITenantProvider.TenantId));
            var tenantEqual = Expression.Equal(tenantIdProperty, currentTenantId);
            var nullConstant = Expression.Constant(null, typeof(string));
            var tenantIsNull = Expression.Equal(currentTenantId, nullConstant);
            var tenantFilter = Expression.OrElse(tenantEqual, tenantIsNull);

            // Soft delete filter: !e.IsDeleted
            var isDeletedProperty = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var falseConstant = Expression.Constant(false);
            var softDeleteFilter = Expression.Equal(isDeletedProperty, falseConstant);

            // Combine: tenantFilter && softDeleteFilter
            var combinedFilter = Expression.AndAlso(tenantFilter, softDeleteFilter);

            var lambda = Expression.Lambda(combinedFilter, parameter);
            modelBuilder.Entity(entityType).HasQueryFilter(lambda);
        }

        private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder, Type entityType)
        {
            var parameter = Expression.Parameter(entityType, "e");
            var isDeletedProperty = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var falseConstant = Expression.Constant(false);
            var filter = Expression.Equal(isDeletedProperty, falseConstant);
            var lambda = Expression.Lambda(filter, parameter);
            modelBuilder.Entity(entityType).HasQueryFilter(lambda);
        }

        private static bool ImplementsGenericTenantEntity(Type type, out Type tenantIdType)
        {
            tenantIdType = null;
            
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ITenantEntity<>))
                {
                    tenantIdType = iface.GetGenericArguments()[0];
                    return true;
                }
            }
            
            return false;
        }

        private static Expression CreateEfPropertyCall(
            Type entityType, 
            Type propertyType, 
            string propertyName,
            ParameterExpression parameter)
        {
            var efPropertyMethod = typeof(EF)
                .GetMethod(nameof(EF.Property))
                .MakeGenericMethod(propertyType);

            return Expression.Call(
                efPropertyMethod,
                parameter,
                Expression.Constant(propertyName));
        }

        private static Guid ParseGuidOrDefault(string value)
        {
            if (string.IsNullOrEmpty(value)) return Guid.Empty;
            return Guid.TryParse(value, out var result) ? result : Guid.Empty;
        }

        private static int ParseIntOrDefault(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            return int.TryParse(value, out var result) ? result : 0;
        }

        #endregion
    }
}


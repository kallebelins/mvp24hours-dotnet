//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Security
{
    /// <summary>
    /// Provides row-level security helpers for MongoDB, implementing automatic
    /// data filtering based on tenant and other security attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Row-Level Security (RLS) ensures that users can only access data they are authorized to see.
    /// This is implemented via aggregation pipeline $match stages that are automatically prepended
    /// to queries.
    /// </para>
    /// <para>
    /// <strong>Security Model:</strong>
    /// <code>
    /// ┌─────────────────────────────────────────────────────────────────────┐
    /// │                    Row-Level Security Filters                        │
    /// ├─────────────────────────────────────────────────────────────────────┤
    /// │  Tenant Filter:     TenantId == current_tenant                      │
    /// │  Soft Delete:       IsDeleted == false                              │
    /// │  Data Owner:        OwnerId == current_user (optional)              │
    /// │  Department:        DepartmentId IN user_departments (optional)     │
    /// │  Custom:            User-defined predicates                         │
    /// └─────────────────────────────────────────────────────────────────────┘
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create RLS helper:
    /// var rls = new MongoDbRowLevelSecurity(tenantProvider, currentUserProvider, logger);
    /// 
    /// // Get filtered collection:
    /// var filter = rls.CreateSecurityFilter&lt;Invoice&gt;();
    /// var results = await collection.Find(filter).ToListAsync();
    /// 
    /// // Apply to aggregation pipeline:
    /// var pipeline = collection.Aggregate()
    ///     .Match(rls.CreateSecurityFilter&lt;Invoice&gt;())
    ///     .Group(...);
    /// </code>
    /// </example>
    public class MongoDbRowLevelSecurity
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly IRowLevelSecurityPolicy _defaultPolicy;
        private readonly ILogger<MongoDbRowLevelSecurity> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbRowLevelSecurity"/> class.
        /// </summary>
        /// <param name="tenantProvider">The tenant provider for multi-tenancy filtering.</param>
        /// <param name="currentUserProvider">The current user provider for ownership filtering.</param>
        /// <param name="logger">Optional logger for structured logging.</param>
        /// <param name="defaultPolicy">Optional default security policy.</param>
        public MongoDbRowLevelSecurity(
            ITenantProvider tenantProvider = null,
            ICurrentUserProvider currentUserProvider = null,
            ILogger<MongoDbRowLevelSecurity> logger = null,
            IRowLevelSecurityPolicy defaultPolicy = null)
        {
            _tenantProvider = tenantProvider;
            _currentUserProvider = currentUserProvider;
            _logger = logger;
            _defaultPolicy = defaultPolicy;
        }

        /// <summary>
        /// Creates a security filter definition for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>A filter definition that enforces row-level security.</returns>
        public FilterDefinition<T> CreateSecurityFilter<T>() where T : class
        {
            var filters = new List<FilterDefinition<T>>();

            // Tenant filter
            if (typeof(ITenantEntity).IsAssignableFrom(typeof(T)) && _tenantProvider?.HasTenant == true)
            {
                var tenantFilter = Builders<T>.Filter.Eq(
                    nameof(ITenantEntity.TenantId),
                    _tenantProvider.TenantId);
                filters.Add(tenantFilter);

                _logger?.LogDebug("Applied tenant filter for entity {EntityType} with TenantId {TenantId}",
                    typeof(T).Name, _tenantProvider.TenantId);
            }

            // Soft delete filter
            if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
            {
                var softDeleteFilter = Builders<T>.Filter.Eq(
                    nameof(ISoftDeletable.IsDeleted),
                    false);
                filters.Add(softDeleteFilter);
            }

            // Apply default policy
            if (_defaultPolicy != null)
            {
                var policyFilter = _defaultPolicy.CreateFilter<T>(_tenantProvider, _currentUserProvider);
                if (policyFilter != null)
                {
                    filters.Add(policyFilter);
                }
            }

            if (filters.Count == 0)
            {
                return Builders<T>.Filter.Empty;
            }

            return Builders<T>.Filter.And(filters);
        }

        /// <summary>
        /// Creates a security filter with additional custom filters.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="additionalFilters">Additional filters to apply.</param>
        /// <returns>A combined filter definition.</returns>
        public FilterDefinition<T> CreateSecurityFilter<T>(
            params FilterDefinition<T>[] additionalFilters)
            where T : class
        {
            var securityFilter = CreateSecurityFilter<T>();

            if (additionalFilters == null || additionalFilters.Length == 0)
            {
                return securityFilter;
            }

            var allFilters = new List<FilterDefinition<T>> { securityFilter };
            allFilters.AddRange(additionalFilters.Where(f => f != null));

            return Builders<T>.Filter.And(allFilters);
        }

        /// <summary>
        /// Creates a BSON document representing the security filter for use in aggregation pipelines.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>A BSON document for the $match stage.</returns>
        public BsonDocument CreateSecurityMatchStage<T>() where T : class
        {
            var filter = CreateSecurityFilter<T>();
            var serializerRegistry = MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<T>();
            
            return filter.Render(documentSerializer, serializerRegistry);
        }

        /// <summary>
        /// Wraps an existing filter with security constraints.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="filter">The existing filter.</param>
        /// <returns>A filter with security constraints applied.</returns>
        public FilterDefinition<T> WrapWithSecurity<T>(FilterDefinition<T> filter) where T : class
        {
            var securityFilter = CreateSecurityFilter<T>();
            return Builders<T>.Filter.And(securityFilter, filter);
        }

        /// <summary>
        /// Wraps an expression filter with security constraints.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="predicate">The filter predicate.</param>
        /// <returns>A filter with security constraints applied.</returns>
        public FilterDefinition<T> WrapWithSecurity<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            var filter = Builders<T>.Filter.Where(predicate);
            return WrapWithSecurity(filter);
        }

        /// <summary>
        /// Creates a pipeline definition that starts with security filters.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <returns>An aggregate fluent interface with security match stage.</returns>
        public IAggregateFluent<T> CreateSecureAggregate<T>(IMongoCollection<T> collection) where T : class
        {
            var securityFilter = CreateSecurityFilter<T>();
            return collection.Aggregate().Match(securityFilter);
        }

        /// <summary>
        /// Validates that an entity passes security checks before modification.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entity">The entity to validate.</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when entity fails security validation.</exception>
        public void ValidateEntityAccess<T>(T entity) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            // Validate tenant access
            if (entity is ITenantEntity tenantEntity && _tenantProvider?.HasTenant == true)
            {
                if (!string.IsNullOrEmpty(tenantEntity.TenantId) &&
                    tenantEntity.TenantId != _tenantProvider.TenantId)
                {
                    throw new UnauthorizedAccessException(
                        $"Access denied. Entity belongs to tenant '{tenantEntity.TenantId}' " +
                        $"but current tenant is '{_tenantProvider.TenantId}'.");
                }
            }

            // Validate soft delete
            if (entity is ISoftDeletable softDeletable && softDeletable.IsDeleted)
            {
                throw new InvalidOperationException(
                    "Cannot access a soft-deleted entity.");
            }

            // Apply custom policy validation
            _defaultPolicy?.ValidateAccess(entity, _tenantProvider, _currentUserProvider);
        }
    }

    /// <summary>
    /// Defines a custom row-level security policy.
    /// </summary>
    public interface IRowLevelSecurityPolicy
    {
        /// <summary>
        /// Creates a filter definition based on the security policy.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="tenantProvider">The tenant provider.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <returns>A filter definition, or null if no filter applies.</returns>
        FilterDefinition<T> CreateFilter<T>(
            ITenantProvider tenantProvider,
            ICurrentUserProvider currentUserProvider) where T : class;

        /// <summary>
        /// Validates access to an entity.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entity">The entity to validate.</param>
        /// <param name="tenantProvider">The tenant provider.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when access is denied.</exception>
        void ValidateAccess<T>(
            T entity,
            ITenantProvider tenantProvider,
            ICurrentUserProvider currentUserProvider) where T : class;
    }

    /// <summary>
    /// A security policy that filters entities by owner (CreatedBy or OwnerId).
    /// </summary>
    public class OwnerBasedSecurityPolicy : IRowLevelSecurityPolicy
    {
        private readonly string _ownerFieldName;
        private readonly Func<ICurrentUserProvider, bool> _isAdminCheck;

        /// <summary>
        /// Initializes a new instance of the <see cref="OwnerBasedSecurityPolicy"/> class.
        /// </summary>
        /// <param name="ownerFieldName">The name of the owner field (default: "CreatedBy").</param>
        /// <param name="isAdminCheck">Optional function to check if current user is admin. If null, no admin bypass.</param>
        public OwnerBasedSecurityPolicy(
            string ownerFieldName = "CreatedBy",
            Func<ICurrentUserProvider, bool> isAdminCheck = null)
        {
            _ownerFieldName = ownerFieldName;
            _isAdminCheck = isAdminCheck;
        }

        /// <inheritdoc />
        public FilterDefinition<T> CreateFilter<T>(
            ITenantProvider tenantProvider,
            ICurrentUserProvider currentUserProvider) where T : class
        {
            // Skip if user is admin and bypass is allowed
            if (_isAdminCheck != null && currentUserProvider != null && _isAdminCheck(currentUserProvider))
            {
                return null;
            }

            // Skip if no user context
            if (currentUserProvider == null || string.IsNullOrEmpty(currentUserProvider.UserId))
            {
                return null;
            }

            // Check if entity has the owner field
            var entityType = typeof(T);
            var ownerProperty = entityType.GetProperty(_ownerFieldName);
            if (ownerProperty == null)
            {
                return null;
            }

            return Builders<T>.Filter.Eq(_ownerFieldName, currentUserProvider.UserId);
        }

        /// <inheritdoc />
        public void ValidateAccess<T>(
            T entity,
            ITenantProvider tenantProvider,
            ICurrentUserProvider currentUserProvider) where T : class
        {
            // Skip if user is admin
            if (_isAdminCheck != null && currentUserProvider != null && _isAdminCheck(currentUserProvider))
            {
                return;
            }

            // Skip if no user context
            if (currentUserProvider == null || string.IsNullOrEmpty(currentUserProvider.UserId))
            {
                return;
            }

            var entityType = typeof(T);
            var ownerProperty = entityType.GetProperty(_ownerFieldName);
            if (ownerProperty == null)
            {
                return;
            }

            var ownerValue = ownerProperty.GetValue(entity)?.ToString();
            if (!string.IsNullOrEmpty(ownerValue) && ownerValue != currentUserProvider.UserId)
            {
                throw new UnauthorizedAccessException(
                    $"Access denied. Entity is owned by '{ownerValue}' but current user is '{currentUserProvider.UserId}'.");
            }
        }
    }

    /// <summary>
    /// A composite security policy that combines multiple policies.
    /// </summary>
    public class CompositeSecurityPolicy : IRowLevelSecurityPolicy
    {
        private readonly IList<IRowLevelSecurityPolicy> _policies;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeSecurityPolicy"/> class.
        /// </summary>
        /// <param name="policies">The policies to combine.</param>
        public CompositeSecurityPolicy(params IRowLevelSecurityPolicy[] policies)
        {
            _policies = policies?.ToList() ?? new List<IRowLevelSecurityPolicy>();
        }

        /// <summary>
        /// Adds a policy to the composite.
        /// </summary>
        /// <param name="policy">The policy to add.</param>
        /// <returns>This instance for chaining.</returns>
        public CompositeSecurityPolicy AddPolicy(IRowLevelSecurityPolicy policy)
        {
            if (policy != null)
            {
                _policies.Add(policy);
            }
            return this;
        }

        /// <inheritdoc />
        public FilterDefinition<T> CreateFilter<T>(
            ITenantProvider tenantProvider,
            ICurrentUserProvider currentUserProvider) where T : class
        {
            var filters = new List<FilterDefinition<T>>();

            foreach (var policy in _policies)
            {
                var filter = policy.CreateFilter<T>(tenantProvider, currentUserProvider);
                if (filter != null)
                {
                    filters.Add(filter);
                }
            }

            if (filters.Count == 0)
            {
                return null;
            }

            return Builders<T>.Filter.And(filters);
        }

        /// <inheritdoc />
        public void ValidateAccess<T>(
            T entity,
            ITenantProvider tenantProvider,
            ICurrentUserProvider currentUserProvider) where T : class
        {
            foreach (var policy in _policies)
            {
                policy.ValidateAccess(entity, tenantProvider, currentUserProvider);
            }
        }
    }

    /// <summary>
    /// Extension methods for applying row-level security to MongoDB operations.
    /// </summary>
    public static class RowLevelSecurityExtensions
    {
        /// <summary>
        /// Applies row-level security filters to a find operation.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="rls">The row-level security helper.</param>
        /// <param name="additionalFilter">Optional additional filter.</param>
        /// <returns>A find fluent interface with security applied.</returns>
        public static IFindFluent<T, T> SecureFind<T>(
            this IMongoCollection<T> collection,
            MongoDbRowLevelSecurity rls,
            FilterDefinition<T> additionalFilter = null)
            where T : class
        {
            var filter = additionalFilter != null
                ? rls.WrapWithSecurity(additionalFilter)
                : rls.CreateSecurityFilter<T>();

            return collection.Find(filter);
        }

        /// <summary>
        /// Applies row-level security filters to a find operation using an expression.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="rls">The row-level security helper.</param>
        /// <param name="predicate">The filter predicate.</param>
        /// <returns>A find fluent interface with security applied.</returns>
        public static IFindFluent<T, T> SecureFind<T>(
            this IMongoCollection<T> collection,
            MongoDbRowLevelSecurity rls,
            Expression<Func<T, bool>> predicate)
            where T : class
        {
            var filter = rls.WrapWithSecurity(predicate);
            return collection.Find(filter);
        }

        /// <summary>
        /// Creates a secure aggregation pipeline with RLS match stage.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="rls">The row-level security helper.</param>
        /// <returns>An aggregate fluent interface with security applied.</returns>
        public static IAggregateFluent<T> SecureAggregate<T>(
            this IMongoCollection<T> collection,
            MongoDbRowLevelSecurity rls)
            where T : class
        {
            return rls.CreateSecureAggregate(collection);
        }

        /// <summary>
        /// Counts documents with row-level security applied.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="rls">The row-level security helper.</param>
        /// <param name="additionalFilter">Optional additional filter.</param>
        /// <returns>The count of matching documents.</returns>
        public static long SecureCount<T>(
            this IMongoCollection<T> collection,
            MongoDbRowLevelSecurity rls,
            FilterDefinition<T> additionalFilter = null)
            where T : class
        {
            var filter = additionalFilter != null
                ? rls.WrapWithSecurity(additionalFilter)
                : rls.CreateSecurityFilter<T>();

            return collection.CountDocuments(filter);
        }
    }
}


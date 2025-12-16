//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Testing;

/// <summary>
/// In-memory fake implementation of <see cref="IRepository{T}"/> for unit testing.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This class provides a simple in-memory implementation of the repository pattern
/// for unit testing scenarios where you don't want database dependencies.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item>No database dependencies</item>
/// <item>Fast execution</item>
/// <item>Configurable behavior</item>
/// <item>Support for all IRepository operations</item>
/// </list>
/// </para>
/// <para>
/// <strong>Limitations:</strong>
/// <list type="bullet">
/// <item>No transaction support</item>
/// <item>No relationship loading (LoadRelation)</item>
/// <item>Simple LINQ-to-Objects evaluation</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a fake repository
/// var repository = new RepositoryFake&lt;Customer&gt;();
/// 
/// // Add some test data
/// repository.Add(new Customer { Id = 1, Name = "Test Customer", Active = true });
/// repository.CommitChanges();
/// 
/// // Use in tests
/// var customer = repository.GetById(1);
/// Assert.NotNull(customer);
/// Assert.Equal("Test Customer", customer.Name);
/// 
/// // Test queries
/// var activeCustomers = repository.GetBy(c => c.Active);
/// Assert.Single(activeCustomers);
/// </code>
/// </example>
public class RepositoryFake<TEntity> : IRepository<TEntity>, ICommitChanges, IDisposable
    where TEntity : class, IEntityBase
{
    private readonly List<TEntity> _entities = [];
    private readonly List<TEntity> _pendingAdds = [];
    private readonly List<TEntity> _pendingModifies = [];
    private readonly List<TEntity> _pendingRemoves = [];
    private readonly Func<TEntity, object> _keySelector;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance with the default key selector.
    /// </summary>
    /// <remarks>
    /// Uses the EntityKey property from IEntityBase as the key selector.
    /// </remarks>
    public RepositoryFake()
        : this(e => e.EntityKey)
    {
    }

    /// <summary>
    /// Initializes a new instance with a custom key selector.
    /// </summary>
    /// <param name="keySelector">Function to extract the entity key.</param>
    /// <example>
    /// <code>
    /// var repository = new RepositoryFake&lt;Customer&gt;(c => c.Id);
    /// </code>
    /// </example>
    public RepositoryFake(Func<TEntity, object> keySelector)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    /// <summary>
    /// Initializes a new instance with initial data.
    /// </summary>
    /// <param name="initialData">Initial entities to populate the repository.</param>
    /// <example>
    /// <code>
    /// var repository = new RepositoryFake&lt;Customer&gt;(new[]
    /// {
    ///     new Customer { Id = 1, Name = "Customer 1" },
    ///     new Customer { Id = 2, Name = "Customer 2" }
    /// });
    /// </code>
    /// </example>
    public RepositoryFake(IEnumerable<TEntity> initialData)
        : this()
    {
        if (initialData != null)
        {
            _entities.AddRange(initialData);
        }
    }

    /// <summary>
    /// Initializes a new instance with initial data and a custom key selector.
    /// </summary>
    /// <param name="initialData">Initial entities to populate the repository.</param>
    /// <param name="keySelector">Function to extract the entity key.</param>
    public RepositoryFake(IEnumerable<TEntity> initialData, Func<TEntity, object> keySelector)
        : this(keySelector)
    {
        if (initialData != null)
        {
            _entities.AddRange(initialData);
        }
    }

    /// <summary>
    /// Gets all entities in the repository (including committed and pending).
    /// </summary>
    public IReadOnlyList<TEntity> AllEntities => _entities.ToList().AsReadOnly();

    /// <summary>
    /// Gets entities pending to be added.
    /// </summary>
    public IReadOnlyList<TEntity> PendingAdds => _pendingAdds.AsReadOnly();

    /// <summary>
    /// Gets entities pending to be modified.
    /// </summary>
    public IReadOnlyList<TEntity> PendingModifies => _pendingModifies.AsReadOnly();

    /// <summary>
    /// Gets entities pending to be removed.
    /// </summary>
    public IReadOnlyList<TEntity> PendingRemoves => _pendingRemoves.AsReadOnly();

    #region IQuery Implementation

    /// <inheritdoc />
    public bool ListAny()
    {
        return _entities.Any();
    }

    /// <inheritdoc />
    public int ListCount()
    {
        return _entities.Count;
    }

    /// <inheritdoc />
    public IList<TEntity> List()
    {
        return List(null);
    }

    /// <inheritdoc />
    public IList<TEntity> List(IPagingCriteria? criteria)
    {
        var query = _entities.AsQueryable();
        return ApplyCriteria(query, criteria).ToList();
    }

    /// <inheritdoc />
    public bool GetByAny(Expression<Func<TEntity, bool>> clause)
    {
        var query = _entities.AsQueryable();
        if (clause != null)
        {
            query = query.Where(clause);
        }
        return query.Any();
    }

    /// <inheritdoc />
    public int GetByCount(Expression<Func<TEntity, bool>> clause)
    {
        var query = _entities.AsQueryable();
        if (clause != null)
        {
            query = query.Where(clause);
        }
        return query.Count();
    }

    /// <inheritdoc />
    public IList<TEntity> GetBy(Expression<Func<TEntity, bool>> clause)
    {
        return GetBy(clause, null);
    }

    /// <inheritdoc />
    public IList<TEntity> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria)
    {
        var query = _entities.AsQueryable();
        if (clause != null)
        {
            query = query.Where(clause);
        }
        return ApplyCriteria(query, criteria).ToList();
    }

    /// <inheritdoc />
    public TEntity? GetById(object id)
    {
        return GetById(id, null);
    }

    /// <inheritdoc />
    public TEntity? GetById(object id, IPagingCriteria? criteria)
    {
        return _entities.FirstOrDefault(e => Equals(_keySelector(e), id));
    }

    #endregion

    #region IQueryRelation Implementation

    /// <inheritdoc />
    public void LoadRelation<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> propertyExpression)
        where TProperty : class
    {
        // No-op in fake - relationships would need to be set up manually
    }

    /// <inheritdoc />
    public void LoadRelation<TProperty>(
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
        Expression<Func<TProperty, bool>>? clause = null,
        int limit = 0)
        where TProperty : class
    {
        // No-op in fake - relationships would need to be set up manually
    }

    /// <inheritdoc />
    public void LoadRelationSortByAscending<TProperty, TKey>(
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
        Expression<Func<TProperty, TKey>> orderKey,
        Expression<Func<TProperty, bool>>? clause = null,
        int limit = 0)
        where TProperty : class
    {
        // No-op in fake
    }

    /// <inheritdoc />
    public void LoadRelationSortByDescending<TProperty, TKey>(
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
        Expression<Func<TProperty, TKey>> orderKey,
        Expression<Func<TProperty, bool>>? clause = null,
        int limit = 0)
        where TProperty : class
    {
        // No-op in fake
    }

    #endregion

    #region ICommand Implementation

    /// <inheritdoc />
    public void Add(TEntity entity)
    {
        if (entity == null) return;
        _pendingAdds.Add(entity);
    }

    /// <inheritdoc />
    public void Add(IList<TEntity> entities)
    {
        if (entities.AnySafe())
        {
            foreach (var entity in entities)
            {
                Add(entity);
            }
        }
    }

    /// <inheritdoc />
    public void Modify(TEntity entity)
    {
        if (entity == null) return;
        _pendingModifies.Add(entity);
    }

    /// <inheritdoc />
    public void Modify(IList<TEntity> entities)
    {
        if (entities.AnySafe())
        {
            foreach (var entity in entities)
            {
                Modify(entity);
            }
        }
    }

    /// <inheritdoc />
    public void Remove(TEntity entity)
    {
        if (entity == null) return;
        _pendingRemoves.Add(entity);
    }

    /// <inheritdoc />
    public void Remove(IList<TEntity> entities)
    {
        if (entities.AnySafe())
        {
            foreach (var entity in entities)
            {
                Remove(entity);
            }
        }
    }

    /// <inheritdoc />
    public void RemoveById(object id)
    {
        var entity = GetById(id);
        if (entity != null)
        {
            Remove(entity);
        }
    }

    /// <inheritdoc />
    public void RemoveById(IList<object> ids)
    {
        if (ids.AnySafe())
        {
            foreach (var id in ids)
            {
                RemoveById(id);
            }
        }
    }

    #endregion

    #region Fake-Specific Methods

    /// <summary>
    /// Commits all pending changes (simulates SaveChanges).
    /// </summary>
    /// <returns>The number of state entries written.</returns>
    /// <remarks>
    /// This method processes all pending adds, modifies, and removes,
    /// updating the internal entity list accordingly.
    /// </remarks>
    /// <example>
    /// <code>
    /// repository.Add(new Customer { Id = 1, Name = "Test" });
    /// var changes = repository.CommitChanges();
    /// Assert.Equal(1, changes);
    /// </code>
    /// </example>
    public int CommitChanges()
    {
        var changeCount = 0;

        // Process adds
        foreach (var entity in _pendingAdds)
        {
            _entities.Add(entity);
            changeCount++;
        }
        _pendingAdds.Clear();

        // Process modifies
        foreach (var modifiedEntity in _pendingModifies)
        {
            var key = _keySelector(modifiedEntity);
            var existingIndex = _entities.FindIndex(e => Equals(_keySelector(e), key));
            if (existingIndex >= 0)
            {
                _entities[existingIndex] = modifiedEntity;
                changeCount++;
            }
        }
        _pendingModifies.Clear();

        // Process removes
        foreach (var entity in _pendingRemoves)
        {
            var key = _keySelector(entity);
            var existingEntity = _entities.FirstOrDefault(e => Equals(_keySelector(e), key));
            if (existingEntity != null)
            {
                _entities.Remove(existingEntity);
                changeCount++;
            }
        }
        _pendingRemoves.Clear();

        return changeCount;
    }

    /// <summary>
    /// Clears all entities from the repository.
    /// </summary>
    public void Clear()
    {
        _entities.Clear();
        _pendingAdds.Clear();
        _pendingModifies.Clear();
        _pendingRemoves.Clear();
    }

    /// <summary>
    /// Resets pending changes without committing them.
    /// </summary>
    public void ResetPendingChanges()
    {
        _pendingAdds.Clear();
        _pendingModifies.Clear();
        _pendingRemoves.Clear();
    }

    /// <summary>
    /// Seeds the repository with initial data.
    /// </summary>
    /// <param name="entities">The entities to seed.</param>
    public void SeedData(IEnumerable<TEntity> entities)
    {
        if (entities != null)
        {
            _entities.AddRange(entities);
        }
    }

    /// <summary>
    /// Seeds the repository with data using a seeder.
    /// </summary>
    /// <param name="seedAction">Action to generate seed data.</param>
    /// <example>
    /// <code>
    /// repository.SeedData(list =>
    /// {
    ///     for (int i = 1; i <= 10; i++)
    ///     {
    ///         list.Add(new Customer { Id = i, Name = $"Customer {i}" });
    ///     }
    /// });
    /// </code>
    /// </example>
    public void SeedData(Action<List<TEntity>> seedAction)
    {
        ArgumentNullException.ThrowIfNull(seedAction);
        var entities = new List<TEntity>();
        seedAction(entities);
        _entities.AddRange(entities);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Applies paging criteria to the query.
    /// </summary>
    private static IQueryable<TEntity> ApplyCriteria(IQueryable<TEntity> query, IPagingCriteria? criteria)
    {
        if (criteria == null) return query;

        // Apply ordering
        if (criteria.OrderBy?.Count > 0)
        {
            var isFirst = true;
            foreach (var orderClause in criteria.OrderBy)
            {
                var parts = orderClause.Split(' ');
                var propertyName = parts[0];
                var isDescending = parts.Length > 1 && 
                    parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

                var parameter = Expression.Parameter(typeof(TEntity), "e");
                var property = Expression.PropertyOrField(parameter, propertyName);
                var lambda = Expression.Lambda(property, parameter);

                var methodName = isFirst
                    ? (isDescending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy))
                    : (isDescending ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy));

                var method = typeof(Queryable).GetMethods()
                    .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(TEntity), property.Type);

                query = (IQueryable<TEntity>)method.Invoke(null, [query, lambda])!;
                isFirst = false;
            }
        }

        // Apply paging
        if (criteria.Offset > 0)
        {
            query = query.Skip(criteria.Offset);
        }

        if (criteria.Limit > 0)
        {
            query = query.Take(criteria.Limit);
        }

        return query;
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            Clear();
        }

        _disposed = true;
    }

    #endregion
}


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
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Testing;

/// <summary>
/// In-memory fake implementation of <see cref="IRepositoryAsync{T}"/> for unit testing.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This class provides an async in-memory implementation of the repository pattern
/// for unit testing scenarios where you don't want database dependencies.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a fake repository
/// var repository = new RepositoryFakeAsync&lt;Customer&gt;();
/// 
/// // Add some test data
/// await repository.AddAsync(new Customer { Id = 1, Name = "Test Customer", Active = true });
/// await repository.CommitChangesAsync();
/// 
/// // Use in tests
/// var customer = await repository.GetByIdAsync(1);
/// Assert.NotNull(customer);
/// Assert.Equal("Test Customer", customer.Name);
/// </code>
/// </example>
public class RepositoryFakeAsync<TEntity> : IRepositoryAsync<TEntity>, ICommitChangesAsync, IDisposable
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
    public RepositoryFakeAsync()
        : this(e => e.EntityKey)
    {
    }

    /// <summary>
    /// Initializes a new instance with a custom key selector.
    /// </summary>
    /// <param name="keySelector">Function to extract the entity key.</param>
    public RepositoryFakeAsync(Func<TEntity, object> keySelector)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    /// <summary>
    /// Initializes a new instance with initial data.
    /// </summary>
    /// <param name="initialData">Initial entities to populate the repository.</param>
    public RepositoryFakeAsync(IEnumerable<TEntity> initialData)
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
    public RepositoryFakeAsync(IEnumerable<TEntity> initialData, Func<TEntity, object> keySelector)
        : this(keySelector)
    {
        if (initialData != null)
        {
            _entities.AddRange(initialData);
        }
    }

    /// <summary>
    /// Gets all entities in the repository.
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

    #region IQueryAsync Implementation

    /// <inheritdoc />
    public Task<bool> ListAnyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_entities.Any());
    }

    /// <inheritdoc />
    public Task<int> ListCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_entities.Count);
    }

    /// <inheritdoc />
    public Task<IList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return ListAsync(null, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IList<TEntity>> ListAsync(IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        var query = _entities.AsQueryable();
        var result = ApplyCriteria(query, criteria).ToList();
        return Task.FromResult<IList<TEntity>>(result);
    }

    /// <inheritdoc />
    public Task<bool> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
    {
        var query = _entities.AsQueryable();
        if (clause != null)
        {
            query = query.Where(clause);
        }
        return Task.FromResult(query.Any());
    }

    /// <inheritdoc />
    public Task<int> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
    {
        var query = _entities.AsQueryable();
        if (clause != null)
        {
            query = query.Where(clause);
        }
        return Task.FromResult(query.Count());
    }

    /// <inheritdoc />
    public Task<IList<TEntity>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
    {
        return GetByAsync(clause, null, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IList<TEntity>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        var query = _entities.AsQueryable();
        if (clause != null)
        {
            query = query.Where(clause);
        }
        var result = ApplyCriteria(query, criteria).ToList();
        return Task.FromResult<IList<TEntity>>(result);
    }

    /// <inheritdoc />
    public Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return GetByIdAsync(id, null, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TEntity?> GetByIdAsync(object id, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        var entity = _entities.FirstOrDefault(e => Equals(_keySelector(e), id));
        return Task.FromResult(entity);
    }

    #endregion

    #region IQueryRelationAsync Implementation

    /// <inheritdoc />
    public Task LoadRelationAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> propertyExpression, CancellationToken cancellationToken = default)
        where TProperty : class
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LoadRelationAsync<TProperty>(
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
        Expression<Func<TProperty, bool>>? clause = null,
        int limit = 0,
        CancellationToken cancellationToken = default)
        where TProperty : class
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LoadRelationSortByAscendingAsync<TProperty, TKey>(
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
        Expression<Func<TProperty, TKey>> orderKey,
        Expression<Func<TProperty, bool>>? clause = null,
        int limit = 0,
        CancellationToken cancellationToken = default)
        where TProperty : class
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LoadRelationSortByDescendingAsync<TProperty, TKey>(
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
        Expression<Func<TProperty, TKey>> orderKey,
        Expression<Func<TProperty, bool>>? clause = null,
        int limit = 0,
        CancellationToken cancellationToken = default)
        where TProperty : class
    {
        return Task.CompletedTask;
    }

    #endregion

    #region ICommandAsync Implementation

    /// <inheritdoc />
    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity != null)
        {
            _pendingAdds.Add(entity);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AddAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (entities.AnySafe())
        {
            foreach (var entity in entities)
            {
                _pendingAdds.Add(entity);
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ModifyAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity != null)
        {
            _pendingModifies.Add(entity);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ModifyAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (entities.AnySafe())
        {
            foreach (var entity in entities)
            {
                _pendingModifies.Add(entity);
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity != null)
        {
            _pendingRemoves.Add(entity);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (entities.AnySafe())
        {
            foreach (var entity in entities)
            {
                _pendingRemoves.Add(entity);
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await RemoveAsync(entity, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default)
    {
        if (ids.AnySafe())
        {
            foreach (var id in ids)
            {
                await RemoveByIdAsync(id, cancellationToken);
            }
        }
    }

    #endregion

    #region Fake-Specific Methods

    /// <summary>
    /// Commits all pending changes asynchronously (simulates SaveChangesAsync).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    public Task<int> CommitChangesAsync(CancellationToken cancellationToken = default)
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

        return Task.FromResult(changeCount);
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

    #endregion

    #region Private Methods

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


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Mvp24Hours.Infrastructure.Cqrs.Projections;

/// <summary>
/// In-memory implementation of <see cref="IReadModelRepository{T}"/>.
/// Useful for testing and development environments.
/// </summary>
/// <typeparam name="T">The read model type.</typeparam>
/// <remarks>
/// <para>
/// <strong>Warning:</strong> This implementation stores data in memory
/// and will lose all data when the application restarts.
/// For production, use a persistent implementation (SQL, MongoDB, etc.).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // For testing
/// var repository = new InMemoryReadModelRepository&lt;OrderSummary&gt;();
/// 
/// // Use in projection handler
/// await repository.UpsertAsync(new OrderSummary { Id = orderId, ... });
/// 
/// // Query
/// var orders = await repository.FindAsync(o => o.Status == "Placed");
/// </code>
/// </example>
public class InMemoryReadModelRepository<T> : IAdvancedReadModelRepository<T> where T : class
{
    private readonly ConcurrentDictionary<object, T> _store = new();
    private readonly Func<T, object> _keySelector;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance with automatic key detection.
    /// Looks for properties named "Id" or "{TypeName}Id".
    /// </summary>
    public InMemoryReadModelRepository()
    {
        _keySelector = CreateDefaultKeySelector();
    }

    /// <summary>
    /// Initializes a new instance with a custom key selector.
    /// </summary>
    /// <param name="keySelector">Function to extract the key from an entity.</param>
    public InMemoryReadModelRepository(Func<T, object> keySelector)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    #region [ Query Operations ]

    /// <inheritdoc />
    public Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default)
    {
        if (id == null) return Task.FromResult<T?>(null);

        _store.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => GetByIdAsync<string>(id, cancellationToken);

    /// <inheritdoc />
    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => GetByIdAsync<Guid>(id, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var compiled = predicate.Compile();
        var result = _store.Values.Where(compiled).ToList();
        return Task.FromResult<IReadOnlyList<T>>(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var compiled = predicate.Compile();
        var result = _store.Values
            .Where(compiled)
            .Skip(skip)
            .Take(take)
            .ToList();
        return Task.FromResult<IReadOnlyList<T>>(result);
    }

    /// <inheritdoc />
    public Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var compiled = predicate.Compile();
        var result = _store.Values.FirstOrDefault(compiled);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<T>>(_store.Values.ToList());
    }

    /// <inheritdoc />
    public Task<long> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        if (predicate == null)
        {
            return Task.FromResult<long>(_store.Count);
        }

        var compiled = predicate.Compile();
        return Task.FromResult<long>(_store.Values.Count(compiled));
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var compiled = predicate.Compile();
        return Task.FromResult(_store.Values.Any(compiled));
    }

    /// <inheritdoc />
    public IQueryable<T> AsQueryable()
    {
        return _store.Values.AsQueryable();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<T>> FindOrderedAsync<TKey>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TKey>> orderBy,
        bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var compiledPredicate = predicate.Compile();
        var compiledOrderBy = orderBy.Compile();

        var filtered = _store.Values.Where(compiledPredicate);
        var ordered = descending
            ? filtered.OrderByDescending(compiledOrderBy)
            : filtered.OrderBy(compiledOrderBy);

        return Task.FromResult<IReadOnlyList<T>>(ordered.ToList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<T>> FindOrderedAsync<TKey>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TKey>> orderBy,
        int skip,
        int take,
        bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var compiledPredicate = predicate.Compile();
        var compiledOrderBy = orderBy.Compile();

        var filtered = _store.Values.Where(compiledPredicate);
        var ordered = descending
            ? filtered.OrderByDescending(compiledOrderBy)
            : filtered.OrderBy(compiledOrderBy);

        return Task.FromResult<IReadOnlyList<T>>(ordered.Skip(skip).Take(take).ToList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TResult>> ProjectAsync<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var compiledPredicate = predicate.Compile();
        var compiledSelector = selector.Compile();

        var result = _store.Values
            .Where(compiledPredicate)
            .Select(compiledSelector)
            .ToList();

        return Task.FromResult<IReadOnlyList<TResult>>(result);
    }

    #endregion

    #region [ Write Operations ]

    /// <inheritdoc />
    public Task InsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var key = _keySelector(entity);
        if (!_store.TryAdd(key, entity))
        {
            throw new InvalidOperationException($"Entity with key '{key}' already exists.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var key = _keySelector(entity);
        if (!_store.ContainsKey(key))
        {
            throw new InvalidOperationException($"Entity with key '{key}' not found.");
        }

        _store[key] = entity;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var key = _keySelector(entity);
        _store[key] = entity;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync<TId>(TId id, CancellationToken cancellationToken = default)
    {
        if (id != null)
        {
            _store.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> DeleteAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var compiled = predicate.Compile();
        var toDelete = _store.Where(kvp => compiled(kvp.Value)).Select(kvp => kvp.Key).ToList();

        foreach (var key in toDelete)
        {
            _store.TryRemove(key, out _);
        }

        return Task.FromResult<long>(toDelete.Count);
    }

    #endregion

    #region [ Bulk Operations ]

    /// <inheritdoc />
    public Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        _store.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task BulkInsertAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (var entity in entities)
        {
            var key = _keySelector(entity);
            _store.TryAdd(key, entity);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task BulkUpsertAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        foreach (var entity in entities)
        {
            var key = _keySelector(entity);
            _store[key] = entity;
        }

        return Task.CompletedTask;
    }

    #endregion

    #region [ Helpers ]

    /// <summary>
    /// Gets the total count of items in the store.
    /// </summary>
    public int Count => _store.Count;

    /// <summary>
    /// Clears all data from the store.
    /// </summary>
    public void Clear() => _store.Clear();

    private static Func<T, object> CreateDefaultKeySelector()
    {
        var type = typeof(T);
        
        // Look for "Id" property
        var idProperty = type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        
        // Look for "{TypeName}Id" property
        if (idProperty == null)
        {
            idProperty = type.GetProperty($"{type.Name}Id", BindingFlags.Public | BindingFlags.Instance);
        }

        if (idProperty == null)
        {
            throw new InvalidOperationException(
                $"Cannot find Id property on type {type.Name}. " +
                "Please provide a custom key selector.");
        }

        return entity =>
        {
            var value = idProperty.GetValue(entity);
            return value ?? throw new InvalidOperationException(
                $"Id property on entity of type {type.Name} returned null.");
        };
    }

    #endregion
}



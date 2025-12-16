//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Testing;

/// <summary>
/// In-memory fake implementation of <see cref="IUnitOfWork"/> for MongoDB unit testing.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a simple in-memory implementation of the Unit of Work pattern
/// for unit testing scenarios where you don't want database dependencies.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item>No MongoDB dependencies</item>
/// <item>Fast execution</item>
/// <item>Repository tracking</item>
/// <item>Simulated commit/rollback</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a fake unit of work
/// var unitOfWork = new MongoUnitOfWorkFake();
/// 
/// // Get repository
/// var repository = unitOfWork.GetRepository&lt;Customer&gt;();
/// 
/// // Add data
/// repository.Add(new Customer { Id = ObjectId.GenerateNewId(), Name = "Test" });
/// 
/// // Commit
/// unitOfWork.SaveChanges();
/// </code>
/// </example>
public class MongoUnitOfWorkFake : IUnitOfWork, IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private bool _disposed;

    /// <summary>
    /// Gets a repository for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>A fake repository instance.</returns>
    public IRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class, IEntityBase
    {
        return (IRepository<TEntity>)_repositories.GetOrAdd(
            typeof(TEntity),
            _ => new MongoRepositoryFake<TEntity>());
    }

    /// <summary>
    /// Saves all pending changes across all repositories.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total number of state entries written.</returns>
    public int SaveChanges(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            Rollback();
            return 0;
        }

        var totalChanges = 0;

        foreach (var repo in _repositories.Values)
        {
            if (repo is MongoRepositoryFake<IEntityBase> typedRepo)
            {
                totalChanges += typedRepo.CommitChanges();
            }
            else
            {
                // Use reflection for other entity types
                var commitMethod = repo.GetType().GetMethod("CommitChanges");
                if (commitMethod != null)
                {
                    var result = commitMethod.Invoke(repo, null);
                    if (result is int changes)
                    {
                        totalChanges += changes;
                    }
                }
            }
        }

        return totalChanges;
    }

    /// <summary>
    /// Rolls back all pending changes across all repositories.
    /// </summary>
    public void Rollback()
    {
        foreach (var repo in _repositories.Values)
        {
            var resetMethod = repo.GetType().GetMethod("ResetPendingChanges");
            resetMethod?.Invoke(repo, null);
        }
    }

    /// <summary>
    /// Clears all repositories.
    /// </summary>
    public void Clear()
    {
        foreach (var repo in _repositories.Values)
        {
            var clearMethod = repo.GetType().GetMethod("Clear");
            clearMethod?.Invoke(repo, null);
        }
        _repositories.Clear();
    }

    /// <summary>
    /// Gets the number of tracked repositories.
    /// </summary>
    public int RepositoryCount => _repositories.Count;

    /// <summary>
    /// Gets the underlying database connection.
    /// </summary>
    /// <returns>Always returns null for fake implementation.</returns>
    /// <remarks>
    /// MongoDB uses a different connection model than SQL databases.
    /// This method exists for interface compatibility only.
    /// </remarks>
    public IDbConnection GetConnection()
    {
        // MongoDB doesn't use IDbConnection - return null for fake
        return null!;
    }

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
            foreach (var repo in _repositories.Values)
            {
                if (repo is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _repositories.Clear();
        }

        _disposed = true;
    }
}

/// <summary>
/// Async in-memory fake implementation of <see cref="IUnitOfWorkAsync"/> for MongoDB unit testing.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a simple in-memory implementation of the async Unit of Work pattern
/// for unit testing scenarios where you don't want database dependencies.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a fake unit of work
/// var unitOfWork = new MongoUnitOfWorkFakeAsync();
/// 
/// // Get repository
/// var repository = unitOfWork.GetRepository&lt;Customer&gt;();
/// 
/// // Add data
/// await repository.AddAsync(new Customer { Id = ObjectId.GenerateNewId(), Name = "Test" });
/// 
/// // Commit
/// await unitOfWork.SaveChangesAsync();
/// </code>
/// </example>
public class MongoUnitOfWorkFakeAsync : IUnitOfWorkAsync, IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private bool _disposed;

    /// <summary>
    /// Gets an async repository for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>A fake async repository instance.</returns>
    public IRepositoryAsync<TEntity> GetRepository<TEntity>()
        where TEntity : class, IEntityBase
    {
        return (IRepositoryAsync<TEntity>)_repositories.GetOrAdd(
            typeof(TEntity),
            _ => new MongoRepositoryFakeAsync<TEntity>());
    }

    /// <summary>
    /// Saves all pending changes across all repositories asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total number of state entries written.</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var totalChanges = 0;

        foreach (var repo in _repositories.Values)
        {
            // Use reflection for async commit
            var commitMethod = repo.GetType().GetMethod("CommitChangesAsync");
            if (commitMethod != null)
            {
                var task = (Task<int>?)commitMethod.Invoke(repo, [cancellationToken]);
                if (task != null)
                {
                    totalChanges += await task;
                }
            }
        }

        return totalChanges;
    }

    /// <summary>
    /// Rolls back all pending changes across all repositories asynchronously.
    /// </summary>
    public Task RollbackAsync()
    {
        foreach (var repo in _repositories.Values)
        {
            var resetMethod = repo.GetType().GetMethod("ResetPendingChanges");
            resetMethod?.Invoke(repo, null);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all repositories.
    /// </summary>
    public void Clear()
    {
        foreach (var repo in _repositories.Values)
        {
            var clearMethod = repo.GetType().GetMethod("Clear");
            clearMethod?.Invoke(repo, null);
        }
        _repositories.Clear();
    }

    /// <summary>
    /// Gets the number of tracked repositories.
    /// </summary>
    public int RepositoryCount => _repositories.Count;

    /// <summary>
    /// Gets the underlying database connection.
    /// </summary>
    /// <returns>Always returns null for fake implementation.</returns>
    /// <remarks>
    /// MongoDB uses a different connection model than SQL databases.
    /// This method exists for interface compatibility only.
    /// </remarks>
    public IDbConnection GetConnection()
    {
        // MongoDB doesn't use IDbConnection - return null for fake
        return null!;
    }

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
            foreach (var repo in _repositories.Values)
            {
                if (repo is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _repositories.Clear();
        }

        _disposed = true;
    }
}


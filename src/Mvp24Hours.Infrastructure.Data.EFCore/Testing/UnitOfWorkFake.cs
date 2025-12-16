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

namespace Mvp24Hours.Infrastructure.Data.EFCore.Testing;

/// <summary>
/// In-memory fake implementation of <see cref="IUnitOfWork"/> for unit testing.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a simple in-memory implementation of the Unit of Work pattern
/// for unit testing scenarios where you don't want database dependencies.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item>No database dependencies</item>
/// <item>Automatic repository registration</item>
/// <item>SaveChanges tracking</item>
/// <item>Rollback support</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a fake unit of work
/// var unitOfWork = new UnitOfWorkFake();
/// 
/// // Get repositories
/// var customerRepo = unitOfWork.GetRepository&lt;Customer&gt;();
/// var orderRepo = unitOfWork.GetRepository&lt;Order&gt;();
/// 
/// // Perform operations
/// customerRepo.Add(new Customer { Id = 1, Name = "Test" });
/// orderRepo.Add(new Order { Id = 1, CustomerId = 1 });
/// 
/// // Save changes
/// unitOfWork.SaveChanges();
/// 
/// // Verify
/// Assert.Equal(1, unitOfWork.SaveChangesCallCount);
/// </code>
/// </example>
public class UnitOfWorkFake : IUnitOfWork, IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private bool _disposed;

    /// <summary>
    /// Gets the number of times SaveChanges was called.
    /// </summary>
    public int SaveChangesCallCount { get; private set; }

    /// <summary>
    /// Gets the total number of entities affected by SaveChanges.
    /// </summary>
    public int TotalChanges { get; private set; }

    /// <summary>
    /// Gets or sets whether SaveChanges should throw an exception.
    /// Useful for testing error scenarios.
    /// </summary>
    public Exception? SaveChangesException { get; set; }

    /// <summary>
    /// Gets or sets the result to return from SaveChanges.
    /// If null, returns the actual change count.
    /// </summary>
    public int? SaveChangesResult { get; set; }

    /// <inheritdoc />
    public IRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class, IEntityBase
    {
        return (IRepository<TEntity>)_repositories.GetOrAdd(
            typeof(TEntity),
            _ => new RepositoryFake<TEntity>());
    }

    /// <summary>
    /// Gets a typed repository for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>The fake repository for the entity type.</returns>
    public RepositoryFake<TEntity> GetFakeRepository<TEntity>()
        where TEntity : class, IEntityBase
    {
        return (RepositoryFake<TEntity>)GetRepository<TEntity>();
    }

    /// <summary>
    /// Registers a pre-configured repository for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <example>
    /// <code>
    /// var customerRepo = new RepositoryFake&lt;Customer&gt;(new[]
    /// {
    ///     new Customer { Id = 1, Name = "Customer 1" }
    /// });
    /// unitOfWork.RegisterRepository(customerRepo);
    /// </code>
    /// </example>
    public void RegisterRepository<TEntity>(RepositoryFake<TEntity> repository)
        where TEntity : class, IEntityBase
    {
        _repositories[typeof(TEntity)] = repository 
            ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public int SaveChanges(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (SaveChangesException != null)
        {
            throw SaveChangesException;
        }

        SaveChangesCallCount++;

        if (SaveChangesResult.HasValue)
        {
            TotalChanges += SaveChangesResult.Value;
            return SaveChangesResult.Value;
        }

        var changeCount = 0;
        foreach (var repository in _repositories.Values)
        {
            if (repository is ICommitChanges commitChanges)
            {
                changeCount += commitChanges.CommitChanges();
            }
        }

        TotalChanges += changeCount;
        return changeCount;
    }

    /// <inheritdoc />
    public void Rollback()
    {
        foreach (var repository in _repositories.Values)
        {
            if (repository is RepositoryFake<IEntityBase> fake)
            {
                fake.ResetPendingChanges();
            }
        }
        RollbackCallCount++;
    }

    /// <summary>
    /// Gets the number of times Rollback was called.
    /// </summary>
    public int RollbackCallCount { get; private set; }

    /// <inheritdoc />
    public IDbConnection GetConnection()
    {
        // Return null as there's no actual database connection in the fake
        return null!;
    }

    /// <summary>
    /// Resets the call count and total changes counters.
    /// </summary>
    public void ResetCounters()
    {
        SaveChangesCallCount = 0;
        TotalChanges = 0;
        RollbackCallCount = 0;
    }

    /// <summary>
    /// Clears all repositories and their data.
    /// </summary>
    public void Clear()
    {
        foreach (var repository in _repositories.Values)
        {
            if (repository is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _repositories.Clear();
        ResetCounters();
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
            Clear();
        }

        _disposed = true;
    }
}

/// <summary>
/// In-memory fake implementation of <see cref="IUnitOfWorkAsync"/> for unit testing.
/// </summary>
/// <remarks>
/// <para>
/// This class provides an async in-memory implementation of the Unit of Work pattern
/// for unit testing scenarios where you don't want database dependencies.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a fake unit of work
/// var unitOfWork = new UnitOfWorkFakeAsync();
/// 
/// // Get repositories
/// var customerRepo = unitOfWork.GetRepository&lt;Customer&gt;();
/// 
/// // Perform operations
/// await customerRepo.AddAsync(new Customer { Id = 1, Name = "Test" });
/// 
/// // Save changes
/// await unitOfWork.SaveChangesAsync();
/// 
/// // Verify
/// Assert.Equal(1, unitOfWork.SaveChangesCallCount);
/// </code>
/// </example>
public class UnitOfWorkFakeAsync : IUnitOfWorkAsync, IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private bool _disposed;

    /// <summary>
    /// Gets the number of times SaveChangesAsync was called.
    /// </summary>
    public int SaveChangesCallCount { get; private set; }

    /// <summary>
    /// Gets the total number of entities affected by SaveChangesAsync.
    /// </summary>
    public int TotalChanges { get; private set; }

    /// <summary>
    /// Gets or sets whether SaveChangesAsync should throw an exception.
    /// </summary>
    public Exception? SaveChangesException { get; set; }

    /// <summary>
    /// Gets or sets the result to return from SaveChangesAsync.
    /// </summary>
    public int? SaveChangesResult { get; set; }

    /// <summary>
    /// Gets or sets a delay to simulate slow database operations.
    /// </summary>
    public TimeSpan SaveChangesDelay { get; set; } = TimeSpan.Zero;

    /// <inheritdoc />
    public IRepositoryAsync<TEntity> GetRepository<TEntity>()
        where TEntity : class, IEntityBase
    {
        return (IRepositoryAsync<TEntity>)_repositories.GetOrAdd(
            typeof(TEntity),
            _ => new RepositoryFakeAsync<TEntity>());
    }

    /// <summary>
    /// Gets a typed repository for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>The fake repository for the entity type.</returns>
    public RepositoryFakeAsync<TEntity> GetFakeRepository<TEntity>()
        where TEntity : class, IEntityBase
    {
        return (RepositoryFakeAsync<TEntity>)GetRepository<TEntity>();
    }

    /// <summary>
    /// Registers a pre-configured repository for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    public void RegisterRepository<TEntity>(RepositoryFakeAsync<TEntity> repository)
        where TEntity : class, IEntityBase
    {
        _repositories[typeof(TEntity)] = repository 
            ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (SaveChangesDelay > TimeSpan.Zero)
        {
            await Task.Delay(SaveChangesDelay, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (SaveChangesException != null)
        {
            throw SaveChangesException;
        }

        SaveChangesCallCount++;

        if (SaveChangesResult.HasValue)
        {
            TotalChanges += SaveChangesResult.Value;
            return SaveChangesResult.Value;
        }

        var changeCount = 0;
        foreach (var repository in _repositories.Values)
        {
            if (repository is ICommitChangesAsync commitChanges)
            {
                changeCount += await commitChanges.CommitChangesAsync(cancellationToken);
            }
        }

        TotalChanges += changeCount;
        return changeCount;
    }

    /// <inheritdoc />
    public Task RollbackAsync()
    {
        foreach (var repository in _repositories.Values)
        {
            if (repository is RepositoryFakeAsync<IEntityBase> fake)
            {
                fake.ResetPendingChanges();
            }
        }
        RollbackCallCount++;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the number of times RollbackAsync was called.
    /// </summary>
    public int RollbackCallCount { get; private set; }

    /// <inheritdoc />
    public IDbConnection GetConnection()
    {
        // Return null as there's no actual database connection in the fake
        return null!;
    }

    /// <summary>
    /// Resets the call count and total changes counters.
    /// </summary>
    public void ResetCounters()
    {
        SaveChangesCallCount = 0;
        TotalChanges = 0;
        RollbackCallCount = 0;
    }

    /// <summary>
    /// Clears all repositories and their data.
    /// </summary>
    public void Clear()
    {
        foreach (var repository in _repositories.Values)
        {
            if (repository is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _repositories.Clear();
        ResetCounters();
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
            Clear();
        }

        _disposed = true;
    }
}

/// <summary>
/// Interface for committing changes in fake repositories.
/// </summary>
internal interface ICommitChanges
{
    /// <summary>
    /// Commits pending changes.
    /// </summary>
    /// <returns>The number of changes committed.</returns>
    int CommitChanges();
}

/// <summary>
/// Interface for committing changes asynchronously in fake repositories.
/// </summary>
internal interface ICommitChangesAsync
{
    /// <summary>
    /// Commits pending changes asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of changes committed.</returns>
    Task<int> CommitChangesAsync(CancellationToken cancellationToken = default);
}


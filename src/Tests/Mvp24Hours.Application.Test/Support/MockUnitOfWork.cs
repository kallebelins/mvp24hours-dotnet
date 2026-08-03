//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;

namespace Mvp24Hours.Application.Test.Support;

/// <summary>
/// Mock implementation of IUnitOfWorkAsync for unit testing.
/// </summary>
public class MockUnitOfWorkAsync : IUnitOfWorkAsync
{
    private bool _throwOnSaveChanges;
    private int _saveChangesResult = 1;

    public bool SaveChangesCalled { get; private set; }
    public bool RollbackCalled { get; private set; }

    public void SetThrowOnSaveChanges(bool shouldThrow = true)
    {
        _throwOnSaveChanges = shouldThrow;
    }

    public void SetSaveChangesResult(int result)
    {
        _saveChangesResult = result;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        if (_throwOnSaveChanges)
        {
            throw new InvalidOperationException("Simulated database error");
        }
        return Task.FromResult(_saveChangesResult);
    }

    public Task RollbackAsync()
    {
        RollbackCalled = true;
        return Task.CompletedTask;
    }

    public IRepositoryAsync<T> GetRepository<T>() where T : class, IEntityBase
    {
        // For transaction tests, we don't need actual repository functionality
        return null!;
    }

    public IDbConnection GetConnection()
    {
        return null!;
    }

    public void Dispose() { }

    public void Reset()
    {
        SaveChangesCalled = false;
        RollbackCalled = false;
        _throwOnSaveChanges = false;
        _saveChangesResult = 1;
    }
}

/// <summary>
/// Mock implementation of IUnitOfWork for synchronous unit testing.
/// </summary>
public class MockUnitOfWork : IUnitOfWork
{
    private bool _throwOnSaveChanges;
    private int _saveChangesResult = 1;

    public bool SaveChangesCalled { get; private set; }
    public bool RollbackCalled { get; private set; }

    public void SetThrowOnSaveChanges(bool shouldThrow = true)
    {
        _throwOnSaveChanges = shouldThrow;
    }

    public void SetSaveChangesResult(int result)
    {
        _saveChangesResult = result;
    }

    public int SaveChanges(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        if (_throwOnSaveChanges)
        {
            throw new InvalidOperationException("Simulated database error");
        }
        return _saveChangesResult;
    }

    public void Rollback()
    {
        RollbackCalled = true;
    }

    public IRepository<T> GetRepository<T>() where T : class, IEntityBase
    {
        return null!;
    }

    public IDbConnection GetConnection()
    {
        return null!;
    }

    public void Dispose() { }

    public void Reset()
    {
        SaveChangesCalled = false;
        RollbackCalled = false;
        _throwOnSaveChanges = false;
        _saveChangesResult = 1;
    }
}

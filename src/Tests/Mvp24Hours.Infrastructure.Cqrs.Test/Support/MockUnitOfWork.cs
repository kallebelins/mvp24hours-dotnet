//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Data;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Support;

/// <summary>
/// Mock implementation of IUnitOfWorkAsync for testing.
/// </summary>
public class MockUnitOfWorkAsync : IUnitOfWorkAsync
{
    private bool _disposed;

    public List<string> OperationsLog { get; } = [];
    public int SaveChangesCallCount { get; private set; }
    public int RollbackCallCount { get; private set; }
    public bool ShouldThrowOnSave { get; set; }
    public Exception? ExceptionToThrow { get; set; }
    public int RowsAffected { get; set; } = 1;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ShouldThrowOnSave)
        {
            throw ExceptionToThrow ?? new InvalidOperationException("SaveChanges failed");
        }

        SaveChangesCallCount++;
        OperationsLog.Add("SaveChanges");
        return Task.FromResult(RowsAffected);
    }

    public Task RollbackAsync()
    {
        RollbackCallCount++;
        OperationsLog.Add("Rollback");
        return Task.CompletedTask;
    }

    public IRepositoryAsync<T> GetRepository<T>() where T : class, IEntityBase
    {
        OperationsLog.Add($"GetRepository<{typeof(T).Name}>");
        throw new NotImplementedException("Use mock for specific repository tests");
    }

    public IDbConnection GetConnection()
    {
        OperationsLog.Add("GetConnection");
        throw new NotImplementedException("Use mock for connection tests");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            OperationsLog.Add("Dispose");
            _disposed = true;
        }
    }

    public void Reset()
    {
        SaveChangesCallCount = 0;
        RollbackCallCount = 0;
        ShouldThrowOnSave = false;
        ExceptionToThrow = null;
        RowsAffected = 1;
        OperationsLog.Clear();
    }
}


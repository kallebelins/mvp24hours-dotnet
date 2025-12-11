//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using System.Data;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Support;

/// <summary>
/// Mock implementation of IUnitOfWorkAsync for testing.
/// </summary>
public class MockUnitOfWorkAsync : IUnitOfWorkAsync
{
    private bool _disposed;
    private readonly List<string> _operationsLog = new();
    
    public List<string> OperationsLog => _operationsLog;
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
        _operationsLog.Add("SaveChanges");
        return Task.FromResult(RowsAffected);
    }

    public Task RollbackAsync()
    {
        RollbackCallCount++;
        _operationsLog.Add("Rollback");
        return Task.CompletedTask;
    }

    public IRepositoryAsync<T> GetRepository<T>() where T : class, IEntityBase
    {
        _operationsLog.Add($"GetRepository<{typeof(T).Name}>");
        throw new NotImplementedException("Use mock for specific repository tests");
    }

    public IDbConnection GetConnection()
    {
        _operationsLog.Add("GetConnection");
        throw new NotImplementedException("Use mock for connection tests");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _operationsLog.Add("Dispose");
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
        _operationsLog.Clear();
    }
}


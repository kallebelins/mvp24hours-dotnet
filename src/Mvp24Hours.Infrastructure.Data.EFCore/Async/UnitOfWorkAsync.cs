//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;

namespace Mvp24Hours.Infrastructure.Data.EFCore;

/// <summary>
///  <see cref="Mvp24Hours.Core.Contract.Data.IUnitOfWorkAsync"/>
/// </summary>
public class UnitOfWorkAsync : IUnitOfWorkAsync
{
    #region [ Ctor ]
    public UnitOfWorkAsync(DbContext _dbContext, Dictionary<Type, object> _repositories)
    {
        DbContext = _dbContext ?? throw new ArgumentNullException(nameof(_dbContext));
        repositories = _repositories ?? throw new ArgumentNullException(nameof(_repositories));
    }

    [ActivatorUtilitiesConstructor]
    public UnitOfWorkAsync(DbContext _dbContext, IServiceProvider _serviceProvider)
    {
        DbContext = _dbContext ?? throw new ArgumentNullException(nameof(_dbContext));
        serviceProvider = _serviceProvider ?? throw new ArgumentNullException(nameof(_serviceProvider));
        repositories = [];
    }

    #endregion

    #region [ Properties ]

    protected DbContext? DbContext { get; private set; }
    private readonly Dictionary<Type, object> repositories;
    private readonly IServiceProvider? serviceProvider;

    public IRepositoryAsync<T> GetRepository<T>()
        where T : class, IEntityBase
    {
        if (!repositories.ContainsKey(typeof(T)))
        {
            if (serviceProvider is null)
            {
                throw new InvalidOperationException("This UnitOfWork instance was created without a service provider; repositories must be supplied explicitly.");
            }
            IRepositoryAsync<T> repo = serviceProvider.GetService<IRepositoryAsync<T>>()
                ?? throw new InvalidOperationException($"Repository for type {typeof(T).Name} is not registered.");
            repositories.Add(typeof(T), repo);
        }
        return (IRepositoryAsync<T>)repositories[typeof(T)];
    }

    public IDbConnection GetConnection()
    {
        return DbContext?.Database?.GetDbConnection()
            ?? throw new InvalidOperationException("DbContext is not available.");
    }

    #endregion

    #region [ IDisposable ]

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing
            && DbContext != null)
        {
            DbContext.Dispose();
        }
    }

    #endregion

    #region [ Unit of Work ]

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            ArgumentNullException.ThrowIfNull(DbContext);
            return await DbContext.SaveChangesAsync(cancellationToken);
        }
        await RollbackAsync();
        return await Task.FromResult(0);
    }
    public async Task RollbackAsync()
    {
        ArgumentNullException.ThrowIfNull(DbContext);
        var changedEntries = DbContext.ChangeTracker.Entries()
        .Where(x => x.State != EntityState.Unchanged).ToList();

        foreach (EntityEntry? entry in changedEntries)
        {
            switch (entry.State)
            {
                case EntityState.Modified:
                    entry.CurrentValues.SetValues(entry.OriginalValues);
                    entry.State = EntityState.Unchanged;
                    break;
                case EntityState.Added:
                    entry.State = EntityState.Detached;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Unchanged;
                    break;
            }
        }
        await Task.CompletedTask;
    }

    #endregion
}

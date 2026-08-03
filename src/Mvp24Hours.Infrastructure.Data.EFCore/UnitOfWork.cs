//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;

namespace Mvp24Hours.Infrastructure.Data.EFCore;

/// <summary>
///  <see cref="Mvp24Hours.Core.Contract.Data.IUnitOfWork"/>
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    #region [ Ctor ]
    public UnitOfWork(DbContext _dbContext, Dictionary<Type, object> _repositories, ILogger<UnitOfWork>? logger = null)
    {
        DbContext = _dbContext ?? throw new ArgumentNullException(nameof(_dbContext));
        this._repositories = _repositories ?? throw new ArgumentNullException(nameof(_repositories));
        _logger = logger;
    }

    [ActivatorUtilitiesConstructor]
    public UnitOfWork(DbContext _dbContext, IServiceProvider _serviceProvider, ILogger<UnitOfWork>? logger = null)
    {
        DbContext = _dbContext ?? throw new ArgumentNullException(nameof(_dbContext));
        this._serviceProvider = _serviceProvider ?? throw new ArgumentNullException(nameof(_serviceProvider));
        _repositories = [];
        _logger = logger;
    }

    #endregion

    #region [ Properties ]

    protected DbContext DbContext { get; private set; }
    private readonly IServiceProvider? _serviceProvider;
    private readonly ILogger<UnitOfWork>? _logger;

    private readonly Dictionary<Type, object> _repositories;

    public IRepository<T> GetRepository<T>()
        where T : class, IEntityBase
    {
        if (!_repositories.ContainsKey(typeof(T)))
        {
            if (_serviceProvider is null)
            {
                throw new InvalidOperationException("This UnitOfWork instance was created without a service provider; repositories must be supplied explicitly.");
            }
            IRepository<T> repository = _serviceProvider.GetService<IRepository<T>>()
                ?? throw new InvalidOperationException($"Repository for type {typeof(T).Name} is not registered.");
            _repositories.Add(typeof(T), repository);
        }
        return (IRepository<T>)_repositories[typeof(T)];
    }

    public IDbConnection GetConnection()
    {
        return DbContext?.Database?.GetDbConnection()!;
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

    /// <summary>
    ///  <see cref="IUnitOfWork.SaveChanges()"/>
    /// </summary>
    public int SaveChanges(CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("UnitOfWork: SaveChanges started");
        try
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                return DbContext.SaveChanges();
            }
            Rollback();
            return default;
        }
        finally { _logger?.LogDebug("UnitOfWork: SaveChanges finished"); }
    }

    /// <summary>
    ///  <see cref="Mvp24Hours.Core.Contract.Data.IUnitOfWork.Rollback()"/>
    /// </summary>
    public void Rollback()
    {
        _logger?.LogDebug("UnitOfWork: Rollback started");
        try
        {
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
        }
        finally { _logger?.LogDebug("UnitOfWork: Rollback finished"); }
    }

    #endregion
}

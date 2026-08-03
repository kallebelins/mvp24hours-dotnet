//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;

namespace Mvp24Hours.Infrastructure.Data.MongoDb;

/// <summary>
///  <see cref="Mvp24Hours.Core.Contract.Data.IUnitOfWork"/>
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    #region [ Ctor ]

    public UnitOfWork(Mvp24HoursContext dbContext, Dictionary<Type, object> repositories, ILogger<UnitOfWork>? logger = null)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this._repositories = repositories ?? throw new ArgumentNullException(nameof(repositories));
        _logger = logger;

        DbContext.StartSession();
    }

    [ActivatorUtilitiesConstructor]
    public UnitOfWork(Mvp24HoursContext dbContext, IServiceProvider serviceProvider, ILogger<UnitOfWork>? logger = null)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _repositories = [];
        _logger = logger;

        DbContext.StartSession();
    }

    #endregion

    #region [ Properties ]

    private readonly Dictionary<Type, object> _repositories;

    protected Mvp24HoursContext? DbContext { get; private set; }
    private readonly IServiceProvider? _serviceProvider;
    private readonly ILogger<UnitOfWork>? _logger;

    /// <summary>
    ///  <see cref="Mvp24Hours.Core.Contract.Data.IUnitOfWork"/>
    /// </summary>
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Info Code Smell", "S1133:Deprecated code should be removed", Justification = "Maintain implementation reference standards.")]
    [Obsolete("MongoDb does not support IDbConnection. Use the database (IMongoDatabase) from context.")]
    public IDbConnection GetConnection()
    {
        throw new NotSupportedException("MongoDb does not support IDbConnection. Use the database (IMongoDatabase) from context.");
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
            DbContext = null;
        }
    }

    #endregion

    #region [ Unit of Work ]

    /// <summary>
    ///  <see cref="IUnitOfWork.SaveChanges()"/>
    /// </summary>
    public int SaveChanges(CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("MongoDB UnitOfWork SaveChanges started");
        try
        {
            ArgumentNullException.ThrowIfNull(DbContext);
            DbContext.SaveChanges(cancellationToken);
            return 1;
        }
        catch (Exception)
        {
            Rollback();
            return 0;
        }
        finally
        {
            _logger?.LogDebug("MongoDB UnitOfWork SaveChanges completed");
        }
    }

    /// <summary>
    ///  <see cref="Mvp24Hours.Core.Contract.Data.IUnitOfWork.Rollback()"/>
    /// </summary>
    public void Rollback()
    {
        _logger?.LogDebug("MongoDB UnitOfWork Rollback started");
        try
        {
            ArgumentNullException.ThrowIfNull(DbContext);
            DbContext.Rollback();
        }
        finally { _logger?.LogDebug("MongoDB UnitOfWork Rollback completed"); }
    }

    #endregion
}

//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace Mvp24Hours.Infrastructure.Data.MongoDb
{
    /// <summary>
    ///  <see cref="Mvp24Hours.Core.Contract.Data.IUnitOfWork"/>
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        #region [ Ctor ]

        public UnitOfWork(Mvp24HoursContext dbContext, Dictionary<Type, object> _repositories, ILogger<UnitOfWork> logger = null)
        {
            this.DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this.repositories = _repositories ?? throw new ArgumentNullException(nameof(_repositories));
            _logger = logger;

            DbContext.StartSession();
        }

        [ActivatorUtilitiesConstructor]
        public UnitOfWork(Mvp24HoursContext _dbContext, IServiceProvider _serviceProvider, ILogger<UnitOfWork> logger = null)
        {
            this.DbContext = _dbContext ?? throw new ArgumentNullException(nameof(_dbContext));
            this.serviceProvider = _serviceProvider ?? throw new ArgumentNullException(nameof(_serviceProvider));
            this.repositories = [];
            _logger = logger;

            DbContext.StartSession();
        }

        #endregion

        #region [ Properties ]

        private readonly Dictionary<Type, object> repositories;

        protected Mvp24HoursContext DbContext { get; private set; }
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<UnitOfWork> _logger;

        /// <summary>
        ///  <see cref="Mvp24Hours.Core.Contract.Data.IUnitOfWork"/>
        /// </summary>
        public IRepository<T> GetRepository<T>()
            where T : class, IEntityBase
        {
            if (!this.repositories.ContainsKey(typeof(T)))
            {
                this.repositories.Add(typeof(T), serviceProvider.GetService<IRepository<T>>());
            }
            return repositories[typeof(T)] as IRepository<T>;
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
                && this.DbContext != null)
            {
                this.DbContext = null;
            }
        }

        #endregion

        #region [ Unit of Work ]

        /// <summary>
        ///  <see cref="Mvp24Hours.Core.Contract.Data.IUnitOfWork.SaveChanges()"/>
        /// </summary>
        public int SaveChanges(CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("MongoDB UnitOfWork SaveChanges started");
            try
            {
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
                DbContext.Rollback();
            }
            finally { _logger?.LogDebug("MongoDB UnitOfWork Rollback completed"); }
        }

        #endregion
    }
}

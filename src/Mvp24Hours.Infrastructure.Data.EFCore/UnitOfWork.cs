//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace Mvp24Hours.Infrastructure.Data.EFCore
{
    /// <summary>
    ///  <see cref="Mvp24Hours.Core.Contract.Data.IUnitOfWork"/>
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        #region [ Ctor ]
        public UnitOfWork(DbContext _dbContext, Dictionary<Type, object> _repositories, ILogger<UnitOfWork>? logger = null)
        {
            this.DbContext = _dbContext ?? throw new ArgumentNullException(nameof(_dbContext));
            this.repositories = _repositories ?? throw new ArgumentNullException(nameof(_repositories));
            this._logger = logger;
        }

        [ActivatorUtilitiesConstructor]
        public UnitOfWork(DbContext _dbContext, IServiceProvider _serviceProvider, ILogger<UnitOfWork>? logger = null)
        {
            this.DbContext = _dbContext ?? throw new ArgumentNullException(nameof(_dbContext));
            this.serviceProvider = _serviceProvider ?? throw new ArgumentNullException(nameof(_serviceProvider));
            this.repositories = [];
            this._logger = logger;
        }

        #endregion

        #region [ Properties ]

        protected DbContext DbContext { get; private set; }
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<UnitOfWork>? _logger;

        readonly Dictionary<Type, object> repositories;

        public IRepository<T> GetRepository<T>()
            where T : class, IEntityBase
        {
            if (!this.repositories.ContainsKey(typeof(T)))
            {
                this.repositories.Add(typeof(T), serviceProvider.GetService<IRepository<T>>());
            }
            return repositories[typeof(T)] as IRepository<T>;
        }

        public IDbConnection GetConnection()
        {
            return DbContext?.Database?.GetDbConnection();
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
                this.DbContext.Dispose();
            }
        }

        #endregion

        #region [ Unit of Work ]

        /// <summary>
        ///  <see cref="Mvp24Hours.Core.Contract.Data.IUnitOfWork.SaveChanges()"/>
        /// </summary>
        public int SaveChanges(CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("UnitOfWork: SaveChanges started");
            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    return this.DbContext.SaveChanges();
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
                var changedEntries = this.DbContext.ChangeTracker.Entries()
                .Where(x => x.State != EntityState.Unchanged).ToList();

                foreach (var entry in changedEntries)
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
}

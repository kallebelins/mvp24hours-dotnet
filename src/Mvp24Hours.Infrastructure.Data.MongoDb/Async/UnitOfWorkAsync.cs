//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb
{
    /// <summary>
    ///  <see cref="IUnitOfWorkAsync"/>
    /// </summary>
    public class UnitOfWorkAsync : IUnitOfWorkAsync
    {
        #region [ Ctor ]

        public UnitOfWorkAsync(Mvp24HoursContext dbContext, Dictionary<Type, object> _repositories)
        {
            this.DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this.repositories = _repositories ?? throw new ArgumentNullException(nameof(_repositories));

            DbContext.StartSessionAsync().Wait();
        }

        [ActivatorUtilitiesConstructor]
        public UnitOfWorkAsync(Mvp24HoursContext _dbContext, IServiceProvider _serviceProvider)
        {
            this.DbContext = _dbContext ?? throw new ArgumentNullException(nameof(_dbContext));
            this.serviceProvider = _serviceProvider ?? throw new ArgumentNullException(nameof(_serviceProvider));
            repositories = [];

            DbContext.StartSessionAsync().Wait();
        }

        #endregion

        #region [ Ctor ]

        public UnitOfWorkAsync(Mvp24HoursContext dbContext)
        {
            this.DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            repositories = [];

            DbContext.StartSessionAsync().Wait();
        }

        #endregion

        #region [ Properties ]

        private readonly Dictionary<Type, object> repositories;

        protected Mvp24HoursContext DbContext { get; private set; }
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        ///  <see cref="IUnitOfWorkAsync"/>
        /// </summary>
        public IRepositoryAsync<T> GetRepository<T>()
            where T : class, IEntityBase
        {
            if (!repositories.ContainsKey(typeof(T)))
            {
                repositories.Add(typeof(T), serviceProvider.GetService<IRepositoryAsync<T>>());
            }
            return repositories[typeof(T)] as IRepositoryAsync<T>;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Info Code Smell", "S1133:Deprecated code should be removed", Justification = "Maintain implementation reference standards.")]
        [Obsolete("MongoDb does not support IDbConnection. Use the database (IMongoDatabase) from context.", true)]
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
        ///  <see cref="IUnitOfWorkAsync.SaveChangesAsync(CancellationToken)"/>
        /// </summary>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-unitofwork-savechangesasync-start");
            try
            {
                await DbContext.SaveChangesAsync(cancellationToken);
                return 1;
            }
            catch (Exception)
            {
                await RollbackAsync();
                return 0;
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-unitofwork-savechangesasync-end");
            }
        }

        /// <summary>
        ///  <see cref="IUnitOfWorkAsync.RollbackAsync()"/>
        /// </summary>
        public async Task RollbackAsync()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-unitofwork-rollbackasync-start");
            try
            {
                await DbContext.RollbackAsync();
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-unitofwork-rollbackasync-end"); }
        }

        #endregion
    }
}

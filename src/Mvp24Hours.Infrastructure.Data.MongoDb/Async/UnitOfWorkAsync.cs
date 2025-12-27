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
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb
{
    /// <summary>
    ///  <see cref="IUnitOfWorkAsync"/>
    /// </summary>
    public class UnitOfWorkAsync : IUnitOfWorkAsync
    {
        private readonly ILogger<UnitOfWorkAsync> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWorkAsync"/> class.
        /// </summary>
        /// <param name="dbContext">MongoDB context.</param>
        /// <param name="repositories">Dictionary of repositories.</param>
        /// <param name="logger">Optional logger instance.</param>
        public UnitOfWorkAsync(Mvp24HoursContext dbContext, Dictionary<Type, object> _repositories, ILogger<UnitOfWorkAsync> logger = null)
        {
            this.DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this.repositories = _repositories ?? throw new ArgumentNullException(nameof(_repositories));
            _logger = logger;

            DbContext.StartSessionAsync().Wait();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWorkAsync"/> class.
        /// </summary>
        /// <param name="dbContext">MongoDB context.</param>
        /// <param name="serviceProvider">Service provider for resolving repositories.</param>
        /// <param name="logger">Optional logger instance.</param>
        [ActivatorUtilitiesConstructor]
        public UnitOfWorkAsync(Mvp24HoursContext _dbContext, IServiceProvider _serviceProvider, ILogger<UnitOfWorkAsync> logger = null)
        {
            this.DbContext = _dbContext ?? throw new ArgumentNullException(nameof(_dbContext));
            this.serviceProvider = _serviceProvider ?? throw new ArgumentNullException(nameof(_serviceProvider));
            repositories = [];
            _logger = logger;

            DbContext.StartSessionAsync().Wait();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWorkAsync"/> class.
        /// </summary>
        /// <param name="dbContext">MongoDB context.</param>
        /// <param name="logger">Optional logger instance.</param>
        public UnitOfWorkAsync(Mvp24HoursContext dbContext, ILogger<UnitOfWorkAsync> logger = null)
        {
            this.DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            repositories = [];
            _logger = logger;

            DbContext.StartSessionAsync().Wait();
        }
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
            _logger?.LogDebug("Saving changes to MongoDB");
            try
            {
                await DbContext.SaveChangesAsync(cancellationToken);
                _logger?.LogDebug("Successfully saved changes to MongoDB");
                return 1;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving changes to MongoDB, rolling back");
                await RollbackAsync();
                return 0;
            }
        }

        /// <summary>
        ///  <see cref="IUnitOfWorkAsync.RollbackAsync()"/>
        /// </summary>
        public async Task RollbackAsync()
        {
            _logger?.LogDebug("Rolling back MongoDB transaction");
            try
            {
                await DbContext.RollbackAsync();
                _logger?.LogDebug("Successfully rolled back MongoDB transaction");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error rolling back MongoDB transaction");
                throw;
            }
        }

        #endregion
    }
}

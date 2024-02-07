//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Desing Pattern: UnitOfWork
    /// Description: Maintains a list of objects affected by a business transaction and 
	/// coordinates writing out changes and resolving competition issues. (Martin Fowler)
    /// Learn more: http://martinfowler.com/eaaCatalog/unitOfWork.html
    /// </summary>
    public interface IUnitOfWorkAsync : IDisposable
    {
        /// <summary>
        /// Persists the actions taken in the transaction
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Discard changes in transaction
        /// </summary>
        Task RollbackAsync();
        /// <summary>
        /// Gets a repository
        /// </summary>
        /// <returns>An asynchronous repository for an entity instance</returns>
        IRepositoryAsync<T> GetRepository<T>() where T : class, IEntityBase;
        /// <summary>
        /// Get context connection
        /// </summary>
        IDbConnection GetConnection();
    }
}

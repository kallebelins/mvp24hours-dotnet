//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic
{
    /// <summary>
    /// Asynchronous service for using repository with paginated results and unit of work
    /// </summary>
    /// <typeparam name="TEntity">Represents an entity</typeparam>
    public class RepositoryPagingServiceAsync<TEntity, TUoW> : RepositoryServiceAsync<TEntity, TUoW>, IQueryPagingServiceAsync<TEntity>
        where TEntity : class, IEntityBase
        where TUoW : class, IUnitOfWorkAsync
    {
        #region [ Fields ]
        private readonly ILogger<RepositoryPagingServiceAsync<TEntity, TUoW>> _pagingLogger;
        #endregion

        #region [ Ctor ]
        public RepositoryPagingServiceAsync(TUoW _unitOfWork)
            : base(_unitOfWork)
        {
            _pagingLogger = NullLogger<RepositoryPagingServiceAsync<TEntity, TUoW>>.Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public RepositoryPagingServiceAsync(TUoW _unitOfWork, IValidator<TEntity> validator, ILogger<RepositoryPagingServiceAsync<TEntity, TUoW>> logger = null)
            : base(_unitOfWork, validator)
        {
            _pagingLogger = logger ?? NullLogger<RepositoryPagingServiceAsync<TEntity, TUoW>>.Instance;
        }
        #endregion

        #region [ Implements IQueryPagingServiceAsync ]

        public virtual async Task<IPagingResult<IList<TEntity>>> GetByWithPaginationAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria = null, CancellationToken cancellationToken = default)
        {
            _pagingLogger.LogDebug("application-repositorypagingserviceasync-getbywithpaginationasync");
            var repo = UnitOfWork.GetRepository<TEntity>();
            return await repo.ToBusinessPagingAsync(clause, criteria);
        }

        public virtual async Task<IPagingResult<IList<TEntity>>> ListWithPaginationAsync(IPagingCriteria criteria = null, CancellationToken cancellationToken = default)
        {
            _pagingLogger.LogDebug("application-repositorypagingserviceasync-listwithpaginationasync");
            var repo = UnitOfWork.GetRepository<TEntity>();
            return await repo.ToBusinessPagingAsync(criteria);
        }

        #endregion
    }
}

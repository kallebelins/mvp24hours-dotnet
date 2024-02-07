//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
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
        #region [ Ctor ]
        public RepositoryPagingServiceAsync(TUoW _unitOfWork)
            : base(_unitOfWork)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public RepositoryPagingServiceAsync(TUoW _unitOfWork, IValidator<TEntity> validator)
            : base(_unitOfWork, validator)
        {
        }
        #endregion

        #region [ Implements IQueryPagingServiceAsync ]

        public virtual async Task<IPagingResult<IList<TEntity>>> GetByWithPaginationAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria = null, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositorypagingserviceasync-getbywithpaginationasync");
            var repo = UnitOfWork.GetRepository<TEntity>();
            return await repo.ToBusinessPagingAsync(clause, criteria);
        }

        public virtual async Task<IPagingResult<IList<TEntity>>> ListWithPaginationAsync(IPagingCriteria criteria = null, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositorypagingserviceasync-listwithpaginationasync");
            var repo = UnitOfWork.GetRepository<TEntity>();
            return await repo.ToBusinessPagingAsync(criteria);
        }

        #endregion
    }
}

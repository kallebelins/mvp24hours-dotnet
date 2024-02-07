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

namespace Mvp24Hours.Application.Logic
{
    /// <summary>
    /// Base service for using repository with paginated results and unit of work
    /// </summary>
    /// <typeparam name="TEntity">Represents an entity</typeparam>
    public class RepositoryPagingService<TEntity, TUoW> : RepositoryService<TEntity, TUoW>, IQueryPagingService<TEntity>
        where TEntity : class, IEntityBase
        where TUoW : class, IUnitOfWork
    {
        #region [ Ctor ]
        /// <summary>
        /// 
        /// </summary>
        public RepositoryPagingService(TUoW _unitOfWork)
            : base(_unitOfWork)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public RepositoryPagingService(TUoW _unitOfWork, IValidator<TEntity> validator)
            : base(_unitOfWork, validator)
        {
        }
        #endregion

        #region [ Implements IQueryPagingService ]

        public virtual IPagingResult<IList<TEntity>> GetByWithPagination(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria = null)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositorypagingservice-getbywithpagination");
            var repo = UnitOfWork.GetRepository<TEntity>();
            return repo.ToBusinessPaging(clause, criteria);
        }

        public virtual IPagingResult<IList<TEntity>> ListWithPagination(IPagingCriteria criteria = null)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositorypagingservice-listwithpagination");
            var repo = UnitOfWork.GetRepository<TEntity>();
            return repo.ToBusinessPaging(criteria);
        }

        #endregion
    }
}

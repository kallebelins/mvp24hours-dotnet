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
        #region [ Fields ]
        private readonly ILogger<RepositoryPagingService<TEntity, TUoW>> _pagingLogger;
        #endregion

        #region [ Ctor ]
        /// <summary>
        /// 
        /// </summary>
        public RepositoryPagingService(TUoW _unitOfWork)
            : base(_unitOfWork)
        {
            _pagingLogger = NullLogger<RepositoryPagingService<TEntity, TUoW>>.Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public RepositoryPagingService(TUoW _unitOfWork, IValidator<TEntity> validator, ILogger<RepositoryPagingService<TEntity, TUoW>> logger = null)
            : base(_unitOfWork, validator)
        {
            _pagingLogger = logger ?? NullLogger<RepositoryPagingService<TEntity, TUoW>>.Instance;
        }
        #endregion

        #region [ Implements IQueryPagingService ]

        public virtual IPagingResult<IList<TEntity>> GetByWithPagination(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria = null)
        {
            _pagingLogger.LogDebug("application-repositorypagingservice-getbywithpagination");
            var repo = UnitOfWork.GetRepository<TEntity>();
            return repo.ToBusinessPaging(clause, criteria);
        }

        public virtual IPagingResult<IList<TEntity>> ListWithPagination(IPagingCriteria criteria = null)
        {
            _pagingLogger.LogDebug("application-repositorypagingservice-listwithpagination");
            var repo = UnitOfWork.GetRepository<TEntity>();
            return repo.ToBusinessPaging(criteria);
        }

        #endregion
    }
}

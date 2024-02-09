//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic
{
    /// <summary>
    /// Asynchronous service for using repository and unit of work
    /// </summary>
    /// <typeparam name="TEntity">Represents an entity</typeparam>
    /// <remarks>
    /// 
    /// </remarks>
    public class RepositoryServiceAsync<TEntity, TUoW>(TUoW unitOfWork, IValidator<TEntity> validator) : IQueryServiceAsync<TEntity>, ICommandServiceAsync<TEntity>
        where TEntity : class, IEntityBase
        where TUoW : class, IUnitOfWorkAsync
    {
        #region [ Properties / Fields ]

        private readonly IRepositoryAsync<TEntity> repository = unitOfWork.GetRepository<TEntity>();
        private readonly TUoW unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IValidator<TEntity> validator = validator;

        /// <summary>
        /// Gets unit of work instance
        /// </summary>
        /// <returns>T</returns>
        protected virtual TUoW UnitOfWork => unitOfWork;

        /// <summary>
        /// Gets repository instance
        /// </summary>
        /// <returns>T</returns>
        protected virtual IRepositoryAsync<TEntity> Repository => repository;

        /// <summary>
        /// Defines a validator for a particular type.
        /// </summary>
        protected virtual IValidator<TEntity> Validator => validator;

        #endregion

        #region [ Ctor ]
        /// <summary>
        /// 
        /// </summary>
        public RepositoryServiceAsync(TUoW unitOfWork)
            : this(unitOfWork, null)
        {
        }
        #endregion

        #region [ Implements IQueryServiceAsync ]

        public virtual Task<IBusinessResult<bool>> ListAnyAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-listanyasync");
            return this.UnitOfWork
                .GetRepository<TEntity>()
                .ListAnyAsync(cancellationToken: cancellationToken)
                .ToBusinessAsync();
        }

        public virtual Task<IBusinessResult<int>> ListCountAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-listcountasync");
            return this.UnitOfWork
                .GetRepository<TEntity>()
                .ListCountAsync(cancellationToken: cancellationToken)
                .ToBusinessAsync();
        }

        public virtual Task<IBusinessResult<IList<TEntity>>> ListAsync(CancellationToken cancellationToken = default)
        {
            return this.ListAsync(null, cancellationToken: cancellationToken);
        }

        public virtual Task<IBusinessResult<IList<TEntity>>> ListAsync(IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-listasync");
            return this.UnitOfWork
                .GetRepository<TEntity>()
                .ListAsync(criteria, cancellationToken: cancellationToken)
                .ToBusinessAsync();
        }

        public virtual Task<IBusinessResult<bool>> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-getbyanyasync");
            return this.UnitOfWork
                .GetRepository<TEntity>()
                .GetByAnyAsync(clause, cancellationToken: cancellationToken)
                .ToBusinessAsync();
        }

        public virtual Task<IBusinessResult<int>> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-getbycountasync");
            return this.UnitOfWork
                .GetRepository<TEntity>()
                .GetByCountAsync(clause, cancellationToken: cancellationToken)
                .ToBusinessAsync();
        }

        public virtual Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            return GetByAsync(clause, null, cancellationToken: cancellationToken);
        }

        public virtual Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-getbyasync");
            return UnitOfWork
                .GetRepository<TEntity>()
                .GetByAsync(clause, criteria, cancellationToken: cancellationToken)
                .ToBusinessAsync();
        }

        public virtual Task<IBusinessResult<TEntity>> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return this.GetByIdAsync(id, null, cancellationToken: cancellationToken);
        }

        public virtual Task<IBusinessResult<TEntity>> GetByIdAsync(object id, IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-getbyidasync");
            return this.UnitOfWork
                .GetRepository<TEntity>()
                .GetByIdAsync(id, criteria, cancellationToken: cancellationToken)
                .ToBusinessAsync();
        }

        #endregion

        #region [ Implements ICommandServiceAsync ]

        public virtual async Task<IBusinessResult<int>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-addasync");
            var errors = entity.TryValidate(Validator);
            if (!errors.AnySafe())
            {
                await this.UnitOfWork
                    .GetRepository<TEntity>()
                    .AddAsync(entity, cancellationToken: cancellationToken);
                return await this.UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
                    .ToBusinessAsync();
            }
            return errors.ToBusiness<int>();
        }

        public virtual async Task<IBusinessResult<int>> AddAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-addlistasync");
            if (!entities.AnySafe())
            {
                return 0.ToBusiness();
            }

            foreach (var entity in entities)
            {
                var errors = entity.TryValidate(Validator);
                if (errors.AnySafe())
                {
                    return errors.ToBusiness<int>();
                }
            }
            var rep = this.UnitOfWork.GetRepository<TEntity>();
            await Task.WhenAll(tasks: entities.Select(entity => rep.AddAsync(entity, cancellationToken: cancellationToken)));
            return await this.UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
                .ToBusinessAsync();
        }

        public virtual async Task<IBusinessResult<int>> ModifyAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-modifyasync");
            var errors = entity.TryValidate(Validator);
            if (!errors.AnySafe())
            {
                await this.UnitOfWork
                    .GetRepository<TEntity>()
                    .ModifyAsync(entity, cancellationToken: cancellationToken);
                return await this.UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
                    .ToBusinessAsync();
            }
            return errors.ToBusiness<int>();
        }

        public virtual async Task<IBusinessResult<int>> ModifyAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-modifylistasync");
            if (!entities.AnySafe())
            {
                return 0.ToBusiness();
            }

            foreach (var entity in entities)
            {
                var errors = entity.TryValidate(Validator);
                if (errors.AnySafe())
                {
                    return errors.ToBusiness<int>();
                }
            }
            var rep = this.UnitOfWork.GetRepository<TEntity>();
            await Task.WhenAll(entities.Select(entity => rep.ModifyAsync(entity, cancellationToken: cancellationToken)));
            return await this.UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
                .ToBusinessAsync();
        }

        public virtual async Task<IBusinessResult<int>> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-removeasync");
            await this.UnitOfWork.GetRepository<TEntity>().RemoveAsync(entity, cancellationToken: cancellationToken);
            return await this.UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
                    .ToBusinessAsync();
        }

        public virtual async Task<IBusinessResult<int>> RemoveAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-removelistasync");
            if (!entities.AnySafe())
            {
                return 0.ToBusiness();
            }

            var rep = this.UnitOfWork.GetRepository<TEntity>();
            await Task.WhenAll(entities?.Select(entity => rep.RemoveAsync(entity, cancellationToken: cancellationToken)));
            return await this.UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
                .ToBusinessAsync();
        }

        public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-removebyidasync");
            await this.UnitOfWork.GetRepository<TEntity>().RemoveByIdAsync(id, cancellationToken: cancellationToken);
            return await this.UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
                    .ToBusinessAsync();
        }

        public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryserviceasync-removebyidlistasync");
            if (!ids.AnySafe())
            {
                return 0.ToBusiness();
            }

            var rep = this.UnitOfWork.GetRepository<TEntity>();
            await Task.WhenAll(ids?.Select(entity => rep.RemoveByIdAsync(entity, cancellationToken: cancellationToken)));
            return await this.UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
                .ToBusinessAsync();
        }

        #endregion
    }
}

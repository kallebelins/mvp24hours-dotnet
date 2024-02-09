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
using System.Linq.Expressions;

namespace Mvp24Hours.Application.Logic
{
    /// <summary>
    /// Base service for using repository and unit of work
    /// </summary>
    /// <typeparam name="TEntity">Represents an entity</typeparam>
    /// <remarks>
    /// 
    /// </remarks>
    public class RepositoryService<TEntity, TUoW>(TUoW unitOfWork, IValidator<TEntity> validator) : IQueryService<TEntity>, ICommandService<TEntity>
        where TEntity : class, IEntityBase
        where TUoW : class, IUnitOfWork
    {
        #region [ Properties / Fields ]

        private readonly IRepository<TEntity> repository = unitOfWork.GetRepository<TEntity>();
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
        protected virtual IRepository<TEntity> Repository => repository;

        /// <summary>
        /// Defines a validator for a particular type.
        /// </summary>
        protected virtual IValidator<TEntity> Validator => validator;

        #endregion

        #region [ Ctor ]
        /// <summary>
        /// 
        /// </summary>
        public RepositoryService(TUoW unitOfWork)
            : this(unitOfWork, null)
        {
        }
        #endregion

        #region [ Implements IQueryService ]

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.ListAny()"/>
        /// </summary>
        public virtual IBusinessResult<bool> ListAny()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-listany");
            return this.UnitOfWork
                .GetRepository<TEntity>()
                .ListAny()
                .ToBusiness();
        }

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.ListCount()"/>
        /// </summary>
        public virtual IBusinessResult<int> ListCount()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-listcount");
            return this.UnitOfWork
                .GetRepository<TEntity>()
                .ListCount()
                .ToBusiness();
        }

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.List()"/>
        /// </summary>
        public virtual IBusinessResult<IList<TEntity>> List()
        {
            return this.List(null);
        }

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.List(IPagingCriteria)"/>
        /// </summary>
        public virtual IBusinessResult<IList<TEntity>> List(IPagingCriteria criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-list");
            return this.UnitOfWork
                .GetRepository<TEntity>()
                .List(criteria)
                .ToBusiness();
        }

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.GetByAny(Expression{Func{T, bool}})"/>
        /// </summary>
        public virtual IBusinessResult<bool> GetByAny(Expression<Func<TEntity, bool>> clause)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-getbyany");
            return this.UnitOfWork
                .GetRepository<TEntity>()
                .GetByAny(clause)
                .ToBusiness();
        }

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.GetByCount(Expression{Func{T, bool}})"/>
        /// </summary>
        public virtual IBusinessResult<int> GetByCount(Expression<Func<TEntity, bool>> clause)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-getbycount");
            return this.UnitOfWork
                .GetRepository<TEntity>()
                .GetByCount(clause)
                .ToBusiness();
        }

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.GetBy(Expression{Func{T, bool}})"/>
        /// </summary>
        public virtual IBusinessResult<IList<TEntity>> GetBy(Expression<Func<TEntity, bool>> clause)
        {
            return GetBy(clause, null);
        }

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.GetBy(Expression{Func{T, bool}}, IPagingCriteria)"/>
        /// </summary>
        public virtual IBusinessResult<IList<TEntity>> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-getby");
            return UnitOfWork
                .GetRepository<TEntity>()
                .GetBy(clause, criteria)
                .ToBusiness();
        }

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.GetById(int)"/>
        /// </summary>
        public virtual IBusinessResult<TEntity> GetById(object id)
        {
            return this.GetById(id, null);
        }

        /// <summary>
        /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.GetById(int, IPagingCriteria)"/>
        /// </summary>
        public virtual IBusinessResult<TEntity> GetById(object id, IPagingCriteria criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-getbyid");
            return this.UnitOfWork
                .GetRepository<TEntity>()
                .GetById(id, criteria)
                .ToBusiness();
        }

        #endregion

        #region [ Implements ICommandService ]

        public virtual IBusinessResult<int> Add(TEntity entity)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-add");
            var errors = entity.TryValidate(Validator);
            if (!errors.AnySafe())
            {
                this.UnitOfWork
                    .GetRepository<TEntity>()
                    .Add(entity);
                return this.UnitOfWork.SaveChanges()
                    .ToBusiness();
            }
            return errors.ToBusiness<int>();
        }

        public virtual IBusinessResult<int> Add(IList<TEntity> entities)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-addlist");
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
            foreach (var entity in entities)
            {
                rep.Add(entity);
            }
            return this.UnitOfWork.SaveChanges()
                .ToBusiness();
        }

        public virtual IBusinessResult<int> Modify(TEntity entity)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-modify");
            var errors = entity.TryValidate(Validator);
            if (!errors.AnySafe())
            {
                this.UnitOfWork
                    .GetRepository<TEntity>()
                    .Modify(entity);
                return this.UnitOfWork.SaveChanges()
                    .ToBusiness();
            }
            return errors.ToBusiness<int>();
        }

        public virtual IBusinessResult<int> Modify(IList<TEntity> entities)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-modifylist");
            if (!entities.AnySafe())
            {
                return 0.ToBusiness();
            }

            var rep = this.UnitOfWork.GetRepository<TEntity>();
            foreach (var entity in entities)
            {
                rep.Modify(entity);
            }
            return this.UnitOfWork.SaveChanges()
                .ToBusiness();
        }

        public virtual IBusinessResult<int> Remove(TEntity entity)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-remove");
            this.UnitOfWork.GetRepository<TEntity>().Remove(entity);
            return this.UnitOfWork.SaveChanges()
                .ToBusiness();
        }

        public virtual IBusinessResult<int> Remove(IList<TEntity> entities)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-removelist");
            if (!entities.AnySafe())
            {
                return 0.ToBusiness();
            }

            var rep = this.UnitOfWork.GetRepository<TEntity>();
            foreach (var entity in entities)
            {
                rep.Remove(entity);
            }
            return this.UnitOfWork.SaveChanges()
                .ToBusiness();
        }

        public virtual IBusinessResult<int> RemoveById(object id)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-removebyid");
            this.UnitOfWork.GetRepository<TEntity>().RemoveById(id);
            return this.UnitOfWork.SaveChanges()
                .ToBusiness();
        }

        public virtual IBusinessResult<int> RemoveById(IList<object> ids)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-repositoryservice-removebyidlist");
            if (!ids.AnySafe())
            {
                return 0.ToBusiness();
            }

            var rep = this.UnitOfWork.GetRepository<TEntity>();
            foreach (var id in ids)
            {
                rep.RemoveById(id);
            }
            return this.UnitOfWork.SaveChanges()
                .ToBusiness();
        }

        #endregion
    }
}

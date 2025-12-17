//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using AutoMapper;
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
    /// Abstract base class for application services with automatic DTO mapping support.
    /// Provides a unified implementation of query and command operations with Entity/DTO conversion.
    /// </summary>
    /// <typeparam name="TEntity">The entity type managed by this service.</typeparam>
    /// <typeparam name="TDto">The DTO type used for data transfer.</typeparam>
    /// <typeparam name="TUoW">The unit of work type.</typeparam>
    /// <remarks>
    /// <para>
    /// This class extends <see cref="ApplicationServiceBase{TEntity, TUoW}"/> to include automatic
    /// mapping between entities and DTOs using AutoMapper.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Automatic Entity ↔ DTO mapping via AutoMapper</item>
    /// <item>All query operations return DTOs instead of entities</item>
    /// <item>Command operations accept DTOs and convert to entities internally</item>
    /// <item>FluentValidation integration for entity validation</item>
    /// <item>Telemetry logging for all operations</item>
    /// <item>Transaction management via Unit of Work</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// public class CustomerService : ApplicationServiceBaseWithDto&lt;Customer, CustomerDto, MyDbContext&gt;
    /// {
    ///     public CustomerService(MyDbContext unitOfWork, IMapper mapper) 
    ///         : base(unitOfWork, mapper) { }
    ///     
    ///     // Add custom business logic here
    ///     public IBusinessResult&lt;CustomerDto&gt; FindByEmail(string email)
    ///     {
    ///         return GetBy(c => c.Email == email);
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <seealso cref="IApplicationServiceWithDto{TEntity,TDto}"/>
    /// <seealso cref="IReadOnlyApplicationServiceWithDto{TEntity,TDto}"/>
    public abstract class ApplicationServiceBaseWithDto<TEntity, TDto, TUoW>
        : IApplicationServiceWithDto<TEntity, TDto>, IReadOnlyApplicationServiceWithDto<TEntity, TDto>
        where TEntity : class, IEntityBase
        where TDto : class
        where TUoW : class, IUnitOfWork
    {
        #region [ Properties / Fields ]

        private readonly IRepository<TEntity> _repository;
        private readonly TUoW _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<TEntity>? _entityValidator;
        private readonly IValidator<TDto>? _dtoValidator;

        /// <summary>
        /// Gets the unit of work instance for managing transactions.
        /// </summary>
        protected virtual TUoW UnitOfWork => _unitOfWork;

        /// <summary>
        /// Gets the repository instance for data access operations.
        /// </summary>
        protected virtual IRepository<TEntity> Repository => _repository;

        /// <summary>
        /// Gets the AutoMapper instance for Entity/DTO mapping.
        /// </summary>
        protected virtual IMapper Mapper => _mapper;

        /// <summary>
        /// Gets the validator instance for entity validation.
        /// </summary>
        protected virtual IValidator<TEntity>? EntityValidator => _entityValidator;

        /// <summary>
        /// Gets the validator instance for DTO validation.
        /// </summary>
        protected virtual IValidator<TDto>? DtoValidator => _dtoValidator;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationServiceBaseWithDto{TEntity, TDto, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork or mapper is null.</exception>
        protected ApplicationServiceBaseWithDto(TUoW unitOfWork, IMapper mapper)
            : this(unitOfWork, mapper, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationServiceBaseWithDto{TEntity, TDto, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <param name="entityValidator">The validator for entity validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork or mapper is null.</exception>
        protected ApplicationServiceBaseWithDto(TUoW unitOfWork, IMapper mapper, IValidator<TEntity>? entityValidator)
            : this(unitOfWork, mapper, entityValidator, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationServiceBaseWithDto{TEntity, TDto, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <param name="entityValidator">The validator for entity validation.</param>
        /// <param name="dtoValidator">The validator for DTO validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork or mapper is null.</exception>
        protected ApplicationServiceBaseWithDto(
            TUoW unitOfWork,
            IMapper mapper,
            IValidator<TEntity>? entityValidator,
            IValidator<TDto>? dtoValidator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _repository = unitOfWork.GetRepository<TEntity>();
            _entityValidator = entityValidator;
            _dtoValidator = dtoValidator;
        }

        #endregion

        #region [ Query Operations ]

        /// <inheritdoc/>
        public virtual IBusinessResult<bool> ListAny()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-listany");
            return _repository.ListAny().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> ListCount()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-listcount");
            return _repository.ListCount().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<IList<TDto>> List()
        {
            return List(null);
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<IList<TDto>> List(IPagingCriteria? criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-list");
            var entities = _repository.List(criteria);
            var dtos = _mapper.Map<IList<TDto>>(entities);
            return dtos.ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<bool> GetByAny(Expression<Func<TEntity, bool>> clause)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-getbyany");
            return _repository.GetByAny(clause).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> GetByCount(Expression<Func<TEntity, bool>> clause)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-getbycount");
            return _repository.GetByCount(clause).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<IList<TDto>> GetBy(Expression<Func<TEntity, bool>> clause)
        {
            return GetBy(clause, null);
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<IList<TDto>> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-getby");
            var entities = _repository.GetBy(clause, criteria);
            var dtos = _mapper.Map<IList<TDto>>(entities);
            return dtos.ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<TDto> GetById(object id)
        {
            return GetById(id, null);
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<TDto> GetById(object id, IPagingCriteria? criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-getbyid");
            var entity = _repository.GetById(id, criteria);
            var dto = _mapper.Map<TDto>(entity);
            return dto.ToBusiness();
        }

        #endregion

        #region [ Command Operations ]

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Add(TDto dto)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-add");

            // Validate DTO if validator is available
            var dtoErrors = dto.TryValidate(_dtoValidator);
            if (dtoErrors.AnySafe())
            {
                return dtoErrors.ToBusiness<int>();
            }

            // Map DTO to Entity
            var entity = _mapper.Map<TEntity>(dto);

            // Validate Entity if validator is available
            var entityErrors = entity.TryValidate(_entityValidator);
            if (entityErrors.AnySafe())
            {
                return entityErrors.ToBusiness<int>();
            }

            _repository.Add(entity);
            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Add(IList<TDto> dtos)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-addlist");

            if (!dtos.AnySafe())
            {
                return 0.ToBusiness();
            }

            var entities = new List<TEntity>();

            foreach (var dto in dtos)
            {
                // Validate DTO if validator is available
                var dtoErrors = dto.TryValidate(_dtoValidator);
                if (dtoErrors.AnySafe())
                {
                    return dtoErrors.ToBusiness<int>();
                }

                // Map DTO to Entity
                var entity = _mapper.Map<TEntity>(dto);

                // Validate Entity if validator is available
                var entityErrors = entity.TryValidate(_entityValidator);
                if (entityErrors.AnySafe())
                {
                    return entityErrors.ToBusiness<int>();
                }

                entities.Add(entity);
            }

            foreach (var entity in entities)
            {
                _repository.Add(entity);
            }

            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Modify(TDto dto)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-modify");

            // Validate DTO if validator is available
            var dtoErrors = dto.TryValidate(_dtoValidator);
            if (dtoErrors.AnySafe())
            {
                return dtoErrors.ToBusiness<int>();
            }

            // Map DTO to Entity
            var entity = _mapper.Map<TEntity>(dto);

            // Validate Entity if validator is available
            var entityErrors = entity.TryValidate(_entityValidator);
            if (entityErrors.AnySafe())
            {
                return entityErrors.ToBusiness<int>();
            }

            _repository.Modify(entity);
            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Modify(IList<TDto> dtos)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-modifylist");

            if (!dtos.AnySafe())
            {
                return 0.ToBusiness();
            }

            var entities = new List<TEntity>();

            foreach (var dto in dtos)
            {
                // Validate DTO if validator is available
                var dtoErrors = dto.TryValidate(_dtoValidator);
                if (dtoErrors.AnySafe())
                {
                    return dtoErrors.ToBusiness<int>();
                }

                // Map DTO to Entity
                var entity = _mapper.Map<TEntity>(dto);

                // Validate Entity if validator is available
                var entityErrors = entity.TryValidate(_entityValidator);
                if (entityErrors.AnySafe())
                {
                    return entityErrors.ToBusiness<int>();
                }

                entities.Add(entity);
            }

            foreach (var entity in entities)
            {
                _repository.Modify(entity);
            }

            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Remove(TDto dto)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-remove");

            // Map DTO to Entity
            var entity = _mapper.Map<TEntity>(dto);

            _repository.Remove(entity);
            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Remove(IList<TDto> dtos)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-removelist");

            if (!dtos.AnySafe())
            {
                return 0.ToBusiness();
            }

            foreach (var dto in dtos)
            {
                var entity = _mapper.Map<TEntity>(dto);
                _repository.Remove(entity);
            }

            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> RemoveById(object id)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-removebyid");
            _repository.RemoveById(id);
            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> RemoveById(IList<object> ids)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebasewithdto-removebyidlist");

            if (!ids.AnySafe())
            {
                return 0.ToBusiness();
            }

            foreach (var id in ids)
            {
                _repository.RemoveById(id);
            }

            return _unitOfWork.SaveChanges().ToBusiness();
        }

        #endregion

        #region [ Protected Methods for Customization ]

        /// <summary>
        /// Maps an entity to a DTO. Override this method for custom mapping logic.
        /// </summary>
        /// <param name="entity">The entity to map.</param>
        /// <returns>The mapped DTO.</returns>
        protected virtual TDto MapToDto(TEntity entity)
        {
            return _mapper.Map<TDto>(entity);
        }

        /// <summary>
        /// Maps a DTO to an entity. Override this method for custom mapping logic.
        /// </summary>
        /// <param name="dto">The DTO to map.</param>
        /// <returns>The mapped entity.</returns>
        protected virtual TEntity MapToEntity(TDto dto)
        {
            return _mapper.Map<TEntity>(dto);
        }

        /// <summary>
        /// Maps a collection of entities to DTOs. Override this method for custom mapping logic.
        /// </summary>
        /// <param name="entities">The entities to map.</param>
        /// <returns>The mapped DTOs.</returns>
        protected virtual IList<TDto> MapToDtos(IEnumerable<TEntity> entities)
        {
            return _mapper.Map<IList<TDto>>(entities);
        }

        /// <summary>
        /// Maps a collection of DTOs to entities. Override this method for custom mapping logic.
        /// </summary>
        /// <param name="dtos">The DTOs to map.</param>
        /// <returns>The mapped entities.</returns>
        protected virtual IList<TEntity> MapToEntities(IEnumerable<TDto> dtos)
        {
            return _mapper.Map<IList<TEntity>>(dtos);
        }

        #endregion
    }
}


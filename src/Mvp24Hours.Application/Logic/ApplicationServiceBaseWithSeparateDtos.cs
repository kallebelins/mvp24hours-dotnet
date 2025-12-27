//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Mvp24Hours.Application.Logic
{
    /// <summary>
    /// Abstract base class for application services with separate DTOs for create, update, and read operations.
    /// Provides a unified implementation of CRUD operations with distinct DTO types for each operation.
    /// </summary>
    /// <typeparam name="TEntity">The entity type managed by this service.</typeparam>
    /// <typeparam name="TDto">The DTO type used for read operations (queries).</typeparam>
    /// <typeparam name="TCreateDto">The DTO type used for create operations.</typeparam>
    /// <typeparam name="TUpdateDto">The DTO type used for update operations.</typeparam>
    /// <typeparam name="TUoW">The unit of work type.</typeparam>
    /// <remarks>
    /// <para>
    /// This class provides a complete implementation with separate DTO types for different operations,
    /// which is common in real-world applications where:
    /// <list type="bullet">
    /// <item>Read DTOs may contain computed fields or nested data</item>
    /// <item>Create DTOs may exclude auto-generated fields (Id, CreatedAt)</item>
    /// <item>Update DTOs may only allow certain fields to be modified</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Separate DTO types for Read, Create, and Update operations</item>
    /// <item>Automatic Entity ↔ DTO mapping via AutoMapper</item>
    /// <item>PATCH support (partial updates with non-null values)</item>
    /// <item>FluentValidation integration for entity and DTO validation</item>
    /// <item>Telemetry logging for all operations</item>
    /// <item>Transaction management via Unit of Work</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// public class CustomerService : ApplicationServiceBaseWithSeparateDtos&lt;
    ///     Customer, 
    ///     CustomerDto,           // For reads - includes all fields
    ///     CreateCustomerDto,     // For creates - excludes Id, CreatedAt
    ///     UpdateCustomerDto,     // For updates - only editable fields
    ///     MyDbContext&gt;
    /// {
    ///     public CustomerService(MyDbContext unitOfWork, IMapper mapper) 
    ///         : base(unitOfWork, mapper) { }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <seealso cref="IApplicationServiceWithSeparateDtos{TEntity,TDto,TCreateDto,TUpdateDto}"/>
    /// <seealso cref="IReadOnlyApplicationServiceWithSeparateDtos{TEntity,TDto}"/>
    public abstract class ApplicationServiceBaseWithSeparateDtos<TEntity, TDto, TCreateDto, TUpdateDto, TUoW>
        : IApplicationServiceWithSeparateDtos<TEntity, TDto, TCreateDto, TUpdateDto>,
          IReadOnlyApplicationServiceWithSeparateDtos<TEntity, TDto>
        where TEntity : class, IEntityBase
        where TDto : class
        where TCreateDto : class
        where TUpdateDto : class
        where TUoW : class, IUnitOfWork
    {
        #region [ Properties / Fields ]

        private readonly IRepository<TEntity> _repository;
        private readonly TUoW _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<TEntity>? _entityValidator;
        private readonly IValidator<TCreateDto>? _createDtoValidator;
        private readonly IValidator<TUpdateDto>? _updateDtoValidator;
        private readonly ILogger _logger;

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
        /// Gets the validator instance for create DTO validation.
        /// </summary>
        protected virtual IValidator<TCreateDto>? CreateDtoValidator => _createDtoValidator;

        /// <summary>
        /// Gets the validator instance for update DTO validation.
        /// </summary>
        protected virtual IValidator<TUpdateDto>? UpdateDtoValidator => _updateDtoValidator;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork or mapper is null.</exception>
        protected ApplicationServiceBaseWithSeparateDtos(TUoW unitOfWork, IMapper mapper)
            : this(unitOfWork, mapper, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <param name="entityValidator">The validator for entity validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork or mapper is null.</exception>
        protected ApplicationServiceBaseWithSeparateDtos(TUoW unitOfWork, IMapper mapper, IValidator<TEntity>? entityValidator)
            : this(unitOfWork, mapper, entityValidator, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <param name="entityValidator">The validator for entity validation.</param>
        /// <param name="createDtoValidator">The validator for create DTO validation.</param>
        /// <param name="updateDtoValidator">The validator for update DTO validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork or mapper is null.</exception>
        protected ApplicationServiceBaseWithSeparateDtos(
            TUoW unitOfWork,
            IMapper mapper,
            IValidator<TEntity>? entityValidator,
            IValidator<TCreateDto>? createDtoValidator,
            IValidator<TUpdateDto>? updateDtoValidator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _repository = unitOfWork.GetRepository<TEntity>();
            _entityValidator = entityValidator;
            _createDtoValidator = createDtoValidator;
            _updateDtoValidator = updateDtoValidator;
            _logger = NullLogger.Instance;
        }

        #endregion

        #region [ Query Operations ]

        /// <inheritdoc/>
        public virtual IBusinessResult<bool> ListAny()
        {
            _logger.LogDebug("application-separatedtos-listany");
            return _repository.ListAny().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> ListCount()
        {
            _logger.LogDebug("application-separatedtos-listcount");
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
            _logger.LogDebug("application-separatedtos-list");
            var entities = _repository.List(criteria);
            var dtos = MapToDtos(entities);
            return dtos.ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<bool> GetByAny(Expression<Func<TEntity, bool>> clause)
        {
            _logger.LogDebug("application-separatedtos-getbyany");
            return _repository.GetByAny(clause).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> GetByCount(Expression<Func<TEntity, bool>> clause)
        {
            _logger.LogDebug("application-separatedtos-getbycount");
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
            _logger.LogDebug("application-separatedtos-getby");
            var entities = _repository.GetBy(clause, criteria);
            var dtos = MapToDtos(entities);
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
            _logger.LogDebug("application-separatedtos-getbyid");
            var entity = _repository.GetById(id, criteria);
            var dto = MapToDto(entity);
            return dto.ToBusiness();
        }

        #endregion

        #region [ Create Operations ]

        /// <inheritdoc/>
        public virtual IBusinessResult<TDto> Add(TCreateDto dto)
        {
            _logger.LogDebug("application-separatedtos-add");

            // Validate create DTO if validator is available
            var dtoErrors = dto.TryValidate(_createDtoValidator);
            if (dtoErrors.AnySafe())
            {
                return dtoErrors.ToBusiness<TDto>();
            }

            // Map create DTO to Entity
            var entity = MapCreateDtoToEntity(dto);

            // Validate Entity if validator is available
            var entityErrors = entity.TryValidate(_entityValidator);
            if (entityErrors.AnySafe())
            {
                return entityErrors.ToBusiness<TDto>();
            }

            _repository.Add(entity);
            _unitOfWork.SaveChanges();

            // Return the created entity as read DTO
            var resultDto = MapToDto(entity);
            return resultDto.ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Add(IList<TCreateDto> dtos)
        {
            _logger.LogDebug("application-separatedtos-addlist");

            if (!dtos.AnySafe())
            {
                return 0.ToBusiness();
            }

            var entities = new List<TEntity>();

            foreach (var dto in dtos)
            {
                // Validate create DTO if validator is available
                var dtoErrors = dto.TryValidate(_createDtoValidator);
                if (dtoErrors.AnySafe())
                {
                    return dtoErrors.ToBusiness<int>();
                }

                // Map create DTO to Entity
                var entity = MapCreateDtoToEntity(dto);

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

        #endregion

        #region [ Update Operations ]

        /// <inheritdoc/>
        public virtual IBusinessResult<TDto> Modify(object id, TUpdateDto dto)
        {
            _logger.LogDebug("application-separatedtos-modify");

            // Validate update DTO if validator is available
            var dtoErrors = dto.TryValidate(_updateDtoValidator);
            if (dtoErrors.AnySafe())
            {
                return dtoErrors.ToBusiness<TDto>();
            }

            // Get existing entity
            var existingEntity = _repository.GetById(id);
            if (existingEntity == null)
            {
                return BusinessResult.Failure<TDto>("Entity not found", "NotFound");
            }

            // Map update DTO to existing entity
            MapUpdateDtoToEntity(dto, existingEntity);

            // Validate Entity if validator is available
            var entityErrors = existingEntity.TryValidate(_entityValidator);
            if (entityErrors.AnySafe())
            {
                return entityErrors.ToBusiness<TDto>();
            }

            _repository.Modify(existingEntity);
            _unitOfWork.SaveChanges();

            // Return the updated entity as read DTO
            var resultDto = MapToDto(existingEntity);
            return resultDto.ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<TDto> Patch(object id, TUpdateDto dto)
        {
            _logger.LogDebug("application-separatedtos-patch");

            // Get existing entity
            var existingEntity = _repository.GetById(id);
            if (existingEntity == null)
            {
                return BusinessResult.Failure<TDto>("Entity not found", "NotFound");
            }

            // Apply only non-null values from update DTO to entity
            ApplyPatchToEntity(dto, existingEntity);

            // Validate Entity if validator is available (after patch)
            var entityErrors = existingEntity.TryValidate(_entityValidator);
            if (entityErrors.AnySafe())
            {
                return entityErrors.ToBusiness<TDto>();
            }

            _repository.Modify(existingEntity);
            _unitOfWork.SaveChanges();

            // Return the updated entity as read DTO
            var resultDto = MapToDto(existingEntity);
            return resultDto.ToBusiness();
        }

        #endregion

        #region [ Delete Operations ]

        /// <inheritdoc/>
        public virtual IBusinessResult<int> RemoveById(object id)
        {
            _logger.LogDebug("application-separatedtos-removebyid");
            _repository.RemoveById(id);
            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> RemoveById(IList<object> ids)
        {
            _logger.LogDebug("application-separatedtos-removebyidlist");

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
        /// Maps an entity to a read DTO. Override this method for custom mapping logic.
        /// </summary>
        /// <param name="entity">The entity to map.</param>
        /// <returns>The mapped read DTO.</returns>
        protected virtual TDto MapToDto(TEntity entity)
        {
            return _mapper.Map<TDto>(entity);
        }

        /// <summary>
        /// Maps a collection of entities to read DTOs. Override this method for custom mapping logic.
        /// </summary>
        /// <param name="entities">The entities to map.</param>
        /// <returns>The mapped read DTOs.</returns>
        protected virtual IList<TDto> MapToDtos(IEnumerable<TEntity> entities)
        {
            return _mapper.Map<IList<TDto>>(entities);
        }

        /// <summary>
        /// Maps a create DTO to a new entity. Override this method for custom mapping logic.
        /// </summary>
        /// <param name="dto">The create DTO to map.</param>
        /// <returns>The mapped new entity.</returns>
        protected virtual TEntity MapCreateDtoToEntity(TCreateDto dto)
        {
            return _mapper.Map<TEntity>(dto);
        }

        /// <summary>
        /// Maps an update DTO to an existing entity (full update). Override this method for custom mapping logic.
        /// </summary>
        /// <param name="dto">The update DTO to map.</param>
        /// <param name="entity">The existing entity to update.</param>
        protected virtual void MapUpdateDtoToEntity(TUpdateDto dto, TEntity entity)
        {
            _mapper.Map(dto, entity);
        }

        /// <summary>
        /// Applies a partial update (PATCH) from an update DTO to an existing entity.
        /// Only non-null properties from the DTO are applied to the entity.
        /// Override this method for custom PATCH logic.
        /// </summary>
        /// <param name="dto">The update DTO containing partial data.</param>
        /// <param name="entity">The existing entity to update.</param>
        protected virtual void ApplyPatchToEntity(TUpdateDto dto, TEntity entity)
        {
            var dtoType = typeof(TUpdateDto);
            var entityType = typeof(TEntity);

            foreach (var dtoProperty in dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!dtoProperty.CanRead)
                    continue;

                var dtoValue = dtoProperty.GetValue(dto);

                // Skip null values for PATCH
                if (dtoValue == null)
                    continue;

                // Find matching property in entity
                var entityProperty = entityType.GetProperty(dtoProperty.Name, BindingFlags.Public | BindingFlags.Instance);
                if (entityProperty == null || !entityProperty.CanWrite)
                    continue;

                // Check if types are compatible
                if (!entityProperty.PropertyType.IsAssignableFrom(dtoProperty.PropertyType))
                    continue;

                // Apply the value
                entityProperty.SetValue(entity, dtoValue);
            }
        }

        #endregion
    }
}


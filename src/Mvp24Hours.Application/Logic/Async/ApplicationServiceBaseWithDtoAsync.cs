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
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic
{
    /// <summary>
    /// Asynchronous abstract base class for application services with automatic DTO mapping support.
    /// Provides a unified implementation of async query and command operations with Entity/DTO conversion.
    /// </summary>
    /// <typeparam name="TEntity">The entity type managed by this service.</typeparam>
    /// <typeparam name="TDto">The DTO type used for data transfer.</typeparam>
    /// <typeparam name="TUoW">The unit of work type.</typeparam>
    /// <remarks>
    /// <para>
    /// This class provides a complete async implementation with automatic mapping between entities
    /// and DTOs using AutoMapper.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Automatic Entity ↔ DTO mapping via AutoMapper</item>
    /// <item>All query operations return DTOs instead of entities</item>
    /// <item>Command operations accept DTOs and convert to entities internally</item>
    /// <item>Full async/await support with CancellationToken</item>
    /// <item>FluentValidation integration for entity and DTO validation</item>
    /// <item>Telemetry logging for all operations</item>
    /// <item>Transaction management via Unit of Work</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// public class CustomerService : ApplicationServiceBaseWithDtoAsync&lt;Customer, CustomerDto, MyDbContext&gt;
    /// {
    ///     public CustomerService(MyDbContext unitOfWork, IMapper mapper) 
    ///         : base(unitOfWork, mapper) { }
    ///     
    ///     // Add custom business logic here
    ///     public Task&lt;IBusinessResult&lt;IList&lt;CustomerDto&gt;&gt;&gt; FindByEmailAsync(string email, CancellationToken ct = default)
    ///     {
    ///         return GetByAsync(c => c.Email == email, ct);
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <seealso cref="IApplicationServiceWithDtoAsync{TEntity,TDto}"/>
    /// <seealso cref="IReadOnlyApplicationServiceWithDtoAsync{TEntity,TDto}"/>
    public abstract class ApplicationServiceBaseWithDtoAsync<TEntity, TDto, TUoW>
        : IApplicationServiceWithDtoAsync<TEntity, TDto>, IReadOnlyApplicationServiceWithDtoAsync<TEntity, TDto>
        where TEntity : class, IEntityBase
        where TDto : class
        where TUoW : class, IUnitOfWorkAsync
    {
        #region [ Properties / Fields ]

        private readonly IRepositoryAsync<TEntity> _repository;
        private readonly TUoW _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<TEntity>? _entityValidator;
        private readonly IValidator<TDto>? _dtoValidator;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the unit of work instance for managing transactions.
        /// </summary>
        protected virtual TUoW UnitOfWork => _unitOfWork;

        /// <summary>
        /// Gets the repository instance for data access operations.
        /// </summary>
        protected virtual IRepositoryAsync<TEntity> Repository => _repository;

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
        /// Initializes a new instance of the <see cref="ApplicationServiceBaseWithDtoAsync{TEntity, TDto, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork or mapper is null.</exception>
        protected ApplicationServiceBaseWithDtoAsync(TUoW unitOfWork, IMapper mapper)
            : this(unitOfWork, mapper, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationServiceBaseWithDtoAsync{TEntity, TDto, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <param name="entityValidator">The validator for entity validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork or mapper is null.</exception>
        protected ApplicationServiceBaseWithDtoAsync(TUoW unitOfWork, IMapper mapper, IValidator<TEntity>? entityValidator)
            : this(unitOfWork, mapper, entityValidator, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationServiceBaseWithDtoAsync{TEntity, TDto, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <param name="entityValidator">The validator for entity validation.</param>
        /// <param name="dtoValidator">The validator for DTO validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork or mapper is null.</exception>
        protected ApplicationServiceBaseWithDtoAsync(
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
            _logger = NullLogger.Instance;
        }

        #endregion

        #region [ Query Operations ]

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<bool>> ListAnyAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-listanyasync");
            return _repository.ListAnyAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<int>> ListCountAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-listcountasync");
            return _repository.ListCountAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<IList<TDto>>> ListAsync(CancellationToken cancellationToken = default)
        {
            return ListAsync(null, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<IList<TDto>>> ListAsync(IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-listasync");
            var entities = await _repository.ListAsync(criteria, cancellationToken: cancellationToken);
            var dtos = MapToDtos(entities);
            return dtos.ToBusiness();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<bool>> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-getbyanyasync");
            return _repository.GetByAnyAsync(clause, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<int>> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-getbycountasync");
            return _repository.GetByCountAsync(clause, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<IList<TDto>>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            return GetByAsync(clause, null, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<IList<TDto>>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-getbyasync");
            var entities = await _repository.GetByAsync(clause, criteria, cancellationToken: cancellationToken);
            var dtos = MapToDtos(entities);
            return dtos.ToBusiness();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<TDto>> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(id, null, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<TDto>> GetByIdAsync(object id, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-getbyidasync");
            var entity = await _repository.GetByIdAsync(id, criteria, cancellationToken: cancellationToken);
            var dto = MapToDto(entity);
            return dto.ToBusiness();
        }

        #endregion

        #region [ Command Operations ]

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> AddAsync(TDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-addasync");

            // Validate DTO if validator is available
            var dtoErrors = dto.TryValidate(_dtoValidator);
            if (dtoErrors.AnySafe())
            {
                return dtoErrors.ToBusiness<int>();
            }

            // Map DTO to Entity
            var entity = MapToEntity(dto);

            // Validate Entity if validator is available
            var entityErrors = entity.TryValidate(_entityValidator);
            if (entityErrors.AnySafe())
            {
                return entityErrors.ToBusiness<int>();
            }

            await _repository.AddAsync(entity, cancellationToken: cancellationToken);
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> AddAsync(IList<TDto> dtos, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-addlistasync");

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
                var entity = MapToEntity(dto);

                // Validate Entity if validator is available
                var entityErrors = entity.TryValidate(_entityValidator);
                if (entityErrors.AnySafe())
                {
                    return entityErrors.ToBusiness<int>();
                }

                entities.Add(entity);
            }

            await Task.WhenAll(entities.Select(entity => _repository.AddAsync(entity, cancellationToken: cancellationToken)));
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> ModifyAsync(TDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-modifyasync");

            // Validate DTO if validator is available
            var dtoErrors = dto.TryValidate(_dtoValidator);
            if (dtoErrors.AnySafe())
            {
                return dtoErrors.ToBusiness<int>();
            }

            // Map DTO to Entity
            var entity = MapToEntity(dto);

            // Validate Entity if validator is available
            var entityErrors = entity.TryValidate(_entityValidator);
            if (entityErrors.AnySafe())
            {
                return entityErrors.ToBusiness<int>();
            }

            await _repository.ModifyAsync(entity, cancellationToken: cancellationToken);
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> ModifyAsync(IList<TDto> dtos, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-modifylistasync");

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
                var entity = MapToEntity(dto);

                // Validate Entity if validator is available
                var entityErrors = entity.TryValidate(_entityValidator);
                if (entityErrors.AnySafe())
                {
                    return entityErrors.ToBusiness<int>();
                }

                entities.Add(entity);
            }

            await Task.WhenAll(entities.Select(entity => _repository.ModifyAsync(entity, cancellationToken: cancellationToken)));
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveAsync(TDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-removeasync");

            // Map DTO to Entity
            var entity = MapToEntity(dto);

            await _repository.RemoveAsync(entity, cancellationToken: cancellationToken);
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveAsync(IList<TDto> dtos, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-removelistasync");

            if (!dtos.AnySafe())
            {
                return 0.ToBusiness();
            }

            await Task.WhenAll(dtos.Select(dto =>
            {
                var entity = MapToEntity(dto);
                return _repository.RemoveAsync(entity, cancellationToken: cancellationToken);
            }));

            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-removebyidasync");
            await _repository.RemoveByIdAsync(id, cancellationToken: cancellationToken);
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("application-applicationservicebasewithdtoasync-removebyidlistasync");

            if (!ids.AnySafe())
            {
                return 0.ToBusiness();
            }

            await Task.WhenAll(ids.Select(id => _repository.RemoveByIdAsync(id, cancellationToken: cancellationToken)));
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
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


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic
{
    /// <summary>
    /// Asynchronous command service base class with high-performance bulk operations
    /// and separate Create/Update DTO support.
    /// </summary>
    /// <typeparam name="TEntity">The entity type for persistence.</typeparam>
    /// <typeparam name="TCreateDto">The DTO type for create operations.</typeparam>
    /// <typeparam name="TUpdateDto">The DTO type for update operations.</typeparam>
    /// <typeparam name="TUoW">The unit of work/DbContext type.</typeparam>
    /// <remarks>
    /// <para>
    /// This class supports the common RESTful API pattern where POST (create) and PUT/PATCH (update)
    /// operations have different payload structures. It provides optimized bulk operations with
    /// separate validation and mapping for each DTO type.
    /// </para>
    /// <para>
    /// <strong>Use Cases:</strong>
    /// <list type="bullet">
    /// <item>APIs with different request bodies for POST vs PUT/PATCH</item>
    /// <item>Scenarios where create requires different fields than update</item>
    /// <item>Importing data with different validation rules for new vs existing records</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomerBulkService : BulkCommandServiceWithSeparateDtosBaseAsync&lt;
    ///     Customer, CreateCustomerDto, UpdateCustomerDto, MyDbContext&gt;
    /// {
    ///     public CustomerBulkService(
    ///         MyDbContext dbContext, 
    ///         IMapper mapper,
    ///         IValidator&lt;CreateCustomerDto&gt; createValidator,
    ///         IValidator&lt;UpdateCustomerDto&gt; updateValidator) 
    ///         : base(dbContext, mapper, createValidator, updateValidator) { }
    ///     
    ///     public async Task ImportNewCustomersAsync(IList&lt;CreateCustomerDto&gt; dtos, CancellationToken ct)
    ///     {
    ///         var result = await BulkAddAsync(dtos, new BulkOperationOptions { BatchSize = 5000 }, ct);
    ///         Console.WriteLine($"Created {result.Data.RowsAffected} customers");
    ///     }
    ///     
    ///     public async Task UpdateCustomersAsync(IList&lt;UpdateCustomerDto&gt; dtos, CancellationToken ct)
    ///     {
    ///         var result = await BulkModifyAsync(dtos, new BulkOperationOptions { BatchSize = 5000 }, ct);
    ///         Console.WriteLine($"Updated {result.Data.RowsAffected} customers");
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IBulkCommandServiceWithSeparateDtosAsync{TCreateDto, TUpdateDto}"/>
    /// <seealso cref="BulkCommandServiceBaseAsync{TEntity, TUoW}"/>
    public abstract class BulkCommandServiceWithSeparateDtosBaseAsync<TEntity, TCreateDto, TUpdateDto, TUoW>
        : BulkCommandServiceBaseAsync<TEntity, TUoW>, IBulkCommandServiceWithSeparateDtosAsync<TCreateDto, TUpdateDto>
        where TEntity : class, IEntityBase
        where TCreateDto : class
        where TUpdateDto : class
        where TUoW : DbContext, IUnitOfWorkAsync
    {
        #region [ Properties / Fields ]

        private readonly IMapper _mapper;
        private readonly IValidator<TCreateDto>? _createDtoValidator;
        private readonly IValidator<TUpdateDto>? _updateDtoValidator;
        private readonly IValidator<TEntity>? _entityValidator;

        /// <summary>
        /// Gets the AutoMapper instance for Entity/DTO mapping.
        /// </summary>
        protected IMapper Mapper => _mapper;

        /// <summary>
        /// Gets the validator for create DTO validation.
        /// </summary>
        protected IValidator<TCreateDto>? CreateDtoValidator => _createDtoValidator;

        /// <summary>
        /// Gets the validator for update DTO validation.
        /// </summary>
        protected IValidator<TUpdateDto>? UpdateDtoValidator => _updateDtoValidator;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance with DbContext and mapper.
        /// </summary>
        /// <param name="dbContext">The DbContext for database operations.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <exception cref="ArgumentNullException">Thrown when dbContext or mapper is null.</exception>
        protected BulkCommandServiceWithSeparateDtosBaseAsync(TUoW dbContext, IMapper mapper)
            : this(dbContext, mapper, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance with DbContext, mapper, and validators.
        /// </summary>
        /// <param name="dbContext">The DbContext for database operations.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <param name="createDtoValidator">The validator for create DTO validation.</param>
        /// <param name="updateDtoValidator">The validator for update DTO validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when dbContext or mapper is null.</exception>
        protected BulkCommandServiceWithSeparateDtosBaseAsync(
            TUoW dbContext,
            IMapper mapper,
            IValidator<TCreateDto>? createDtoValidator,
            IValidator<TUpdateDto>? updateDtoValidator)
            : this(dbContext, mapper, createDtoValidator, updateDtoValidator, null)
        {
        }

        /// <summary>
        /// Initializes a new instance with DbContext, mapper, and all validators.
        /// </summary>
        /// <param name="dbContext">The DbContext for database operations.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <param name="createDtoValidator">The validator for create DTO validation.</param>
        /// <param name="updateDtoValidator">The validator for update DTO validation.</param>
        /// <param name="entityValidator">The validator for entity validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when dbContext or mapper is null.</exception>
        protected BulkCommandServiceWithSeparateDtosBaseAsync(
            TUoW dbContext,
            IMapper mapper,
            IValidator<TCreateDto>? createDtoValidator,
            IValidator<TUpdateDto>? updateDtoValidator,
            IValidator<TEntity>? entityValidator)
            : base(dbContext, entityValidator)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createDtoValidator = createDtoValidator;
            _updateDtoValidator = updateDtoValidator;
            _entityValidator = entityValidator;
        }

        #endregion

        #region [ IBulkCommandServiceWithSeparateDtosAsync - Bulk Add ]

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(
            IList<TCreateDto> dtos,
            CancellationToken cancellationToken = default)
        {
            return BulkAddAsync(dtos, new BulkOperationOptions(), cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(
            IList<TCreateDto> dtos,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceseparatedtosasync-bulkaddasync-start",
                $"count:{dtos?.Count ?? 0}|batchSize:{options.BatchSize}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate and map create DTOs to entities
                var mappingResult = await ValidateAndMapCreateDtosAsync(dtos, cancellationToken);
                if (!mappingResult.HasData() || mappingResult.Data == null)
                {
                    stopwatch.Stop();
                    return BulkOperationResult.Failure(
                        "One or more create DTOs failed validation.",
                        stopwatch.Elapsed).ToBusiness();
                }

                var entities = mappingResult.Data;

                // Execute bulk insert using base class
                var result = await base.BulkAddAsync(entities, options, cancellationToken);

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceseparatedtosasync-bulkaddasync-end",
                    $"rows:{result.Data?.RowsAffected ?? 0}|elapsed:{stopwatch.ElapsedMilliseconds}ms|success:{result.Data?.IsSuccess ?? false}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "application-bulkcommandserviceseparatedtosasync-bulkaddasync-error",
                    $"error:{ex.Message}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed).ToBusiness();
            }
        }

        #endregion

        #region [ IBulkCommandServiceWithSeparateDtosAsync - Bulk Modify ]

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(
            IList<TUpdateDto> dtos,
            CancellationToken cancellationToken = default)
        {
            return BulkModifyAsync(dtos, new BulkOperationOptions(), cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(
            IList<TUpdateDto> dtos,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceseparatedtosasync-bulkmodifyasync-start",
                $"count:{dtos?.Count ?? 0}|batchSize:{options.BatchSize}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate and map update DTOs to entities
                var mappingResult = await ValidateAndMapUpdateDtosAsync(dtos, cancellationToken);
                if (!mappingResult.HasData() || mappingResult.Data == null)
                {
                    stopwatch.Stop();
                    return BulkOperationResult.Failure(
                        "One or more update DTOs failed validation.",
                        stopwatch.Elapsed).ToBusiness();
                }

                var entities = mappingResult.Data;

                // Execute bulk update using base class
                var result = await base.BulkModifyAsync(entities, options, cancellationToken);

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceseparatedtosasync-bulkmodifyasync-end",
                    $"rows:{result.Data?.RowsAffected ?? 0}|elapsed:{stopwatch.ElapsedMilliseconds}ms|success:{result.Data?.IsSuccess ?? false}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "application-bulkcommandserviceseparatedtosasync-bulkmodifyasync-error",
                    $"error:{ex.Message}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed).ToBusiness();
            }
        }

        #endregion

        #region [ Protected Helpers ]

        /// <summary>
        /// Maps a create DTO to its corresponding entity.
        /// </summary>
        /// <param name="dto">The create DTO to map.</param>
        /// <returns>The mapped entity.</returns>
        protected virtual TEntity MapCreateDtoToEntity(TCreateDto dto)
        {
            return _mapper.Map<TEntity>(dto);
        }

        /// <summary>
        /// Maps an update DTO to its corresponding entity.
        /// </summary>
        /// <param name="dto">The update DTO to map.</param>
        /// <returns>The mapped entity.</returns>
        protected virtual TEntity MapUpdateDtoToEntity(TUpdateDto dto)
        {
            return _mapper.Map<TEntity>(dto);
        }

        /// <summary>
        /// Validates all create DTOs and maps them to entities.
        /// </summary>
        /// <param name="dtos">The create DTOs to validate and map.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A business result containing the mapped entities if validation passes.</returns>
        protected virtual Task<IBusinessResult<IList<TEntity>>> ValidateAndMapCreateDtosAsync(
            IList<TCreateDto>? dtos,
            CancellationToken cancellationToken = default)
        {
            if (dtos == null || dtos.Count == 0)
            {
                return Task.FromResult<IBusinessResult<IList<TEntity>>>(
                    Array.Empty<TEntity>().ToList().ToBusiness<IList<TEntity>>());
            }

            var entities = new List<TEntity>(dtos.Count);

            foreach (var dto in dtos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Validate create DTO if validator is available
                if (_createDtoValidator != null)
                {
                    var dtoErrors = dto.TryValidate(_createDtoValidator);
                    if (dtoErrors.AnySafe())
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceseparatedtosasync-validatecreatedtos-failed",
                            $"dtoType:{typeof(TCreateDto).Name}|errorCount:{dtoErrors.Count}");
                        return Task.FromResult(dtoErrors.ToBusiness<IList<TEntity>>());
                    }
                }

                // Map create DTO to Entity
                var entity = MapCreateDtoToEntity(dto);

                // Validate Entity if validator is available
                if (_entityValidator != null)
                {
                    var entityErrors = entity.TryValidate(_entityValidator);
                    if (entityErrors.AnySafe())
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceseparatedtosasync-validateentities-failed",
                            $"entityType:{typeof(TEntity).Name}|errorCount:{entityErrors.Count}");
                        return Task.FromResult(entityErrors.ToBusiness<IList<TEntity>>());
                    }
                }

                entities.Add(entity);
            }

            return Task.FromResult<IBusinessResult<IList<TEntity>>>(entities.ToBusiness<IList<TEntity>>());
        }

        /// <summary>
        /// Validates all update DTOs and maps them to entities.
        /// </summary>
        /// <param name="dtos">The update DTOs to validate and map.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A business result containing the mapped entities if validation passes.</returns>
        protected virtual Task<IBusinessResult<IList<TEntity>>> ValidateAndMapUpdateDtosAsync(
            IList<TUpdateDto>? dtos,
            CancellationToken cancellationToken = default)
        {
            if (dtos == null || dtos.Count == 0)
            {
                return Task.FromResult<IBusinessResult<IList<TEntity>>>(
                    Array.Empty<TEntity>().ToList().ToBusiness<IList<TEntity>>());
            }

            var entities = new List<TEntity>(dtos.Count);

            foreach (var dto in dtos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Validate update DTO if validator is available
                if (_updateDtoValidator != null)
                {
                    var dtoErrors = dto.TryValidate(_updateDtoValidator);
                    if (dtoErrors.AnySafe())
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceseparatedtosasync-validateupdatedtos-failed",
                            $"dtoType:{typeof(TUpdateDto).Name}|errorCount:{dtoErrors.Count}");
                        return Task.FromResult(dtoErrors.ToBusiness<IList<TEntity>>());
                    }
                }

                // Map update DTO to Entity
                var entity = MapUpdateDtoToEntity(dto);

                // Validate Entity if validator is available
                if (_entityValidator != null)
                {
                    var entityErrors = entity.TryValidate(_entityValidator);
                    if (entityErrors.AnySafe())
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceseparatedtosasync-validateentities-failed",
                            $"entityType:{typeof(TEntity).Name}|errorCount:{entityErrors.Count}");
                        return Task.FromResult(entityErrors.ToBusiness<IList<TEntity>>());
                    }
                }

                entities.Add(entity);
            }

            return Task.FromResult<IBusinessResult<IList<TEntity>>>(entities.ToBusiness<IList<TEntity>>());
        }

        #endregion
    }
}


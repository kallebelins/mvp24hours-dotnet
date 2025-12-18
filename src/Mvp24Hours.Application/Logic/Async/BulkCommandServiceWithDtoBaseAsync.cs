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
using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic
{
    /// <summary>
    /// Asynchronous command service base class with high-performance bulk operations and DTO support.
    /// Provides optimized batch processing with automatic Entity/DTO mapping.
    /// </summary>
    /// <typeparam name="TEntity">The entity type for persistence.</typeparam>
    /// <typeparam name="TDto">The DTO type for data transfer.</typeparam>
    /// <typeparam name="TUoW">The unit of work/DbContext type.</typeparam>
    /// <remarks>
    /// <para>
    /// This class provides bulk operations with automatic DTO-to-Entity mapping,
    /// bypassing EF Core change tracking for significantly better performance
    /// when processing large datasets (1000+ entities).
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Automatic DTO to Entity mapping via AutoMapper</item>
    /// <item>Validation of DTOs before bulk operation</item>
    /// <item>Progress callback for long-running operations</item>
    /// <item>Configurable batch size and timeout</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomerBulkService : BulkCommandServiceWithDtoBaseAsync&lt;Customer, CustomerDto, MyDbContext&gt;
    /// {
    ///     public CustomerBulkService(MyDbContext dbContext, IMapper mapper, IValidator&lt;CustomerDto&gt; validator) 
    ///         : base(dbContext, mapper, validator) { }
    ///     
    ///     public async Task ImportFromCsvAsync(IList&lt;CustomerDto&gt; dtos, CancellationToken ct)
    ///     {
    ///         var options = new BulkOperationOptions
    ///         {
    ///             BatchSize = 5000,
    ///             ProgressCallback = (processed, total) =&gt; 
    ///                 Console.WriteLine($"Importing: {processed}/{total}")
    ///         };
    ///         
    ///         var result = await BulkAddAsync(dtos, options, ct);
    ///         Console.WriteLine($"Imported {result.Data.RowsAffected} in {result.Data.ElapsedTime}");
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IBulkCommandServiceWithDtoAsync{TDto}"/>
    /// <seealso cref="BulkCommandServiceBaseAsync{TEntity, TUoW}"/>
    public abstract class BulkCommandServiceWithDtoBaseAsync<TEntity, TDto, TUoW>
        : BulkCommandServiceBaseAsync<TEntity, TUoW>, IBulkCommandServiceWithDtoAsync<TDto>
        where TEntity : class, IEntityBase
        where TDto : class
        where TUoW : DbContext, IUnitOfWorkAsync
    {
        #region [ Properties / Fields ]

        private readonly IMapper _mapper;
        private readonly IValidator<TDto>? _dtoValidator;
        private readonly IValidator<TEntity>? _entityValidator;

        /// <summary>
        /// Gets the AutoMapper instance for Entity/DTO mapping.
        /// </summary>
        protected IMapper Mapper => _mapper;

        /// <summary>
        /// Gets the validator for DTO validation.
        /// </summary>
        protected IValidator<TDto>? DtoValidator => _dtoValidator;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance with DbContext and mapper.
        /// </summary>
        /// <param name="dbContext">The DbContext for database operations.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <exception cref="ArgumentNullException">Thrown when dbContext or mapper is null.</exception>
        protected BulkCommandServiceWithDtoBaseAsync(TUoW dbContext, IMapper mapper)
            : this(dbContext, mapper, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance with DbContext, mapper, and DTO validator.
        /// </summary>
        /// <param name="dbContext">The DbContext for database operations.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <param name="dtoValidator">The validator for DTO validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when dbContext or mapper is null.</exception>
        protected BulkCommandServiceWithDtoBaseAsync(TUoW dbContext, IMapper mapper, IValidator<TDto>? dtoValidator)
            : this(dbContext, mapper, dtoValidator, null)
        {
        }

        /// <summary>
        /// Initializes a new instance with DbContext, mapper, and validators.
        /// </summary>
        /// <param name="dbContext">The DbContext for database operations.</param>
        /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
        /// <param name="dtoValidator">The validator for DTO validation.</param>
        /// <param name="entityValidator">The validator for entity validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when dbContext or mapper is null.</exception>
        protected BulkCommandServiceWithDtoBaseAsync(
            TUoW dbContext,
            IMapper mapper,
            IValidator<TDto>? dtoValidator,
            IValidator<TEntity>? entityValidator)
            : base(dbContext, entityValidator)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _dtoValidator = dtoValidator;
            _entityValidator = entityValidator;
        }

        #endregion

        #region [ IBulkCommandServiceWithDtoAsync - Bulk Add ]

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(
            IList<TDto> dtos,
            CancellationToken cancellationToken = default)
        {
            return BulkAddAsync(dtos, new BulkOperationOptions(), cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(
            IList<TDto> dtos,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandservicedtoasync-bulkaddasync-start",
                $"count:{dtos?.Count ?? 0}|batchSize:{options.BatchSize}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate and map DTOs to entities
                var mappingResult = await ValidateAndMapDtosAsync(dtos, cancellationToken);
                if (!mappingResult.HasData() || mappingResult.Data == null)
                {
                    stopwatch.Stop();
                    return BulkOperationResult.Failure(
                        "One or more DTOs failed validation.",
                        stopwatch.Elapsed).ToBusiness();
                }

                var entities = mappingResult.Data;

                // Execute bulk insert using base class
                var result = await base.BulkAddAsync(entities, options, cancellationToken);

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandservicedtoasync-bulkaddasync-end",
                    $"rows:{result.Data?.RowsAffected ?? 0}|elapsed:{stopwatch.ElapsedMilliseconds}ms|success:{result.Data?.IsSuccess ?? false}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "application-bulkcommandservicedtoasync-bulkaddasync-error",
                    $"error:{ex.Message}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed).ToBusiness();
            }
        }

        #endregion

        #region [ IBulkCommandServiceWithDtoAsync - Bulk Modify ]

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(
            IList<TDto> dtos,
            CancellationToken cancellationToken = default)
        {
            return BulkModifyAsync(dtos, new BulkOperationOptions(), cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(
            IList<TDto> dtos,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandservicedtoasync-bulkmodifyasync-start",
                $"count:{dtos?.Count ?? 0}|batchSize:{options.BatchSize}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate and map DTOs to entities
                var mappingResult = await ValidateAndMapDtosAsync(dtos, cancellationToken);
                if (!mappingResult.HasData() || mappingResult.Data == null)
                {
                    stopwatch.Stop();
                    return BulkOperationResult.Failure(
                        "One or more DTOs failed validation.",
                        stopwatch.Elapsed).ToBusiness();
                }

                var entities = mappingResult.Data;

                // Execute bulk update using base class
                var result = await base.BulkModifyAsync(entities, options, cancellationToken);

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandservicedtoasync-bulkmodifyasync-end",
                    $"rows:{result.Data?.RowsAffected ?? 0}|elapsed:{stopwatch.ElapsedMilliseconds}ms|success:{result.Data?.IsSuccess ?? false}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "application-bulkcommandservicedtoasync-bulkmodifyasync-error",
                    $"error:{ex.Message}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed).ToBusiness();
            }
        }

        #endregion

        #region [ IBulkCommandServiceWithDtoAsync - Bulk Remove ]

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<BulkOperationResult>> BulkRemoveAsync(
            IList<TDto> dtos,
            CancellationToken cancellationToken = default)
        {
            return BulkRemoveAsync(dtos, new BulkOperationOptions(), cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<BulkOperationResult>> BulkRemoveAsync(
            IList<TDto> dtos,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandservicedtoasync-bulkremoveasync-start",
                $"count:{dtos?.Count ?? 0}|batchSize:{options.BatchSize}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Map DTOs to entities (no validation needed for delete)
                var entities = MapDtosToEntities(dtos);

                // Execute bulk delete using base class
                var result = await base.BulkRemoveAsync(entities, options, cancellationToken);

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandservicedtoasync-bulkremoveasync-end",
                    $"rows:{result.Data?.RowsAffected ?? 0}|elapsed:{stopwatch.ElapsedMilliseconds}ms|success:{result.Data?.IsSuccess ?? false}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "application-bulkcommandservicedtoasync-bulkremoveasync-error",
                    $"error:{ex.Message}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed).ToBusiness();
            }
        }

        #endregion

        #region [ Protected Helpers ]

        /// <summary>
        /// Maps a DTO to its corresponding entity.
        /// </summary>
        /// <param name="dto">The DTO to map.</param>
        /// <returns>The mapped entity.</returns>
        protected virtual TEntity MapDtoToEntity(TDto dto)
        {
            return _mapper.Map<TEntity>(dto);
        }

        /// <summary>
        /// Maps a collection of DTOs to entities.
        /// </summary>
        /// <param name="dtos">The DTOs to map.</param>
        /// <returns>A list of mapped entities.</returns>
        protected virtual IList<TEntity> MapDtosToEntities(IList<TDto>? dtos)
        {
            if (dtos == null || dtos.Count == 0)
            {
                return Array.Empty<TEntity>();
            }

            return dtos.Select(MapDtoToEntity).ToList();
        }

        /// <summary>
        /// Validates all DTOs and maps them to entities.
        /// </summary>
        /// <param name="dtos">The DTOs to validate and map.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A business result containing the mapped entities if validation passes.</returns>
        protected virtual Task<IBusinessResult<IList<TEntity>>> ValidateAndMapDtosAsync(
            IList<TDto>? dtos,
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

                // Validate DTO if validator is available
                if (_dtoValidator != null)
                {
                    var dtoErrors = dto.TryValidate(_dtoValidator);
                    if (dtoErrors.AnySafe())
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandservicedtoasync-validatedtos-failed",
                            $"dtoType:{typeof(TDto).Name}|errorCount:{dtoErrors.Count}");
                        return Task.FromResult(dtoErrors.ToBusiness<IList<TEntity>>());
                    }
                }

                // Map DTO to Entity
                var entity = MapDtoToEntity(dto);

                // Validate Entity if validator is available
                if (_entityValidator != null)
                {
                    var entityErrors = entity.TryValidate(_entityValidator);
                    if (entityErrors.AnySafe())
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandservicedtoasync-validateentities-failed",
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


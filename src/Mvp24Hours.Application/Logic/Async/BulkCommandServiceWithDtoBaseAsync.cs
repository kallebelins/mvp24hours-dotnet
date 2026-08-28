//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Logic;

/// <summary>
/// Asynchronous command service base class with high-performance bulk operations and DTO support.
/// Provides optimized batch processing with automatic Entity/DTO mapping.
/// </summary>
/// <typeparam name="TEntity">The entity type for persistence.</typeparam>
/// <typeparam name="TDto">The DTO type for data transfer.</typeparam>
/// <typeparam name="TUoW">The unit of work type.</typeparam>
/// <remarks>
/// <para>
/// This class provides bulk operations with automatic DTO-to-Entity mapping,
/// bypassing change tracking for significantly better performance
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
/// public class CustomerBulkService : BulkCommandServiceWithDtoBaseAsync&lt;Customer, CustomerDto, IUnitOfWorkAsync&gt;
/// {
///     public CustomerBulkService(
///         IUnitOfWorkAsync unitOfWork,
///         IBulkOperationsAsync&lt;Customer&gt; bulkOperations,
///         IMapper mapper,
///         IValidator&lt;CustomerDto&gt; validator) 
///         : base(unitOfWork, bulkOperations, mapper, validator) { }
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
/// <remarks>
/// Initializes a new instance with unit of work, bulk operations, mapper, and validators.
/// </remarks>
/// <param name="unitOfWork">The unit of work for transaction management.</param>
/// <param name="bulkOperations">The bulk operations provider for high-performance batch processing.</param>
/// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
/// <param name="dtoValidator">The validator for DTO validation.</param>
/// <param name="entityValidator">The validator for entity validation.</param>
/// <param name="logger">The logger for logging operations. When omitted, logging is disabled via <c>NullLogger.Instance</c>.</param>
/// <exception cref="ArgumentNullException">Thrown when unitOfWork, bulkOperations, or mapper is null.</exception>
public abstract class BulkCommandServiceWithDtoBaseAsync<TEntity, TDto, TUoW>(
    TUoW unitOfWork,
    IBulkOperationsAsync<TEntity> bulkOperations,
    IMapper mapper,
    IValidator<TDto>? dtoValidator,
    IValidator<TEntity>? entityValidator,
    ILogger? logger = null)
    : BulkCommandServiceBaseAsync<TEntity, TUoW>(unitOfWork, bulkOperations, entityValidator, logger), IBulkCommandServiceWithDtoAsync<TDto>
    where TEntity : class, IEntityBase
    where TDto : class
    where TUoW : class, IUnitOfWorkAsync
{
    #region [ Properties / Fields ]

    private readonly IValidator<TEntity>? _entityValidator = entityValidator;

    /// <summary>
    /// Gets the AutoMapper instance for Entity/DTO mapping.
    /// </summary>
    protected IMapper Mapper { get; } = mapper ?? throw new ArgumentNullException(nameof(mapper));

    /// <summary>
    /// Gets the validator for DTO validation.
    /// </summary>
    protected IValidator<TDto>? DtoValidator { get; } = dtoValidator;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance with unit of work, bulk operations, and mapper.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="bulkOperations">The bulk operations provider for high-performance batch processing.</param>
    /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
    /// <exception cref="ArgumentNullException">Thrown when unitOfWork, bulkOperations, or mapper is null.</exception>
    protected BulkCommandServiceWithDtoBaseAsync(TUoW unitOfWork, IBulkOperationsAsync<TEntity> bulkOperations, IMapper mapper)
        : this(unitOfWork, bulkOperations, mapper, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance with unit of work, bulk operations, mapper, and DTO validator.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="bulkOperations">The bulk operations provider for high-performance batch processing.</param>
    /// <param name="mapper">The AutoMapper instance for Entity/DTO mapping.</param>
    /// <param name="dtoValidator">The validator for DTO validation.</param>
    /// <exception cref="ArgumentNullException">Thrown when unitOfWork, bulkOperations, or mapper is null.</exception>
    protected BulkCommandServiceWithDtoBaseAsync(TUoW unitOfWork, IBulkOperationsAsync<TEntity> bulkOperations, IMapper mapper, IValidator<TDto>? dtoValidator)
        : this(unitOfWork, bulkOperations, mapper, dtoValidator, null)
    {
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
        Logger.LogDebug("application-bulkcommandservicedtoasync-bulkaddasync-start Count={Count} BatchSize={BatchSize}",
            dtos?.Count ?? 0, options.BatchSize);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate and map DTOs to entities
            IBusinessResult<IList<TEntity>> mappingResult = await ValidateAndMapDtosAsync(dtos, cancellationToken);
            if (!mappingResult.HasData() || mappingResult.Data == null)
            {
                stopwatch.Stop();
                return BulkOperationResult.Failure(
                    "One or more DTOs failed validation.",
                    stopwatch.Elapsed).ToBusiness();
            }

            IList<TEntity> entities = mappingResult.Data;

            // Execute bulk insert using base class
            IBusinessResult<BulkOperationResult> result = await base.BulkAddAsync(entities, options, cancellationToken);

            stopwatch.Stop();

            Logger.LogDebug("application-bulkcommandservicedtoasync-bulkaddasync-end RowsAffected={RowsAffected} ElapsedMs={ElapsedMs} Success={Success}",
                result.Data?.RowsAffected ?? 0, stopwatch.ElapsedMilliseconds, result.Data?.IsSuccess ?? false);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Logger.LogError(ex, "application-bulkcommandservicedtoasync-bulkaddasync-error Error={Error} ElapsedMs={ElapsedMs}",
                ex.Message, stopwatch.ElapsedMilliseconds);

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
        Logger.LogDebug("application-bulkcommandservicedtoasync-bulkmodifyasync-start Count={Count} BatchSize={BatchSize}",
            dtos?.Count ?? 0, options.BatchSize);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate and map DTOs to entities
            IBusinessResult<IList<TEntity>> mappingResult = await ValidateAndMapDtosAsync(dtos, cancellationToken);
            if (!mappingResult.HasData() || mappingResult.Data == null)
            {
                stopwatch.Stop();
                return BulkOperationResult.Failure(
                    "One or more DTOs failed validation.",
                    stopwatch.Elapsed).ToBusiness();
            }

            IList<TEntity> entities = mappingResult.Data;

            // Execute bulk update using base class
            IBusinessResult<BulkOperationResult> result = await base.BulkModifyAsync(entities, options, cancellationToken);

            stopwatch.Stop();

            Logger.LogDebug("application-bulkcommandservicedtoasync-bulkmodifyasync-end RowsAffected={RowsAffected} ElapsedMs={ElapsedMs} Success={Success}",
                result.Data?.RowsAffected ?? 0, stopwatch.ElapsedMilliseconds, result.Data?.IsSuccess ?? false);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Logger.LogError(ex, "application-bulkcommandservicedtoasync-bulkmodifyasync-error Error={Error} ElapsedMs={ElapsedMs}",
                ex.Message, stopwatch.ElapsedMilliseconds);

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
        Logger.LogDebug("application-bulkcommandservicedtoasync-bulkremoveasync-start Count={Count} BatchSize={BatchSize}",
            dtos?.Count ?? 0, options.BatchSize);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Map DTOs to entities (no validation needed for delete)
            IList<TEntity> entities = MapDtosToEntities(dtos);

            // Execute bulk delete using base class
            IBusinessResult<BulkOperationResult> result = await base.BulkRemoveAsync(entities, options, cancellationToken);

            stopwatch.Stop();

            Logger.LogDebug("application-bulkcommandservicedtoasync-bulkremoveasync-end RowsAffected={RowsAffected} ElapsedMs={ElapsedMs} Success={Success}",
                result.Data?.RowsAffected ?? 0, stopwatch.ElapsedMilliseconds, result.Data?.IsSuccess ?? false);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Logger.LogError(ex, "application-bulkcommandservicedtoasync-bulkremoveasync-error Error={Error} ElapsedMs={ElapsedMs}",
                ex.Message, stopwatch.ElapsedMilliseconds);

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
        return Mapper.Map<TEntity>(dto);
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

        return [.. dtos.Select(MapDtoToEntity)];
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

        foreach (TDto dto in dtos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Validate DTO if validator is available
            if (DtoValidator != null)
            {
                IList<IMessageResult> dtoErrors = dto.TryValidate(DtoValidator);
                if (dtoErrors.AnySafe())
                {
                    Logger.LogDebug("application-bulkcommandservicedtoasync-validatedtos-failed DtoType={DtoType} ErrorCount={ErrorCount}",
                        typeof(TDto).Name, dtoErrors.Count);
                    return Task.FromResult(dtoErrors.ToBusiness<IList<TEntity>>());
                }
            }

            // Map DTO to Entity
            TEntity entity = MapDtoToEntity(dto);

            // Validate Entity if validator is available
            if (_entityValidator != null)
            {
                IList<IMessageResult> entityErrors = entity.TryValidate(_entityValidator);
                if (entityErrors.AnySafe())
                {
                    Logger.LogDebug("application-bulkcommandservicedtoasync-validateentities-failed EntityType={EntityType} ErrorCount={ErrorCount}",
                        typeof(TEntity).Name, entityErrors.Count);
                    return Task.FromResult(entityErrors.ToBusiness<IList<TEntity>>());
                }
            }

            entities.Add(entity);
        }

        return Task.FromResult<IBusinessResult<IList<TEntity>>>(entities.ToBusiness<IList<TEntity>>());
    }

    #endregion
}

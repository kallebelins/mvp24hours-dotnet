//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Observability;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Logic.Observability;

/// <summary>
/// Observable async application service with structured logging, OpenTelemetry tracing,
/// metrics collection, and audit trail support.
/// </summary>
/// <typeparam name="TEntity">The entity type managed by this service.</typeparam>
/// <typeparam name="TUoW">The unit of work type.</typeparam>
/// <remarks>
/// <para>
/// This class extends the standard application service with full observability:
/// <list type="bullet">
/// <item><strong>Structured Logging</strong> - All operations are logged with context</item>
/// <item><strong>OpenTelemetry Tracing</strong> - Activities/spans for distributed tracing</item>
/// <item><strong>Metrics</strong> - Operation counts, durations, and success rates</item>
/// <item><strong>Audit Trail</strong> - Command operations can be audited</item>
/// <item><strong>Correlation ID</strong> - Automatic propagation of correlation IDs</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CustomerService : ObservableApplicationServiceBaseAsync&lt;Customer, MyDbContext&gt;
/// {
///     public CustomerService(
///         MyDbContext unitOfWork,
///         ILogger&lt;CustomerService&gt; logger,
///         ICorrelationIdAccessor correlationId,
///         IOperationMetrics metrics,
///         IApplicationAuditStore? auditStore = null,
///         IValidator&lt;Customer&gt;? validator = null)
///         : base(unitOfWork, logger, correlationId, metrics, auditStore, validator)
///     {
///     }
/// }
/// </code>
/// </example>
public abstract class ObservableApplicationServiceBaseAsync<TEntity, TUoW>
    : IApplicationServiceAsync<TEntity>, IReadOnlyApplicationServiceAsync<TEntity>
    where TEntity : class, IEntityBase
    where TUoW : class, IUnitOfWorkAsync
{
    #region [ Fields ]

    private readonly IRepositoryAsync<TEntity> _repository;
    private readonly TUoW _unitOfWork;
    private readonly ILogger _logger;
    private readonly ICorrelationIdAccessor _correlationId;
    private readonly IOperationMetrics _metrics;
    private readonly IApplicationAuditStore? _auditStore;
    private readonly IValidator<TEntity>? _validator;

    private readonly string _serviceName;
    private readonly string _entityTypeName;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        MaxDepth = 3,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the unit of work instance.
    /// </summary>
    protected virtual TUoW UnitOfWork => _unitOfWork;

    /// <summary>
    /// Gets the repository instance.
    /// </summary>
    protected virtual IRepositoryAsync<TEntity> Repository => _repository;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger => _logger;

    /// <summary>
    /// Gets the correlation ID accessor.
    /// </summary>
    protected ICorrelationIdAccessor CorrelationIdAccessor => _correlationId;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Creates a new instance of the observable application service.
    /// </summary>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="correlationId">The correlation ID accessor.</param>
    /// <param name="metrics">The metrics collector.</param>
    /// <param name="auditStore">Optional audit store for command audit trail.</param>
    /// <param name="validator">Optional entity validator.</param>
    protected ObservableApplicationServiceBaseAsync(
        TUoW unitOfWork,
        ILogger logger,
        ICorrelationIdAccessor correlationId,
        IOperationMetrics metrics,
        IApplicationAuditStore? auditStore = null,
        IValidator<TEntity>? validator = null)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _correlationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _auditStore = auditStore;
        _validator = validator;
        _repository = unitOfWork.GetRepository<TEntity>();

        _serviceName = GetType().Name;
        _entityTypeName = typeof(TEntity).Name;
    }

    #endregion

    #region [ Query Operations ]

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<bool>> ListAnyAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteQueryAsync(
            "ListAny",
            async ct => await _repository.ListAnyAsync(cancellationToken: ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<int>> ListCountAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteQueryAsync(
            "ListCount",
            async ct => await _repository.ListCountAsync(cancellationToken: ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<IBusinessResult<IList<TEntity>>> ListAsync(CancellationToken cancellationToken = default)
    {
        return ListAsync(null, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<IList<TEntity>>> ListAsync(IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        return await ExecuteQueryAsync(
            "List",
            async ct =>
            {
                var result = await _repository.ListAsync(criteria, cancellationToken: ct);
                return result ?? new List<TEntity>();
            },
            cancellationToken,
            metadata: criteria != null ? new { Offset = criteria.Offset, Limit = criteria.Limit } : null);
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<bool>> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
    {
        return await ExecuteQueryAsync(
            "GetByAny",
            async ct => await _repository.GetByAnyAsync(clause, cancellationToken: ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<int>> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
    {
        return await ExecuteQueryAsync(
            "GetByCount",
            async ct => await _repository.GetByCountAsync(clause, cancellationToken: ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
    {
        return GetByAsync(clause, null, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        return await ExecuteQueryAsync(
            "GetBy",
            async ct =>
            {
                var result = await _repository.GetByAsync(clause, criteria, cancellationToken: ct);
                return result ?? new List<TEntity>();
            },
            cancellationToken,
            metadata: criteria != null ? new { Offset = criteria.Offset, Limit = criteria.Limit } : null);
    }

    /// <inheritdoc />
    public virtual Task<IBusinessResult<TEntity>> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return GetByIdAsync(id, null, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<TEntity>> GetByIdAsync(object id, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        return await ExecuteQueryAsync(
            "GetById",
            async ct => await _repository.GetByIdAsync(id, criteria, cancellationToken: ct),
            cancellationToken,
            entityId: id);
    }

    #endregion

    #region [ Command Operations ]

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<int>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var errors = entity.TryValidate(_validator);
        if (errors.AnySafe())
        {
            LogValidationFailure("Add", errors);
            return errors.ToBusiness<int>();
        }

        return await ExecuteCommandAsync(
            "Add",
            async ct =>
            {
                await _repository.AddAsync(entity, cancellationToken: ct);
                return await _unitOfWork.SaveChangesAsync(cancellationToken: ct);
            },
            cancellationToken,
            entityId: GetEntityId(entity),
            inputData: entity);
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<int>> AddAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        foreach (var entity in entities)
        {
            var errors = entity.TryValidate(_validator);
            if (errors.AnySafe())
            {
                LogValidationFailure("Add", errors);
                return errors.ToBusiness<int>();
            }
        }

        return await ExecuteCommandAsync(
            "AddBatch",
            async ct =>
            {
                await Task.WhenAll(entities.Select(e => _repository.AddAsync(e, cancellationToken: ct)));
                return await _unitOfWork.SaveChangesAsync(cancellationToken: ct);
            },
            cancellationToken,
            entityId: $"[{entities.Count} entities]");
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<int>> ModifyAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var errors = entity.TryValidate(_validator);
        if (errors.AnySafe())
        {
            LogValidationFailure("Modify", errors);
            return errors.ToBusiness<int>();
        }

        return await ExecuteCommandAsync(
            "Modify",
            async ct =>
            {
                await _repository.ModifyAsync(entity, cancellationToken: ct);
                return await _unitOfWork.SaveChangesAsync(cancellationToken: ct);
            },
            cancellationToken,
            entityId: GetEntityId(entity),
            inputData: entity);
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<int>> ModifyAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        foreach (var entity in entities)
        {
            var errors = entity.TryValidate(_validator);
            if (errors.AnySafe())
            {
                LogValidationFailure("Modify", errors);
                return errors.ToBusiness<int>();
            }
        }

        return await ExecuteCommandAsync(
            "ModifyBatch",
            async ct =>
            {
                await Task.WhenAll(entities.Select(e => _repository.ModifyAsync(e, cancellationToken: ct)));
                return await _unitOfWork.SaveChangesAsync(cancellationToken: ct);
            },
            cancellationToken,
            entityId: $"[{entities.Count} entities]");
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<int>> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteCommandAsync(
            "Remove",
            async ct =>
            {
                await _repository.RemoveAsync(entity, cancellationToken: ct);
                return await _unitOfWork.SaveChangesAsync(cancellationToken: ct);
            },
            cancellationToken,
            entityId: GetEntityId(entity));
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<int>> RemoveAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        return await ExecuteCommandAsync(
            "RemoveBatch",
            async ct =>
            {
                await Task.WhenAll(entities.Select(e => _repository.RemoveAsync(e, cancellationToken: ct)));
                return await _unitOfWork.SaveChangesAsync(cancellationToken: ct);
            },
            cancellationToken,
            entityId: $"[{entities.Count} entities]");
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return await ExecuteCommandAsync(
            "RemoveById",
            async ct =>
            {
                await _repository.RemoveByIdAsync(id, cancellationToken: ct);
                return await _unitOfWork.SaveChangesAsync(cancellationToken: ct);
            },
            cancellationToken,
            entityId: id);
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default)
    {
        if (!ids.AnySafe())
        {
            return 0.ToBusiness();
        }

        return await ExecuteCommandAsync(
            "RemoveByIdBatch",
            async ct =>
            {
                await Task.WhenAll(ids.Select(id => _repository.RemoveByIdAsync(id, cancellationToken: ct)));
                return await _unitOfWork.SaveChangesAsync(cancellationToken: ct);
            },
            cancellationToken,
            entityId: $"[{ids.Count} ids]");
    }

    #endregion

    #region [ Specification Pattern ]

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<bool>> AnyBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return await ExecuteQueryAsync(
            $"AnyBySpecification<{typeof(TSpec).Name}>",
            async ct =>
            {
                if (specification == null)
                    return false;

                if (_repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
                    return await readOnlyRepo.AnyBySpecificationAsync(specification, ct);

                return await _repository.GetByAnyAsync(specification.IsSatisfiedByExpression, cancellationToken: ct);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<int>> CountBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return await ExecuteQueryAsync(
            $"CountBySpecification<{typeof(TSpec).Name}>",
            async ct =>
            {
                if (specification == null)
                    return 0;

                if (_repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
                    return await readOnlyRepo.CountBySpecificationAsync(specification, ct);

                return await _repository.GetByCountAsync(specification.IsSatisfiedByExpression, cancellationToken: ct);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<IList<TEntity>>> GetBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return await ExecuteQueryAsync(
            $"GetBySpecification<{typeof(TSpec).Name}>",
            async ct =>
            {
                if (specification == null)
                    return (IList<TEntity>)new List<TEntity>();

                if (_repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
                    return await readOnlyRepo.GetBySpecificationAsync(specification, ct);

                var result = await _repository.GetByAsync(specification.IsSatisfiedByExpression, null, cancellationToken: ct);
                return result ?? new List<TEntity>();
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<TEntity?>> GetSingleBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return await ExecuteQueryAsync(
            $"GetSingleBySpecification<{typeof(TSpec).Name}>",
            async ct =>
            {
                if (specification == null)
                    return (TEntity?)null;

                if (_repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
                    return await readOnlyRepo.GetSingleBySpecificationAsync(specification, ct);

                var result = await _repository.GetByAsync(specification.IsSatisfiedByExpression, null, cancellationToken: ct);
                return result?.SingleOrDefault();
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IBusinessResult<TEntity?>> GetFirstBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return await ExecuteQueryAsync(
            $"GetFirstBySpecification<{typeof(TSpec).Name}>",
            async ct =>
            {
                if (specification == null)
                    return (TEntity?)null;

                if (_repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
                    return await readOnlyRepo.GetFirstBySpecificationAsync(specification, ct);

                var result = await _repository.GetByAsync(specification.IsSatisfiedByExpression, null, cancellationToken: ct);
                return result?.FirstOrDefault();
            },
            cancellationToken);
    }

    #endregion

    #region [ Private Helper Methods ]

    private async Task<IBusinessResult<T>> ExecuteQueryAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken,
        object? entityId = null,
        object? metadata = null)
    {
        var stopwatch = Stopwatch.StartNew();
        Activity? activity = null;

        try
        {
            _metrics.RecordOperationStart(_serviceName, operationName, "Query");
            activity = ApplicationActivitySource.StartQueryActivity(_serviceName, operationName, _entityTypeName);
            SetActivityContext(activity, entityId);

            LogOperationStart(operationName, "Query", entityId, metadata);

            var result = await operation(cancellationToken);
            stopwatch.Stop();

            ApplicationActivitySource.SetSuccess(activity);
            _metrics.RecordOperationSuccess(_serviceName, operationName, "Query", stopwatch.ElapsedMilliseconds);

            LogOperationSuccess(operationName, "Query", stopwatch.ElapsedMilliseconds, GetResultCount(result));

            return result.ToBusiness();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            ApplicationActivitySource.SetError(activity, ex);
            _metrics.RecordOperationFailure(_serviceName, operationName, "Query", stopwatch.ElapsedMilliseconds, ex.GetType().Name);

            LogOperationFailure(operationName, "Query", stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    private async Task<IBusinessResult<int>> ExecuteCommandAsync(
        string operationName,
        Func<CancellationToken, Task<int>> operation,
        CancellationToken cancellationToken,
        object? entityId = null,
        object? inputData = null)
    {
        var stopwatch = Stopwatch.StartNew();
        Activity? activity = null;
        ApplicationAuditEntry? auditEntry = null;

        try
        {
            _metrics.RecordOperationStart(_serviceName, operationName, "Command");
            activity = ApplicationActivitySource.StartCommandActivity(_serviceName, operationName, _entityTypeName);
            SetActivityContext(activity, entityId);

            auditEntry = CreateAuditEntry(operationName, entityId, inputData);

            LogOperationStart(operationName, "Command", entityId);

            var result = await operation(cancellationToken);
            stopwatch.Stop();

            ApplicationActivitySource.SetAffectedRows(activity, result);
            ApplicationActivitySource.SetSuccess(activity);
            _metrics.RecordOperationSuccess(_serviceName, operationName, "Command", stopwatch.ElapsedMilliseconds);

            if (auditEntry != null)
            {
                auditEntry.IsSuccess = true;
                auditEntry.DurationMs = stopwatch.ElapsedMilliseconds;
                await SaveAuditEntryAsync(auditEntry, cancellationToken);
            }

            LogOperationSuccess(operationName, "Command", stopwatch.ElapsedMilliseconds, result);

            return result.ToBusiness();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            ApplicationActivitySource.SetError(activity, ex);
            _metrics.RecordOperationFailure(_serviceName, operationName, "Command", stopwatch.ElapsedMilliseconds, ex.GetType().Name);

            if (auditEntry != null)
            {
                auditEntry.IsSuccess = false;
                auditEntry.DurationMs = stopwatch.ElapsedMilliseconds;
                auditEntry.ErrorType = ex.GetType().FullName;
                auditEntry.ErrorMessage = ex.Message;
                await SaveAuditEntryAsync(auditEntry, cancellationToken);
            }

            LogOperationFailure(operationName, "Command", stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    private void SetActivityContext(Activity? activity, object? entityId)
    {
        ApplicationActivitySource.SetCorrelationContext(
            activity,
            _correlationId.CorrelationId,
            _correlationId.CausationId);

        if (entityId != null)
            ApplicationActivitySource.SetEntityId(activity, entityId);
    }

    private ApplicationAuditEntry? CreateAuditEntry(string operationName, object? entityId, object? inputData)
    {
        if (_auditStore == null)
            return null;

        return new ApplicationAuditEntry
        {
            OperationName = operationName,
            OperationType = "Command",
            ServiceName = _serviceName,
            EntityType = _entityTypeName,
            EntityIds = entityId?.ToString(),
            CorrelationId = _correlationId.CorrelationId,
            CausationId = _correlationId.CausationId,
            InputData = inputData != null ? SafeSerialize(inputData) : null,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private async Task SaveAuditEntryAsync(ApplicationAuditEntry entry, CancellationToken cancellationToken)
    {
        if (_auditStore == null)
            return;

        try
        {
            await _auditStore.SaveAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[{ServiceName}] Failed to save audit entry for {OperationName}: {Message}",
                _serviceName, entry.OperationName, ex.Message);
        }
    }

    private void LogOperationStart(string operationName, string operationType, object? entityId = null, object? metadata = null)
    {
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = _correlationId.CorrelationId,
            ["ServiceName"] = _serviceName,
            ["EntityType"] = _entityTypeName,
            ["OperationType"] = operationType
        }))
        {
            if (entityId != null)
            {
                _logger.LogDebug(
                    "Starting {OperationType} operation {ServiceName}.{OperationName} for entity {EntityId}",
                    operationType, _serviceName, operationName, entityId);
            }
            else
            {
                _logger.LogDebug(
                    "Starting {OperationType} operation {ServiceName}.{OperationName}",
                    operationType, _serviceName, operationName);
            }
        }
    }

    private void LogOperationSuccess(string operationName, string operationType, long durationMs, object? resultInfo = null)
    {
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = _correlationId.CorrelationId,
            ["ServiceName"] = _serviceName,
            ["DurationMs"] = durationMs
        }))
        {
            _logger.LogInformation(
                "Completed {OperationType} operation {ServiceName}.{OperationName} in {DurationMs}ms. Result: {ResultInfo}",
                operationType, _serviceName, operationName, durationMs, resultInfo ?? "N/A");
        }
    }

    private void LogOperationFailure(string operationName, string operationType, long durationMs, Exception ex)
    {
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = _correlationId.CorrelationId,
            ["ServiceName"] = _serviceName,
            ["DurationMs"] = durationMs
        }))
        {
            _logger.LogError(
                ex,
                "Failed {OperationType} operation {ServiceName}.{OperationName} after {DurationMs}ms: {ErrorMessage}",
                operationType, _serviceName, operationName, durationMs, ex.Message);
        }
    }

    private void LogValidationFailure(string operationName, IList<Mvp24Hours.Core.Contract.ValueObjects.Logic.IMessageResult> errors)
    {
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = _correlationId.CorrelationId,
            ["ServiceName"] = _serviceName
        }))
        {
            var errorMessages = errors.Select(e => e.Message);
            _logger.LogWarning(
                "Validation failed for {ServiceName}.{OperationName}: {Errors}",
                _serviceName, operationName, string.Join(", ", errorMessages));
        }
    }

    private static object? GetEntityId(TEntity entity)
    {
        // Try common ID property names
        var idProperty = typeof(TEntity).GetProperty("Id")
            ?? typeof(TEntity).GetProperty("ID")
            ?? typeof(TEntity).GetProperty($"{typeof(TEntity).Name}Id");

        return idProperty?.GetValue(entity);
    }

    private static int? GetResultCount<T>(T result)
    {
        if (result is System.Collections.ICollection collection)
            return collection.Count;
        return null;
    }

    private string? SafeSerialize<T>(T obj)
    {
        if (obj == null)
            return null;

        try
        {
            return JsonSerializer.Serialize(obj, JsonOptions);
        }
        catch
        {
            return $"[{typeof(T).Name}]";
        }
    }

    #endregion
}


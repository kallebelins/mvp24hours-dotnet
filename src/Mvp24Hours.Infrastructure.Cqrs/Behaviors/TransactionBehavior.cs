//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using CoreHasDomainEvents = Mvp24Hours.Core.Contract.Domain.Entity.IHasDomainEvents;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Marker interface for commands that should be executed within a transaction.
/// The transaction will be committed if the handler succeeds, or rolled back on failure.
/// </summary>
/// <remarks>
/// <para>
/// Apply this interface to commands that modify data and need transactional guarantees.
/// The <see cref="TransactionBehavior{TRequest, TResponse}"/> will automatically
/// wrap the command execution in a transaction.
/// </para>
/// <para>
/// <strong>Note:</strong> The actual transaction is managed by the <see cref="IUnitOfWorkAsync"/>
/// implementation. The behavior calls SaveChangesAsync on success and RollbackAsync on failure.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CreateOrderCommand : IMediatorCommand&lt;Order&gt;, ITransactional
/// {
///     public string CustomerName { get; init; } = string.Empty;
///     public List&lt;OrderItemDto&gt; Items { get; init; } = new();
/// }
/// </code>
/// </example>
public interface ITransactional
{
}

/// <summary>
/// Pipeline behavior that wraps command execution in a database transaction.
/// Only applies to requests that implement <see cref="ITransactional"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior integrates with <see cref="IUnitOfWorkAsync"/> to provide
/// transactional consistency for commands.
/// </para>
/// <para>
/// <strong>Transaction Flow:</strong>
/// <list type="number">
/// <item>Handler executes and may modify entities</item>
/// <item>If successful, SaveChangesAsync is called to commit</item>
/// <item>If any exception occurs, RollbackAsync is called</item>
/// </list>
/// </para>
/// <para>
/// <strong>Note:</strong> Domain events should be dispatched AFTER the transaction
/// is committed. Use <see cref="TransactionWithEventsBehavior{TRequest, TResponse}"/>
/// for automatic domain event dispatching.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(TransactionBehavior&lt;,&gt;));
/// </code>
/// </example>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IUnitOfWorkAsync? _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>>? _logger;

    /// <summary>
    /// Creates a new instance of the TransactionBehavior.
    /// </summary>
    /// <param name="unitOfWork">Optional unit of work for transaction management.</param>
    /// <param name="logger">Optional logger for recording transaction operations.</param>
    /// <remarks>
    /// If no unit of work is provided, this behavior does nothing.
    /// This allows it to be registered globally without affecting non-transactional requests.
    /// </remarks>
    public TransactionBehavior(
        IUnitOfWorkAsync? unitOfWork = null,
        ILogger<TransactionBehavior<TRequest, TResponse>>? logger = null)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only apply transaction if the request implements ITransactional and we have a UnitOfWork
        if (request is not ITransactional || _unitOfWork == null)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;

        _logger?.LogDebug(
            "[Transaction] Starting transaction for {RequestName}",
            requestName);

        try
        {
            // Execute the handler
            var response = await next();

            // Commit the transaction
            var affectedRows = await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger?.LogDebug(
                "[Transaction] Committed transaction for {RequestName} ({AffectedRows} rows affected)",
                requestName,
                affectedRows);

            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "[Transaction] Rolling back transaction for {RequestName}: {Message}",
                requestName,
                ex.Message);

            // Rollback on any exception
            try
            {
                await _unitOfWork.RollbackAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger?.LogError(
                    rollbackEx,
                    "[Transaction] Error during rollback for {RequestName}: {Message}",
                    requestName,
                    rollbackEx.Message);
            }

            throw;
        }
    }
}

/// <summary>
/// Pipeline behavior that wraps command execution in a transaction and dispatches domain events.
/// Only applies to requests that implement <see cref="ITransactional"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior extends <see cref="TransactionBehavior{TRequest, TResponse}"/> to also
/// dispatch domain events after the transaction is successfully committed.
/// </para>
/// <para>
/// <strong>Event Dispatching:</strong>
/// Domain events are collected from the response if it implements <see cref="IHasDomainEvents"/>,
/// and dispatched after the transaction commits.
/// </para>
/// </remarks>
public sealed class TransactionWithEventsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IUnitOfWorkAsync? _unitOfWork;
    private readonly IDomainEventDispatcher? _eventDispatcher;
    private readonly ILogger<TransactionWithEventsBehavior<TRequest, TResponse>>? _logger;

    /// <summary>
    /// Creates a new instance of the TransactionWithEventsBehavior.
    /// </summary>
    /// <param name="unitOfWork">Optional unit of work for transaction management.</param>
    /// <param name="eventDispatcher">Optional domain event dispatcher.</param>
    /// <param name="logger">Optional logger for recording operations.</param>
    public TransactionWithEventsBehavior(
        IUnitOfWorkAsync? unitOfWork = null,
        IDomainEventDispatcher? eventDispatcher = null,
        ILogger<TransactionWithEventsBehavior<TRequest, TResponse>>? logger = null)
    {
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only apply if the request implements ITransactional and we have a UnitOfWork
        if (request is not ITransactional || _unitOfWork == null)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;

        _logger?.LogDebug(
            "[Transaction] Starting transaction with events for {RequestName}",
            requestName);

        try
        {
            // Execute the handler
            var response = await next();

            // Commit the transaction
            var affectedRows = await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger?.LogDebug(
                "[Transaction] Committed transaction for {RequestName} ({AffectedRows} rows affected)",
                requestName,
                affectedRows);

            // Dispatch domain events from the response if applicable
            if (_eventDispatcher != null && response is CoreHasDomainEvents entityWithEvents)
            {
                await _eventDispatcher.DispatchEventsAsync(entityWithEvents, cancellationToken);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "[Transaction] Rolling back transaction for {RequestName}: {Message}",
                requestName,
                ex.Message);

            try
            {
                await _unitOfWork.RollbackAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger?.LogError(
                    rollbackEx,
                    "[Transaction] Error during rollback for {RequestName}: {Message}",
                    requestName,
                    rollbackEx.Message);
            }

            throw;
        }
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Marker interface for a command that returns a response of type <typeparamref name="TResponse"/>.
/// Commands represent write operations that modify state in the system.
/// </summary>
/// <typeparam name="TResponse">The type of response expected from the command handler.</typeparam>
/// <remarks>
/// <para>
/// <strong>CQRS Pattern:</strong> In Command Query Responsibility Segregation (CQRS),
/// commands are used for write operations (Create, Update, Delete) while queries are used
/// for read operations.
/// </para>
/// <para>
/// <strong>Important:</strong> This interface (<see cref="IMediatorCommand{TResponse}"/>) is used for 
/// CQRS/Mediator commands and is DIFFERENT from <see cref="Mvp24Hours.Core.Contract.Data.ICommand{TEntity}"/>
/// which is used for Repository operations (Add/Modify/Remove entities in database).
/// </para>
/// <para>
/// <strong>Naming Convention:</strong>
/// <list type="bullet">
/// <item><c>IMediatorCommand</c> - CQRS commands sent through Mediator (this interface)</item>
/// <item><c>ICommand&lt;TEntity&gt;</c> - Repository pattern for database operations (existing)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Command with response (e.g., returns the created entity)
/// public class CreateOrderCommand : IMediatorCommand&lt;Order&gt;
/// {
///     public string CustomerName { get; init; } = string.Empty;
///     public List&lt;OrderItem&gt; Items { get; init; } = new();
/// }
/// 
/// public class CreateOrderCommandHandler : IMediatorCommandHandler&lt;CreateOrderCommand, Order&gt;
/// {
///     private readonly IUnitOfWork _unitOfWork;
///     
///     public CreateOrderCommandHandler(IUnitOfWork unitOfWork)
///     {
///         _unitOfWork = unitOfWork;
///     }
///     
///     public async Task&lt;Order&gt; Handle(CreateOrderCommand request, CancellationToken cancellationToken)
///     {
///         var order = new Order { CustomerName = request.CustomerName };
///         _unitOfWork.GetRepository&lt;Order&gt;().Add(order);
///         await _unitOfWork.SaveChangesAsync();
///         return order;
///     }
/// }
/// </code>
/// </example>
public interface IMediatorCommand<out TResponse> : IMediatorRequest<TResponse>
{
}

/// <summary>
/// Marker interface for a command that doesn't return a meaningful value.
/// Used for write operations where no return data is needed.
/// </summary>
/// <remarks>
/// <para>
/// Use this interface for operations like delete, update status, or any action
/// that modifies state without needing to return the result.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DeleteOrderCommand : IMediatorCommand
/// {
///     public int OrderId { get; init; }
/// }
/// 
/// public class DeleteOrderCommandHandler : IMediatorCommandHandler&lt;DeleteOrderCommand&gt;
/// {
///     private readonly IUnitOfWork _unitOfWork;
///     
///     public DeleteOrderCommandHandler(IUnitOfWork unitOfWork)
///     {
///         _unitOfWork = unitOfWork;
///     }
///     
///     public async Task&lt;Unit&gt; Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
///     {
///         _unitOfWork.GetRepository&lt;Order&gt;().RemoveById(request.OrderId);
///         await _unitOfWork.SaveChangesAsync();
///         return Unit.Value;
///     }
/// }
/// </code>
/// </example>
public interface IMediatorCommand : IMediatorCommand<Unit>
{
}

/// <summary>
/// Defines a handler for a command of type <typeparamref name="TCommand"/>
/// that returns a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
/// <typeparam name="TResponse">The type of response to return.</typeparam>
/// <remarks>
/// This is a semantic alias for <see cref="IMediatorRequestHandler{TRequest, TResponse}"/>
/// to improve code readability when working with CQRS patterns.
/// </remarks>
public interface IMediatorCommandHandler<in TCommand, TResponse> : IMediatorRequestHandler<TCommand, TResponse>
    where TCommand : IMediatorCommand<TResponse>
{
}

/// <summary>
/// Defines a handler for a command that doesn't return a meaningful value.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
/// <remarks>
/// This is a semantic alias for <see cref="IMediatorRequestHandler{TRequest}"/>
/// to improve code readability when working with CQRS patterns.
/// </remarks>
public interface IMediatorCommandHandler<in TCommand> : IMediatorCommandHandler<TCommand, Unit>
    where TCommand : IMediatorCommand
{
}


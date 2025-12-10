//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Marker interface for a query that returns a response of type <typeparamref name="TResponse"/>.
/// Queries represent read operations that don't modify state in the system.
/// </summary>
/// <typeparam name="TResponse">The type of response expected from the query handler.</typeparam>
/// <remarks>
/// <para>
/// <strong>CQRS Pattern:</strong> In Command Query Responsibility Segregation (CQRS),
/// queries are used for read operations while commands are used for write operations.
/// </para>
/// <para>
/// <strong>Important:</strong> This interface (<see cref="IMediatorQuery{TResponse}"/>) is used for 
/// CQRS/Mediator queries and is DIFFERENT from <see cref="Mvp24Hours.Core.Contract.Data.IQuery{TEntity}"/>
/// which is used for Repository query operations.
/// </para>
/// <para>
/// <strong>Naming Convention:</strong>
/// <list type="bullet">
/// <item><c>IMediatorQuery</c> - CQRS queries sent through Mediator (this interface)</item>
/// <item><c>IQuery&lt;TEntity&gt;</c> - Repository pattern for database read operations (existing)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Best Practices:</strong>
/// <list type="bullet">
/// <item>Queries should be idempotent and not cause side effects</item>
/// <item>Queries can use read-optimized data sources or views</item>
/// <item>Queries should never modify the state of the system</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query to get a single entity
/// public class GetOrderByIdQuery : IMediatorQuery&lt;Order?&gt;
/// {
///     public int OrderId { get; init; }
/// }
/// 
/// public class GetOrderByIdQueryHandler : IMediatorQueryHandler&lt;GetOrderByIdQuery, Order?&gt;
/// {
///     private readonly IUnitOfWork _unitOfWork;
///     
///     public GetOrderByIdQueryHandler(IUnitOfWork unitOfWork)
///     {
///         _unitOfWork = unitOfWork;
///     }
///     
///     public async Task&lt;Order?&gt; Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
///     {
///         return await _unitOfWork.GetRepository&lt;Order&gt;()
///             .GetByIdAsync(request.OrderId);
///     }
/// }
/// </code>
/// </example>
public interface IMediatorQuery<out TResponse> : IMediatorRequest<TResponse>
{
}

/// <summary>
/// Defines a handler for a query of type <typeparamref name="TQuery"/>
/// that returns a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TQuery">The type of query to handle.</typeparam>
/// <typeparam name="TResponse">The type of response to return.</typeparam>
/// <remarks>
/// This is a semantic alias for <see cref="IMediatorRequestHandler{TRequest, TResponse}"/>
/// to improve code readability when working with CQRS patterns.
/// </remarks>
/// <example>
/// <code>
/// // Query for paginated results
/// public class GetOrdersQuery : IMediatorQuery&lt;IPagingResult&lt;Order&gt;&gt;
/// {
///     public int Page { get; init; } = 1;
///     public int PageSize { get; init; } = 10;
///     public string? CustomerName { get; init; }
/// }
/// 
/// public class GetOrdersQueryHandler : IMediatorQueryHandler&lt;GetOrdersQuery, IPagingResult&lt;Order&gt;&gt;
/// {
///     private readonly IUnitOfWork _unitOfWork;
///     
///     public GetOrdersQueryHandler(IUnitOfWork unitOfWork)
///     {
///         _unitOfWork = unitOfWork;
///     }
///     
///     public async Task&lt;IPagingResult&lt;Order&gt;&gt; Handle(GetOrdersQuery request, CancellationToken cancellationToken)
///     {
///         var repository = _unitOfWork.GetRepository&lt;Order&gt;();
///         
///         var criteria = new PagingCriteria(request.Page, request.PageSize);
///         
///         if (!string.IsNullOrEmpty(request.CustomerName))
///         {
///             return await repository.GetByAsync(
///                 o => o.CustomerName.Contains(request.CustomerName),
///                 criteria);
///         }
///         
///         return await repository.ListAsync(criteria);
///     }
/// }
/// </code>
/// </example>
public interface IMediatorQueryHandler<in TQuery, TResponse> : IMediatorRequestHandler<TQuery, TResponse>
    where TQuery : IMediatorQuery<TResponse>
{
}


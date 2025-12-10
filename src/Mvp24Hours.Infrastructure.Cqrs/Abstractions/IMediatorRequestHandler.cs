//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Defines a handler for a request of type <typeparamref name="TRequest"/>
/// that returns a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request to handle.</typeparam>
/// <typeparam name="TResponse">The type of response to return.</typeparam>
/// <remarks>
/// <para>
/// Each request type must have exactly one handler. If multiple handlers are registered
/// for the same request type, only one will be resolved (typically the last registered).
/// </para>
/// <para>
/// Handlers are resolved from the dependency injection container at runtime.
/// Make sure to register your handlers using the <c>AddMvpMediator</c> extension method.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class GetUserByIdQuery : IMediatorQuery&lt;User&gt;
/// {
///     public int UserId { get; init; }
/// }
/// 
/// public class GetUserByIdQueryHandler : IMediatorRequestHandler&lt;GetUserByIdQuery, User&gt;
/// {
///     private readonly IUserRepository _repository;
///     
///     public GetUserByIdQueryHandler(IUserRepository repository)
///     {
///         _repository = repository;
///     }
///     
///     public async Task&lt;User&gt; Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
///     {
///         return await _repository.GetByIdAsync(request.UserId, cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface IMediatorRequestHandler<in TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    /// <summary>
    /// Handles the request and returns the response.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The response from handling the request.</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Defines a handler for a request that doesn't return a meaningful value.
/// </summary>
/// <typeparam name="TRequest">The type of request to handle.</typeparam>
/// <remarks>
/// <para>
/// This is a convenience interface that inherits from <see cref="IMediatorRequestHandler{TRequest, Unit}"/>.
/// The handler must return <see cref="Unit.Value"/> or <see cref="Unit.Task"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DeleteUserCommand : IMediatorCommand
/// {
///     public int UserId { get; init; }
/// }
/// 
/// public class DeleteUserCommandHandler : IMediatorRequestHandler&lt;DeleteUserCommand&gt;
/// {
///     private readonly IUserRepository _repository;
///     
///     public DeleteUserCommandHandler(IUserRepository repository)
///     {
///         _repository = repository;
///     }
///     
///     public async Task&lt;Unit&gt; Handle(DeleteUserCommand request, CancellationToken cancellationToken)
///     {
///         await _repository.DeleteAsync(request.UserId, cancellationToken);
///         return Unit.Value;
///     }
/// }
/// </code>
/// </example>
public interface IMediatorRequestHandler<in TRequest> : IMediatorRequestHandler<TRequest, Unit>
    where TRequest : IMediatorRequest<Unit>
{
}


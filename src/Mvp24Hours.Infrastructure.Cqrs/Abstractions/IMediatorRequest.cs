//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Marker interface for a request that returns a response of type <typeparamref name="TResponse"/>.
/// This is the base interface for all Mediator requests (commands and queries).
/// </summary>
/// <typeparam name="TResponse">The type of response expected from the handler.</typeparam>
/// <remarks>
/// <para>
/// <strong>Important:</strong> This interface is different from <see cref="Mvp24Hours.Core.Contract.Data.ICommand{TEntity}"/>
/// which is used for Repository operations (Add/Modify/Remove entities).
/// </para>
/// <para>
/// <see cref="IMediatorRequest{TResponse}"/> is used for CQRS pattern with the Mediator,
/// while the Repository's ICommand is used for direct database operations.
/// </para>
/// <para>
/// For semantic clarity, consider using <see cref="IMediatorCommand{TResponse}"/> for write operations
/// and <see cref="IMediatorQuery{TResponse}"/> for read operations instead of this base interface.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define a request
/// public class GetUserByIdRequest : IMediatorRequest&lt;User&gt;
/// {
///     public int UserId { get; init; }
/// }
/// 
/// // Implement the handler
/// public class GetUserByIdHandler : IMediatorRequestHandler&lt;GetUserByIdRequest, User&gt;
/// {
///     public async Task&lt;User&gt; Handle(GetUserByIdRequest request, CancellationToken cancellationToken)
///     {
///         // Implementation...
///         return user;
///     }
/// }
/// </code>
/// </example>
public interface IMediatorRequest<out TResponse>
{
}

/// <summary>
/// Marker interface for a request that doesn't return a meaningful value.
/// Internally uses <see cref="Unit"/> as the response type.
/// </summary>
/// <remarks>
/// <para>
/// Use this interface for commands that perform actions without returning data,
/// such as delete operations or state changes.
/// </para>
/// <para>
/// For semantic clarity, consider using <see cref="IMediatorCommand"/> instead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DeleteUserRequest : IMediatorRequest
/// {
///     public int UserId { get; init; }
/// }
/// 
/// public class DeleteUserHandler : IMediatorRequestHandler&lt;DeleteUserRequest&gt;
/// {
///     public async Task&lt;Unit&gt; Handle(DeleteUserRequest request, CancellationToken cancellationToken)
///     {
///         // Delete logic...
///         return Unit.Value;
///     }
/// }
/// </code>
/// </example>
public interface IMediatorRequest : IMediatorRequest<Unit>
{
}


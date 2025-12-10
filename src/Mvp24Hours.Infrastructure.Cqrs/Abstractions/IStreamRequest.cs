//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Marker interface for a request that returns a stream of responses (IAsyncEnumerable).
/// Useful for operations that return multiple results asynchronously.
/// </summary>
/// <typeparam name="TResponse">The type of each item in the response stream.</typeparam>
/// <remarks>
/// <para>
/// Stream requests are useful for:
/// <list type="bullet">
/// <item>Large result sets that should be processed incrementally</item>
/// <item>Real-time data feeds</item>
/// <item>Long-running operations with progress updates</item>
/// <item>Memory-efficient processing of large datasets</item>
/// </list>
/// </para>
/// <para>
/// <strong>Note:</strong> Stream requests do not support pipeline behaviors
/// in the same way as regular requests, as they return IAsyncEnumerable.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define a stream request
/// public class GetOrderItemsStreamRequest : IStreamRequest&lt;OrderItem&gt;
/// {
///     public int OrderId { get; init; }
/// }
/// 
/// // Implement the handler
/// public class GetOrderItemsStreamHandler : IStreamRequestHandler&lt;GetOrderItemsStreamRequest, OrderItem&gt;
/// {
///     private readonly IOrderRepository _repository;
///     
///     public GetOrderItemsStreamHandler(IOrderRepository repository)
///     {
///         _repository = repository;
///     }
///     
///     public async IAsyncEnumerable&lt;OrderItem&gt; Handle(
///         GetOrderItemsStreamRequest request,
///         [EnumeratorCancellation] CancellationToken cancellationToken)
///     {
///         await foreach (var item in _repository.GetItemsStreamAsync(request.OrderId, cancellationToken))
///         {
///             yield return item;
///         }
///     }
/// }
/// 
/// // Usage
/// await foreach (var item in mediator.CreateStream(new GetOrderItemsStreamRequest { OrderId = 1 }))
/// {
///     Console.WriteLine($"Processing item: {item.Name}");
/// }
/// </code>
/// </example>
public interface IStreamRequest<out TResponse>
{
}

/// <summary>
/// Defines a handler for a stream request of type <typeparamref name="TRequest"/>.
/// </summary>
/// <typeparam name="TRequest">The type of stream request to handle.</typeparam>
/// <typeparam name="TResponse">The type of each item in the response stream.</typeparam>
/// <remarks>
/// <para>
/// The handler must return an <see cref="IAsyncEnumerable{T}"/> which allows
/// the caller to process items as they become available, rather than waiting
/// for all items to be collected.
/// </para>
/// <para>
/// Use the <c>[EnumeratorCancellation]</c> attribute on the cancellation token
/// parameter to properly support cancellation in async enumerators.
/// </para>
/// </remarks>
public interface IStreamRequestHandler<in TRequest, out TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <summary>
    /// Handles the stream request and returns an async stream of responses.
    /// </summary>
    /// <param name="request">The stream request to handle.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the stream.</param>
    /// <returns>An async enumerable of response items.</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}


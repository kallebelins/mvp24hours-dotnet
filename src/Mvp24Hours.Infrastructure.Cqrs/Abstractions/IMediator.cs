//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Main Mediator interface that combines request sending, notification publishing, and streaming capabilities.
/// Inherits from <see cref="ISender"/>, <see cref="IPublisher"/>, and <see cref="IStreamSender"/> 
/// to allow injection of more specific interfaces.
/// </summary>
/// <remarks>
/// <para>
/// The Mediator pattern provides a centralized point for communication between components,
/// reducing direct dependencies and promoting loose coupling.
/// </para>
/// <para>
/// <strong>When to use IMediator vs ISender/IPublisher:</strong>
/// <list type="bullet">
/// <item>Use <see cref="IMediator"/> when you need both request/response and notification capabilities.</item>
/// <item>Use <see cref="ISender"/> when you only need to send requests (commands/queries).</item>
/// <item>Use <see cref="IPublisher"/> when you only need to publish notifications.</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderService
/// {
///     private readonly IMediator _mediator;
///     
///     public OrderService(IMediator mediator)
///     {
///         _mediator = mediator;
///     }
///     
///     public async Task&lt;Order&gt; CreateOrderAsync(CreateOrderCommand command)
///     {
///         // Send command and get response
///         var order = await _mediator.SendAsync(command);
///         
///         // Publish notification to all interested handlers
///         await _mediator.PublishAsync(new OrderCreatedNotification(order.Id));
///         
///         return order;
///     }
/// }
/// </code>
/// </example>
public interface IMediator : ISender, IPublisher, IStreamSender
{
}

/// <summary>
/// Interface for sending requests to their respective handlers.
/// A subset of <see cref="IMediator"/> useful when you only need request/response functionality.
/// </summary>
/// <remarks>
/// <para>
/// Requests follow a one-to-one pattern: each request type has exactly one handler.
/// This is suitable for commands and queries in CQRS patterns.
/// </para>
/// <para>
/// <strong>Note:</strong> This is different from <see cref="IPublisher"/> which supports
/// multiple handlers for a single notification.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class QueryService
/// {
///     private readonly ISender _sender;
///     
///     public QueryService(ISender sender)
///     {
///         _sender = sender;
///     }
///     
///     public Task&lt;User&gt; GetUserAsync(int userId)
///     {
///         return _sender.SendAsync(new GetUserQuery { UserId = userId });
///     }
/// }
/// </code>
/// </example>
public interface ISender
{
    /// <summary>
    /// Sends a request to its corresponding handler and returns the response.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the handler.</typeparam>
    /// <param name="request">The request to be processed.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The response from the handler.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no handler is registered for the request type.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the request is null.
    /// </exception>
    Task<TResponse> SendAsync<TResponse>(IMediatorRequest<TResponse> request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for sending stream requests that return IAsyncEnumerable.
/// A subset of <see cref="IMediator"/> useful when you only need streaming functionality.
/// </summary>
/// <remarks>
/// <para>
/// Stream requests allow processing large result sets incrementally without
/// loading all data into memory at once.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DataProcessor
/// {
///     private readonly IStreamSender _streamSender;
///     
///     public DataProcessor(IStreamSender streamSender)
///     {
///         _streamSender = streamSender;
///     }
///     
///     public async Task ProcessLargeDatasetAsync()
///     {
///         await foreach (var item in _streamSender.CreateStream(new GetAllItemsRequest()))
///         {
///             await ProcessItemAsync(item);
///         }
///     }
/// }
/// </code>
/// </example>
public interface IStreamSender
{
    /// <summary>
    /// Creates a stream from a stream request, returning an IAsyncEnumerable of responses.
    /// </summary>
    /// <typeparam name="TResponse">The type of each item in the response stream.</typeparam>
    /// <param name="request">The stream request to process.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the stream.</param>
    /// <returns>An async enumerable of response items.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no handler is registered for the stream request type.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the request is null.
    /// </exception>
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for publishing notifications to multiple handlers.
/// A subset of <see cref="IMediator"/> useful when you only need notification functionality.
/// </summary>
/// <remarks>
/// <para>
/// Notifications follow a one-to-many pattern: each notification can have zero or more handlers.
/// This is suitable for domain events and cross-cutting concerns.
/// </para>
/// <para>
/// <strong>Note:</strong> Unlike <see cref="ISender"/>, notifications don't return a response
/// and can have multiple handlers or no handlers at all (fire-and-forget pattern).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class EventPublisher
/// {
///     private readonly IPublisher _publisher;
///     
///     public EventPublisher(IPublisher publisher)
///     {
///         _publisher = publisher;
///     }
///     
///     public Task NotifyOrderCreatedAsync(int orderId)
///     {
///         return _publisher.PublishAsync(new OrderCreatedNotification(orderId));
///     }
/// }
/// </code>
/// </example>
public interface IPublisher
{
    /// <summary>
    /// Publishes a notification to all registered handlers.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification to publish.</typeparam>
    /// <param name="notification">The notification to be published.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the notification is null.
    /// </exception>
    /// <remarks>
    /// If no handlers are registered for the notification type, this method completes successfully
    /// without performing any action (notifications are fire-and-forget).
    /// </remarks>
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : IMediatorNotification;
}


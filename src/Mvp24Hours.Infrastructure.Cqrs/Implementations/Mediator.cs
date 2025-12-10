//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Runtime.CompilerServices;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Implementations;

/// <summary>
/// Default implementation of <see cref="IMediator"/>.
/// Resolves handlers from the DI container and executes the pipeline of behaviors.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses reflection to create wrapper instances that handle
/// the dynamic resolution of handlers and behaviors at runtime.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> This class is thread-safe and can be used as a singleton
/// or scoped service. However, handlers and behaviors are resolved from the DI container
/// with their configured lifetimes.
/// </para>
/// </remarks>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates a new instance of the Mediator.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving handlers and behaviors.</param>
    /// <exception cref="ArgumentNullException">Thrown when serviceProvider is null.</exception>
    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public async Task<TResponse> SendAsync<TResponse>(IMediatorRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        // Create the wrapper for the specific request type
        var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, responseType);
        var wrapper = (RequestHandlerWrapperBase<TResponse>)Activator.CreateInstance(wrapperType)!;

        return await wrapper.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : IMediatorNotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = notification.GetType();

        // Create the wrapper for the specific notification type
        var wrapperType = typeof(NotificationHandlerWrapper<>).MakeGenericType(notificationType);
        var wrapper = (NotificationHandlerWrapperBase)Activator.CreateInstance(wrapperType)!;

        await wrapper.Handle(notification, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        // Create the wrapper for the specific stream request type
        var wrapperType = typeof(StreamRequestHandlerWrapper<,>).MakeGenericType(requestType, responseType);
        var wrapper = (StreamRequestHandlerWrapperBase<TResponse>)Activator.CreateInstance(wrapperType)!;

        await foreach (var item in wrapper.Handle(request, _serviceProvider, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}

/// <summary>
/// Abstract base class for request handler wrappers.
/// Enables invoking handlers in a generic way.
/// </summary>
/// <typeparam name="TResponse">The type of response.</typeparam>
internal abstract class RequestHandlerWrapperBase<TResponse>
{
    /// <summary>
    /// Handles the request by resolving the appropriate handler and executing the pipeline.
    /// </summary>
    public abstract Task<TResponse> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Typed wrapper for request handlers.
/// Resolves the handler and behaviors from the container and executes the pipeline.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
internal sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapperBase<TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    public override async Task<TResponse> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;

        // Resolve the handler
        var handler = serviceProvider.GetService(typeof(IMediatorRequestHandler<TRequest, TResponse>)) as IMediatorRequestHandler<TRequest, TResponse>
            ?? throw new InvalidOperationException(
                $"Handler not found for type '{typeof(TRequest).Name}'. " +
                $"Make sure an IMediatorRequestHandler<{typeof(TRequest).Name}, {typeof(TResponse).Name}> is registered in the DI container.");

        // Resolve the pipeline behaviors
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>()?.ToList()
            ?? new List<IPipelineBehavior<TRequest, TResponse>>();

        // Create the final delegate that invokes the handler
        RequestHandlerDelegate<TResponse> handlerDelegate = () => handler.Handle(typedRequest, cancellationToken);

        // Build the pipeline of behaviors from back to front
        // The last behavior added will be the first to execute
        foreach (var behavior in behaviors.AsEnumerable().Reverse())
        {
            var next = handlerDelegate;
            handlerDelegate = () => behavior.Handle(typedRequest, next, cancellationToken);
        }

        return await handlerDelegate();
    }
}

/// <summary>
/// Abstract base class for notification handler wrappers.
/// </summary>
internal abstract class NotificationHandlerWrapperBase
{
    /// <summary>
    /// Handles the notification by resolving all handlers and executing them.
    /// </summary>
    public abstract Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Typed wrapper for notification handlers.
/// Resolves all handlers and executes them sequentially or in parallel.
/// </summary>
/// <typeparam name="TNotification">The type of notification.</typeparam>
internal sealed class NotificationHandlerWrapper<TNotification> : NotificationHandlerWrapperBase
    where TNotification : IMediatorNotification
{
    public override async Task Handle(object notification, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedNotification = (TNotification)notification;

        // Resolve all handlers for this notification
        var handlers = serviceProvider.GetServices<IMediatorNotificationHandler<TNotification>>()?.ToList()
            ?? new List<IMediatorNotificationHandler<TNotification>>();

        if (handlers.Count == 0)
        {
            // Notifications can have no handlers - this is valid
            return;
        }

        // Execute all handlers sequentially by default
        // Sequential execution maintains order and simplifies debugging
        // A configuration option could allow parallel execution
        foreach (var handler in handlers)
        {
            await handler.Handle(typedNotification, cancellationToken);
        }
    }
}

/// <summary>
/// Abstract base class for stream request handler wrappers.
/// </summary>
/// <typeparam name="TResponse">The type of each response item.</typeparam>
internal abstract class StreamRequestHandlerWrapperBase<TResponse>
{
    /// <summary>
    /// Handles the stream request by resolving the handler and returning the stream.
    /// </summary>
    public abstract IAsyncEnumerable<TResponse> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Typed wrapper for stream request handlers.
/// </summary>
/// <typeparam name="TRequest">The type of stream request.</typeparam>
/// <typeparam name="TResponse">The type of each response item.</typeparam>
internal sealed class StreamRequestHandlerWrapper<TRequest, TResponse> : StreamRequestHandlerWrapperBase<TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public override IAsyncEnumerable<TResponse> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;

        // Resolve the handler
        var handler = serviceProvider.GetService(typeof(IStreamRequestHandler<TRequest, TResponse>)) as IStreamRequestHandler<TRequest, TResponse>
            ?? throw new InvalidOperationException(
                $"Stream handler not found for type '{typeof(TRequest).Name}'. " +
                $"Make sure an IStreamRequestHandler<{typeof(TRequest).Name}, {typeof(TResponse).Name}> is registered in the DI container.");

        return handler.Handle(typedRequest, cancellationToken);
    }
}

/// <summary>
/// Internal extension methods for IServiceProvider.
/// </summary>
internal static class ServiceProviderExtensions
{
    /// <summary>
    /// Resolves all services registered for the specified type.
    /// </summary>
    public static IEnumerable<T>? GetServices<T>(this IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService(typeof(IEnumerable<T>)) as IEnumerable<T>;
    }

    /// <summary>
    /// Resolves a single service for the specified type.
    /// </summary>
    public static object? GetService(this IServiceProvider serviceProvider, Type serviceType)
    {
        return serviceProvider.GetService(serviceType);
    }
}


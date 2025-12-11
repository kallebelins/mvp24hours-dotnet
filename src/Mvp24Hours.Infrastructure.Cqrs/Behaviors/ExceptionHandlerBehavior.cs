//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Pipeline behavior that orchestrates exception handlers.
/// Allows fine-grained exception handling using <see cref="IExceptionHandler{TRequest, TResponse, TException}"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
/// <remarks>
/// <para>
/// This behavior catches exceptions and attempts to find a matching exception handler.
/// Handlers are checked in order of specificity (most specific exception type first).
/// </para>
/// <para>
/// <strong>Handler Resolution Order:</strong>
/// <list type="number">
/// <item>Exact exception type handlers (IExceptionHandler&lt;TRequest, TResponse, TExactException&gt;)</item>
/// <item>Base exception type handlers (checked up the inheritance chain)</item>
/// <item>Global exception handlers (IExceptionHandlerGlobal&lt;TException&gt;)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Placement:</strong> This behavior should be placed near the beginning of the pipeline
/// (after UnhandledExceptionBehavior) to catch exceptions from subsequent behaviors.
/// </para>
/// </remarks>
public class ExceptionHandlerBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExceptionHandlerBehavior<TRequest, TResponse>>? _logger;

    /// <summary>
    /// Creates a new instance of the exception handler behavior.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving exception handlers.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public ExceptionHandlerBehavior(
        IServiceProvider serviceProvider,
        ILogger<ExceptionHandlerBehavior<TRequest, TResponse>>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Exception caught, attempting to find handler for {ExceptionType}", ex.GetType().Name);

            // Try to handle the exception
            var result = await TryHandleExceptionAsync(request, ex, cancellationToken);

            if (result.IsHandled)
            {
                _logger?.LogDebug("Exception {ExceptionType} was handled", ex.GetType().Name);
                return result.Response!;
            }

            if (result.ShouldRethrow && result.ExceptionToRethrow is not null)
            {
                _logger?.LogDebug("Rethrowing different exception {ExceptionType}", result.ExceptionToRethrow.GetType().Name);
                throw result.ExceptionToRethrow;
            }

            // No handler found or handler indicated not to handle, rethrow original
            throw;
        }
    }

    private async Task<ExceptionHandlingResult<TResponse>> TryHandleExceptionAsync(
        TRequest request,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var exceptionType = exception.GetType();

        // Try to find type-specific handlers, walking up the exception hierarchy
        var currentType = exceptionType;
        while (currentType != null && currentType != typeof(object))
        {
            var result = await TryHandleWithTypeAsync(request, exception, currentType, cancellationToken);
            if (result.IsHandled || result.ShouldRethrow)
            {
                return result;
            }

            currentType = currentType.BaseType;
        }

        // Try global handlers
        return await TryHandleWithGlobalAsync(request, exception, exceptionType, cancellationToken);
    }

    private async Task<ExceptionHandlingResult<TResponse>> TryHandleWithTypeAsync(
        TRequest request,
        Exception exception,
        Type exceptionType,
        CancellationToken cancellationToken)
    {
        // Build the handler type: IExceptionHandler<TRequest, TResponse, TException>
        var handlerType = typeof(IExceptionHandler<,,>).MakeGenericType(
            typeof(TRequest),
            typeof(TResponse),
            exceptionType);

        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            if (handler is null) continue;

            _logger?.LogDebug("Trying exception handler {HandlerType} for {ExceptionType}",
                handler.GetType().Name, exceptionType.Name);

            try
            {
                // Use reflection to call HandleAsync
                var handleMethod = handlerType.GetMethod("HandleAsync");
                if (handleMethod is null) continue;

                var task = handleMethod.Invoke(handler, [request, exception, cancellationToken]);
                if (task is null) continue;

                await (Task)task;

                // Get the result using reflection
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty is null) continue;

                var resultValue = resultProperty.GetValue(task);
                if (resultValue is not null)
                {
                    var result = ConvertToTypedResult(resultValue);
                    if (result.IsHandled || result.ShouldRethrow)
                    {
                        return result;
                    }
                }
            }
            catch (Exception handlerEx)
            {
                _logger?.LogWarning(handlerEx, "Exception handler {HandlerType} threw an exception",
                    handler.GetType().Name);
                // Handler itself threw, continue to next handler
            }
        }

        return ExceptionHandlingResult<TResponse>.NotHandled;
    }

    private async Task<ExceptionHandlingResult<TResponse>> TryHandleWithGlobalAsync(
        TRequest request,
        Exception exception,
        Type exceptionType,
        CancellationToken cancellationToken)
    {
        // Build the global handler type: IExceptionHandlerGlobal<TException>
        var currentType = exceptionType;
        while (currentType != null && currentType != typeof(object))
        {
            var handlerType = typeof(IExceptionHandlerGlobal<>).MakeGenericType(currentType);
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null) continue;

                _logger?.LogDebug("Trying global exception handler {HandlerType} for {ExceptionType}",
                    handler.GetType().Name, currentType.Name);

                try
                {
                    // Use reflection to call HandleAsync
                    var handleMethod = handlerType.GetMethod("HandleAsync");
                    if (handleMethod is null) continue;

                    var task = handleMethod.Invoke(handler, [request, exception, cancellationToken]);
                    if (task is null) continue;

                    await (Task)task;

                    // Get the result using reflection
                    var resultProperty = task.GetType().GetProperty("Result");
                    if (resultProperty is null) continue;

                    var resultValue = resultProperty.GetValue(task);
                    if (resultValue is not null)
                    {
                        var result = ConvertFromGlobalResult(resultValue);
                        if (result.IsHandled || result.ShouldRethrow)
                        {
                            return result;
                        }
                    }
                }
                catch (Exception handlerEx)
                {
                    _logger?.LogWarning(handlerEx, "Global exception handler {HandlerType} threw an exception",
                        handler.GetType().Name);
                    // Handler itself threw, continue to next handler
                }
            }

            currentType = currentType.BaseType;
        }

        return ExceptionHandlingResult<TResponse>.NotHandled;
    }

    private static ExceptionHandlingResult<TResponse> ConvertToTypedResult(object resultValue)
    {
        var resultType = resultValue.GetType();

        var isHandledProp = resultType.GetProperty("IsHandled");
        var responseProp = resultType.GetProperty("Response");
        var exceptionProp = resultType.GetProperty("ExceptionToRethrow");

        var isHandled = isHandledProp?.GetValue(resultValue) as bool? ?? false;
        var response = responseProp?.GetValue(resultValue);
        var exceptionToRethrow = exceptionProp?.GetValue(resultValue) as Exception;

        if (exceptionToRethrow is not null)
        {
            return ExceptionHandlingResult<TResponse>.Rethrow(exceptionToRethrow);
        }

        if (isHandled && response is TResponse typedResponse)
        {
            return ExceptionHandlingResult<TResponse>.Handled(typedResponse);
        }

        return ExceptionHandlingResult<TResponse>.NotHandled;
    }

    private static ExceptionHandlingResult<TResponse> ConvertFromGlobalResult(object resultValue)
    {
        var resultType = resultValue.GetType();

        var isHandledProp = resultType.GetProperty("IsHandled");
        var responseProp = resultType.GetProperty("Response");
        var exceptionProp = resultType.GetProperty("ExceptionToRethrow");

        var isHandled = isHandledProp?.GetValue(resultValue) as bool? ?? false;
        var response = responseProp?.GetValue(resultValue);
        var exceptionToRethrow = exceptionProp?.GetValue(resultValue) as Exception;

        if (exceptionToRethrow is not null)
        {
            return ExceptionHandlingResult<TResponse>.Rethrow(exceptionToRethrow);
        }

        if (isHandled)
        {
            // Try to convert the response
            if (response is TResponse typedResponse)
            {
                return ExceptionHandlingResult<TResponse>.Handled(typedResponse);
            }

            // If TResponse is Unit or object, try to use default
            if (typeof(TResponse) == typeof(Unit))
            {
                return ExceptionHandlingResult<TResponse>.Handled((TResponse)(object)Unit.Value);
            }
        }

        return ExceptionHandlingResult<TResponse>.NotHandled;
    }
}


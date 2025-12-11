//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Pipeline behavior that executes registered pipeline hooks at appropriate lifecycle points.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
/// <remarks>
/// <para>
/// This behavior coordinates all <see cref="IPipelineHook"/> and <see cref="IPipelineHook{TRequest}"/>
/// implementations, calling them at the appropriate moments in the pipeline lifecycle.
/// </para>
/// <para>
/// <strong>Placement:</strong> This behavior should be placed at the very beginning of the pipeline
/// (before UnhandledExceptionBehavior) to capture the complete lifecycle including exceptions.
/// </para>
/// </remarks>
public class PipelineHookBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PipelineHookBehavior<TRequest, TResponse>>? _logger;

    /// <summary>
    /// Creates a new instance of the pipeline hook behavior.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving hooks.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public PipelineHookBehavior(
        IServiceProvider serviceProvider,
        ILogger<PipelineHookBehavior<TRequest, TResponse>>? logger = null)
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
        var requestType = typeof(TRequest);
        var responseType = typeof(TResponse);
        var stopwatch = Stopwatch.StartNew();

        // Execute OnPipelineStart hooks
        await ExecuteStartHooksAsync(request, requestType, cancellationToken);

        try
        {
            var response = await next();
            stopwatch.Stop();

            // Execute OnPipelineComplete hooks
            await ExecuteCompleteHooksAsync(request, response, requestType, responseType, stopwatch.ElapsedMilliseconds, cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Execute OnPipelineError hooks
            await ExecuteErrorHooksAsync(request, ex, requestType, stopwatch.ElapsedMilliseconds, cancellationToken);

            throw;
        }
    }

    private async Task ExecuteStartHooksAsync(
        TRequest request,
        Type requestType,
        CancellationToken cancellationToken)
    {
        // Global hooks
        var globalHooks = _serviceProvider.GetServices<IPipelineHook>() ?? [];
        foreach (var hook in globalHooks)
        {
            try
            {
                _logger?.LogTrace("Executing global pipeline start hook {HookType}", hook.GetType().Name);
                await hook.OnPipelineStartAsync(request, requestType, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Global pipeline hook {HookType} OnPipelineStart failed", hook.GetType().Name);
            }
        }

        // Typed hooks
        var typedHooks = _serviceProvider.GetServices<IPipelineHook<TRequest>>() ?? [];
        foreach (var hook in typedHooks)
        {
            try
            {
                _logger?.LogTrace("Executing typed pipeline start hook {HookType} for {RequestType}",
                    hook.GetType().Name, requestType.Name);
                await hook.OnPipelineStartAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Typed pipeline hook {HookType} OnPipelineStart failed", hook.GetType().Name);
            }
        }
    }

    private async Task ExecuteCompleteHooksAsync(
        TRequest request,
        TResponse response,
        Type requestType,
        Type responseType,
        long elapsedMilliseconds,
        CancellationToken cancellationToken)
    {
        // Global hooks
        var globalHooks = _serviceProvider.GetServices<IPipelineHook>() ?? [];
        foreach (var hook in globalHooks)
        {
            try
            {
                _logger?.LogTrace("Executing global pipeline complete hook {HookType}", hook.GetType().Name);
                await hook.OnPipelineCompleteAsync(request, response, requestType, responseType, elapsedMilliseconds, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Global pipeline hook {HookType} OnPipelineComplete failed", hook.GetType().Name);
            }
        }

        // Typed hooks
        var typedHooks = _serviceProvider.GetServices<IPipelineHook<TRequest>>() ?? [];
        foreach (var hook in typedHooks)
        {
            try
            {
                _logger?.LogTrace("Executing typed pipeline complete hook {HookType} for {RequestType}",
                    hook.GetType().Name, requestType.Name);
                await hook.OnPipelineCompleteAsync(request, response, elapsedMilliseconds, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Typed pipeline hook {HookType} OnPipelineComplete failed", hook.GetType().Name);
            }
        }
    }

    private async Task ExecuteErrorHooksAsync(
        TRequest request,
        Exception exception,
        Type requestType,
        long elapsedMilliseconds,
        CancellationToken cancellationToken)
    {
        // Global hooks
        var globalHooks = _serviceProvider.GetServices<IPipelineHook>() ?? [];
        foreach (var hook in globalHooks)
        {
            try
            {
                _logger?.LogTrace("Executing global pipeline error hook {HookType}", hook.GetType().Name);
                await hook.OnPipelineErrorAsync(request, exception, requestType, elapsedMilliseconds, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Global pipeline hook {HookType} OnPipelineError failed", hook.GetType().Name);
            }
        }

        // Typed hooks
        var typedHooks = _serviceProvider.GetServices<IPipelineHook<TRequest>>() ?? [];
        foreach (var hook in typedHooks)
        {
            try
            {
                _logger?.LogTrace("Executing typed pipeline error hook {HookType} for {RequestType}",
                    hook.GetType().Name, requestType.Name);
                await hook.OnPipelineErrorAsync(request, exception, elapsedMilliseconds, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Typed pipeline hook {HookType} OnPipelineError failed", hook.GetType().Name);
            }
        }
    }
}


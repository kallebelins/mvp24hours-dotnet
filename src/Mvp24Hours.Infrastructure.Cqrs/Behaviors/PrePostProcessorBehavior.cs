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
/// Pipeline behavior that orchestrates pre-processors and post-processors.
/// Executes all registered <see cref="IPreProcessor{TRequest}"/> before the handler
/// and all <see cref="IPostProcessor{TRequest, TResponse}"/> after successful completion.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
/// <remarks>
/// <para>
/// This behavior provides hooks for extensibility without creating full pipeline behaviors.
/// It's recommended to place this behavior early in the pipeline to ensure pre-processors
/// run before validation and other behaviors.
/// </para>
/// <para>
/// <strong>Execution Order:</strong>
/// <list type="number">
/// <item>Global pre-processors (IPreProcessorGlobal)</item>
/// <item>Type-specific pre-processors (IPreProcessor&lt;TRequest&gt;)</item>
/// <item>[Handler executes]</item>
/// <item>Type-specific post-processors (IPostProcessor&lt;TRequest, TResponse&gt;)</item>
/// <item>Global post-processors (IPostProcessorGlobal)</item>
/// </list>
/// </para>
/// </remarks>
public class PrePostProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PrePostProcessorBehavior<TRequest, TResponse>>? _logger;

    /// <summary>
    /// Creates a new instance of the pre/post processor behavior.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving processors.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public PrePostProcessorBehavior(
        IServiceProvider serviceProvider,
        ILogger<PrePostProcessorBehavior<TRequest, TResponse>>? logger = null)
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
        // Execute global pre-processors
        var globalPreProcessors = _serviceProvider.GetServices<IPreProcessorGlobal>() ?? [];
        foreach (var processor in globalPreProcessors)
        {
            _logger?.LogDebug("Executing global pre-processor {ProcessorType}", processor.GetType().Name);
            await processor.ProcessAsync(request, cancellationToken);
        }

        // Execute type-specific pre-processors
        var preProcessors = _serviceProvider.GetServices<IPreProcessor<TRequest>>() ?? [];
        foreach (var processor in preProcessors)
        {
            _logger?.LogDebug("Executing pre-processor {ProcessorType} for {RequestType}",
                processor.GetType().Name, typeof(TRequest).Name);
            await processor.ProcessAsync(request, cancellationToken);
        }

        // Execute the handler (and rest of the pipeline)
        var response = await next();

        // Execute type-specific post-processors
        var postProcessors = _serviceProvider.GetServices<IPostProcessor<TRequest, TResponse>>() ?? [];
        foreach (var processor in postProcessors)
        {
            _logger?.LogDebug("Executing post-processor {ProcessorType} for {RequestType}",
                processor.GetType().Name, typeof(TRequest).Name);
            await processor.ProcessAsync(request, response, cancellationToken);
        }

        // Execute global post-processors
        var globalPostProcessors = _serviceProvider.GetServices<IPostProcessorGlobal>() ?? [];
        foreach (var processor in globalPostProcessors)
        {
            _logger?.LogDebug("Executing global post-processor {ProcessorType}", processor.GetType().Name);
            await processor.ProcessAsync(request, response, cancellationToken);
        }

        return response;
    }
}


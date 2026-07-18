//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
/// <remarks>
/// Creates a new instance of the pre/post processor behavior.
/// </remarks>
/// <param name="serviceProvider">Service provider for resolving processors.</param>
/// <param name="logger">Optional logger for diagnostics.</param>
public class PrePostProcessorBehavior<TRequest, TResponse>(
    IServiceProvider serviceProvider,
    ILogger<PrePostProcessorBehavior<TRequest, TResponse>>? logger = null) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger<PrePostProcessorBehavior<TRequest, TResponse>>? _logger = logger;

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Execute global pre-processors
        IEnumerable<IPreProcessorGlobal> globalPreProcessors = _serviceProvider.GetServices<IPreProcessorGlobal>() ?? [];
        foreach (IPreProcessorGlobal processor in globalPreProcessors)
        {
            _logger?.LogDebug("Executing global pre-processor {ProcessorType}", processor.GetType().Name);
            await processor.ProcessAsync(request, cancellationToken);
        }

        // Execute type-specific pre-processors
        IEnumerable<IPreProcessor<TRequest>> preProcessors = _serviceProvider.GetServices<IPreProcessor<TRequest>>() ?? [];
        foreach (IPreProcessor<TRequest> processor in preProcessors)
        {
            _logger?.LogDebug("Executing pre-processor {ProcessorType} for {RequestType}",
                processor.GetType().Name, typeof(TRequest).Name);
            await processor.ProcessAsync(request, cancellationToken);
        }

        // Execute the handler (and rest of the pipeline)
        TResponse? response = await next();

        // Execute type-specific post-processors
        IEnumerable<IPostProcessor<TRequest, TResponse>> postProcessors = _serviceProvider.GetServices<IPostProcessor<TRequest, TResponse>>() ?? [];
        foreach (IPostProcessor<TRequest, TResponse> processor in postProcessors)
        {
            _logger?.LogDebug("Executing post-processor {ProcessorType} for {RequestType}",
                processor.GetType().Name, typeof(TRequest).Name);
            await processor.ProcessAsync(request, response, cancellationToken);
        }

        // Execute global post-processors
        IEnumerable<IPostProcessorGlobal> globalPostProcessors = _serviceProvider.GetServices<IPostProcessorGlobal>() ?? [];
        foreach (IPostProcessorGlobal processor in globalPostProcessors)
        {
            _logger?.LogDebug("Executing global post-processor {ProcessorType}", processor.GetType().Name);
            await processor.ProcessAsync(request, response, cancellationToken);
        }

        return response;
    }
}


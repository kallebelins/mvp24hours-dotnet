//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Observability;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Pipeline behavior that creates OpenTelemetry-compatible traces using the Activity API.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior integrates with the .NET Activity API to create distributed traces
/// that can be exported to any OpenTelemetry-compatible backend (Jaeger, Zipkin, etc.).
/// </para>
/// <para>
/// <strong>Prerequisites:</strong>
/// Configure OpenTelemetry to include the Mvp24Hours source:
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(builder =>
///     {
///         builder.AddSource(MediatorActivitySource.SourceName);
///     });
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register the behavior
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(TracingBehavior&lt;,&gt;));
/// 
/// // Configure OpenTelemetry
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(builder =>
///     {
///         builder
///             .AddSource("Mvp24Hours.Mediator")
///             .AddAspNetCoreInstrumentation()
///             .AddJaegerExporter(o =>
///             {
///                 o.AgentHost = "localhost";
///                 o.AgentPort = 6831;
///             });
///     });
/// </code>
/// </example>
public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IRequestContextAccessor? _contextAccessor;

    /// <summary>
    /// Creates a new instance of the TracingBehavior.
    /// </summary>
    /// <param name="contextAccessor">Optional request context accessor for tracing context.</param>
    public TracingBehavior(IRequestContextAccessor? contextAccessor = null)
    {
        _contextAccessor = contextAccessor;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestType = GetRequestType();
        var context = _contextAccessor?.Context;

        using var activity = MediatorActivitySource.StartRequestActivity(requestName, requestType, context);

        if (activity == null)
        {
            // No listeners registered, skip tracing
            return await next();
        }

        try
        {
            var response = await next();

            MediatorActivitySource.SetSuccess(activity);

            return response;
        }
        catch (Exception ex)
        {
            MediatorActivitySource.SetError(activity, ex);
            throw;
        }
    }

    private static string GetRequestType()
    {
        var requestType = typeof(TRequest);

        if (requestType.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IMediatorCommand")))
        {
            return "Command";
        }

        if (requestType.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IMediatorQuery")))
        {
            return "Query";
        }

        return "Request";
    }
}


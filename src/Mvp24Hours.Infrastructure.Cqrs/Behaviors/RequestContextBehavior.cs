//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Mvp24Hours.Infrastructure.Cqrs.Observability;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Pipeline behavior that establishes and propagates request context information.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior is responsible for:
/// <list type="bullet">
/// <item>Creating or retrieving the correlation ID from HTTP headers</item>
/// <item>Establishing a new request context for each mediator request</item>
/// <item>Making the context available through <see cref="IRequestContextAccessor"/></item>
/// </list>
/// </para>
/// <para>
/// <strong>Registration Order:</strong> This behavior should be registered early in the pipeline
/// (after UnhandledException but before other behaviors) to ensure context is available
/// throughout the request processing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddScoped&lt;IRequestContextAccessor, RequestContextAccessor&gt;();
/// services.AddSingleton&lt;IRequestContextFactory, RequestContextFactory&gt;();
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(RequestContextBehavior&lt;,&gt;));
/// 
/// // Access context in handlers
/// public class MyHandler : IMediatorCommandHandler&lt;MyCommand&gt;
/// {
///     private readonly IRequestContextAccessor _contextAccessor;
///     
///     public async Task&lt;Unit&gt; Handle(MyCommand command, CancellationToken ct)
///     {
///         var context = _contextAccessor.Context;
///         _logger.LogInformation("CorrelationId: {Id}", context?.CorrelationId);
///         return Unit.Value;
///     }
/// }
/// </code>
/// </example>
/// <remarks>
/// Creates a new instance of the RequestContextBehavior.
/// </remarks>
/// <param name="contextAccessor">The request context accessor.</param>
/// <param name="contextFactory">The request context factory.</param>
/// <param name="httpContextAccessor">Optional HTTP context accessor for web scenarios.</param>
/// <param name="userContext">Optional user context for authentication scenarios.</param>
/// <param name="logger">Optional logger.</param>
public sealed class RequestContextBehavior<TRequest, TResponse>(
    IRequestContextAccessor contextAccessor,
    IRequestContextFactory contextFactory,
    IHttpContextAccessor? httpContextAccessor = null,
    IUserContext? userContext = null,
    ILogger<RequestContextBehavior<TRequest, TResponse>>? logger = null) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IRequestContextAccessor _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    private readonly IRequestContextFactory _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    private readonly IHttpContextAccessor? _httpContextAccessor = httpContextAccessor;
    private readonly IUserContext? _userContext = userContext;
    private readonly ILogger<RequestContextBehavior<TRequest, TResponse>>? _logger = logger;

    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string CausationIdHeader = "X-Causation-Id";

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Check if we already have a context (nested request)
        IRequestContext? existingContext = _contextAccessor.Context;
        if (existingContext != null)
        {
            // Create a child context for the nested request
            IRequestContext childContext = existingContext.CreateChildContext();
            _contextAccessor.Context = childContext;

            _logger?.LogDebug(
                "[RequestContext] Created child context. CorrelationId: {CorrelationId}, RequestId: {RequestId}, CausationId: {CausationId}",
                childContext.CorrelationId,
                childContext.RequestId,
                childContext.CausationId);

            try
            {
                return await next();
            }
            finally
            {
                // Restore parent context
                _contextAccessor.Context = existingContext;
            }
        }

        // Create a new root context
        string correlationId = GetCorrelationIdFromHttpContext() ?? Guid.NewGuid().ToString("N");
        string? causationId = GetCausationIdFromHttpContext();
        string? userId = _userContext?.UserId;
        string? tenantId = GetTenantIdFromHttpContext();

        IRequestContext context = _contextFactory.Create(
            correlationId: correlationId,
            causationId: causationId,
            userId: userId,
            tenantId: tenantId);

        _contextAccessor.Context = context;

        _logger?.LogDebug(
            "[RequestContext] Created root context for {RequestName}. CorrelationId: {CorrelationId}, RequestId: {RequestId}",
            typeof(TRequest).Name,
            context.CorrelationId,
            context.RequestId);

        try
        {
            return await next();
        }
        finally
        {
            // Clear context
            _contextAccessor.Context = null;
        }
    }

    private string? GetCorrelationIdFromHttpContext()
    {
        HttpContext? httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        // Try to get from header
        if (httpContext.Request.Headers.TryGetValue(CorrelationIdHeader, out StringValues headerValue))
        {
            return headerValue.ToString();
        }

        // Use ASP.NET Core's trace identifier
        return httpContext.TraceIdentifier;
    }

    private string? GetCausationIdFromHttpContext()
    {
        HttpContext? httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        if (httpContext.Request.Headers.TryGetValue(CausationIdHeader, out StringValues headerValue))
        {
            return headerValue.ToString();
        }

        return null;
    }

    private string? GetTenantIdFromHttpContext()
    {
        HttpContext? httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        // Common tenant header names
        string[] tenantHeaders = ["X-Tenant-Id", "X-Tenant", "TenantId"];

        foreach (string header in tenantHeaders)
        {
            if (httpContext.Request.Headers.TryGetValue(header, out StringValues headerValue))
            {
                return headerValue.ToString();
            }
        }

        return null;
    }
}


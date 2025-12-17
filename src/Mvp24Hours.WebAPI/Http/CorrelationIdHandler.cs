//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Middlewares;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Http;

/// <summary>
/// HTTP message handler that propagates Correlation, Causation, and Request IDs
/// to outgoing HTTP requests for distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// This handler ensures that context IDs are propagated when calling other services,
/// enabling end-to-end request tracing across microservices.
/// </para>
/// <para>
/// <strong>Header Propagation:</strong>
/// <list type="bullet">
/// <item>Correlation ID - Same ID across all services for a user request</item>
/// <item>Causation ID - Set to the current Request ID to establish causation chain</item>
/// <item>Tenant ID - Propagated for multi-tenant scenarios</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in Program.cs
/// builder.Services.AddHttpClient("MyApi", client =>
/// {
///     client.BaseAddress = new Uri("https://api.example.com");
/// })
/// .AddMvp24HoursCorrelationIdHandler();
/// 
/// // Or for all HttpClient instances
/// builder.Services.AddTransient&lt;CorrelationIdHandler&gt;();
/// builder.Services.ConfigureAll&lt;HttpClientFactoryOptions&gt;(options =>
/// {
///     options.HttpMessageHandlerBuilderActions.Add(builder =>
///     {
///         builder.AdditionalHandlers.Add(
///             builder.Services.GetRequiredService&lt;CorrelationIdHandler&gt;());
///     });
/// });
/// </code>
/// </example>
public class CorrelationIdHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly RequestContextOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="CorrelationIdHandler"/>.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="options">The request context options.</param>
    public CorrelationIdHandler(
        IHttpContextAccessor httpContextAccessor,
        IOptions<RequestContextOptions> options)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _options = options?.Value ?? new RequestContextOptions();
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext != null && _options.PropagateToOutgoingRequests)
        {
            // Propagate Correlation ID
            var correlationId = httpContext.GetCorrelationId();
            if (!string.IsNullOrEmpty(correlationId) && !request.Headers.Contains(_options.CorrelationIdHeader))
            {
                request.Headers.TryAddWithoutValidation(_options.CorrelationIdHeader, correlationId);
            }

            // Set Causation ID to the current Request ID (establishing the causation chain)
            var requestId = httpContext.GetRequestId();
            if (!string.IsNullOrEmpty(requestId) && !request.Headers.Contains(_options.CausationIdHeader))
            {
                request.Headers.TryAddWithoutValidation(_options.CausationIdHeader, requestId);
            }

            // Propagate Tenant ID
            var tenantId = httpContext.GetTenantId();
            if (!string.IsNullOrEmpty(tenantId) && !request.Headers.Contains(_options.TenantIdHeader))
            {
                request.Headers.TryAddWithoutValidation(_options.TenantIdHeader, tenantId);
            }

            // Propagate W3C Trace Context if enabled
            if (_options.UseW3CTraceContext)
            {
                PropagateW3CTraceContext(request, httpContext);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private void PropagateW3CTraceContext(HttpRequestMessage request, HttpContext httpContext)
    {
        // Check for existing traceparent header in incoming request
        if (httpContext.Request.Headers.TryGetValue(_options.TraceParentHeader, out var traceParent)
            && !string.IsNullOrWhiteSpace(traceParent)
            && !request.Headers.Contains(_options.TraceParentHeader))
        {
            request.Headers.TryAddWithoutValidation(_options.TraceParentHeader, traceParent.ToString());
        }

        // Check for existing tracestate header
        if (httpContext.Request.Headers.TryGetValue(_options.TraceStateHeader, out var traceState)
            && !string.IsNullOrWhiteSpace(traceState)
            && !request.Headers.Contains(_options.TraceStateHeader))
        {
            request.Headers.TryAddWithoutValidation(_options.TraceStateHeader, traceState.ToString());
        }
    }
}

/// <summary>
/// Standalone correlation ID propagation handler that doesn't require ASP.NET Core HttpContext.
/// Useful for background services or console applications.
/// </summary>
/// <remarks>
/// <para>
/// This handler uses an <see cref="ICorrelationContextProvider"/> to get the current
/// correlation context, making it suitable for non-web scenarios.
/// </para>
/// </remarks>
public class CorrelationIdPropagatingHandler : DelegatingHandler
{
    private readonly ICorrelationContextProvider _contextProvider;
    private readonly RequestContextOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="CorrelationIdPropagatingHandler"/>.
    /// </summary>
    /// <param name="contextProvider">The correlation context provider.</param>
    /// <param name="options">The request context options.</param>
    public CorrelationIdPropagatingHandler(
        ICorrelationContextProvider contextProvider,
        IOptions<RequestContextOptions> options)
    {
        _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
        _options = options?.Value ?? new RequestContextOptions();
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = _contextProvider.GetCurrentContext();

        if (context != null && _options.PropagateToOutgoingRequests)
        {
            if (!string.IsNullOrEmpty(context.CorrelationId) && !request.Headers.Contains(_options.CorrelationIdHeader))
            {
                request.Headers.TryAddWithoutValidation(_options.CorrelationIdHeader, context.CorrelationId);
            }

            if (!string.IsNullOrEmpty(context.RequestId) && !request.Headers.Contains(_options.CausationIdHeader))
            {
                request.Headers.TryAddWithoutValidation(_options.CausationIdHeader, context.RequestId);
            }

            if (!string.IsNullOrEmpty(context.TenantId) && !request.Headers.Contains(_options.TenantIdHeader))
            {
                request.Headers.TryAddWithoutValidation(_options.TenantIdHeader, context.TenantId);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// Interface for providing correlation context in non-web scenarios.
/// </summary>
public interface ICorrelationContextProvider
{
    /// <summary>
    /// Gets the current correlation context.
    /// </summary>
    /// <returns>The current correlation context, or null if not available.</returns>
    CorrelationContext? GetCurrentContext();

    /// <summary>
    /// Sets the current correlation context.
    /// </summary>
    /// <param name="context">The context to set.</param>
    void SetCurrentContext(CorrelationContext context);
}

/// <summary>
/// Represents the correlation context for distributed tracing.
/// </summary>
public record CorrelationContext
{
    /// <summary>
    /// Gets or sets the Correlation ID.
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Causation ID.
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// Gets or sets the Request ID.
    /// </summary>
    public string RequestId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the User ID.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets or sets the Tenant ID.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the context was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a new correlation context with auto-generated IDs.
    /// </summary>
    public static CorrelationContext Create(
        string? correlationId = null,
        string? causationId = null,
        string? userId = null,
        string? tenantId = null)
    {
        var id = correlationId ?? Guid.NewGuid().ToString("N");
        return new CorrelationContext
        {
            CorrelationId = id,
            CausationId = causationId,
            RequestId = Guid.NewGuid().ToString("N"),
            UserId = userId,
            TenantId = tenantId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates a child context for nested operations.
    /// </summary>
    public CorrelationContext CreateChild()
    {
        return new CorrelationContext
        {
            CorrelationId = CorrelationId,
            CausationId = RequestId, // Current RequestId becomes child's CausationId
            RequestId = Guid.NewGuid().ToString("N"),
            UserId = UserId,
            TenantId = TenantId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}

/// <summary>
/// Default implementation of <see cref="ICorrelationContextProvider"/> using AsyncLocal.
/// </summary>
public class AsyncLocalCorrelationContextProvider : ICorrelationContextProvider
{
    private static readonly AsyncLocal<CorrelationContext?> _context = new();

    /// <inheritdoc />
    public CorrelationContext? GetCurrentContext() => _context.Value;

    /// <inheritdoc />
    public void SetCurrentContext(CorrelationContext context) => _context.Value = context;

    /// <summary>
    /// Clears the current context.
    /// </summary>
    public void ClearCurrentContext() => _context.Value = null;
}



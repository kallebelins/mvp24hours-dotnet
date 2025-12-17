//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares;

/// <summary>
/// Middleware that establishes request context for correlation and causation ID propagation.
/// This middleware integrates with CQRS module's IRequestContext for distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// This middleware provides:
/// <list type="bullet">
/// <item>Correlation ID extraction from incoming headers or auto-generation</item>
/// <item>Causation ID extraction for event chain tracking</item>
/// <item>Request ID generation for unique request identification</item>
/// <item>Context propagation via HttpContext.Items for downstream access</item>
/// <item>Response header injection for client-side correlation</item>
/// </list>
/// </para>
/// <para>
/// <strong>Integration with CQRS Module:</strong>
/// When the CQRS module's <c>RequestContextBehavior</c> is registered, it will automatically
/// pick up the context established by this middleware via <c>IHttpContextAccessor</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs
/// builder.Services.AddMvp24HoursRequestContext(options =>
/// {
///     options.CorrelationIdHeader = "X-Correlation-ID";
///     options.CausationIdHeader = "X-Causation-ID";
///     options.IncludeInResponse = true;
/// });
/// 
/// var app = builder.Build();
/// app.UseMvp24HoursRequestContext(); // Should be early in the pipeline
/// </code>
/// </example>
public class RequestContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestContextOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="RequestContextMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The request context options.</param>
    public RequestContextMiddleware(
        RequestDelegate next,
        IOptions<RequestContextOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Processes the HTTP request with context establishment.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Extract or generate Correlation ID
        var correlationId = ExtractOrGenerateCorrelationId(context);

        // Extract Causation ID from header (may be null for initial requests)
        var causationId = ExtractCausationId(context);

        // Generate a unique Request ID for this specific request
        var requestId = GenerateRequestId();

        // Extract user and tenant information
        var userId = GetUserId(context);
        var tenantId = GetTenantId(context);

        // Store context in HttpContext.Items for access by downstream components
        context.Items[RequestContextKeys.CorrelationId] = correlationId;
        context.Items[RequestContextKeys.CausationId] = causationId;
        context.Items[RequestContextKeys.RequestId] = requestId;
        context.Items[RequestContextKeys.UserId] = userId;
        context.Items[RequestContextKeys.TenantId] = tenantId;
        context.Items[RequestContextKeys.Timestamp] = DateTimeOffset.UtcNow;

        // Set TraceIdentifier for ASP.NET Core integration
        context.TraceIdentifier = correlationId;

        // Add headers to response
        if (_options.IncludeInResponse)
        {
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(_options.CorrelationIdHeader))
                {
                    context.Response.Headers.Append(_options.CorrelationIdHeader, correlationId);
                }

                if (!context.Response.Headers.ContainsKey(_options.RequestIdHeader))
                {
                    context.Response.Headers.Append(_options.RequestIdHeader, requestId);
                }

                // Include Causation ID in response if present
                if (!string.IsNullOrEmpty(causationId) && !context.Response.Headers.ContainsKey(_options.CausationIdHeader))
                {
                    context.Response.Headers.Append(_options.CausationIdHeader, causationId);
                }

                return Task.CompletedTask;
            });
        }

        await _next(context);
    }

    private string ExtractOrGenerateCorrelationId(HttpContext context)
    {
        // Try to get from header
        if (context.Request.Headers.TryGetValue(_options.CorrelationIdHeader, out var correlationId)
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        // Try alternative headers
        foreach (var header in _options.AlternativeCorrelationHeaders)
        {
            if (context.Request.Headers.TryGetValue(header, out var altCorrelationId)
                && !string.IsNullOrWhiteSpace(altCorrelationId))
            {
                return altCorrelationId.ToString();
            }
        }

        // Generate new one
        return _options.IdGenerator?.Invoke() ?? Guid.NewGuid().ToString("N");
    }

    private string? ExtractCausationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_options.CausationIdHeader, out var causationId)
            && !string.IsNullOrWhiteSpace(causationId))
        {
            return causationId.ToString();
        }

        return null;
    }

    private string GenerateRequestId()
    {
        return _options.IdGenerator?.Invoke() ?? Guid.NewGuid().ToString("N");
    }

    private string? GetUserId(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.Identity.Name;
    }

    private string? GetTenantId(HttpContext context)
    {
        // Check header first
        if (context.Request.Headers.TryGetValue(_options.TenantIdHeader, out var tenantHeader)
            && !string.IsNullOrWhiteSpace(tenantHeader))
        {
            return tenantHeader.ToString();
        }

        // Check claims
        return context.User?.FindFirstValue("tenant_id")
            ?? context.User?.FindFirstValue("tid");
    }
}

/// <summary>
/// Keys for accessing request context values stored in HttpContext.Items.
/// </summary>
public static class RequestContextKeys
{
    /// <summary>Key for Correlation ID.</summary>
    public const string CorrelationId = "Mvp24Hours.RequestContext.CorrelationId";

    /// <summary>Key for Causation ID.</summary>
    public const string CausationId = "Mvp24Hours.RequestContext.CausationId";

    /// <summary>Key for Request ID.</summary>
    public const string RequestId = "Mvp24Hours.RequestContext.RequestId";

    /// <summary>Key for User ID.</summary>
    public const string UserId = "Mvp24Hours.RequestContext.UserId";

    /// <summary>Key for Tenant ID.</summary>
    public const string TenantId = "Mvp24Hours.RequestContext.TenantId";

    /// <summary>Key for Timestamp.</summary>
    public const string Timestamp = "Mvp24Hours.RequestContext.Timestamp";
}

/// <summary>
/// Extension methods for accessing request context from HttpContext.
/// </summary>
public static class HttpContextRequestContextExtensions
{
    /// <summary>
    /// Gets the Correlation ID from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The correlation ID, or the trace identifier if not set.</returns>
    public static string GetCorrelationId(this HttpContext context)
    {
        if (context.Items.TryGetValue(RequestContextKeys.CorrelationId, out var correlationId))
        {
            return correlationId?.ToString() ?? context.TraceIdentifier;
        }
        return context.TraceIdentifier;
    }

    /// <summary>
    /// Gets the Causation ID from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The causation ID, or null if not set.</returns>
    public static string? GetCausationId(this HttpContext context)
    {
        if (context.Items.TryGetValue(RequestContextKeys.CausationId, out var causationId))
        {
            return causationId?.ToString();
        }
        return null;
    }

    /// <summary>
    /// Gets the Request ID from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The request ID, or the trace identifier if not set.</returns>
    public static string GetRequestId(this HttpContext context)
    {
        if (context.Items.TryGetValue(RequestContextKeys.RequestId, out var requestId))
        {
            return requestId?.ToString() ?? context.TraceIdentifier;
        }
        return context.TraceIdentifier;
    }

    /// <summary>
    /// Gets the User ID from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The user ID, or null if not authenticated.</returns>
    public static string? GetUserId(this HttpContext context)
    {
        if (context.Items.TryGetValue(RequestContextKeys.UserId, out var userId))
        {
            return userId?.ToString();
        }
        return null;
    }

    /// <summary>
    /// Gets the Tenant ID from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The tenant ID, or null if not set.</returns>
    public static string? GetTenantId(this HttpContext context)
    {
        if (context.Items.TryGetValue(RequestContextKeys.TenantId, out var tenantId))
        {
            return tenantId?.ToString();
        }
        return null;
    }

    /// <summary>
    /// Gets the request timestamp from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The timestamp when the request was received.</returns>
    public static DateTimeOffset GetRequestTimestamp(this HttpContext context)
    {
        if (context.Items.TryGetValue(RequestContextKeys.Timestamp, out var timestamp) && timestamp is DateTimeOffset dto)
        {
            return dto;
        }
        return DateTimeOffset.UtcNow;
    }
}



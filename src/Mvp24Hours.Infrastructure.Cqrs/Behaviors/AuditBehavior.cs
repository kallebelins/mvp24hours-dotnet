//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Observability;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Pipeline behavior that creates audit trail entries for requests.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior creates audit entries for:
/// <list type="bullet">
/// <item>All requests that implement <see cref="IAuditable"/></item>
/// <item>Optionally, all commands (when configured)</item>
/// </list>
/// </para>
/// <para>
/// The audit entry includes:
/// <list type="bullet">
/// <item>Operation name and type</item>
/// <item>User and tenant information</item>
/// <item>Timing information</item>
/// <item>Success/failure status</item>
/// <item>Optionally, request and response data</item>
/// </list>
/// </para>
/// <para>
/// <strong>Security Note:</strong> Be careful with request/response data logging.
/// Only enable it for non-sensitive operations or ensure data is properly sanitized.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register the behavior
/// services.AddScoped&lt;IAuditStore, EfCoreAuditStore&gt;();
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(AuditBehavior&lt;,&gt;));
/// 
/// // Mark a command as auditable
/// public class DeleteUserCommand : IMediatorCommand, IAuditable
/// {
///     public int UserId { get; init; }
///     
///     public bool ShouldAuditRequestData => true;
/// }
/// </code>
/// </example>
public sealed class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IAuditStore? _auditStore;
    private readonly IRequestContextAccessor? _contextAccessor;
    private readonly IUserContext? _userContext;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>>? _logger;
    private readonly bool _auditAllCommands;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        MaxDepth = 3,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Creates a new instance of the AuditBehavior.
    /// </summary>
    /// <param name="auditStore">Optional audit store for persisting entries.</param>
    /// <param name="contextAccessor">Optional request context accessor.</param>
    /// <param name="userContext">Optional user context.</param>
    /// <param name="httpContextAccessor">Optional HTTP context accessor.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="auditAllCommands">Whether to audit all commands regardless of IAuditable.</param>
    public AuditBehavior(
        IAuditStore? auditStore = null,
        IRequestContextAccessor? contextAccessor = null,
        IUserContext? userContext = null,
        IHttpContextAccessor? httpContextAccessor = null,
        ILogger<AuditBehavior<TRequest, TResponse>>? logger = null,
        bool auditAllCommands = false)
    {
        _auditStore = auditStore;
        _contextAccessor = contextAccessor;
        _userContext = userContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _auditAllCommands = auditAllCommands;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Skip if no audit store is configured
        if (_auditStore == null)
        {
            return await next();
        }

        var auditable = request as IAuditable;
        var isCommand = IsCommand();

        // Skip if request is not auditable and we're not auditing all commands
        if (auditable == null && !(_auditAllCommands && isCommand))
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        var requestType = GetRequestType();
        var context = _contextAccessor?.Context;

        var entry = new AuditEntry
        {
            OperationName = requestName,
            OperationType = requestType,
            CorrelationId = context?.CorrelationId,
            CausationId = context?.CausationId,
            RequestId = context?.RequestId,
            UserId = _userContext?.UserId ?? context?.UserId,
            UserName = _userContext?.UserName,
            TenantId = context?.TenantId,
            Timestamp = DateTimeOffset.UtcNow,
            ClientIp = GetClientIp(),
            UserAgent = GetUserAgent()
        };

        // Capture request data if configured
        if (auditable?.ShouldAuditRequestData == true)
        {
            entry.RequestData = SafeSerialize(request);
        }

        // Add custom metadata
        if (auditable?.GetAuditMetadata() is { } metadata)
        {
            entry.Metadata = new Dictionary<string, string>(metadata);
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            entry.IsSuccess = true;
            entry.DurationMs = stopwatch.ElapsedMilliseconds;

            // Capture response data if configured
            if (auditable?.ShouldAuditResponseData == true)
            {
                entry.ResponseData = SafeSerialize(response);
            }

            await SaveAuditEntryAsync(entry, cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            entry.IsSuccess = false;
            entry.DurationMs = stopwatch.ElapsedMilliseconds;
            entry.ErrorType = ex.GetType().FullName;
            entry.ErrorMessage = ex.Message;

            await SaveAuditEntryAsync(entry, cancellationToken);

            throw;
        }
    }

    private async Task SaveAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await _auditStore!.SaveAsync(entry, cancellationToken);

            _logger?.LogDebug(
                "[Audit] Entry saved. Operation: {Operation}, User: {User}, Success: {Success}",
                entry.OperationName,
                entry.UserId ?? "anonymous",
                entry.IsSuccess);
        }
        catch (Exception ex)
        {
            // Don't fail the request if audit fails, just log
            _logger?.LogError(
                ex,
                "[Audit] Failed to save audit entry for {Operation}",
                entry.OperationName);
        }
    }

    private string? SafeSerialize<T>(T obj)
    {
        if (obj == null)
            return null;

        try
        {
            return JsonSerializer.Serialize(obj, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "[Audit] Failed to serialize data for {Type}",
                typeof(T).Name);
            return $"[Serialization failed: {ex.Message}]";
        }
    }

    private string? GetClientIp()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null)
            return null;

        // Try X-Forwarded-For first
        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ips = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
                return ips[0].Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null)
            return null;

        if (httpContext.Request.Headers.TryGetValue("User-Agent", out var userAgent))
        {
            return userAgent.ToString();
        }

        return null;
    }

    private static bool IsCommand()
    {
        return typeof(TRequest).GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IMediatorCommand"));
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


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Observability;

/// <summary>
/// Default implementation of <see cref="IRequestContext"/>.
/// </summary>
/// <remarks>
/// This is an immutable record that contains all context information for a request.
/// Use <see cref="CreateChildContext"/> to create nested contexts for child operations.
/// </remarks>
public sealed record RequestContext : IRequestContext
{
    /// <summary>
    /// Creates a new request context with the specified values.
    /// </summary>
    public RequestContext(
        string? correlationId = null,
        string? causationId = null,
        string? requestId = null,
        string? userId = null,
        string? tenantId = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        CorrelationId = correlationId ?? GenerateId();
        CausationId = causationId;
        RequestId = requestId ?? GenerateId();
        UserId = userId;
        TenantId = tenantId;
        Timestamp = DateTimeOffset.UtcNow;
        Metadata = metadata ?? new Dictionary<string, object?>();
    }

    /// <inheritdoc />
    public string CorrelationId { get; }

    /// <inheritdoc />
    public string? CausationId { get; }

    /// <inheritdoc />
    public string RequestId { get; }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; }

    /// <inheritdoc />
    public string? UserId { get; }

    /// <inheritdoc />
    public string? TenantId { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <inheritdoc />
    public IRequestContext CreateChildContext()
    {
        return new RequestContext(
            correlationId: CorrelationId,
            causationId: RequestId, // Current RequestId becomes the child's CausationId
            requestId: null, // Will generate a new RequestId
            userId: UserId,
            tenantId: TenantId,
            metadata: Metadata);
    }

    /// <summary>
    /// Creates a new empty context with default values.
    /// </summary>
    public static RequestContext Empty => new();

    /// <summary>
    /// Creates a context from an existing correlation ID (e.g., from HTTP header).
    /// </summary>
    public static RequestContext FromCorrelationId(string correlationId, string? userId = null, string? tenantId = null)
    {
        return new RequestContext(correlationId: correlationId, userId: userId, tenantId: tenantId);
    }

    private static string GenerateId() => Guid.NewGuid().ToString("N");
}

/// <summary>
/// Default implementation of <see cref="IRequestContextAccessor"/> using AsyncLocal for ambient context.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses <see cref="AsyncLocal{T}"/> to maintain the request context
/// across async operations within the same logical execution flow.
/// </para>
/// <para>
/// In ASP.NET Core scenarios, this is typically registered as a scoped service and
/// populated by the <see cref="Mvp24Hours.Infrastructure.Cqrs.Behaviors.RequestContextBehavior{TRequest, TResponse}"/>.
/// </para>
/// </remarks>
public sealed class RequestContextAccessor : IRequestContextAccessor
{
    private static readonly AsyncLocal<ContextHolder> _contextHolder = new();

    /// <inheritdoc />
    public IRequestContext? Context
    {
        get => _contextHolder.Value?.Context;
        set
        {
            var holder = _contextHolder.Value;
            if (holder != null)
            {
                holder.Context = null;
            }

            if (value != null)
            {
                _contextHolder.Value = new ContextHolder { Context = value };
            }
        }
    }

    private sealed class ContextHolder
    {
        public IRequestContext? Context;
    }
}

/// <summary>
/// Default implementation of <see cref="IRequestContextFactory"/>.
/// </summary>
public sealed class RequestContextFactory : IRequestContextFactory
{
    /// <inheritdoc />
    public IRequestContext Create(
        string? correlationId = null,
        string? causationId = null,
        string? userId = null,
        string? tenantId = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        return new RequestContext(
            correlationId: correlationId,
            causationId: causationId,
            userId: userId,
            tenantId: tenantId,
            metadata: metadata);
    }
}


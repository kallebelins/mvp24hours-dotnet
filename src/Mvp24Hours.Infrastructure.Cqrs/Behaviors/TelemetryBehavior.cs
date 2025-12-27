//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Observability;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Pipeline behavior that integrates with ILogger for telemetry and observability.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior uses <see cref="ILogger{T}"/> to emit structured logging for CQRS operations:
/// <list type="bullet">
/// <item>Request start events (Debug level)</item>
/// <item>Request success events with timing (Information level)</item>
/// <item>Request failure events with exception details (Error level)</item>
/// </list>
/// </para>
/// <para>
/// All logs include structured properties for correlation, causation, user context, and timing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register the behavior (automatically registered via AddMvpMediator)
/// services.AddMvpMediator(typeof(MyAssembly));
/// </code>
/// </example>
public sealed class TelemetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IRequestContextAccessor? _contextAccessor;
    private readonly ILogger<TelemetryBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Creates a new instance of the TelemetryBehavior.
    /// </summary>
    /// <param name="logger">Logger instance for telemetry.</param>
    /// <param name="contextAccessor">Optional request context accessor for including context in telemetry.</param>
    public TelemetryBehavior(
        ILogger<TelemetryBehavior<TRequest, TResponse>> logger,
        IRequestContextAccessor? contextAccessor = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _contextAccessor = contextAccessor;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestType = GetRequestType();
        var context = _contextAccessor?.Context;

        // Emit start event
        _logger.LogDebug(
            "Mediator request started. RequestName: {RequestName}, RequestType: {RequestType}, CorrelationId: {CorrelationId}, CausationId: {CausationId}, RequestId: {RequestId}, UserId: {UserId}, TenantId: {TenantId}",
            requestName,
            requestType,
            context?.CorrelationId,
            context?.CausationId,
            context?.RequestId,
            context?.UserId,
            context?.TenantId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            // Emit success event
            _logger.LogInformation(
                "Mediator request succeeded. RequestName: {RequestName}, RequestType: {RequestType}, CorrelationId: {CorrelationId}, CausationId: {CausationId}, RequestId: {RequestId}, UserId: {UserId}, TenantId: {TenantId}, ElapsedMilliseconds: {ElapsedMilliseconds}",
                requestName,
                requestType,
                context?.CorrelationId,
                context?.CausationId,
                context?.RequestId,
                context?.UserId,
                context?.TenantId,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Emit failure event
            _logger.LogError(
                ex,
                "Mediator request failed. RequestName: {RequestName}, RequestType: {RequestType}, CorrelationId: {CorrelationId}, CausationId: {CausationId}, RequestId: {RequestId}, UserId: {UserId}, TenantId: {TenantId}, ElapsedMilliseconds: {ElapsedMilliseconds}, ExceptionType: {ExceptionType}",
                requestName,
                requestType,
                context?.CorrelationId,
                context?.CausationId,
                context?.RequestId,
                context?.UserId,
                context?.TenantId,
                stopwatch.ElapsedMilliseconds,
                ex.GetType().Name);

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

/// <summary>
/// Event names for mediator telemetry.
/// </summary>
public static class TelemetryEventNames
{
    /// <summary>
    /// Emitted when a mediator request starts.
    /// </summary>
    public const string MediatorRequestStart = "mediator-request-start";

    /// <summary>
    /// Emitted when a mediator request succeeds.
    /// </summary>
    public const string MediatorRequestSuccess = "mediator-request-success";

    /// <summary>
    /// Emitted when a mediator request fails.
    /// </summary>
    public const string MediatorRequestFailure = "mediator-request-failure";

    /// <summary>
    /// Emitted when a domain event is raised.
    /// </summary>
    public const string DomainEventRaised = "domain-event-raised";

    /// <summary>
    /// Emitted when a notification is published.
    /// </summary>
    public const string NotificationPublished = "notification-published";

    /// <summary>
    /// Emitted when an integration event is sent.
    /// </summary>
    public const string IntegrationEventSent = "integration-event-sent";

    /// <summary>
    /// Emitted when an audit trail entry is created.
    /// </summary>
    public const string AuditEntryCreated = "audit-entry-created";
}

/// <summary>
/// Data structure for mediator telemetry events.
/// </summary>
public sealed record MediatorTelemetryData
{
    /// <summary>
    /// Gets or sets the name of the request (type name).
    /// </summary>
    public required string RequestName { get; init; }

    /// <summary>
    /// Gets or sets the type of request (Command, Query, Request).
    /// </summary>
    public required string RequestType { get; init; }

    /// <summary>
    /// Gets or sets the correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets or sets the causation ID for event chains.
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// Gets or sets the unique request ID.
    /// </summary>
    public string? RequestId { get; init; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    public long? ElapsedMilliseconds { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of the event.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets whether the request was successful.
    /// </summary>
    public bool? IsSuccess { get; init; }

    /// <summary>
    /// Gets or sets the exception type name if the request failed.
    /// </summary>
    public string? ExceptionType { get; init; }

    /// <summary>
    /// Gets or sets the exception message if the request failed.
    /// </summary>
    public string? ExceptionMessage { get; init; }
}


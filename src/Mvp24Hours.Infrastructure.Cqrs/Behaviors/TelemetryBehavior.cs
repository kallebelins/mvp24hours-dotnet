//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Diagnostics;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Cqrs.Observability;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Pipeline behavior that integrates with the Mvp24Hours telemetry system.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior uses <see cref="TelemetryHelper"/> to emit telemetry events for CQRS operations:
/// <list type="bullet">
/// <item>Request start events</item>
/// <item>Request success events with timing</item>
/// <item>Request failure events with exception details</item>
/// </list>
/// </para>
/// <para>
/// <strong>Telemetry Events Emitted:</strong>
/// <list type="bullet">
/// <item>mediator-request-start</item>
/// <item>mediator-request-success</item>
/// <item>mediator-request-failure</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure telemetry handlers
/// services.AddMvp24HoursTelemetry(TelemetryLevels.Information, (eventName, args) =>
/// {
///     Console.WriteLine($"[{eventName}] {string.Join(", ", args)}");
/// });
/// 
/// // Register the behavior
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(TelemetryBehavior&lt;,&gt;));
/// </code>
/// </example>
public sealed class TelemetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IRequestContextAccessor? _contextAccessor;

    /// <summary>
    /// Creates a new instance of the TelemetryBehavior.
    /// </summary>
    /// <param name="contextAccessor">Optional request context accessor for including context in telemetry.</param>
    public TelemetryBehavior(IRequestContextAccessor? contextAccessor = null)
    {
        _contextAccessor = contextAccessor;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestType = GetRequestType();
        var context = _contextAccessor?.Context;

        // Emit start event
        TelemetryHelper.Execute(
            TelemetryLevels.Verbose,
            TelemetryEventNames.MediatorRequestStart,
            new MediatorTelemetryData
            {
                RequestName = requestName,
                RequestType = requestType,
                CorrelationId = context?.CorrelationId,
                CausationId = context?.CausationId,
                RequestId = context?.RequestId,
                UserId = context?.UserId,
                TenantId = context?.TenantId,
                Timestamp = DateTimeOffset.UtcNow
            });

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            // Emit success event
            TelemetryHelper.Execute(
                TelemetryLevels.Information,
                TelemetryEventNames.MediatorRequestSuccess,
                new MediatorTelemetryData
                {
                    RequestName = requestName,
                    RequestType = requestType,
                    CorrelationId = context?.CorrelationId,
                    CausationId = context?.CausationId,
                    RequestId = context?.RequestId,
                    UserId = context?.UserId,
                    TenantId = context?.TenantId,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTimeOffset.UtcNow,
                    IsSuccess = true
                });

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Emit failure event
            TelemetryHelper.Execute(
                TelemetryLevels.Error,
                TelemetryEventNames.MediatorRequestFailure,
                new MediatorTelemetryData
                {
                    RequestName = requestName,
                    RequestType = requestType,
                    CorrelationId = context?.CorrelationId,
                    CausationId = context?.CausationId,
                    RequestId = context?.RequestId,
                    UserId = context?.UserId,
                    TenantId = context?.TenantId,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTimeOffset.UtcNow,
                    IsSuccess = false,
                    ExceptionType = ex.GetType().Name,
                    ExceptionMessage = ex.Message
                });

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


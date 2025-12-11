//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Diagnostics;

namespace Mvp24Hours.Infrastructure.Cqrs.Observability;

/// <summary>
/// ActivitySource for CQRS operations in OpenTelemetry-compatible tracing.
/// </summary>
/// <remarks>
/// <para>
/// This class provides integration with the .NET Activity API which is automatically
/// exported by OpenTelemetry when configured. Activities created here will appear
/// as spans in your tracing backend (Jaeger, Zipkin, etc.).
/// </para>
/// <para>
/// <strong>Activity Names:</strong>
/// <list type="bullet">
/// <item>Mvp24Hours.Mediator.Request</item>
/// <item>Mvp24Hours.Mediator.Command</item>
/// <item>Mvp24Hours.Mediator.Query</item>
/// <item>Mvp24Hours.Mediator.Notification</item>
/// <item>Mvp24Hours.Mediator.DomainEvent</item>
/// <item>Mvp24Hours.Mediator.IntegrationEvent</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure OpenTelemetry to include Mvp24Hours activities
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(builder =>
///     {
///         builder
///             .AddSource(MediatorActivitySource.SourceName)
///             .AddAspNetCoreInstrumentation()
///             .AddJaegerExporter();
///     });
/// </code>
/// </example>
public static class MediatorActivitySource
{
    /// <summary>
    /// The name of the ActivitySource for Mvp24Hours Mediator operations.
    /// </summary>
    public const string SourceName = "Mvp24Hours.Mediator";

    /// <summary>
    /// The ActivitySource instance used for creating activities.
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName, "1.0.0");

    /// <summary>
    /// Activity names for different operations.
    /// </summary>
    public static class ActivityNames
    {
        /// <summary>Activity name for generic mediator requests.</summary>
        public const string Request = "Mvp24Hours.Mediator.Request";
        /// <summary>Activity name for command operations.</summary>
        public const string Command = "Mvp24Hours.Mediator.Command";
        /// <summary>Activity name for query operations.</summary>
        public const string Query = "Mvp24Hours.Mediator.Query";
        /// <summary>Activity name for notification operations.</summary>
        public const string Notification = "Mvp24Hours.Mediator.Notification";
        /// <summary>Activity name for domain event operations.</summary>
        public const string DomainEvent = "Mvp24Hours.Mediator.DomainEvent";
        /// <summary>Activity name for integration event operations.</summary>
        public const string IntegrationEvent = "Mvp24Hours.Mediator.IntegrationEvent";
        /// <summary>Activity name for saga operations.</summary>
        public const string Saga = "Mvp24Hours.Mediator.Saga";
        /// <summary>Activity name for saga step operations.</summary>
        public const string SagaStep = "Mvp24Hours.Mediator.SagaStep";
    }

    /// <summary>
    /// Tag names for activity attributes.
    /// </summary>
    public static class TagNames
    {
        /// <summary>Tag for the request type name.</summary>
        public const string RequestName = "mediator.request.name";
        /// <summary>Tag for the request type (Command, Query, etc.).</summary>
        public const string RequestType = "mediator.request.type";
        /// <summary>Tag for the correlation ID.</summary>
        public const string CorrelationId = "mediator.correlation_id";
        /// <summary>Tag for the causation ID.</summary>
        public const string CausationId = "mediator.causation_id";
        /// <summary>Tag for the unique request ID.</summary>
        public const string RequestId = "mediator.request_id";
        /// <summary>Tag for the user ID (follows OpenTelemetry semantic conventions).</summary>
        public const string UserId = "enduser.id";
        /// <summary>Tag for the tenant ID.</summary>
        public const string TenantId = "tenant.id";
        /// <summary>Tag indicating if the operation was successful.</summary>
        public const string IsSuccess = "mediator.is_success";
        /// <summary>Tag for the error type name.</summary>
        public const string ErrorType = "error.type";
        /// <summary>Tag for the error message.</summary>
        public const string ErrorMessage = "error.message";
    }

    /// <summary>
    /// Starts an activity for a mediator request.
    /// </summary>
    /// <param name="requestName">The name of the request type.</param>
    /// <param name="requestType">The type of request (Command, Query, etc.).</param>
    /// <param name="context">Optional request context with tracing information.</param>
    /// <returns>An Activity if listeners are registered, null otherwise.</returns>
    public static Activity? StartRequestActivity(
        string requestName,
        string requestType,
        IRequestContext? context = null)
    {
        var activityName = requestType switch
        {
            "Command" => ActivityNames.Command,
            "Query" => ActivityNames.Query,
            _ => ActivityNames.Request
        };

        var activity = Source.StartActivity(activityName, ActivityKind.Internal);

        if (activity == null)
            return null;

        activity.SetTag(TagNames.RequestName, requestName);
        activity.SetTag(TagNames.RequestType, requestType);

        if (context != null)
        {
            activity.SetTag(TagNames.CorrelationId, context.CorrelationId);
            activity.SetTag(TagNames.RequestId, context.RequestId);

            if (context.CausationId != null)
                activity.SetTag(TagNames.CausationId, context.CausationId);

            if (context.UserId != null)
                activity.SetTag(TagNames.UserId, context.UserId);

            if (context.TenantId != null)
                activity.SetTag(TagNames.TenantId, context.TenantId);
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for a notification.
    /// </summary>
    public static Activity? StartNotificationActivity(string notificationName, IRequestContext? context = null)
    {
        var activity = Source.StartActivity(ActivityNames.Notification, ActivityKind.Internal);

        if (activity == null)
            return null;

        activity.SetTag(TagNames.RequestName, notificationName);
        activity.SetTag(TagNames.RequestType, "Notification");

        if (context != null)
        {
            activity.SetTag(TagNames.CorrelationId, context.CorrelationId);
            activity.SetTag(TagNames.RequestId, context.RequestId);
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for a domain event.
    /// </summary>
    public static Activity? StartDomainEventActivity(string eventName, IRequestContext? context = null)
    {
        var activity = Source.StartActivity(ActivityNames.DomainEvent, ActivityKind.Internal);

        if (activity == null)
            return null;

        activity.SetTag(TagNames.RequestName, eventName);
        activity.SetTag(TagNames.RequestType, "DomainEvent");

        if (context != null)
        {
            activity.SetTag(TagNames.CorrelationId, context.CorrelationId);
            activity.SetTag(TagNames.RequestId, context.RequestId);
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for an integration event.
    /// </summary>
    public static Activity? StartIntegrationEventActivity(string eventName, IRequestContext? context = null)
    {
        var activity = Source.StartActivity(ActivityNames.IntegrationEvent, ActivityKind.Producer);

        if (activity == null)
            return null;

        activity.SetTag(TagNames.RequestName, eventName);
        activity.SetTag(TagNames.RequestType, "IntegrationEvent");

        if (context != null)
        {
            activity.SetTag(TagNames.CorrelationId, context.CorrelationId);
            activity.SetTag(TagNames.RequestId, context.RequestId);
        }

        return activity;
    }

    /// <summary>
    /// Marks an activity as successful.
    /// </summary>
    public static void SetSuccess(Activity? activity)
    {
        if (activity == null)
            return;

        activity.SetTag(TagNames.IsSuccess, true);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Marks an activity as failed with exception details.
    /// </summary>
    public static void SetError(Activity? activity, Exception exception)
    {
        if (activity == null)
            return;

        activity.SetTag(TagNames.IsSuccess, false);
        activity.SetTag(TagNames.ErrorType, exception.GetType().FullName);
        activity.SetTag(TagNames.ErrorMessage, exception.Message);
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);

        // Record exception event
        activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace }
        }));
    }
}

/// <summary>
/// Extension methods for Activity operations.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Sets the request context information on an activity.
    /// </summary>
    public static Activity WithContext(this Activity activity, IRequestContext context)
    {
        activity.SetTag(MediatorActivitySource.TagNames.CorrelationId, context.CorrelationId);
        activity.SetTag(MediatorActivitySource.TagNames.RequestId, context.RequestId);

        if (context.CausationId != null)
            activity.SetTag(MediatorActivitySource.TagNames.CausationId, context.CausationId);

        if (context.UserId != null)
            activity.SetTag(MediatorActivitySource.TagNames.UserId, context.UserId);

        if (context.TenantId != null)
            activity.SetTag(MediatorActivitySource.TagNames.TenantId, context.TenantId);

        return activity;
    }

    /// <summary>
    /// Records a custom event on the activity.
    /// </summary>
    public static Activity RecordEvent(this Activity activity, string name, params (string Key, object Value)[] tags)
    {
        var activityTags = new ActivityTagsCollection();
        foreach (var (key, value) in tags)
        {
            activityTags.Add(key, value);
        }

        activity.AddEvent(new ActivityEvent(name, tags: activityTags));
        return activity;
    }
}


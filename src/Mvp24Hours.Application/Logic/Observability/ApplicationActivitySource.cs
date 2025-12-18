//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Diagnostics;

namespace Mvp24Hours.Application.Logic.Observability;

/// <summary>
/// ActivitySource for Application Service operations in OpenTelemetry-compatible tracing.
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
/// <item>Mvp24Hours.Application.Query - Query operations (List, GetBy, GetById)</item>
/// <item>Mvp24Hours.Application.Command - Command operations (Add, Modify, Remove)</item>
/// <item>Mvp24Hours.Application.Specification - Specification-based queries</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure OpenTelemetry to include Mvp24Hours Application activities
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(builder =>
///     {
///         builder
///             .AddSource(ApplicationActivitySource.SourceName)
///             .AddAspNetCoreInstrumentation()
///             .AddJaegerExporter();
///     });
/// </code>
/// </example>
public static class ApplicationActivitySource
{
    /// <summary>
    /// The name of the ActivitySource for Mvp24Hours Application operations.
    /// </summary>
    public const string SourceName = "Mvp24Hours.Application";

    /// <summary>
    /// The ActivitySource instance used for creating activities.
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName, "1.0.0");

    /// <summary>
    /// Activity names for different operations.
    /// </summary>
    public static class ActivityNames
    {
        /// <summary>Activity name for query operations.</summary>
        public const string Query = "Mvp24Hours.Application.Query";
        /// <summary>Activity name for command operations.</summary>
        public const string Command = "Mvp24Hours.Application.Command";
        /// <summary>Activity name for specification-based queries.</summary>
        public const string Specification = "Mvp24Hours.Application.Specification";
        /// <summary>Activity name for validation operations.</summary>
        public const string Validation = "Mvp24Hours.Application.Validation";
        /// <summary>Activity name for transaction operations.</summary>
        public const string Transaction = "Mvp24Hours.Application.Transaction";
    }

    /// <summary>
    /// Tag names for activity attributes.
    /// </summary>
    public static class TagNames
    {
        /// <summary>Tag for the service type name.</summary>
        public const string ServiceName = "application.service.name";
        /// <summary>Tag for the operation name.</summary>
        public const string OperationName = "application.operation.name";
        /// <summary>Tag for the operation type (Query, Command).</summary>
        public const string OperationType = "application.operation.type";
        /// <summary>Tag for the entity type.</summary>
        public const string EntityType = "application.entity.type";
        /// <summary>Tag for the entity ID.</summary>
        public const string EntityId = "application.entity.id";
        /// <summary>Tag for the affected row count.</summary>
        public const string AffectedRows = "application.affected_rows";
        /// <summary>Tag for the result count (for queries).</summary>
        public const string ResultCount = "application.result_count";
        /// <summary>Tag for the correlation ID.</summary>
        public const string CorrelationId = "correlation.id";
        /// <summary>Tag for the causation ID.</summary>
        public const string CausationId = "causation.id";
        /// <summary>Tag for the user ID (follows OpenTelemetry semantic conventions).</summary>
        public const string UserId = "enduser.id";
        /// <summary>Tag for the tenant ID.</summary>
        public const string TenantId = "tenant.id";
        /// <summary>Tag indicating if the operation was successful.</summary>
        public const string IsSuccess = "application.is_success";
        /// <summary>Tag for the error type name.</summary>
        public const string ErrorType = "error.type";
        /// <summary>Tag for the error message.</summary>
        public const string ErrorMessage = "error.message";
    }

    /// <summary>
    /// Starts an activity for a query operation.
    /// </summary>
    /// <param name="serviceName">The name of the service performing the operation.</param>
    /// <param name="operationName">The name of the operation (e.g., "List", "GetById").</param>
    /// <param name="entityType">The entity type being queried.</param>
    /// <returns>An Activity if listeners are registered, null otherwise.</returns>
    public static Activity? StartQueryActivity(string serviceName, string operationName, string? entityType = null)
    {
        var activity = Source.StartActivity(ActivityNames.Query, ActivityKind.Internal);

        if (activity == null)
            return null;

        activity.SetTag(TagNames.ServiceName, serviceName);
        activity.SetTag(TagNames.OperationName, operationName);
        activity.SetTag(TagNames.OperationType, "Query");

        if (entityType != null)
            activity.SetTag(TagNames.EntityType, entityType);

        return activity;
    }

    /// <summary>
    /// Starts an activity for a command operation.
    /// </summary>
    /// <param name="serviceName">The name of the service performing the operation.</param>
    /// <param name="operationName">The name of the operation (e.g., "Add", "Modify", "Remove").</param>
    /// <param name="entityType">The entity type being modified.</param>
    /// <returns>An Activity if listeners are registered, null otherwise.</returns>
    public static Activity? StartCommandActivity(string serviceName, string operationName, string? entityType = null)
    {
        var activity = Source.StartActivity(ActivityNames.Command, ActivityKind.Internal);

        if (activity == null)
            return null;

        activity.SetTag(TagNames.ServiceName, serviceName);
        activity.SetTag(TagNames.OperationName, operationName);
        activity.SetTag(TagNames.OperationType, "Command");

        if (entityType != null)
            activity.SetTag(TagNames.EntityType, entityType);

        return activity;
    }

    /// <summary>
    /// Starts an activity for a specification-based query.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="specificationName">The name of the specification being used.</param>
    /// <param name="entityType">The entity type being queried.</param>
    /// <returns>An Activity if listeners are registered, null otherwise.</returns>
    public static Activity? StartSpecificationActivity(string serviceName, string specificationName, string? entityType = null)
    {
        var activity = Source.StartActivity(ActivityNames.Specification, ActivityKind.Internal);

        if (activity == null)
            return null;

        activity.SetTag(TagNames.ServiceName, serviceName);
        activity.SetTag(TagNames.OperationName, specificationName);
        activity.SetTag(TagNames.OperationType, "Specification");

        if (entityType != null)
            activity.SetTag(TagNames.EntityType, entityType);

        return activity;
    }

    /// <summary>
    /// Sets correlation context tags on an activity.
    /// </summary>
    /// <param name="activity">The activity to set tags on.</param>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="causationId">The causation ID (optional).</param>
    /// <param name="userId">The user ID (optional).</param>
    /// <param name="tenantId">The tenant ID (optional).</param>
    public static void SetCorrelationContext(
        Activity? activity,
        string? correlationId,
        string? causationId = null,
        string? userId = null,
        string? tenantId = null)
    {
        if (activity == null)
            return;

        if (correlationId != null)
            activity.SetTag(TagNames.CorrelationId, correlationId);

        if (causationId != null)
            activity.SetTag(TagNames.CausationId, causationId);

        if (userId != null)
            activity.SetTag(TagNames.UserId, userId);

        if (tenantId != null)
            activity.SetTag(TagNames.TenantId, tenantId);
    }

    /// <summary>
    /// Sets entity information on an activity.
    /// </summary>
    /// <param name="activity">The activity to set tags on.</param>
    /// <param name="entityId">The entity ID.</param>
    public static void SetEntityId(Activity? activity, object? entityId)
    {
        if (activity == null || entityId == null)
            return;

        activity.SetTag(TagNames.EntityId, entityId.ToString());
    }

    /// <summary>
    /// Sets the result count for a query operation.
    /// </summary>
    /// <param name="activity">The activity to set the tag on.</param>
    /// <param name="count">The number of results returned.</param>
    public static void SetResultCount(Activity? activity, int count)
    {
        activity?.SetTag(TagNames.ResultCount, count);
    }

    /// <summary>
    /// Sets the affected rows count for a command operation.
    /// </summary>
    /// <param name="activity">The activity to set the tag on.</param>
    /// <param name="rows">The number of rows affected.</param>
    public static void SetAffectedRows(Activity? activity, int rows)
    {
        activity?.SetTag(TagNames.AffectedRows, rows);
    }

    /// <summary>
    /// Marks an activity as successful.
    /// </summary>
    /// <param name="activity">The activity to mark as successful.</param>
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
    /// <param name="activity">The activity to mark as failed.</param>
    /// <param name="exception">The exception that occurred.</param>
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

    /// <summary>
    /// Records a custom event on the activity.
    /// </summary>
    /// <param name="activity">The activity to record the event on.</param>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="tags">Optional tags for the event.</param>
    public static void RecordEvent(Activity? activity, string eventName, params (string Key, object? Value)[] tags)
    {
        if (activity == null)
            return;

        var activityTags = new ActivityTagsCollection();
        foreach (var (key, value) in tags)
        {
            if (value != null)
                activityTags.Add(key, value);
        }

        activity.AddEvent(new ActivityEvent(eventName, tags: activityTags));
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Diagnostics;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Observability;

/// <summary>
/// Consolidated ActivitySource for all RabbitMQ operations in OpenTelemetry-compatible tracing.
/// </summary>
/// <remarks>
/// <para>
/// This class provides integration with the .NET Activity API which is automatically
/// exported by OpenTelemetry when configured. Activities created here will appear
/// as spans in your tracing backend (Jaeger, Zipkin, Application Insights, etc.).
/// </para>
/// <para>
/// <strong>Activity Names:</strong>
/// <list type="bullet">
/// <item>Mvp24Hours.RabbitMQ.Publish - For message publishing operations</item>
/// <item>Mvp24Hours.RabbitMQ.Consume - For message consumption operations</item>
/// <item>Mvp24Hours.RabbitMQ.Send - For direct message sending operations</item>
/// <item>Mvp24Hours.RabbitMQ.Request - For RPC request operations</item>
/// <item>Mvp24Hours.RabbitMQ.Response - For RPC response operations</item>
/// <item>Mvp24Hours.RabbitMQ.Batch - For batch processing operations</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure OpenTelemetry to include Mvp24Hours RabbitMQ activities
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(builder =>
///     {
///         builder
///             .AddSource(RabbitMQActivitySource.SourceName)
///             .AddAspNetCoreInstrumentation()
///             .AddJaegerExporter();
///     });
/// </code>
/// </example>
public static class RabbitMQActivitySource
{
    /// <summary>
    /// The name of the ActivitySource for Mvp24Hours RabbitMQ operations.
    /// Use this constant when configuring OpenTelemetry.
    /// </summary>
    public const string SourceName = "Mvp24Hours.RabbitMQ";

    /// <summary>
    /// The version of the ActivitySource.
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// The main ActivitySource instance used for creating activities.
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName, Version);

    /// <summary>
    /// Activity names for different RabbitMQ operations.
    /// </summary>
    public static class ActivityNames
    {
        /// <summary>Activity name for message publishing operations.</summary>
        public const string Publish = "Mvp24Hours.RabbitMQ.Publish";

        /// <summary>Activity name for message consumption operations.</summary>
        public const string Consume = "Mvp24Hours.RabbitMQ.Consume";

        /// <summary>Activity name for direct message sending operations.</summary>
        public const string Send = "Mvp24Hours.RabbitMQ.Send";

        /// <summary>Activity name for RPC request operations.</summary>
        public const string Request = "Mvp24Hours.RabbitMQ.Request";

        /// <summary>Activity name for RPC response operations.</summary>
        public const string Response = "Mvp24Hours.RabbitMQ.Response";

        /// <summary>Activity name for batch processing operations.</summary>
        public const string Batch = "Mvp24Hours.RabbitMQ.Batch";

        /// <summary>Activity name for connection operations.</summary>
        public const string Connection = "Mvp24Hours.RabbitMQ.Connection";

        /// <summary>Activity name for channel operations.</summary>
        public const string Channel = "Mvp24Hours.RabbitMQ.Channel";

        /// <summary>Activity name for scheduled message operations.</summary>
        public const string Scheduled = "Mvp24Hours.RabbitMQ.Scheduled";

        /// <summary>Activity name for saga operations.</summary>
        public const string Saga = "Mvp24Hours.RabbitMQ.Saga";
    }

    /// <summary>
    /// Semantic convention tags for messaging operations following OpenTelemetry standards.
    /// </summary>
    public static class Tags
    {
        /// <summary>The messaging system being used (always "rabbitmq").</summary>
        public const string MessagingSystem = "messaging.system";

        /// <summary>The type of operation (send, receive, process).</summary>
        public const string MessagingOperation = "messaging.operation";

        /// <summary>The destination exchange name.</summary>
        public const string MessagingDestination = "messaging.destination";

        /// <summary>The destination type (topic, queue, fanout, direct).</summary>
        public const string MessagingDestinationKind = "messaging.destination_kind";

        /// <summary>The routing key used.</summary>
        public const string MessagingRoutingKey = "messaging.rabbitmq.routing_key";

        /// <summary>The unique message identifier.</summary>
        public const string MessagingMessageId = "messaging.message_id";

        /// <summary>The type of message being processed.</summary>
        public const string MessagingMessageType = "messaging.message_type";

        /// <summary>The correlation ID for request/response scenarios.</summary>
        public const string MessagingCorrelationId = "messaging.correlation_id";

        /// <summary>The causation ID for event chain tracking.</summary>
        public const string MessagingCausationId = "messaging.causation_id";

        /// <summary>The conversation ID for multi-message conversations.</summary>
        public const string MessagingConversationId = "messaging.conversation_id";

        /// <summary>The queue name for consumers.</summary>
        public const string MessagingConsumerQueue = "messaging.consumer.queue";

        /// <summary>The consumer tag.</summary>
        public const string MessagingConsumerTag = "messaging.consumer.tag";

        /// <summary>Whether the message was redelivered.</summary>
        public const string MessagingRedelivered = "messaging.redelivered";

        /// <summary>The number of times the message was redelivered.</summary>
        public const string MessagingRedeliveryCount = "messaging.redelivery_count";

        /// <summary>The message payload size in bytes.</summary>
        public const string MessagingPayloadSize = "messaging.payload_size_bytes";

        /// <summary>The message priority.</summary>
        public const string MessagingPriority = "messaging.priority";

        /// <summary>Whether the message is persistent.</summary>
        public const string MessagingPersistent = "messaging.persistent";

        /// <summary>The message expiration/TTL.</summary>
        public const string MessagingExpiration = "messaging.expiration";

        /// <summary>The tenant ID for multi-tenant scenarios.</summary>
        public const string TenantId = "tenant.id";

        /// <summary>The user ID for the operation.</summary>
        public const string UserId = "user.id";

        /// <summary>The batch size for batch operations.</summary>
        public const string BatchSize = "messaging.batch.size";

        /// <summary>Error type when the operation fails.</summary>
        public const string ErrorType = "error.type";

        /// <summary>Error message when the operation fails.</summary>
        public const string ErrorMessage = "error.message";
    }

    /// <summary>
    /// Starts a new activity for a publish operation.
    /// </summary>
    /// <param name="messageType">The type of message being published.</param>
    /// <param name="exchange">The target exchange name.</param>
    /// <param name="routingKey">The routing key.</param>
    /// <returns>The started activity, or null if no listeners are registered.</returns>
    public static Activity? StartPublishActivity(string messageType, string exchange, string? routingKey = null)
    {
        var activity = Source.StartActivity($"RabbitMQ Publish {messageType}", ActivityKind.Producer);

        if (activity != null)
        {
            activity.SetTag(Tags.MessagingSystem, "rabbitmq");
            activity.SetTag(Tags.MessagingOperation, "send");
            activity.SetTag(Tags.MessagingDestination, exchange);
            activity.SetTag(Tags.MessagingDestinationKind, "topic");
            activity.SetTag(Tags.MessagingMessageType, messageType);

            if (!string.IsNullOrEmpty(routingKey))
            {
                activity.SetTag(Tags.MessagingRoutingKey, routingKey);
            }
        }

        return activity;
    }

    /// <summary>
    /// Starts a new activity for a consume operation.
    /// </summary>
    /// <param name="messageType">The type of message being consumed.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="parentContext">Optional parent activity context for distributed tracing.</param>
    /// <returns>The started activity, or null if no listeners are registered.</returns>
    public static Activity? StartConsumeActivity(string messageType, string queueName, ActivityContext parentContext = default)
    {
        var activity = Source.StartActivity(
            $"RabbitMQ Consume {messageType}",
            ActivityKind.Consumer,
            parentContext);

        if (activity != null)
        {
            activity.SetTag(Tags.MessagingSystem, "rabbitmq");
            activity.SetTag(Tags.MessagingOperation, "receive");
            activity.SetTag(Tags.MessagingConsumerQueue, queueName);
            activity.SetTag(Tags.MessagingDestinationKind, "queue");
            activity.SetTag(Tags.MessagingMessageType, messageType);
        }

        return activity;
    }

    /// <summary>
    /// Starts a new activity for a request/response operation.
    /// </summary>
    /// <param name="requestType">The type of request.</param>
    /// <param name="responseType">The expected response type.</param>
    /// <param name="destinationQueue">The destination queue.</param>
    /// <returns>The started activity, or null if no listeners are registered.</returns>
    public static Activity? StartRequestActivity(string requestType, string responseType, string destinationQueue)
    {
        var activity = Source.StartActivity($"RabbitMQ Request {requestType}", ActivityKind.Client);

        if (activity != null)
        {
            activity.SetTag(Tags.MessagingSystem, "rabbitmq");
            activity.SetTag(Tags.MessagingOperation, "send");
            activity.SetTag(Tags.MessagingDestination, destinationQueue);
            activity.SetTag(Tags.MessagingDestinationKind, "queue");
            activity.SetTag(Tags.MessagingMessageType, requestType);
            activity.SetTag("messaging.response_type", responseType);
        }

        return activity;
    }

    /// <summary>
    /// Starts a new activity for a batch processing operation.
    /// </summary>
    /// <param name="messageType">The type of messages in the batch.</param>
    /// <param name="batchSize">The number of messages in the batch.</param>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The started activity, or null if no listeners are registered.</returns>
    public static Activity? StartBatchActivity(string messageType, int batchSize, string queueName)
    {
        var activity = Source.StartActivity($"RabbitMQ Batch {messageType}", ActivityKind.Consumer);

        if (activity != null)
        {
            activity.SetTag(Tags.MessagingSystem, "rabbitmq");
            activity.SetTag(Tags.MessagingOperation, "process");
            activity.SetTag(Tags.MessagingConsumerQueue, queueName);
            activity.SetTag(Tags.MessagingMessageType, messageType);
            activity.SetTag(Tags.BatchSize, batchSize);
        }

        return activity;
    }

    /// <summary>
    /// Starts a new activity for a saga operation.
    /// </summary>
    /// <param name="sagaType">The saga type name.</param>
    /// <param name="correlationId">The saga correlation ID.</param>
    /// <returns>The started activity, or null if no listeners are registered.</returns>
    public static Activity? StartSagaActivity(string sagaType, string correlationId)
    {
        var activity = Source.StartActivity($"RabbitMQ Saga {sagaType}", ActivityKind.Internal);

        if (activity != null)
        {
            activity.SetTag(Tags.MessagingSystem, "rabbitmq");
            activity.SetTag(Tags.MessagingOperation, "process");
            activity.SetTag(Tags.MessagingCorrelationId, correlationId);
            activity.SetTag("saga.type", sagaType);
        }

        return activity;
    }

    /// <summary>
    /// Records an error on the activity.
    /// </summary>
    /// <param name="activity">The activity to record the error on.</param>
    /// <param name="exception">The exception that occurred.</param>
    public static void RecordException(Activity? activity, Exception exception)
    {
        if (activity == null) return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag(Tags.ErrorType, exception.GetType().FullName);
        activity.SetTag(Tags.ErrorMessage, exception.Message);

        activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace }
        }));
    }

    /// <summary>
    /// Sets success status on the activity.
    /// </summary>
    /// <param name="activity">The activity to mark as successful.</param>
    public static void SetSuccess(Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Adds correlation and tracing information to the activity.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="messageId">The message ID.</param>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="causationId">The causation ID.</param>
    public static void EnrichWithIds(Activity? activity, string? messageId, string? correlationId, string? causationId)
    {
        if (activity == null) return;

        if (!string.IsNullOrEmpty(messageId))
            activity.SetTag(Tags.MessagingMessageId, messageId);

        if (!string.IsNullOrEmpty(correlationId))
            activity.SetTag(Tags.MessagingCorrelationId, correlationId);

        if (!string.IsNullOrEmpty(causationId))
            activity.SetTag(Tags.MessagingCausationId, causationId);
    }

    /// <summary>
    /// Adds multi-tenancy information to the activity.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="userId">The user ID.</param>
    public static void EnrichWithTenancy(Activity? activity, string? tenantId, string? userId)
    {
        if (activity == null) return;

        if (!string.IsNullOrEmpty(tenantId))
            activity.SetTag(Tags.TenantId, tenantId);

        if (!string.IsNullOrEmpty(userId))
            activity.SetTag(Tags.UserId, userId);
    }

    /// <summary>
    /// Adds message delivery information to the activity.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="redelivered">Whether the message was redelivered.</param>
    /// <param name="redeliveryCount">The number of times the message was redelivered.</param>
    public static void EnrichWithDeliveryInfo(Activity? activity, bool redelivered, int redeliveryCount)
    {
        if (activity == null) return;

        activity.SetTag(Tags.MessagingRedelivered, redelivered);
        activity.SetTag(Tags.MessagingRedeliveryCount, redeliveryCount);
    }
}


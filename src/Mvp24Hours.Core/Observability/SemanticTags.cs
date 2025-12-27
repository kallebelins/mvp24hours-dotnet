//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// OpenTelemetry semantic conventions for tag names.
/// </summary>
/// <remarks>
/// <para>
/// This class defines tag names following OpenTelemetry Semantic Conventions.
/// Using standardized tag names ensures compatibility with observability tools
/// and enables rich querying and visualization.
/// </para>
/// <para>
/// <strong>References:</strong>
/// <list type="bullet">
/// <item><see href="https://opentelemetry.io/docs/specs/semconv/">OpenTelemetry Semantic Conventions</see></item>
/// <item><see href="https://opentelemetry.io/docs/specs/semconv/general/trace/">General Trace Semantic Conventions</see></item>
/// </list>
/// </para>
/// </remarks>
public static class SemanticTags
{
    #region General

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public const string CorrelationId = "correlation.id";

    /// <summary>
    /// Causation ID for event causality tracking.
    /// </summary>
    public const string CausationId = "causation.id";

    /// <summary>
    /// Unique identifier for the operation/request.
    /// </summary>
    public const string OperationId = "operation.id";

    /// <summary>
    /// Name of the operation.
    /// </summary>
    public const string OperationName = "operation.name";

    /// <summary>
    /// Type of the operation (e.g., Command, Query, Event).
    /// </summary>
    public const string OperationType = "operation.type";

    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public const string OperationSuccess = "operation.success";

    /// <summary>
    /// Duration of the operation in milliseconds.
    /// </summary>
    public const string OperationDurationMs = "operation.duration_ms";

    #endregion

    #region Enduser (User Context)

    /// <summary>
    /// User ID following OpenTelemetry conventions.
    /// </summary>
    public const string EnduserId = "enduser.id";

    /// <summary>
    /// User name.
    /// </summary>
    public const string EnduserName = "enduser.name";

    /// <summary>
    /// User roles (comma-separated).
    /// </summary>
    public const string EnduserRoles = "enduser.roles";

    /// <summary>
    /// User scope/permissions.
    /// </summary>
    public const string EnduserScope = "enduser.scope";

    #endregion

    #region Multi-tenancy

    /// <summary>
    /// Tenant ID for multi-tenancy.
    /// </summary>
    public const string TenantId = "tenant.id";

    /// <summary>
    /// Tenant name.
    /// </summary>
    public const string TenantName = "tenant.name";

    #endregion

    #region Error

    /// <summary>
    /// Error type (exception class name).
    /// </summary>
    public const string ErrorType = "error.type";

    /// <summary>
    /// Error message.
    /// </summary>
    public const string ErrorMessage = "error.message";

    /// <summary>
    /// Error code (application-specific).
    /// </summary>
    public const string ErrorCode = "error.code";

    /// <summary>
    /// Error category (e.g., Validation, Business, Infrastructure).
    /// </summary>
    public const string ErrorCategory = "error.category";

    #endregion

    #region Exception Events

    /// <summary>
    /// Exception type for exception events.
    /// </summary>
    public const string ExceptionType = "exception.type";

    /// <summary>
    /// Exception message for exception events.
    /// </summary>
    public const string ExceptionMessage = "exception.message";

    /// <summary>
    /// Exception stacktrace for exception events.
    /// </summary>
    public const string ExceptionStacktrace = "exception.stacktrace";

    /// <summary>
    /// Whether the exception was handled.
    /// </summary>
    public const string ExceptionEscaped = "exception.escaped";

    #endregion

    #region Database (db.*)

    /// <summary>
    /// Database system name (e.g., sqlserver, postgresql, mongodb).
    /// </summary>
    public const string DbSystem = "db.system";

    /// <summary>
    /// Database name.
    /// </summary>
    public const string DbName = "db.name";

    /// <summary>
    /// SQL statement or database operation.
    /// </summary>
    public const string DbStatement = "db.statement";

    /// <summary>
    /// Type of database operation (e.g., SELECT, INSERT, UPDATE, DELETE).
    /// </summary>
    public const string DbOperation = "db.operation";

    /// <summary>
    /// Name of the table being operated on.
    /// </summary>
    public const string DbSqlTable = "db.sql.table";

    /// <summary>
    /// Number of rows affected.
    /// </summary>
    public const string DbRowsAffected = "db.rows_affected";

    /// <summary>
    /// Database user.
    /// </summary>
    public const string DbUser = "db.user";

    /// <summary>
    /// Connection string (sanitized).
    /// </summary>
    public const string DbConnectionString = "db.connection_string";

    #endregion

    #region HTTP (http.*)

    /// <summary>
    /// HTTP method (GET, POST, etc.).
    /// </summary>
    public const string HttpMethod = "http.method";

    /// <summary>
    /// HTTP request method (same as http.method, newer convention).
    /// </summary>
    public const string HttpRequestMethod = "http.request.method";

    /// <summary>
    /// Full URL of the HTTP request.
    /// </summary>
    public const string HttpUrl = "http.url";

    /// <summary>
    /// HTTP response status code.
    /// </summary>
    public const string HttpStatusCode = "http.status_code";

    /// <summary>
    /// HTTP response status code (newer convention).
    /// </summary>
    public const string HttpResponseStatusCode = "http.response.status_code";

    /// <summary>
    /// URL path.
    /// </summary>
    public const string UrlPath = "url.path";

    /// <summary>
    /// URL query string.
    /// </summary>
    public const string UrlQuery = "url.query";

    /// <summary>
    /// Server address.
    /// </summary>
    public const string ServerAddress = "server.address";

    /// <summary>
    /// Server port.
    /// </summary>
    public const string ServerPort = "server.port";

    #endregion

    #region Messaging (messaging.*)

    /// <summary>
    /// Messaging system (e.g., rabbitmq, kafka).
    /// </summary>
    public const string MessagingSystem = "messaging.system";

    /// <summary>
    /// Message destination name (queue or exchange).
    /// </summary>
    public const string MessagingDestinationName = "messaging.destination.name";

    /// <summary>
    /// Type of destination (queue, topic).
    /// </summary>
    public const string MessagingDestinationKind = "messaging.destination.kind";

    /// <summary>
    /// Message ID.
    /// </summary>
    public const string MessagingMessageId = "messaging.message.id";

    /// <summary>
    /// Correlation ID for messaging.
    /// </summary>
    public const string MessagingCorrelationId = "messaging.message.correlation_id";

    /// <summary>
    /// Message payload size in bytes.
    /// </summary>
    public const string MessagingPayloadSize = "messaging.message.payload_size_bytes";

    /// <summary>
    /// Consumer group ID.
    /// </summary>
    public const string MessagingConsumerGroup = "messaging.consumer.group";

    /// <summary>
    /// Routing key for RabbitMQ.
    /// </summary>
    public const string MessagingRabbitmqRoutingKey = "messaging.rabbitmq.routing_key";

    #endregion

    #region CQRS/Mediator

    /// <summary>
    /// Request type name for mediator.
    /// </summary>
    public const string MediatorRequestName = "mediator.request.name";

    /// <summary>
    /// Request type (Command, Query, Notification).
    /// </summary>
    public const string MediatorRequestType = "mediator.request.type";

    /// <summary>
    /// Handler type name.
    /// </summary>
    public const string MediatorHandlerName = "mediator.handler.name";

    /// <summary>
    /// Behavior name in pipeline.
    /// </summary>
    public const string MediatorBehaviorName = "mediator.behavior.name";

    #endregion

    #region Pipeline

    /// <summary>
    /// Pipeline name.
    /// </summary>
    public const string PipelineName = "pipeline.name";

    /// <summary>
    /// Operation name within pipeline.
    /// </summary>
    public const string PipelineOperationName = "pipeline.operation.name";

    /// <summary>
    /// Operation index in pipeline.
    /// </summary>
    public const string PipelineOperationIndex = "pipeline.operation.index";

    /// <summary>
    /// Total operations in pipeline.
    /// </summary>
    public const string PipelineTotalOperations = "pipeline.total_operations";

    #endregion

    #region Cache

    /// <summary>
    /// Cache system (redis, memory).
    /// </summary>
    public const string CacheSystem = "cache.system";

    /// <summary>
    /// Cache key.
    /// </summary>
    public const string CacheKey = "cache.key";

    /// <summary>
    /// Whether cache hit occurred.
    /// </summary>
    public const string CacheHit = "cache.hit";

    /// <summary>
    /// Cache operation type (get, set, delete).
    /// </summary>
    public const string CacheOperation = "cache.operation";

    #endregion

    #region Background Jobs

    /// <summary>
    /// Job ID.
    /// </summary>
    public const string JobId = "job.id";

    /// <summary>
    /// Job type name.
    /// </summary>
    public const string JobType = "job.type";

    /// <summary>
    /// Job queue name.
    /// </summary>
    public const string JobQueue = "job.queue";

    /// <summary>
    /// Job attempt number.
    /// </summary>
    public const string JobAttempt = "job.attempt";

    /// <summary>
    /// Whether job is recurring.
    /// </summary>
    public const string JobRecurring = "job.recurring";

    #endregion
}

/// <summary>
/// Standard event names for activity events.
/// </summary>
public static class SemanticEvents
{
    /// <summary>
    /// Exception event name following OpenTelemetry conventions.
    /// </summary>
    public const string Exception = "exception";

    /// <summary>
    /// Retry attempt event.
    /// </summary>
    public const string RetryAttempt = "retry.attempt";

    /// <summary>
    /// Circuit breaker state change event.
    /// </summary>
    public const string CircuitBreakerStateChange = "circuit_breaker.state_change";

    /// <summary>
    /// Cache hit event.
    /// </summary>
    public const string CacheHit = "cache.hit";

    /// <summary>
    /// Cache miss event.
    /// </summary>
    public const string CacheMiss = "cache.miss";

    /// <summary>
    /// Validation failure event.
    /// </summary>
    public const string ValidationFailure = "validation.failure";

    /// <summary>
    /// Authorization failure event.
    /// </summary>
    public const string AuthorizationFailure = "authorization.failure";

    /// <summary>
    /// Message published event.
    /// </summary>
    public const string MessagePublished = "message.published";

    /// <summary>
    /// Message consumed event.
    /// </summary>
    public const string MessageConsumed = "message.consumed";

    /// <summary>
    /// Slow query detected event.
    /// </summary>
    public const string SlowQueryDetected = "slow_query.detected";
}


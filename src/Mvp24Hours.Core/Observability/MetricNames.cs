//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// Standardized metric names following OpenTelemetry semantic conventions.
/// </summary>
/// <remarks>
/// <para>
/// Metric names follow these conventions:
/// <list type="bullet">
/// <item>Prefix: <c>mvp24hours.{module}.</c></item>
/// <item>Use lowercase with dots as separators</item>
/// <item>Use descriptive names that indicate what is being measured</item>
/// <item>Counters: use <c>_total</c> suffix for cumulative counts</item>
/// <item>Histograms: use <c>_duration</c> or <c>_size</c> suffix</item>
/// <item>Gauges: use present tense (e.g., <c>_active</c>, <c>_current</c>)</item>
/// </list>
/// </para>
/// <para>
/// <strong>References:</strong>
/// <list type="bullet">
/// <item><see href="https://opentelemetry.io/docs/specs/semconv/general/metrics/">OpenTelemetry Metrics Conventions</see></item>
/// <item><see href="https://prometheus.io/docs/practices/naming/">Prometheus Naming Best Practices</see></item>
/// </list>
/// </para>
/// </remarks>
public static class MetricNames
{
    #region Pipeline Metrics

    /// <summary>
    /// Total number of pipeline executions.
    /// </summary>
    public const string PipelineExecutionsTotal = "mvp24hours.pipe.executions_total";

    /// <summary>
    /// Total number of failed pipeline executions.
    /// </summary>
    public const string PipelineExecutionsFailedTotal = "mvp24hours.pipe.executions_failed_total";

    /// <summary>
    /// Duration of pipeline executions in milliseconds.
    /// </summary>
    public const string PipelineExecutionDuration = "mvp24hours.pipe.execution_duration_ms";

    /// <summary>
    /// Total number of operation executions within pipelines.
    /// </summary>
    public const string PipelineOperationsTotal = "mvp24hours.pipe.operations_total";

    /// <summary>
    /// Total number of failed operations within pipelines.
    /// </summary>
    public const string PipelineOperationsFailedTotal = "mvp24hours.pipe.operations_failed_total";

    /// <summary>
    /// Duration of individual operation executions in milliseconds.
    /// </summary>
    public const string PipelineOperationDuration = "mvp24hours.pipe.operation_duration_ms";

    /// <summary>
    /// Number of currently active pipelines.
    /// </summary>
    public const string PipelineActiveCount = "mvp24hours.pipe.active_count";

    #endregion

    #region Repository/Data Metrics

    /// <summary>
    /// Total number of database queries executed.
    /// </summary>
    public const string DataQueriesTotal = "mvp24hours.data.queries_total";

    /// <summary>
    /// Total number of failed database queries.
    /// </summary>
    public const string DataQueriesFailedTotal = "mvp24hours.data.queries_failed_total";

    /// <summary>
    /// Duration of database query executions in milliseconds.
    /// </summary>
    public const string DataQueryDuration = "mvp24hours.data.query_duration_ms";

    /// <summary>
    /// Total number of database commands (insert, update, delete) executed.
    /// </summary>
    public const string DataCommandsTotal = "mvp24hours.data.commands_total";

    /// <summary>
    /// Total number of failed database commands.
    /// </summary>
    public const string DataCommandsFailedTotal = "mvp24hours.data.commands_failed_total";

    /// <summary>
    /// Duration of database command executions in milliseconds.
    /// </summary>
    public const string DataCommandDuration = "mvp24hours.data.command_duration_ms";

    /// <summary>
    /// Total number of SaveChanges operations.
    /// </summary>
    public const string DataSaveChangesTotal = "mvp24hours.data.save_changes_total";

    /// <summary>
    /// Duration of SaveChanges operations in milliseconds.
    /// </summary>
    public const string DataSaveChangesDuration = "mvp24hours.data.save_changes_duration_ms";

    /// <summary>
    /// Total number of rows affected by database operations.
    /// </summary>
    public const string DataRowsAffectedTotal = "mvp24hours.data.rows_affected_total";

    /// <summary>
    /// Number of active database connections.
    /// </summary>
    public const string DataConnectionsActive = "mvp24hours.data.connections_active";

    /// <summary>
    /// Number of idle database connections in pool.
    /// </summary>
    public const string DataConnectionsIdle = "mvp24hours.data.connections_idle";

    /// <summary>
    /// Total number of slow queries detected.
    /// </summary>
    public const string DataSlowQueriesTotal = "mvp24hours.data.slow_queries_total";

    /// <summary>
    /// Total number of bulk operations.
    /// </summary>
    public const string DataBulkOperationsTotal = "mvp24hours.data.bulk_operations_total";

    /// <summary>
    /// Total number of transactions.
    /// </summary>
    public const string DataTransactionsTotal = "mvp24hours.data.transactions_total";

    /// <summary>
    /// Total number of transaction rollbacks.
    /// </summary>
    public const string DataTransactionRollbacksTotal = "mvp24hours.data.transaction_rollbacks_total";

    #endregion

    #region CQRS/Mediator Metrics

    /// <summary>
    /// Total number of commands processed.
    /// </summary>
    public const string CqrsCommandsTotal = "mvp24hours.cqrs.commands_total";

    /// <summary>
    /// Total number of failed commands.
    /// </summary>
    public const string CqrsCommandsFailedTotal = "mvp24hours.cqrs.commands_failed_total";

    /// <summary>
    /// Duration of command processing in milliseconds.
    /// </summary>
    public const string CqrsCommandDuration = "mvp24hours.cqrs.command_duration_ms";

    /// <summary>
    /// Total number of queries processed.
    /// </summary>
    public const string CqrsQueriesTotal = "mvp24hours.cqrs.queries_total";

    /// <summary>
    /// Total number of failed queries.
    /// </summary>
    public const string CqrsQueriesFailedTotal = "mvp24hours.cqrs.queries_failed_total";

    /// <summary>
    /// Duration of query processing in milliseconds.
    /// </summary>
    public const string CqrsQueryDuration = "mvp24hours.cqrs.query_duration_ms";

    /// <summary>
    /// Total number of notifications published.
    /// </summary>
    public const string CqrsNotificationsTotal = "mvp24hours.cqrs.notifications_total";

    /// <summary>
    /// Total number of failed notification handlers.
    /// </summary>
    public const string CqrsNotificationsFailedTotal = "mvp24hours.cqrs.notifications_failed_total";

    /// <summary>
    /// Duration of notification handling in milliseconds.
    /// </summary>
    public const string CqrsNotificationDuration = "mvp24hours.cqrs.notification_duration_ms";

    /// <summary>
    /// Total number of domain events dispatched.
    /// </summary>
    public const string CqrsDomainEventsTotal = "mvp24hours.cqrs.domain_events_total";

    /// <summary>
    /// Total number of integration events published.
    /// </summary>
    public const string CqrsIntegrationEventsTotal = "mvp24hours.cqrs.integration_events_total";

    /// <summary>
    /// Total number of behavior executions.
    /// </summary>
    public const string CqrsBehaviorsTotal = "mvp24hours.cqrs.behaviors_total";

    /// <summary>
    /// Duration of behavior execution in milliseconds.
    /// </summary>
    public const string CqrsBehaviorDuration = "mvp24hours.cqrs.behavior_duration_ms";

    /// <summary>
    /// Total number of saga instances.
    /// </summary>
    public const string CqrsSagasTotal = "mvp24hours.cqrs.sagas_total";

    /// <summary>
    /// Total number of completed sagas.
    /// </summary>
    public const string CqrsSagasCompletedTotal = "mvp24hours.cqrs.sagas_completed_total";

    /// <summary>
    /// Total number of failed sagas.
    /// </summary>
    public const string CqrsSagasFailedTotal = "mvp24hours.cqrs.sagas_failed_total";

    /// <summary>
    /// Total number of validation failures.
    /// </summary>
    public const string CqrsValidationFailuresTotal = "mvp24hours.cqrs.validation_failures_total";

    /// <summary>
    /// Total number of cache hits in caching behavior.
    /// </summary>
    public const string CqrsCacheHitsTotal = "mvp24hours.cqrs.cache_hits_total";

    /// <summary>
    /// Total number of cache misses in caching behavior.
    /// </summary>
    public const string CqrsCacheMissesTotal = "mvp24hours.cqrs.cache_misses_total";

    /// <summary>
    /// Total number of idempotent request deduplication.
    /// </summary>
    public const string CqrsIdempotentDuplicatesTotal = "mvp24hours.cqrs.idempotent_duplicates_total";

    /// <summary>
    /// Total number of retry attempts.
    /// </summary>
    public const string CqrsRetriesTotal = "mvp24hours.cqrs.retries_total";

    /// <summary>
    /// Total number of circuit breaker trips.
    /// </summary>
    public const string CqrsCircuitBreakerTripsTotal = "mvp24hours.cqrs.circuit_breaker_trips_total";

    #endregion

    #region RabbitMQ/Messaging Metrics

    /// <summary>
    /// Total number of messages published.
    /// </summary>
    public const string MessagingPublishedTotal = "mvp24hours.messaging.published_total";

    /// <summary>
    /// Total number of failed message publications.
    /// </summary>
    public const string MessagingPublishFailedTotal = "mvp24hours.messaging.publish_failed_total";

    /// <summary>
    /// Duration of message publishing in milliseconds.
    /// </summary>
    public const string MessagingPublishDuration = "mvp24hours.messaging.publish_duration_ms";

    /// <summary>
    /// Total number of messages consumed.
    /// </summary>
    public const string MessagingConsumedTotal = "mvp24hours.messaging.consumed_total";

    /// <summary>
    /// Total number of failed message consumptions.
    /// </summary>
    public const string MessagingConsumeFailedTotal = "mvp24hours.messaging.consume_failed_total";

    /// <summary>
    /// Duration of message consumption in milliseconds.
    /// </summary>
    public const string MessagingConsumeDuration = "mvp24hours.messaging.consume_duration_ms";

    /// <summary>
    /// Total number of messages acknowledged.
    /// </summary>
    public const string MessagingAcknowledgedTotal = "mvp24hours.messaging.acknowledged_total";

    /// <summary>
    /// Total number of messages rejected.
    /// </summary>
    public const string MessagingRejectedTotal = "mvp24hours.messaging.rejected_total";

    /// <summary>
    /// Total number of messages requeued.
    /// </summary>
    public const string MessagingRequeuedTotal = "mvp24hours.messaging.requeued_total";

    /// <summary>
    /// Total number of messages sent to dead letter queue.
    /// </summary>
    public const string MessagingDeadLetteredTotal = "mvp24hours.messaging.dead_lettered_total";

    /// <summary>
    /// Number of messages in queue (gauge).
    /// </summary>
    public const string MessagingQueueDepth = "mvp24hours.messaging.queue_depth";

    /// <summary>
    /// Number of active consumers (gauge).
    /// </summary>
    public const string MessagingConsumersActive = "mvp24hours.messaging.consumers_active";

    /// <summary>
    /// Total number of batch operations.
    /// </summary>
    public const string MessagingBatchesTotal = "mvp24hours.messaging.batches_total";

    /// <summary>
    /// Size of message batches (histogram).
    /// </summary>
    public const string MessagingBatchSize = "mvp24hours.messaging.batch_size";

    /// <summary>
    /// Size of message payload in bytes (histogram).
    /// </summary>
    public const string MessagingPayloadSize = "mvp24hours.messaging.payload_size_bytes";

    /// <summary>
    /// Total number of connection attempts.
    /// </summary>
    public const string MessagingConnectionsTotal = "mvp24hours.messaging.connections_total";

    /// <summary>
    /// Number of active connections (gauge).
    /// </summary>
    public const string MessagingConnectionsActive = "mvp24hours.messaging.connections_active";

    /// <summary>
    /// Total number of connection failures.
    /// </summary>
    public const string MessagingConnectionFailuresTotal = "mvp24hours.messaging.connection_failures_total";

    #endregion

    #region Cache Metrics

    /// <summary>
    /// Total number of cache get operations.
    /// </summary>
    public const string CacheGetsTotal = "mvp24hours.cache.gets_total";

    /// <summary>
    /// Total number of cache hits.
    /// </summary>
    public const string CacheHitsTotal = "mvp24hours.cache.hits_total";

    /// <summary>
    /// Total number of cache misses.
    /// </summary>
    public const string CacheMissesTotal = "mvp24hours.cache.misses_total";

    /// <summary>
    /// Total number of cache set operations.
    /// </summary>
    public const string CacheSetsTotal = "mvp24hours.cache.sets_total";

    /// <summary>
    /// Total number of cache remove operations.
    /// </summary>
    public const string CacheRemovesTotal = "mvp24hours.cache.removes_total";

    /// <summary>
    /// Total number of cache invalidations.
    /// </summary>
    public const string CacheInvalidationsTotal = "mvp24hours.cache.invalidations_total";

    /// <summary>
    /// Duration of cache operations in milliseconds.
    /// </summary>
    public const string CacheOperationDuration = "mvp24hours.cache.operation_duration_ms";

    /// <summary>
    /// Size of cached items in bytes.
    /// </summary>
    public const string CacheItemSizeBytes = "mvp24hours.cache.item_size_bytes";

    /// <summary>
    /// Number of items currently in cache (gauge).
    /// </summary>
    public const string CacheItemsCount = "mvp24hours.cache.items_count";

    /// <summary>
    /// Total size of cache in bytes (gauge).
    /// </summary>
    public const string CacheTotalSizeBytes = "mvp24hours.cache.total_size_bytes";

    /// <summary>
    /// Cache hit ratio percentage (gauge).
    /// </summary>
    public const string CacheHitRatio = "mvp24hours.cache.hit_ratio";

    #endregion

    #region CronJob Metrics

    /// <summary>
    /// Total number of job executions.
    /// </summary>
    public const string CronJobExecutionsTotal = "mvp24hours.cronjob.executions_total";

    /// <summary>
    /// Total number of failed job executions.
    /// </summary>
    public const string CronJobExecutionsFailedTotal = "mvp24hours.cronjob.executions_failed_total";

    /// <summary>
    /// Duration of job executions in milliseconds.
    /// </summary>
    public const string CronJobExecutionDuration = "mvp24hours.cronjob.execution_duration_ms";

    /// <summary>
    /// Number of active/running jobs (gauge).
    /// </summary>
    public const string CronJobActiveCount = "mvp24hours.cronjob.active_count";

    /// <summary>
    /// Number of scheduled jobs (gauge).
    /// </summary>
    public const string CronJobScheduledCount = "mvp24hours.cronjob.scheduled_count";

    /// <summary>
    /// Time since last execution in seconds (gauge).
    /// </summary>
    public const string CronJobLastExecutionAge = "mvp24hours.cronjob.last_execution_age_seconds";

    #endregion

    #region WebAPI Metrics

    /// <summary>
    /// Total number of HTTP requests.
    /// </summary>
    public const string HttpRequestsTotal = "mvp24hours.http.requests_total";

    /// <summary>
    /// Total number of failed HTTP requests.
    /// </summary>
    public const string HttpRequestsFailedTotal = "mvp24hours.http.requests_failed_total";

    /// <summary>
    /// Duration of HTTP requests in milliseconds.
    /// </summary>
    public const string HttpRequestDuration = "mvp24hours.http.request_duration_ms";

    /// <summary>
    /// Size of HTTP request body in bytes.
    /// </summary>
    public const string HttpRequestSizeBytes = "mvp24hours.http.request_size_bytes";

    /// <summary>
    /// Size of HTTP response body in bytes.
    /// </summary>
    public const string HttpResponseSizeBytes = "mvp24hours.http.response_size_bytes";

    /// <summary>
    /// Number of active HTTP requests (gauge).
    /// </summary>
    public const string HttpActiveRequests = "mvp24hours.http.active_requests";

    /// <summary>
    /// Total number of rate limit hits.
    /// </summary>
    public const string HttpRateLimitHitsTotal = "mvp24hours.http.rate_limit_hits_total";

    /// <summary>
    /// Total number of idempotent request duplicates.
    /// </summary>
    public const string HttpIdempotentDuplicatesTotal = "mvp24hours.http.idempotent_duplicates_total";

    #endregion

    #region Infrastructure Metrics

    /// <summary>
    /// Total number of outbound HTTP client requests.
    /// </summary>
    public const string HttpClientRequestsTotal = "mvp24hours.http_client.requests_total";

    /// <summary>
    /// Total number of failed HTTP client requests.
    /// </summary>
    public const string HttpClientRequestsFailedTotal = "mvp24hours.http_client.requests_failed_total";

    /// <summary>
    /// Duration of HTTP client requests in milliseconds.
    /// </summary>
    public const string HttpClientRequestDuration = "mvp24hours.http_client.request_duration_ms";

    /// <summary>
    /// Total number of emails sent.
    /// </summary>
    public const string EmailsSentTotal = "mvp24hours.email.sent_total";

    /// <summary>
    /// Total number of failed email sends.
    /// </summary>
    public const string EmailsFailedTotal = "mvp24hours.email.failed_total";

    /// <summary>
    /// Total number of SMS sent.
    /// </summary>
    public const string SmsSentTotal = "mvp24hours.sms.sent_total";

    /// <summary>
    /// Total number of failed SMS sends.
    /// </summary>
    public const string SmsFailedTotal = "mvp24hours.sms.failed_total";

    /// <summary>
    /// Total number of file storage operations.
    /// </summary>
    public const string FileStorageOperationsTotal = "mvp24hours.file_storage.operations_total";

    /// <summary>
    /// Size of files in storage operations (histogram).
    /// </summary>
    public const string FileStorageFileSizeBytes = "mvp24hours.file_storage.file_size_bytes";

    /// <summary>
    /// Total number of distributed lock acquisitions.
    /// </summary>
    public const string DistributedLockAcquisitionsTotal = "mvp24hours.distributed_lock.acquisitions_total";

    /// <summary>
    /// Total number of distributed lock acquisition failures.
    /// </summary>
    public const string DistributedLockFailuresTotal = "mvp24hours.distributed_lock.failures_total";

    /// <summary>
    /// Duration of lock hold time in milliseconds.
    /// </summary>
    public const string DistributedLockHoldDuration = "mvp24hours.distributed_lock.hold_duration_ms";

    /// <summary>
    /// Time waiting for lock acquisition in milliseconds.
    /// </summary>
    public const string DistributedLockWaitDuration = "mvp24hours.distributed_lock.wait_duration_ms";

    /// <summary>
    /// Total number of background jobs executed.
    /// </summary>
    public const string BackgroundJobsTotal = "mvp24hours.background_job.total";

    /// <summary>
    /// Total number of failed background jobs.
    /// </summary>
    public const string BackgroundJobsFailedTotal = "mvp24hours.background_job.failed_total";

    /// <summary>
    /// Duration of background job execution in milliseconds.
    /// </summary>
    public const string BackgroundJobDuration = "mvp24hours.background_job.duration_ms";

    /// <summary>
    /// Number of pending background jobs (gauge).
    /// </summary>
    public const string BackgroundJobsPending = "mvp24hours.background_job.pending";

    #endregion
}

/// <summary>
/// Standard tag names for metrics dimensions.
/// </summary>
public static class MetricTags
{
    /// <summary>Operation name (e.g., GetById, Create, Update).</summary>
    public const string Operation = "operation";

    /// <summary>Entity or aggregate type name.</summary>
    public const string EntityType = "entity_type";

    /// <summary>Status of the operation (success, failure).</summary>
    public const string Status = "status";

    /// <summary>Error type for failed operations.</summary>
    public const string ErrorType = "error_type";

    /// <summary>Database system (sqlserver, postgresql, mongodb).</summary>
    public const string DbSystem = "db_system";

    /// <summary>Database name.</summary>
    public const string DbName = "db_name";

    /// <summary>Messaging system (rabbitmq, kafka).</summary>
    public const string MessagingSystem = "messaging_system";

    /// <summary>Queue or exchange name.</summary>
    public const string QueueName = "queue_name";

    /// <summary>Consumer group name.</summary>
    public const string ConsumerGroup = "consumer_group";

    /// <summary>Message type.</summary>
    public const string MessageType = "message_type";

    /// <summary>Pipeline name.</summary>
    public const string PipelineName = "pipeline_name";

    /// <summary>Operation name within pipeline.</summary>
    public const string OperationName = "operation_name";

    /// <summary>Command type name.</summary>
    public const string CommandType = "command_type";

    /// <summary>Query type name.</summary>
    public const string QueryType = "query_type";

    /// <summary>Handler type name.</summary>
    public const string HandlerType = "handler_type";

    /// <summary>Behavior name.</summary>
    public const string BehaviorName = "behavior_name";

    /// <summary>Cache name or provider.</summary>
    public const string CacheName = "cache_name";

    /// <summary>Cache operation type.</summary>
    public const string CacheOperation = "cache_operation";

    /// <summary>Job type name.</summary>
    public const string JobType = "job_type";

    /// <summary>Job queue name.</summary>
    public const string JobQueue = "job_queue";

    /// <summary>HTTP method (GET, POST, etc.).</summary>
    public const string HttpMethod = "http_method";

    /// <summary>HTTP status code.</summary>
    public const string HttpStatusCode = "http_status_code";

    /// <summary>HTTP route/path template.</summary>
    public const string HttpRoute = "http_route";

    /// <summary>Tenant identifier.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>Success status value.</summary>
    public const string StatusSuccess = "success";

    /// <summary>Failure status value.</summary>
    public const string StatusFailure = "failure";
}


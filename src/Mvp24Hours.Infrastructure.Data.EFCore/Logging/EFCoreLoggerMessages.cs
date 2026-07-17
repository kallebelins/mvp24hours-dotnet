//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Logging;

/// <summary>
/// High-performance source-generated logger messages for the EFCore module.
/// Uses <see cref="LoggerMessageAttribute"/> for zero-allocation logging.
/// </summary>
/// <remarks>
/// Event IDs: 5000-5999 (EFCore module range). Each message has a unique EventId.
/// </remarks>
public static partial class EFCoreLoggerMessages
{
    #region [ Event IDs - EFCore Module: 5000-5999 ]

    private const int EFCoreEventIdBase = 5000;

    // Query
    public const int QueryExecutedEventId = EFCoreEventIdBase + 1;
    public const int SlowQueryDetectedEventId = EFCoreEventIdBase + 2;
    public const int QueryWithSpecificationEventId = EFCoreEventIdBase + 3;
    public const int CompiledQueryExecutedEventId = EFCoreEventIdBase + 4;

    // Command
    public const int EntityOperationEventId = EFCoreEventIdBase + 5;
    public const int CommandExecutedEventId = EFCoreEventIdBase + 6;

    // Transaction
    public const int TransactionStartedEventId = EFCoreEventIdBase + 7;
    public const int TransactionCommittedEventId = EFCoreEventIdBase + 8;
    public const int TransactionRolledBackEventId = EFCoreEventIdBase + 9;

    // Connection
    public const int ConnectionOpenedEventId = EFCoreEventIdBase + 10;
    public const int ConnectionClosedEventId = EFCoreEventIdBase + 11;
    public const int ConnectionFailedEventId = EFCoreEventIdBase + 12;
    public const int ConnectionPoolStatusEventId = EFCoreEventIdBase + 13;

    // SaveChanges
    public const int SaveChangesStartedEventId = EFCoreEventIdBase + 14;
    public const int SaveChangesCompletedEventId = EFCoreEventIdBase + 15;
    public const int SaveChangesFailedEventId = EFCoreEventIdBase + 16;

    // Audit
    public const int AuditEntryEventId = EFCoreEventIdBase + 17;
    public const int AuditFieldsSetEventId = EFCoreEventIdBase + 18;

    // Soft Delete
    public const int SoftDeleteAppliedEventId = EFCoreEventIdBase + 19;
    public const int SoftDeleteRestoredEventId = EFCoreEventIdBase + 20;

    // Concurrency
    public const int ConcurrencyConflictEventId = EFCoreEventIdBase + 21;
    public const int ConcurrencyTokenUpdatedEventId = EFCoreEventIdBase + 22;

    // Migration
    public const int MigrationStartedEventId = EFCoreEventIdBase + 23;
    public const int MigrationCompletedEventId = EFCoreEventIdBase + 24;
    public const int MigrationAppliedEventId = EFCoreEventIdBase + 25;
    public const int PendingMigrationsEventId = EFCoreEventIdBase + 26;

    // Multi-tenancy
    public const int TenantContextAppliedEventId = EFCoreEventIdBase + 27;
    public const int TenantFilterAppliedEventId = EFCoreEventIdBase + 28;

    // Bulk Operations
    public const int BulkOperationStartedEventId = EFCoreEventIdBase + 29;
    public const int BulkOperationCompletedEventId = EFCoreEventIdBase + 30;
    public const int BulkOperationProgressEventId = EFCoreEventIdBase + 31;

    // Health Check
    public const int HealthCheckResultEventId = EFCoreEventIdBase + 32;
    public const int HealthCheckDegradedEventId = EFCoreEventIdBase + 33;

    // Resilience
    public const int RetryAttemptEventId = EFCoreEventIdBase + 34;
    public const int CircuitBreakerOpenedEventId = EFCoreEventIdBase + 35;
    public const int CircuitBreakerClosedEventId = EFCoreEventIdBase + 36;

    #endregion

    #region [ Query ]

    [LoggerMessage(
        EventId = QueryExecutedEventId,
        Level = LogLevel.Debug,
        Message = "Query executed: {QueryType} on {EntityType}. Duration: {ElapsedMs}ms")]
    public static partial void QueryExecuted(ILogger logger, string queryType, string entityType, long elapsedMs);

    [LoggerMessage(
        EventId = SlowQueryDetectedEventId,
        Level = LogLevel.Warning,
        Message = "Slow query detected: {QueryType} on {EntityType}. Duration: {ElapsedMs}ms (threshold: {ThresholdMs}ms)")]
    public static partial void SlowQueryDetected(ILogger logger, string queryType, string entityType, long elapsedMs, long thresholdMs);

    [LoggerMessage(
        EventId = QueryWithSpecificationEventId,
        Level = LogLevel.Debug,
        Message = "Query executed with specification: {SpecificationName}. Results: {ResultCount}")]
    public static partial void QueryWithSpecification(ILogger logger, string specificationName, int resultCount);

    [LoggerMessage(
        EventId = CompiledQueryExecutedEventId,
        Level = LogLevel.Trace,
        Message = "Compiled query executed: {QueryName}. Duration: {ElapsedMs}ms")]
    public static partial void CompiledQueryExecuted(ILogger logger, string queryName, long elapsedMs);

    #endregion

    #region [ Command ]

    [LoggerMessage(
        EventId = EntityOperationEventId,
        Level = LogLevel.Debug,
        Message = "Entity {Operation}: {EntityType} with Id {EntityId}")]
    public static partial void EntityOperation(ILogger logger, string operation, string entityType, string entityId);

    [LoggerMessage(
        EventId = CommandExecutedEventId,
        Level = LogLevel.Debug,
        Message = "Command executed: {CommandText}. Duration: {ElapsedMs}ms, Rows affected: {RowsAffected}")]
    public static partial void CommandExecuted(ILogger logger, string commandText, long elapsedMs, int rowsAffected);

    #endregion

    #region [ Transaction ]

    [LoggerMessage(
        EventId = TransactionStartedEventId,
        Level = LogLevel.Debug,
        Message = "Transaction started. TransactionId: {TransactionId}")]
    public static partial void TransactionStarted(ILogger logger, string transactionId);

    [LoggerMessage(
        EventId = TransactionCommittedEventId,
        Level = LogLevel.Debug,
        Message = "Transaction committed. TransactionId: {TransactionId}. Duration: {ElapsedMs}ms")]
    public static partial void TransactionCommitted(ILogger logger, string transactionId, long elapsedMs);

    [LoggerMessage(
        EventId = TransactionRolledBackEventId,
        Level = LogLevel.Warning,
        Message = "Transaction rolled back. TransactionId: {TransactionId}")]
    public static partial void TransactionRolledBack(ILogger logger, Exception? exception, string transactionId);

    #endregion

    #region [ Connection ]

    [LoggerMessage(
        EventId = ConnectionOpenedEventId,
        Level = LogLevel.Debug,
        Message = "Database connection opened. Database: {DatabaseName}")]
    public static partial void ConnectionOpened(ILogger logger, string databaseName);

    [LoggerMessage(
        EventId = ConnectionClosedEventId,
        Level = LogLevel.Debug,
        Message = "Database connection closed. Database: {DatabaseName}. Duration: {ElapsedMs}ms")]
    public static partial void ConnectionClosed(ILogger logger, string databaseName, long elapsedMs);

    [LoggerMessage(
        EventId = ConnectionFailedEventId,
        Level = LogLevel.Error,
        Message = "Database connection failed. Database: {DatabaseName}")]
    public static partial void ConnectionFailed(ILogger logger, Exception exception, string databaseName);

    [LoggerMessage(
        EventId = ConnectionPoolStatusEventId,
        Level = LogLevel.Debug,
        Message = "Connection pool status. Active: {ActiveConnections}, Available: {AvailableConnections}")]
    public static partial void ConnectionPoolStatus(ILogger logger, int activeConnections, int availableConnections);

    #endregion

    #region [ SaveChanges ]

    [LoggerMessage(
        EventId = SaveChangesStartedEventId,
        Level = LogLevel.Debug,
        Message = "SaveChanges started. Entries: {EntryCount} (Added: {Added}, Modified: {Modified}, Deleted: {Deleted})")]
    public static partial void SaveChangesStarted(ILogger logger, int entryCount, int added, int modified, int deleted);

    [LoggerMessage(
        EventId = SaveChangesCompletedEventId,
        Level = LogLevel.Debug,
        Message = "SaveChanges completed. Duration: {ElapsedMs}ms. Events dispatched: {EventCount}")]
    public static partial void SaveChangesCompleted(ILogger logger, long elapsedMs, int eventCount);

    [LoggerMessage(
        EventId = SaveChangesFailedEventId,
        Level = LogLevel.Error,
        Message = "SaveChanges failed")]
    public static partial void SaveChangesFailed(ILogger logger, Exception exception);

    #endregion

    #region [ Audit ]

    [LoggerMessage(
        EventId = AuditEntryEventId,
        Level = LogLevel.Information,
        Message = "Audit: {Action} on {EntityType} by {UserId}. EntityId: {EntityId}")]
    public static partial void AuditEntry(ILogger logger, string action, string entityType, string userId, string entityId);

    [LoggerMessage(
        EventId = AuditFieldsSetEventId,
        Level = LogLevel.Debug,
        Message = "Audit fields set for {EntityType}. CreatedBy/ModifiedBy: {UserId}")]
    public static partial void AuditFieldsSet(ILogger logger, string entityType, string userId);

    #endregion

    #region [ Soft Delete ]

    [LoggerMessage(
        EventId = SoftDeleteAppliedEventId,
        Level = LogLevel.Debug,
        Message = "Soft delete applied to {EntityType} with Id {EntityId} by {UserId}")]
    public static partial void SoftDeleteApplied(ILogger logger, string entityType, string entityId, string userId);

    [LoggerMessage(
        EventId = SoftDeleteRestoredEventId,
        Level = LogLevel.Debug,
        Message = "Soft deleted entity {EntityType} with Id {EntityId} restored")]
    public static partial void SoftDeleteRestored(ILogger logger, string entityType, string entityId);

    #endregion

    #region [ Concurrency ]

    [LoggerMessage(
        EventId = ConcurrencyConflictEventId,
        Level = LogLevel.Warning,
        Message = "Concurrency conflict detected for {EntityType} with Id {EntityId}")]
    public static partial void ConcurrencyConflict(ILogger logger, string entityType, string entityId);

    [LoggerMessage(
        EventId = ConcurrencyTokenUpdatedEventId,
        Level = LogLevel.Debug,
        Message = "Concurrency token updated for {EntityType} with Id {EntityId}. NewToken: {NewToken}")]
    public static partial void ConcurrencyTokenUpdated(ILogger logger, string entityType, string entityId, string newToken);

    #endregion

    #region [ Migration ]

    [LoggerMessage(
        EventId = MigrationStartedEventId,
        Level = LogLevel.Information,
        Message = "Database migration started. Database: {DatabaseName}")]
    public static partial void MigrationStarted(ILogger logger, string databaseName);

    [LoggerMessage(
        EventId = MigrationCompletedEventId,
        Level = LogLevel.Information,
        Message = "Database migration completed. Applied: {MigrationCount} migrations")]
    public static partial void MigrationCompleted(ILogger logger, int migrationCount);

    [LoggerMessage(
        EventId = MigrationAppliedEventId,
        Level = LogLevel.Debug,
        Message = "Migration '{MigrationName}' applied")]
    public static partial void MigrationApplied(ILogger logger, string migrationName);

    [LoggerMessage(
        EventId = PendingMigrationsEventId,
        Level = LogLevel.Warning,
        Message = "Pending migrations detected: {PendingCount}")]
    public static partial void PendingMigrations(ILogger logger, int pendingCount);

    #endregion

    #region [ Multi-tenancy ]

    [LoggerMessage(
        EventId = TenantContextAppliedEventId,
        Level = LogLevel.Debug,
        Message = "Tenant context applied. TenantId: {TenantId}")]
    public static partial void TenantContextApplied(ILogger logger, string tenantId);

    [LoggerMessage(
        EventId = TenantFilterAppliedEventId,
        Level = LogLevel.Debug,
        Message = "Tenant filter applied to query for {EntityType}. TenantId: {TenantId}")]
    public static partial void TenantFilterApplied(ILogger logger, string entityType, string tenantId);

    #endregion

    #region [ Bulk Operations ]

    [LoggerMessage(
        EventId = BulkOperationStartedEventId,
        Level = LogLevel.Information,
        Message = "Bulk {Operation} started for {EntityType}. Count: {Count}")]
    public static partial void BulkOperationStarted(ILogger logger, string operation, string entityType, int count);

    [LoggerMessage(
        EventId = BulkOperationCompletedEventId,
        Level = LogLevel.Information,
        Message = "Bulk {Operation} completed for {EntityType}. Count: {Count}, Duration: {ElapsedMs}ms")]
    public static partial void BulkOperationCompleted(ILogger logger, string operation, string entityType, int count, long elapsedMs);

    [LoggerMessage(
        EventId = BulkOperationProgressEventId,
        Level = LogLevel.Debug,
        Message = "Bulk operation progress: {Processed}/{Total} ({PercentComplete}%)")]
    public static partial void BulkOperationProgress(ILogger logger, int processed, int total, int percentComplete);

    #endregion

    #region [ Health Check ]

    [LoggerMessage(
        EventId = HealthCheckResultEventId,
        Level = LogLevel.Debug,
        Message = "Database health check: {Status}. Duration: {ElapsedMs}ms")]
    public static partial void HealthCheckResult(ILogger logger, string status, long elapsedMs);

    [LoggerMessage(
        EventId = HealthCheckDegradedEventId,
        Level = LogLevel.Warning,
        Message = "Database health check degraded: {Reason}")]
    public static partial void HealthCheckDegraded(ILogger logger, string reason);

    #endregion

    #region [ Resilience ]

    [LoggerMessage(
        EventId = RetryAttemptEventId,
        Level = LogLevel.Warning,
        Message = "Database retry attempt {Attempt}/{MaxRetries} for {OperationType}. Delay: {DelayMs}ms")]
    public static partial void RetryAttempt(ILogger logger, int attempt, int maxRetries, string operationType, int delayMs);

    [LoggerMessage(
        EventId = CircuitBreakerOpenedEventId,
        Level = LogLevel.Warning,
        Message = "Database circuit breaker opened. Failures: {FailureCount}")]
    public static partial void CircuitBreakerOpened(ILogger logger, int failureCount);

    [LoggerMessage(
        EventId = CircuitBreakerClosedEventId,
        Level = LogLevel.Information,
        Message = "Database circuit breaker closed")]
    public static partial void CircuitBreakerClosed(ILogger logger);

    #endregion
}

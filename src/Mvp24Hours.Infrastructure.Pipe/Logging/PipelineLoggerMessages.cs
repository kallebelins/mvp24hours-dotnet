//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;

namespace Mvp24Hours.Infrastructure.Pipe.Logging;

/// <summary>
/// High-performance source-generated logger messages for the Pipeline module.
/// Uses <see cref="LoggerMessageAttribute"/> for zero-allocation logging.
/// </summary>
/// <remarks>
/// Event IDs: 2000-2999 (Pipeline module range)
/// </remarks>
public static partial class PipelineLoggerMessages
{
    #region [ Event IDs - Pipeline Module: 2000-2999 ]

    private const int PipelineEventIdBase = 2000;

    public const int PipelineStartedEventId = PipelineEventIdBase + 1;
    public const int PipelineCompletedEventId = PipelineEventIdBase + 2;
    public const int PipelineFailedEventId = PipelineEventIdBase + 3;
    public const int OperationStartedEventId = PipelineEventIdBase + 4;
    public const int OperationCompletedEventId = PipelineEventIdBase + 5;
    public const int OperationFailedEventId = PipelineEventIdBase + 6;
    public const int OperationSkippedEventId = PipelineEventIdBase + 7;
    public const int RetryTriggeredEventId = PipelineEventIdBase + 8;
    public const int CircuitBreakerEventId = PipelineEventIdBase + 9;
    public const int ValidationEventId = PipelineEventIdBase + 10;
    public const int ContextEventId = PipelineEventIdBase + 11;
    public const int ForkJoinEventId = PipelineEventIdBase + 12;
    public const int CheckpointEventId = PipelineEventIdBase + 13;
    public const int SagaEventId = PipelineEventIdBase + 14;
    public const int CacheEventId = PipelineEventIdBase + 15;

    #endregion

    #region [ Pipeline Lifecycle ]

    [LoggerMessage(
        EventId = PipelineStartedEventId,
        Level = LogLevel.Information,
        Message = "Pipeline '{PipelineName}' started. Operations: {OperationCount}")]
    public static partial void PipelineStarted(ILogger logger, string pipelineName, int operationCount);

    [LoggerMessage(
        EventId = PipelineCompletedEventId,
        Level = LogLevel.Information,
        Message = "Pipeline '{PipelineName}' completed in {ElapsedMs}ms. Success: {IsSuccess}")]
    public static partial void PipelineCompleted(ILogger logger, string pipelineName, long elapsedMs, bool isSuccess);

    [LoggerMessage(
        EventId = PipelineFailedEventId,
        Level = LogLevel.Error,
        Message = "Pipeline '{PipelineName}' failed at operation '{FailedOperation}' after {ElapsedMs}ms")]
    public static partial void PipelineFailed(ILogger logger, Exception exception, string pipelineName, string failedOperation, long elapsedMs);

    [LoggerMessage(
        EventId = PipelineFailedEventId,
        Level = LogLevel.Warning,
        Message = "Pipeline '{PipelineName}' breaking on failure at '{FailedOperation}'")]
    public static partial void PipelineBreakOnFailure(ILogger logger, string pipelineName, string failedOperation);

    #endregion

    #region [ Operation Lifecycle ]

    [LoggerMessage(
        EventId = OperationStartedEventId,
        Level = LogLevel.Debug,
        Message = "Operation '{OperationName}' started (Step {Step}/{TotalSteps})")]
    public static partial void OperationStarted(ILogger logger, string operationName, int step, int totalSteps);

    [LoggerMessage(
        EventId = OperationCompletedEventId,
        Level = LogLevel.Debug,
        Message = "Operation '{OperationName}' completed in {ElapsedMs}ms")]
    public static partial void OperationCompleted(ILogger logger, string operationName, long elapsedMs);

    [LoggerMessage(
        EventId = OperationFailedEventId,
        Level = LogLevel.Error,
        Message = "Operation '{OperationName}' failed after {ElapsedMs}ms")]
    public static partial void OperationFailed(ILogger logger, Exception exception, string operationName, long elapsedMs);

    [LoggerMessage(
        EventId = OperationSkippedEventId,
        Level = LogLevel.Debug,
        Message = "Operation '{OperationName}' skipped. Reason: {Reason}")]
    public static partial void OperationSkipped(ILogger logger, string operationName, string reason);

    [LoggerMessage(
        EventId = OperationCompletedEventId,
        Level = LogLevel.Warning,
        Message = "Slow operation '{OperationName}' detected: {ElapsedMs}ms (threshold: {ThresholdMs}ms)")]
    public static partial void SlowOperation(ILogger logger, string operationName, long elapsedMs, long thresholdMs);

    #endregion

    #region [ Resiliency ]

    [LoggerMessage(
        EventId = RetryTriggeredEventId,
        Level = LogLevel.Warning,
        Message = "Retry {Attempt}/{MaxRetries} for operation '{OperationName}'. Delay: {DelayMs}ms")]
    public static partial void RetryTriggered(ILogger logger, int attempt, int maxRetries, string operationName, int delayMs);

    [LoggerMessage(
        EventId = RetryTriggeredEventId,
        Level = LogLevel.Error,
        Message = "Retry exhausted for operation '{OperationName}' after {Attempts} attempts")]
    public static partial void RetryExhausted(ILogger logger, Exception exception, string operationName, int attempts);

    [LoggerMessage(
        EventId = CircuitBreakerEventId,
        Level = LogLevel.Warning,
        Message = "Circuit breaker opened for operation '{OperationName}'. Failures: {FailureCount}")]
    public static partial void CircuitBreakerOpened(ILogger logger, string operationName, int failureCount);

    [LoggerMessage(
        EventId = CircuitBreakerEventId,
        Level = LogLevel.Information,
        Message = "Circuit breaker closed for operation '{OperationName}'")]
    public static partial void CircuitBreakerClosed(ILogger logger, string operationName);

    [LoggerMessage(
        EventId = CircuitBreakerEventId,
        Level = LogLevel.Debug,
        Message = "Circuit breaker half-open for operation '{OperationName}'. Testing...")]
    public static partial void CircuitBreakerHalfOpen(ILogger logger, string operationName);

    #endregion

    #region [ Validation ]

    [LoggerMessage(
        EventId = ValidationEventId,
        Level = LogLevel.Warning,
        Message = "Validation failed in pipeline '{PipelineName}': {ErrorCount} errors")]
    public static partial void ValidationFailed(ILogger logger, string pipelineName, int errorCount);

    [LoggerMessage(
        EventId = ValidationEventId,
        Level = LogLevel.Debug,
        Message = "Validation passed for '{TypeName}' in pipeline '{PipelineName}'")]
    public static partial void ValidationPassed(ILogger logger, string typeName, string pipelineName);

    #endregion

    #region [ Context ]

    [LoggerMessage(
        EventId = ContextEventId,
        Level = LogLevel.Debug,
        Message = "Pipeline context created. CorrelationId: {CorrelationId}")]
    public static partial void ContextCreated(ILogger logger, string correlationId);

    [LoggerMessage(
        EventId = ContextEventId,
        Level = LogLevel.Trace,
        Message = "Context data added: {Key} = {Value}")]
    public static partial void ContextDataAdded(ILogger logger, string key, string value);

    [LoggerMessage(
        EventId = ContextEventId,
        Level = LogLevel.Debug,
        Message = "State snapshot saved at '{SnapshotName}' (Step {Step})")]
    public static partial void StateSnapshotSaved(ILogger logger, string snapshotName, int step);

    #endregion

    #region [ Fork/Join ]

    [LoggerMessage(
        EventId = ForkJoinEventId,
        Level = LogLevel.Information,
        Message = "Forking into {BranchCount} parallel branches")]
    public static partial void ForkedToBranches(ILogger logger, int branchCount);

    [LoggerMessage(
        EventId = ForkJoinEventId,
        Level = LogLevel.Information,
        Message = "Joined {BranchCount} branches in {ElapsedMs}ms")]
    public static partial void JoinedBranches(ILogger logger, int branchCount, long elapsedMs);

    [LoggerMessage(
        EventId = ForkJoinEventId,
        Level = LogLevel.Debug,
        Message = "Branch '{BranchName}' completed in {ElapsedMs}ms")]
    public static partial void BranchCompleted(ILogger logger, string branchName, long elapsedMs);

    #endregion

    #region [ Checkpoint/Resume ]

    [LoggerMessage(
        EventId = CheckpointEventId,
        Level = LogLevel.Information,
        Message = "Checkpoint '{CheckpointId}' saved at step {Step}")]
    public static partial void CheckpointSaved(ILogger logger, string checkpointId, int step);

    [LoggerMessage(
        EventId = CheckpointEventId,
        Level = LogLevel.Information,
        Message = "Resuming from checkpoint '{CheckpointId}' at step {Step}")]
    public static partial void ResumingFromCheckpoint(ILogger logger, string checkpointId, int step);

    [LoggerMessage(
        EventId = CheckpointEventId,
        Level = LogLevel.Debug,
        Message = "Checkpoint '{CheckpointId}' cleanup completed")]
    public static partial void CheckpointCleanedUp(ILogger logger, string checkpointId);

    #endregion

    #region [ Saga ]

    [LoggerMessage(
        EventId = SagaEventId,
        Level = LogLevel.Information,
        Message = "Saga '{SagaName}' starting with {StepCount} steps")]
    public static partial void SagaStarted(ILogger logger, string sagaName, int stepCount);

    [LoggerMessage(
        EventId = SagaEventId,
        Level = LogLevel.Information,
        Message = "Saga '{SagaName}' completed successfully in {ElapsedMs}ms")]
    public static partial void SagaCompleted(ILogger logger, string sagaName, long elapsedMs);

    [LoggerMessage(
        EventId = SagaEventId,
        Level = LogLevel.Warning,
        Message = "Saga '{SagaName}' compensating from step {FailedStep}")]
    public static partial void SagaCompensating(ILogger logger, string sagaName, int failedStep);

    [LoggerMessage(
        EventId = SagaEventId,
        Level = LogLevel.Error,
        Message = "Saga '{SagaName}' failed. Compensation {Status}")]
    public static partial void SagaFailed(ILogger logger, Exception exception, string sagaName, string status);

    [LoggerMessage(
        EventId = SagaEventId,
        Level = LogLevel.Debug,
        Message = "Saga step '{StepName}' compensated in {ElapsedMs}ms")]
    public static partial void SagaStepCompensated(ILogger logger, string stepName, long elapsedMs);

    #endregion

    #region [ Caching ]

    [LoggerMessage(
        EventId = CacheEventId,
        Level = LogLevel.Debug,
        Message = "Cache hit for operation '{OperationName}'. Key: {CacheKey}")]
    public static partial void CacheHit(ILogger logger, string operationName, string cacheKey);

    [LoggerMessage(
        EventId = CacheEventId,
        Level = LogLevel.Debug,
        Message = "Cache miss for operation '{OperationName}'. Key: {CacheKey}")]
    public static partial void CacheMiss(ILogger logger, string operationName, string cacheKey);

    [LoggerMessage(
        EventId = CacheEventId,
        Level = LogLevel.Debug,
        Message = "Cache set for operation '{OperationName}'. Key: {CacheKey}, TTL: {TtlSeconds}s")]
    public static partial void CacheSet(ILogger logger, string operationName, string cacheKey, int ttlSeconds);

    #endregion
}


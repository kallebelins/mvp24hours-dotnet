//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.WebAPI.Logging;

/// <summary>
/// High-performance source-generated logger messages for the WebAPI module.
/// Uses <see cref="LoggerMessageAttribute"/> for zero-allocation logging.
/// </summary>
/// <remarks>
/// Event IDs: 6000-6999 (WebAPI module range). Each message has a unique EventId.
/// </remarks>
public static partial class WebAPILoggerMessages
{
    #region [ Event IDs - WebAPI Module: 6000-6999 ]

    private const int WebAPIEventIdBase = 6000;

    // Request
    public const int RequestStartedEventId = WebAPIEventIdBase + 1;
    public const int RequestHeadersEventId = WebAPIEventIdBase + 2;
    public const int RequestBodyEventId = WebAPIEventIdBase + 3;
    public const int RequestBodyTooLargeEventId = WebAPIEventIdBase + 4;

    // Response
    public const int RequestCompletedEventId = WebAPIEventIdBase + 5;
    public const int SlowRequestEventId = WebAPIEventIdBase + 6;
    public const int ResponseBodyEventId = WebAPIEventIdBase + 7;

    // Exception
    public const int UnhandledExceptionEventId = WebAPIEventIdBase + 8;
    public const int BusinessExceptionEventId = WebAPIEventIdBase + 9;
    public const int ExceptionMappedToProblemDetailsEventId = WebAPIEventIdBase + 10;

    // Rate Limiting
    public const int RateLimitExceededEventId = WebAPIEventIdBase + 11;
    public const int RateLimitCheckEventId = WebAPIEventIdBase + 12;

    // Authentication
    public const int AuthenticationSuccessfulEventId = WebAPIEventIdBase + 13;
    public const int AuthenticationFailedEventId = WebAPIEventIdBase + 14;
    public const int ApiKeyAuthenticationEventId = WebAPIEventIdBase + 15;

    // Authorization
    public const int AuthorizationDeniedEventId = WebAPIEventIdBase + 16;
    public const int AuthorizationGrantedEventId = WebAPIEventIdBase + 17;

    // Validation
    public const int ModelValidationFailedEventId = WebAPIEventIdBase + 18;
    public const int ValidationErrorEventId = WebAPIEventIdBase + 19;

    // Idempotency
    public const int DuplicateRequestDetectedEventId = WebAPIEventIdBase + 20;
    public const int IdempotencyKeyStoredEventId = WebAPIEventIdBase + 21;
    public const int ReturningCachedResponseEventId = WebAPIEventIdBase + 22;

    // Caching
    public const int ResponseCacheHitEventId = WebAPIEventIdBase + 23;
    public const int ResponseCacheMissEventId = WebAPIEventIdBase + 24;
    public const int ResponseCachedEventId = WebAPIEventIdBase + 25;
    public const int OutputCacheInvalidatedEventId = WebAPIEventIdBase + 26;

    // Health Check
    public const int HealthCheckExecutedEventId = WebAPIEventIdBase + 27;
    public const int HealthCheckUnhealthyEventId = WebAPIEventIdBase + 28;

    // Security
    public const int IpBlockedEventId = WebAPIEventIdBase + 29;
    public const int SecurityHeadersAppliedEventId = WebAPIEventIdBase + 30;
    public const int XssAttemptDetectedEventId = WebAPIEventIdBase + 31;
    public const int SqlInjectionAttemptDetectedEventId = WebAPIEventIdBase + 32;

    // Middleware
    public const int MiddlewareExecutedEventId = WebAPIEventIdBase + 33;
    public const int CorrelationIdAssignedEventId = WebAPIEventIdBase + 34;
    public const int RequestContextEnrichedEventId = WebAPIEventIdBase + 35;

    #endregion

    #region [ Request ]

    [LoggerMessage(
        EventId = RequestStartedEventId,
        Level = LogLevel.Information,
        Message = "Request started: {Method} {Path}. CorrelationId: {CorrelationId}")]
    public static partial void RequestStarted(ILogger logger, string method, string path, string correlationId);

    [LoggerMessage(
        EventId = RequestHeadersEventId,
        Level = LogLevel.Debug,
        Message = "Request headers: {Headers}")]
    public static partial void RequestHeaders(ILogger logger, string headers);

    [LoggerMessage(
        EventId = RequestBodyEventId,
        Level = LogLevel.Debug,
        Message = "Request body: {Body}")]
    public static partial void RequestBody(ILogger logger, string body);

    [LoggerMessage(
        EventId = RequestBodyTooLargeEventId,
        Level = LogLevel.Warning,
        Message = "Request body too large: {SizeBytes} bytes (limit: {LimitBytes} bytes)")]
    public static partial void RequestBodyTooLarge(ILogger logger, long sizeBytes, long limitBytes);

    #endregion

    #region [ Response ]

    [LoggerMessage(
        EventId = RequestCompletedEventId,
        Level = LogLevel.Information,
        Message = "Request completed: {Method} {Path}. Status: {StatusCode}. Duration: {ElapsedMs}ms")]
    public static partial void RequestCompleted(ILogger logger, string method, string path, int statusCode, long elapsedMs);

    [LoggerMessage(
        EventId = SlowRequestEventId,
        Level = LogLevel.Warning,
        Message = "Slow request detected: {Method} {Path}. Duration: {ElapsedMs}ms (threshold: {ThresholdMs}ms)")]
    public static partial void SlowRequest(ILogger logger, string method, string path, long elapsedMs, long thresholdMs);

    [LoggerMessage(
        EventId = ResponseBodyEventId,
        Level = LogLevel.Debug,
        Message = "Response body: {Body}")]
    public static partial void ResponseBody(ILogger logger, string body);

    #endregion

    #region [ Exception ]

    [LoggerMessage(
        EventId = UnhandledExceptionEventId,
        Level = LogLevel.Error,
        Message = "Unhandled exception: {ExceptionType} - {ExceptionMessage}")]
    public static partial void UnhandledException(ILogger logger, Exception exception, string exceptionType, string exceptionMessage);

    [LoggerMessage(
        EventId = BusinessExceptionEventId,
        Level = LogLevel.Warning,
        Message = "Business exception: {ExceptionType}. Message: {Message}")]
    public static partial void BusinessException(ILogger logger, string exceptionType, string message);

    [LoggerMessage(
        EventId = ExceptionMappedToProblemDetailsEventId,
        Level = LogLevel.Debug,
        Message = "Exception mapped to ProblemDetails. Status: {StatusCode}, Type: {Type}")]
    public static partial void ExceptionMappedToProblemDetails(ILogger logger, int statusCode, string type);

    #endregion

    #region [ Rate Limiting ]

    [LoggerMessage(
        EventId = RateLimitExceededEventId,
        Level = LogLevel.Warning,
        Message = "Rate limit exceeded for {ClientId}. Limit: {Limit}, Policy: {PolicyName}")]
    public static partial void RateLimitExceeded(ILogger logger, string clientId, int limit, string policyName);

    [LoggerMessage(
        EventId = RateLimitCheckEventId,
        Level = LogLevel.Debug,
        Message = "Rate limit check: {ClientId}. Remaining: {Remaining}/{Limit}")]
    public static partial void RateLimitCheck(ILogger logger, string clientId, int remaining, int limit);

    #endregion

    #region [ Authentication ]

    [LoggerMessage(
        EventId = AuthenticationSuccessfulEventId,
        Level = LogLevel.Information,
        Message = "Authentication successful for user {UserId}")]
    public static partial void AuthenticationSuccessful(ILogger logger, string userId);

    [LoggerMessage(
        EventId = AuthenticationFailedEventId,
        Level = LogLevel.Warning,
        Message = "Authentication failed. Reason: {Reason}")]
    public static partial void AuthenticationFailed(ILogger logger, string reason);

    [LoggerMessage(
        EventId = ApiKeyAuthenticationEventId,
        Level = LogLevel.Debug,
        Message = "API key authentication: {ApiKeyName}")]
    public static partial void ApiKeyAuthentication(ILogger logger, string apiKeyName);

    #endregion

    #region [ Authorization ]

    [LoggerMessage(
        EventId = AuthorizationDeniedEventId,
        Level = LogLevel.Warning,
        Message = "Authorization denied for user {UserId}. Resource: {Resource}, Action: {Action}")]
    public static partial void AuthorizationDenied(ILogger logger, string userId, string resource, string action);

    [LoggerMessage(
        EventId = AuthorizationGrantedEventId,
        Level = LogLevel.Debug,
        Message = "Authorization granted for user {UserId}. Resource: {Resource}")]
    public static partial void AuthorizationGranted(ILogger logger, string userId, string resource);

    #endregion

    #region [ Validation ]

    [LoggerMessage(
        EventId = ModelValidationFailedEventId,
        Level = LogLevel.Warning,
        Message = "Model validation failed. Errors: {ErrorCount}")]
    public static partial void ModelValidationFailed(ILogger logger, int errorCount);

    [LoggerMessage(
        EventId = ValidationErrorEventId,
        Level = LogLevel.Debug,
        Message = "Validation error on {PropertyName}: {ErrorMessage}")]
    public static partial void ValidationError(ILogger logger, string propertyName, string errorMessage);

    #endregion

    #region [ Idempotency ]

    [LoggerMessage(
        EventId = DuplicateRequestDetectedEventId,
        Level = LogLevel.Information,
        Message = "Duplicate request detected. IdempotencyKey: {IdempotencyKey}")]
    public static partial void DuplicateRequestDetected(ILogger logger, string idempotencyKey);

    [LoggerMessage(
        EventId = IdempotencyKeyStoredEventId,
        Level = LogLevel.Debug,
        Message = "Idempotency key stored: {IdempotencyKey}. TTL: {TtlSeconds}s")]
    public static partial void IdempotencyKeyStored(ILogger logger, string idempotencyKey, int ttlSeconds);

    [LoggerMessage(
        EventId = ReturningCachedResponseEventId,
        Level = LogLevel.Debug,
        Message = "Returning cached response for IdempotencyKey: {IdempotencyKey}")]
    public static partial void ReturningCachedResponse(ILogger logger, string idempotencyKey);

    #endregion

    #region [ Caching ]

    [LoggerMessage(
        EventId = ResponseCacheHitEventId,
        Level = LogLevel.Debug,
        Message = "Response cache hit for {Path}. CacheKey: {CacheKey}")]
    public static partial void ResponseCacheHit(ILogger logger, string path, string cacheKey);

    [LoggerMessage(
        EventId = ResponseCacheMissEventId,
        Level = LogLevel.Debug,
        Message = "Response cache miss for {Path}. CacheKey: {CacheKey}")]
    public static partial void ResponseCacheMiss(ILogger logger, string path, string cacheKey);

    [LoggerMessage(
        EventId = ResponseCachedEventId,
        Level = LogLevel.Debug,
        Message = "Response cached for {Path}. Duration: {DurationSeconds}s")]
    public static partial void ResponseCached(ILogger logger, string path, int durationSeconds);

    [LoggerMessage(
        EventId = OutputCacheInvalidatedEventId,
        Level = LogLevel.Debug,
        Message = "Output cache invalidated for tags: {Tags}")]
    public static partial void OutputCacheInvalidated(ILogger logger, string tags);

    #endregion

    #region [ Health Check ]

    [LoggerMessage(
        EventId = HealthCheckExecutedEventId,
        Level = LogLevel.Debug,
        Message = "Health check executed. Status: {Status}. Duration: {ElapsedMs}ms")]
    public static partial void HealthCheckExecuted(ILogger logger, string status, long elapsedMs);

    [LoggerMessage(
        EventId = HealthCheckUnhealthyEventId,
        Level = LogLevel.Warning,
        Message = "Health check {CheckName} unhealthy: {Description}")]
    public static partial void HealthCheckUnhealthy(ILogger logger, string checkName, string description);

    #endregion

    #region [ Security ]

    [LoggerMessage(
        EventId = IpBlockedEventId,
        Level = LogLevel.Warning,
        Message = "IP blocked: {IpAddress}. Reason: {Reason}")]
    public static partial void IpBlocked(ILogger logger, string ipAddress, string reason);

    [LoggerMessage(
        EventId = SecurityHeadersAppliedEventId,
        Level = LogLevel.Debug,
        Message = "Security headers applied: {Headers}")]
    public static partial void SecurityHeadersApplied(ILogger logger, string headers);

    [LoggerMessage(
        EventId = XssAttemptDetectedEventId,
        Level = LogLevel.Warning,
        Message = "Potential XSS attack detected in input: {Field}")]
    public static partial void XssAttemptDetected(ILogger logger, string field);

    [LoggerMessage(
        EventId = SqlInjectionAttemptDetectedEventId,
        Level = LogLevel.Warning,
        Message = "Potential SQL injection detected in input: {Field}")]
    public static partial void SqlInjectionAttemptDetected(ILogger logger, string field);

    #endregion

    #region [ Middleware ]

    [LoggerMessage(
        EventId = MiddlewareExecutedEventId,
        Level = LogLevel.Trace,
        Message = "Middleware '{MiddlewareName}' executed. Duration: {ElapsedMs}ms")]
    public static partial void MiddlewareExecuted(ILogger logger, string middlewareName, long elapsedMs);

    [LoggerMessage(
        EventId = CorrelationIdAssignedEventId,
        Level = LogLevel.Debug,
        Message = "Correlation ID assigned: {CorrelationId}")]
    public static partial void CorrelationIdAssigned(ILogger logger, string correlationId);

    [LoggerMessage(
        EventId = RequestContextEnrichedEventId,
        Level = LogLevel.Debug,
        Message = "Request context enriched. TenantId: {TenantId}, UserId: {UserId}")]
    public static partial void RequestContextEnriched(ILogger logger, string tenantId, string userId);

    #endregion
}

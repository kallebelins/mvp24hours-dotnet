//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Application.Contract.Resilience
{
    /// <summary>
    /// Defines standardized status codes for business result outcomes.
    /// These codes enable consistent error handling and internationalization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Status codes are grouped by category for easy identification:
    /// <list type="bullet">
    /// <item><c>0</c>: Success</item>
    /// <item><c>1xx</c>: Validation errors</item>
    /// <item><c>2xx</c>: Authorization errors</item>
    /// <item><c>3xx</c>: Resource errors (not found, conflict)</item>
    /// <item><c>4xx</c>: Business rule errors</item>
    /// <item><c>5xx</c>: Infrastructure/System errors</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = BusinessResultWithStatus.Failure&lt;Order&gt;(
    ///     ResultStatusCode.NotFound,
    ///     "Order not found"
    /// );
    /// 
    /// if (result.StatusCode == ResultStatusCode.NotFound)
    /// {
    ///     return NotFound(result);
    /// }
    /// </code>
    /// </example>
    public enum ResultStatusCode
    {
        #region [ Success (0) ]

        /// <summary>
        /// The operation completed successfully.
        /// </summary>
        Success = 0,

        #endregion

        #region [ Validation Errors (100-199) ]

        /// <summary>
        /// One or more validation errors occurred.
        /// </summary>
        ValidationFailed = 100,

        /// <summary>
        /// A required field is missing.
        /// </summary>
        RequiredFieldMissing = 101,

        /// <summary>
        /// The field value is invalid.
        /// </summary>
        InvalidFieldValue = 102,

        /// <summary>
        /// The field value exceeds the maximum length.
        /// </summary>
        MaxLengthExceeded = 103,

        /// <summary>
        /// The field value is below the minimum length.
        /// </summary>
        MinLengthNotMet = 104,

        /// <summary>
        /// The field value is outside the valid range.
        /// </summary>
        OutOfRange = 105,

        /// <summary>
        /// The email format is invalid.
        /// </summary>
        InvalidEmail = 106,

        /// <summary>
        /// The phone number format is invalid.
        /// </summary>
        InvalidPhoneNumber = 107,

        /// <summary>
        /// The date/time value is invalid.
        /// </summary>
        InvalidDateTime = 108,

        /// <summary>
        /// The format of the value is invalid.
        /// </summary>
        InvalidFormat = 109,

        /// <summary>
        /// A nested object has validation errors.
        /// </summary>
        NestedValidationFailed = 110,

        #endregion

        #region [ Authorization Errors (200-299) ]

        /// <summary>
        /// The user is not authenticated.
        /// </summary>
        Unauthorized = 200,

        /// <summary>
        /// The user is authenticated but doesn't have permission.
        /// </summary>
        Forbidden = 201,

        /// <summary>
        /// The authentication token has expired.
        /// </summary>
        TokenExpired = 202,

        /// <summary>
        /// The authentication token is invalid.
        /// </summary>
        InvalidToken = 203,

        /// <summary>
        /// The session has expired.
        /// </summary>
        SessionExpired = 204,

        /// <summary>
        /// The user doesn't have the required role.
        /// </summary>
        InsufficientRole = 205,

        /// <summary>
        /// The user doesn't have the required permission.
        /// </summary>
        InsufficientPermission = 206,

        /// <summary>
        /// The API key is invalid or missing.
        /// </summary>
        InvalidApiKey = 207,

        #endregion

        #region [ Resource Errors (300-399) ]

        /// <summary>
        /// The requested resource was not found.
        /// </summary>
        NotFound = 300,

        /// <summary>
        /// A conflict occurred (e.g., duplicate key, optimistic concurrency).
        /// </summary>
        Conflict = 301,

        /// <summary>
        /// The resource already exists.
        /// </summary>
        AlreadyExists = 302,

        /// <summary>
        /// The resource has been deleted.
        /// </summary>
        ResourceDeleted = 303,

        /// <summary>
        /// The resource is locked and cannot be modified.
        /// </summary>
        ResourceLocked = 304,

        /// <summary>
        /// The resource version doesn't match (optimistic concurrency).
        /// </summary>
        VersionMismatch = 305,

        /// <summary>
        /// The resource is in an invalid state for the operation.
        /// </summary>
        InvalidResourceState = 306,

        /// <summary>
        /// The resource reference is invalid.
        /// </summary>
        InvalidReference = 307,

        /// <summary>
        /// A dependent resource was not found.
        /// </summary>
        DependencyNotFound = 308,

        /// <summary>
        /// The resource cannot be deleted due to dependencies.
        /// </summary>
        HasDependencies = 309,

        #endregion

        #region [ Business Rule Errors (400-499) ]

        /// <summary>
        /// A domain/business rule was violated.
        /// </summary>
        DomainRuleViolation = 400,

        /// <summary>
        /// A precondition for the operation was not met.
        /// </summary>
        PreconditionFailed = 401,

        /// <summary>
        /// A postcondition for the operation was not met.
        /// </summary>
        PostconditionFailed = 402,

        /// <summary>
        /// The operation is not allowed in the current state.
        /// </summary>
        InvalidStateTransition = 403,

        /// <summary>
        /// A business limit has been exceeded.
        /// </summary>
        LimitExceeded = 404,

        /// <summary>
        /// Insufficient balance/credit for the operation.
        /// </summary>
        InsufficientBalance = 405,

        /// <summary>
        /// The operation would violate a business constraint.
        /// </summary>
        BusinessConstraintViolation = 406,

        /// <summary>
        /// The operation is not supported for this entity type.
        /// </summary>
        OperationNotSupported = 407,

        /// <summary>
        /// The operation requires approval.
        /// </summary>
        ApprovalRequired = 408,

        /// <summary>
        /// The operation has been rejected.
        /// </summary>
        OperationRejected = 409,

        /// <summary>
        /// The operation has already been processed.
        /// </summary>
        AlreadyProcessed = 410,

        /// <summary>
        /// The operation cannot be reversed.
        /// </summary>
        IrreversibleOperation = 411,

        #endregion

        #region [ Infrastructure/System Errors (500-599) ]

        /// <summary>
        /// An internal error occurred.
        /// </summary>
        InternalError = 500,

        /// <summary>
        /// The operation timed out.
        /// </summary>
        Timeout = 501,

        /// <summary>
        /// An external service is unavailable.
        /// </summary>
        ServiceUnavailable = 502,

        /// <summary>
        /// A database error occurred.
        /// </summary>
        DatabaseError = 503,

        /// <summary>
        /// A network error occurred.
        /// </summary>
        NetworkError = 504,

        /// <summary>
        /// A configuration error occurred.
        /// </summary>
        ConfigurationError = 505,

        /// <summary>
        /// A serialization/deserialization error occurred.
        /// </summary>
        SerializationError = 506,

        /// <summary>
        /// A file system error occurred.
        /// </summary>
        FileSystemError = 507,

        /// <summary>
        /// A circuit breaker is open.
        /// </summary>
        CircuitBreakerOpen = 508,

        /// <summary>
        /// The rate limit has been exceeded.
        /// </summary>
        RateLimitExceeded = 509,

        /// <summary>
        /// A messaging error occurred.
        /// </summary>
        MessagingError = 510,

        /// <summary>
        /// A cache error occurred.
        /// </summary>
        CacheError = 511,

        /// <summary>
        /// The operation was cancelled.
        /// </summary>
        OperationCancelled = 512,

        /// <summary>
        /// A transient error occurred (retry may succeed).
        /// </summary>
        TransientError = 513,

        /// <summary>
        /// Concurrency conflict occurred during save.
        /// </summary>
        ConcurrencyError = 514,

        #endregion

        #region [ Custom/Unknown (900+) ]

        /// <summary>
        /// A custom error code. Check the error message for details.
        /// </summary>
        Custom = 900,

        /// <summary>
        /// An unknown error occurred.
        /// </summary>
        Unknown = 999

        #endregion
    }
}


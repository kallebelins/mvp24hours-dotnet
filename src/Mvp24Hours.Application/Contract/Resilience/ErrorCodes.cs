//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Application.Contract.Resilience
{
    /// <summary>
    /// Standard error codes for internationalization (i18n).
    /// Use these codes to look up localized error messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Error code format: CATEGORY.SUBCATEGORY.ERROR
    /// <list type="bullet">
    /// <item><c>VALIDATION.*</c>: Input validation errors</item>
    /// <item><c>AUTH.*</c>: Authentication/Authorization errors</item>
    /// <item><c>RESOURCE.*</c>: Resource state errors (not found, conflict)</item>
    /// <item><c>DOMAIN.*</c>: Business rule violations</item>
    /// <item><c>SYSTEM.*</c>: Infrastructure/System errors</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Use error codes in results
    /// var result = BusinessResultWithStatus.Failure&lt;Order&gt;(
    ///     ResultStatusCode.NotFound,
    ///     "Order not found",
    ///     ErrorCodes.Resource.NotFound
    /// );
    /// 
    /// // Look up localized message
    /// var localizedMessage = _localizer[ErrorCodes.Resource.NotFound];
    /// </code>
    /// </example>
    public static class ErrorCodes
    {
        /// <summary>
        /// Validation error codes.
        /// </summary>
        public static class Validation
        {
            /// <summary>General validation failure.</summary>
            public const string Failed = "VALIDATION.FAILED";

            /// <summary>A required field is missing.</summary>
            public const string Required = "VALIDATION.REQUIRED";

            /// <summary>The field value is null.</summary>
            public const string ArgumentNull = "VALIDATION.ARGUMENT_NULL";

            /// <summary>The field value is invalid.</summary>
            public const string ArgumentInvalid = "VALIDATION.ARGUMENT_INVALID";

            /// <summary>The value is out of the valid range.</summary>
            public const string OutOfRange = "VALIDATION.OUT_OF_RANGE";

            /// <summary>The value exceeds the maximum length.</summary>
            public const string MaxLength = "VALIDATION.MAX_LENGTH";

            /// <summary>The value is below the minimum length.</summary>
            public const string MinLength = "VALIDATION.MIN_LENGTH";

            /// <summary>The email format is invalid.</summary>
            public const string InvalidEmail = "VALIDATION.EMAIL.INVALID";

            /// <summary>The phone number format is invalid.</summary>
            public const string InvalidPhone = "VALIDATION.PHONE.INVALID";

            /// <summary>The CPF is invalid.</summary>
            public const string InvalidCpf = "VALIDATION.CPF.INVALID";

            /// <summary>The CNPJ is invalid.</summary>
            public const string InvalidCnpj = "VALIDATION.CNPJ.INVALID";

            /// <summary>The URL format is invalid.</summary>
            public const string InvalidUrl = "VALIDATION.URL.INVALID";

            /// <summary>The date format is invalid.</summary>
            public const string InvalidDate = "VALIDATION.DATE.INVALID";

            /// <summary>The date is in the past.</summary>
            public const string DateInPast = "VALIDATION.DATE.IN_PAST";

            /// <summary>The date is in the future.</summary>
            public const string DateInFuture = "VALIDATION.DATE.IN_FUTURE";

            /// <summary>The format is invalid.</summary>
            public const string InvalidFormat = "VALIDATION.FORMAT.INVALID";

            /// <summary>Division by zero attempted.</summary>
            public const string DivideByZero = "VALIDATION.DIVIDE_BY_ZERO";

            /// <summary>Value overflow occurred.</summary>
            public const string Overflow = "VALIDATION.OVERFLOW";

            /// <summary>Nested validation failed.</summary>
            public const string NestedFailed = "VALIDATION.NESTED.FAILED";

            /// <summary>Collection is empty.</summary>
            public const string EmptyCollection = "VALIDATION.COLLECTION.EMPTY";

            /// <summary>Duplicate value.</summary>
            public const string Duplicate = "VALIDATION.DUPLICATE";

            /// <summary>String is empty or whitespace.</summary>
            public const string EmptyString = "VALIDATION.STRING.EMPTY";

            /// <summary>Password is too weak.</summary>
            public const string WeakPassword = "VALIDATION.PASSWORD.WEAK";

            /// <summary>Passwords do not match.</summary>
            public const string PasswordMismatch = "VALIDATION.PASSWORD.MISMATCH";
        }

        /// <summary>
        /// Authentication and authorization error codes.
        /// </summary>
        public static class Auth
        {
            /// <summary>User is not authenticated.</summary>
            public const string Unauthorized = "AUTH.UNAUTHORIZED";

            /// <summary>User doesn't have permission.</summary>
            public const string Forbidden = "AUTH.FORBIDDEN";

            /// <summary>Access denied.</summary>
            public const string AccessDenied = "AUTH.ACCESS_DENIED";

            /// <summary>Token has expired.</summary>
            public const string TokenExpired = "AUTH.TOKEN.EXPIRED";

            /// <summary>Token is invalid.</summary>
            public const string TokenInvalid = "AUTH.TOKEN.INVALID";

            /// <summary>Session has expired.</summary>
            public const string SessionExpired = "AUTH.SESSION.EXPIRED";

            /// <summary>Insufficient role.</summary>
            public const string InsufficientRole = "AUTH.ROLE.INSUFFICIENT";

            /// <summary>Insufficient permission.</summary>
            public const string InsufficientPermission = "AUTH.PERMISSION.INSUFFICIENT";

            /// <summary>Invalid credentials.</summary>
            public const string InvalidCredentials = "AUTH.CREDENTIALS.INVALID";

            /// <summary>Account is locked.</summary>
            public const string AccountLocked = "AUTH.ACCOUNT.LOCKED";

            /// <summary>Account is disabled.</summary>
            public const string AccountDisabled = "AUTH.ACCOUNT.DISABLED";

            /// <summary>API key is invalid.</summary>
            public const string InvalidApiKey = "AUTH.API_KEY.INVALID";

            /// <summary>Two-factor authentication required.</summary>
            public const string TwoFactorRequired = "AUTH.TWO_FACTOR.REQUIRED";
        }

        /// <summary>
        /// Resource state error codes.
        /// </summary>
        public static class Resource
        {
            /// <summary>Resource not found.</summary>
            public const string NotFound = "RESOURCE.NOT_FOUND";

            /// <summary>Resource already exists.</summary>
            public const string AlreadyExists = "RESOURCE.ALREADY_EXISTS";

            /// <summary>Resource conflict.</summary>
            public const string Conflict = "RESOURCE.CONFLICT";

            /// <summary>Resource has been deleted.</summary>
            public const string Deleted = "RESOURCE.DELETED";

            /// <summary>Resource is locked.</summary>
            public const string Locked = "RESOURCE.LOCKED";

            /// <summary>Version mismatch (optimistic concurrency).</summary>
            public const string VersionMismatch = "RESOURCE.VERSION_MISMATCH";

            /// <summary>Invalid resource state.</summary>
            public const string InvalidState = "RESOURCE.STATE.INVALID";

            /// <summary>Invalid reference.</summary>
            public const string InvalidReference = "RESOURCE.REFERENCE.INVALID";

            /// <summary>Dependency not found.</summary>
            public const string DependencyNotFound = "RESOURCE.DEPENDENCY.NOT_FOUND";

            /// <summary>Cannot delete due to dependencies.</summary>
            public const string HasDependencies = "RESOURCE.HAS_DEPENDENCIES";

            /// <summary>Key not found.</summary>
            public const string KeyNotFound = "RESOURCE.KEY.NOT_FOUND";

            /// <summary>File not found.</summary>
            public const string FileNotFound = "RESOURCE.FILE.NOT_FOUND";

            /// <summary>Directory not found.</summary>
            public const string DirectoryNotFound = "RESOURCE.DIRECTORY.NOT_FOUND";
        }

        /// <summary>
        /// Domain/business rule error codes.
        /// </summary>
        public static class Domain
        {
            /// <summary>Business rule violated.</summary>
            public const string RuleViolation = "DOMAIN.RULE_VIOLATION";

            /// <summary>Precondition failed.</summary>
            public const string PreconditionFailed = "DOMAIN.PRECONDITION.FAILED";

            /// <summary>Postcondition failed.</summary>
            public const string PostconditionFailed = "DOMAIN.POSTCONDITION.FAILED";

            /// <summary>Invalid state transition.</summary>
            public const string InvalidTransition = "DOMAIN.TRANSITION.INVALID";

            /// <summary>Limit exceeded.</summary>
            public const string LimitExceeded = "DOMAIN.LIMIT.EXCEEDED";

            /// <summary>Insufficient balance.</summary>
            public const string InsufficientBalance = "DOMAIN.BALANCE.INSUFFICIENT";

            /// <summary>Business constraint violated.</summary>
            public const string ConstraintViolation = "DOMAIN.CONSTRAINT.VIOLATION";

            /// <summary>Approval required.</summary>
            public const string ApprovalRequired = "DOMAIN.APPROVAL.REQUIRED";

            /// <summary>Operation rejected.</summary>
            public const string Rejected = "DOMAIN.OPERATION.REJECTED";

            /// <summary>Already processed.</summary>
            public const string AlreadyProcessed = "DOMAIN.ALREADY_PROCESSED";

            /// <summary>Cannot reverse operation.</summary>
            public const string Irreversible = "DOMAIN.IRREVERSIBLE";

            /// <summary>Business error.</summary>
            public const string BusinessError = "DOMAIN.BUSINESS_ERROR";

            /// <summary>Invariant violated.</summary>
            public const string InvariantViolation = "DOMAIN.INVARIANT.VIOLATION";
        }

        /// <summary>
        /// Operation-related error codes.
        /// </summary>
        public static class Operation
        {
            /// <summary>Invalid state for operation.</summary>
            public const string InvalidState = "OPERATION.STATE.INVALID";

            /// <summary>Operation not supported.</summary>
            public const string NotSupported = "OPERATION.NOT_SUPPORTED";

            /// <summary>Operation not implemented.</summary>
            public const string NotImplemented = "OPERATION.NOT_IMPLEMENTED";

            /// <summary>Operation cancelled.</summary>
            public const string Cancelled = "OPERATION.CANCELLED";

            /// <summary>Operation already in progress.</summary>
            public const string InProgress = "OPERATION.IN_PROGRESS";

            /// <summary>Operation failed.</summary>
            public const string Failed = "OPERATION.FAILED";
        }

        /// <summary>
        /// System/infrastructure error codes.
        /// </summary>
        public static class System
        {
            /// <summary>Internal error.</summary>
            public const string InternalError = "SYSTEM.INTERNAL_ERROR";

            /// <summary>Operation timed out.</summary>
            public const string Timeout = "SYSTEM.TIMEOUT";

            /// <summary>Service unavailable.</summary>
            public const string ServiceUnavailable = "SYSTEM.SERVICE_UNAVAILABLE";

            /// <summary>Database error.</summary>
            public const string DatabaseError = "SYSTEM.DATABASE_ERROR";

            /// <summary>Network error.</summary>
            public const string NetworkError = "SYSTEM.NETWORK_ERROR";

            /// <summary>Configuration error.</summary>
            public const string ConfigurationError = "SYSTEM.CONFIGURATION_ERROR";

            /// <summary>Serialization error.</summary>
            public const string SerializationError = "SYSTEM.SERIALIZATION_ERROR";

            /// <summary>JSON error.</summary>
            public const string JsonError = "SYSTEM.JSON_ERROR";

            /// <summary>File system error.</summary>
            public const string FileSystemError = "SYSTEM.FILESYSTEM_ERROR";

            /// <summary>IO error.</summary>
            public const string IoError = "SYSTEM.IO_ERROR";

            /// <summary>Circuit breaker open.</summary>
            public const string CircuitBreakerOpen = "SYSTEM.CIRCUIT_BREAKER.OPEN";

            /// <summary>Rate limit exceeded.</summary>
            public const string RateLimitExceeded = "SYSTEM.RATE_LIMIT.EXCEEDED";

            /// <summary>Messaging error.</summary>
            public const string MessagingError = "SYSTEM.MESSAGING_ERROR";

            /// <summary>Cache error.</summary>
            public const string CacheError = "SYSTEM.CACHE_ERROR";

            /// <summary>Pipeline error.</summary>
            public const string PipelineError = "SYSTEM.PIPELINE_ERROR";

            /// <summary>Concurrency error.</summary>
            public const string ConcurrencyError = "SYSTEM.CONCURRENCY_ERROR";

            /// <summary>HTTP request error.</summary>
            public const string HttpRequestError = "SYSTEM.HTTP_REQUEST_ERROR";

            /// <summary>Task cancelled.</summary>
            public const string TaskCancelled = "SYSTEM.TASK_CANCELLED";

            /// <summary>Null reference error.</summary>
            public const string NullReference = "SYSTEM.NULL_REFERENCE";

            /// <summary>Transient error.</summary>
            public const string TransientError = "SYSTEM.TRANSIENT_ERROR";
        }

        /// <summary>
        /// Exception-related error codes for inner details.
        /// </summary>
        public static class Exception
        {
            /// <summary>Inner exception details.</summary>
            public const string Inner = "EXCEPTION.INNER";

            /// <summary>Stack trace details.</summary>
            public const string StackTrace = "EXCEPTION.STACK_TRACE";

            /// <summary>Exception type.</summary>
            public const string Type = "EXCEPTION.TYPE";
        }
    }
}


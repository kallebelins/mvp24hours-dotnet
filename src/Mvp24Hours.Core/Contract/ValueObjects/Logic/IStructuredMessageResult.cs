//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Core.Contract.ValueObjects.Logic
{
    /// <summary>
    /// Extended message result with structured error code support.
    /// Provides additional information for error handling and client-side processing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface extends <see cref="IMessageResult"/> to support:
    /// <list type="bullet">
    /// <item><c>ErrorCode</c> - A unique code for the error type (e.g., "USER_NOT_FOUND", "VALIDATION_001")</item>
    /// <item><c>Category</c> - Error category for grouping (e.g., "Validation", "Business", "System")</item>
    /// <item><c>Details</c> - Additional contextual information</item>
    /// <item><c>HelpLink</c> - URL to documentation or help page</item>
    /// <item><c>PropertyName</c> - For validation errors, the name of the invalid property</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Error Code Format:</strong>
    /// It's recommended to use a consistent format for error codes, such as:
    /// <list type="bullet">
    /// <item><c>MODULE_ACTION_ERROR</c> (e.g., "ORDER_CREATE_DUPLICATE")</item>
    /// <item><c>HTTP_STATUS_ERROR</c> (e.g., "404_NOT_FOUND")</item>
    /// <item><c>ERR_NUMERIC_CODE</c> (e.g., "ERR_001")</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a structured validation error
    /// var error = new StructuredMessageResult(
    ///     key: "Email",
    ///     message: "Email format is invalid",
    ///     errorCode: "VALIDATION_EMAIL_INVALID",
    ///     category: ErrorCategory.Validation,
    ///     propertyName: "Email");
    /// 
    /// // Create a business error
    /// var businessError = new StructuredMessageResult(
    ///     key: "OrderLimit",
    ///     message: "Daily order limit exceeded",
    ///     errorCode: "ORDER_LIMIT_EXCEEDED",
    ///     category: ErrorCategory.Business,
    ///     details: new { Limit = 100, Current = 105 });
    /// </code>
    /// </example>
    public interface IStructuredMessageResult : IMessageResult
    {
        /// <summary>
        /// A unique, machine-readable error code.
        /// Use uppercase with underscores for consistency.
        /// </summary>
        /// <example>
        /// Examples: "USER_NOT_FOUND", "VALIDATION_REQUIRED", "PAYMENT_FAILED"
        /// </example>
        string ErrorCode { get; }

        /// <summary>
        /// The category of the error for grouping and handling.
        /// </summary>
        ErrorCategory Category { get; }

        /// <summary>
        /// Optional additional details about the error.
        /// Can be a string or a serializable object.
        /// </summary>
        object Details { get; }

        /// <summary>
        /// Optional URL to documentation or help page for this error.
        /// </summary>
        string HelpLink { get; }

        /// <summary>
        /// For validation errors, the name of the property that failed validation.
        /// </summary>
        string PropertyName { get; }

        /// <summary>
        /// The HTTP status code associated with this error (if applicable).
        /// </summary>
        int? HttpStatusCode { get; }
    }

    /// <summary>
    /// Categories for structured error messages.
    /// </summary>
    public enum ErrorCategory
    {
        /// <summary>
        /// General/unspecified error.
        /// </summary>
        General = 0,

        /// <summary>
        /// Validation error (input validation, format errors).
        /// HTTP 400 Bad Request.
        /// </summary>
        Validation = 1,

        /// <summary>
        /// Business rule violation.
        /// HTTP 422 Unprocessable Entity.
        /// </summary>
        Business = 2,

        /// <summary>
        /// Resource not found.
        /// HTTP 404 Not Found.
        /// </summary>
        NotFound = 3,

        /// <summary>
        /// Authentication required or invalid credentials.
        /// HTTP 401 Unauthorized.
        /// </summary>
        Authentication = 4,

        /// <summary>
        /// Authorization failure (authenticated but no permission).
        /// HTTP 403 Forbidden.
        /// </summary>
        Authorization = 5,

        /// <summary>
        /// Conflict with current state (e.g., duplicate, concurrent modification).
        /// HTTP 409 Conflict.
        /// </summary>
        Conflict = 6,

        /// <summary>
        /// External service or integration error.
        /// HTTP 502 Bad Gateway.
        /// </summary>
        Integration = 7,

        /// <summary>
        /// Internal system error.
        /// HTTP 500 Internal Server Error.
        /// </summary>
        System = 8,

        /// <summary>
        /// Service temporarily unavailable.
        /// HTTP 503 Service Unavailable.
        /// </summary>
        Unavailable = 9
    }
}


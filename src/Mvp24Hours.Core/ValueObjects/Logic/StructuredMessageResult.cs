//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Mvp24Hours.Core.ValueObjects.Logic
{
    /// <summary>
    /// Implementation of <see cref="IStructuredMessageResult"/> with structured error code support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides rich error information for better error handling and client feedback.
    /// Includes factory methods for common error types.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using factory methods
    /// var notFound = StructuredMessageResult.NotFound("User", userId);
    /// var validation = StructuredMessageResult.Validation("Email", "Invalid email format", "email");
    /// var business = StructuredMessageResult.BusinessError("ORDER_LIMIT", "Daily limit exceeded");
    /// 
    /// // Direct construction
    /// var custom = new StructuredMessageResult(
    ///     key: "payment",
    ///     message: "Payment declined",
    ///     errorCode: "PAYMENT_DECLINED",
    ///     category: ErrorCategory.Business,
    ///     details: new { Reason = "Insufficient funds" });
    /// </code>
    /// </example>
    [DataContract, Serializable]
    public class StructuredMessageResult : BaseVO, IStructuredMessageResult
    {
        #region [ Constructors ]

        /// <summary>
        /// Creates a new structured message result.
        /// </summary>
        /// <param name="key">Reference key for the message.</param>
        /// <param name="message">Human-readable message.</param>
        /// <param name="errorCode">Machine-readable error code.</param>
        /// <param name="type">Message type (default: Error).</param>
        /// <param name="category">Error category (default: General).</param>
        /// <param name="details">Additional details object.</param>
        /// <param name="helpLink">URL to help documentation.</param>
        /// <param name="propertyName">Property name for validation errors.</param>
        /// <param name="httpStatusCode">Associated HTTP status code.</param>
        [JsonConstructor]
        public StructuredMessageResult(
            string key,
            string message,
            string errorCode,
            MessageType type = MessageType.Error,
            ErrorCategory category = ErrorCategory.General,
            object details = null,
            string helpLink = null,
            string propertyName = null,
            int? httpStatusCode = null)
        {
            Key = key;
            Message = message;
            ErrorCode = errorCode ?? "UNKNOWN_ERROR";
            Type = type;
            Category = category;
            Details = details;
            HelpLink = helpLink;
            PropertyName = propertyName;
            HttpStatusCode = httpStatusCode ?? GetDefaultHttpStatusCode(category);
        }

        /// <summary>
        /// Creates a simple structured message result with minimal parameters.
        /// </summary>
        public StructuredMessageResult(string message, string errorCode, ErrorCategory category = ErrorCategory.General)
            : this(null, message, errorCode, MessageType.Error, category)
        {
        }

        #endregion

        #region [ Properties ]

        /// <inheritdoc />
        [DataMember]
        public string Key { get; }

        /// <inheritdoc />
        [DataMember]
        public string Message { get; }

        /// <inheritdoc />
        [DataMember]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
        public MessageType Type { get; }

        /// <inheritdoc />
        [DataMember]
        public string CustomType => null;

        /// <inheritdoc />
        [DataMember]
        public string ErrorCode { get; }

        /// <inheritdoc />
        [DataMember]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
        public ErrorCategory Category { get; }

        /// <inheritdoc />
        [DataMember]
        public object Details { get; }

        /// <inheritdoc />
        [DataMember]
        public string HelpLink { get; }

        /// <inheritdoc />
        [DataMember]
        public string PropertyName { get; }

        /// <inheritdoc />
        [DataMember]
        public int? HttpStatusCode { get; }

        #endregion

        #region [ Factory Methods ]

        /// <summary>
        /// Creates a validation error for a specific property.
        /// </summary>
        /// <param name="propertyName">The name of the invalid property.</param>
        /// <param name="message">The validation error message.</param>
        /// <param name="errorCode">Optional error code (default: "VALIDATION_FAILED").</param>
        /// <returns>A structured validation error message.</returns>
        public static StructuredMessageResult Validation(string propertyName, string message, string errorCode = null)
        {
            return new StructuredMessageResult(
                key: propertyName,
                message: message,
                errorCode: errorCode ?? "VALIDATION_FAILED",
                type: MessageType.Error,
                category: ErrorCategory.Validation,
                propertyName: propertyName,
                httpStatusCode: 400);
        }

        /// <summary>
        /// Creates a not found error.
        /// </summary>
        /// <param name="resourceName">The name of the resource that was not found.</param>
        /// <param name="resourceId">Optional identifier of the resource.</param>
        /// <returns>A structured not found error message.</returns>
        public static StructuredMessageResult NotFound(string resourceName, object resourceId = null)
        {
            var message = resourceId != null
                ? $"{resourceName} with identifier '{resourceId}' was not found."
                : $"{resourceName} was not found.";

            return new StructuredMessageResult(
                key: resourceName,
                message: message,
                errorCode: $"{resourceName.ToUpperInvariant().Replace(" ", "_")}_NOT_FOUND",
                type: MessageType.Error,
                category: ErrorCategory.NotFound,
                details: resourceId != null ? new { ResourceId = resourceId } : null,
                httpStatusCode: 404);
        }

        /// <summary>
        /// Creates a business rule violation error.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="details">Optional details object.</param>
        /// <returns>A structured business error message.</returns>
        public static StructuredMessageResult BusinessError(string errorCode, string message, object details = null)
        {
            return new StructuredMessageResult(
                key: errorCode,
                message: message,
                errorCode: errorCode,
                type: MessageType.Error,
                category: ErrorCategory.Business,
                details: details,
                httpStatusCode: 422);
        }

        /// <summary>
        /// Creates an authorization error.
        /// </summary>
        /// <param name="resource">The resource being accessed.</param>
        /// <param name="action">The action being attempted.</param>
        /// <returns>A structured authorization error message.</returns>
        public static StructuredMessageResult Forbidden(string resource, string action = null)
        {
            var message = action != null
                ? $"You do not have permission to {action} {resource}."
                : $"You do not have permission to access {resource}.";

            return new StructuredMessageResult(
                key: "Authorization",
                message: message,
                errorCode: "FORBIDDEN",
                type: MessageType.Error,
                category: ErrorCategory.Authorization,
                details: new { Resource = resource, Action = action },
                httpStatusCode: 403);
        }

        /// <summary>
        /// Creates an authentication error.
        /// </summary>
        /// <param name="message">Optional custom message.</param>
        /// <returns>A structured authentication error message.</returns>
        public static StructuredMessageResult Unauthorized(string message = null)
        {
            return new StructuredMessageResult(
                key: "Authentication",
                message: message ?? "Authentication is required to access this resource.",
                errorCode: "UNAUTHORIZED",
                type: MessageType.Error,
                category: ErrorCategory.Authentication,
                httpStatusCode: 401);
        }

        /// <summary>
        /// Creates a conflict error.
        /// </summary>
        /// <param name="resourceName">The name of the conflicting resource.</param>
        /// <param name="message">The conflict message.</param>
        /// <param name="details">Optional details object.</param>
        /// <returns>A structured conflict error message.</returns>
        public static StructuredMessageResult Conflict(string resourceName, string message, object details = null)
        {
            return new StructuredMessageResult(
                key: resourceName,
                message: message,
                errorCode: $"{resourceName.ToUpperInvariant().Replace(" ", "_")}_CONFLICT",
                type: MessageType.Error,
                category: ErrorCategory.Conflict,
                details: details,
                httpStatusCode: 409);
        }

        /// <summary>
        /// Creates a system error.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorCode">Optional error code.</param>
        /// <returns>A structured system error message.</returns>
        public static StructuredMessageResult SystemError(string message, string errorCode = null)
        {
            return new StructuredMessageResult(
                key: "System",
                message: message,
                errorCode: errorCode ?? "INTERNAL_ERROR",
                type: MessageType.Error,
                category: ErrorCategory.System,
                httpStatusCode: 500);
        }

        /// <summary>
        /// Creates an info message (not an error).
        /// </summary>
        /// <param name="key">The message key.</param>
        /// <param name="message">The info message.</param>
        /// <returns>A structured info message.</returns>
        public static StructuredMessageResult Info(string key, string message)
        {
            return new StructuredMessageResult(
                key: key,
                message: message,
                errorCode: null,
                type: MessageType.Info,
                category: ErrorCategory.General);
        }

        /// <summary>
        /// Creates a warning message.
        /// </summary>
        /// <param name="key">The message key.</param>
        /// <param name="message">The warning message.</param>
        /// <param name="code">Optional warning code.</param>
        /// <returns>A structured warning message.</returns>
        public static StructuredMessageResult Warning(string key, string message, string code = null)
        {
            return new StructuredMessageResult(
                key: key,
                message: message,
                errorCode: code ?? "WARNING",
                type: MessageType.Warning,
                category: ErrorCategory.General);
        }

        #endregion

        #region [ Helper Methods ]

        private static int? GetDefaultHttpStatusCode(ErrorCategory category)
        {
            return category switch
            {
                ErrorCategory.Validation => 400,
                ErrorCategory.NotFound => 404,
                ErrorCategory.Authentication => 401,
                ErrorCategory.Authorization => 403,
                ErrorCategory.Conflict => 409,
                ErrorCategory.Business => 422,
                ErrorCategory.Integration => 502,
                ErrorCategory.System => 500,
                ErrorCategory.Unavailable => 503,
                _ => null
            };
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Key;
            yield return Message;
            yield return ErrorCode;
            yield return Type;
            yield return Category;
        }

        #endregion
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Resilience;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Mvp24Hours.Application.Logic.Resilience
{
    /// <summary>
    /// Default implementation of <see cref="IErrorMessageLocalizer"/>.
    /// Provides fallback English messages for standard error codes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation provides default English messages and can be extended
    /// or replaced with a resource-based implementation for full i18n support.
    /// </para>
    /// </remarks>
    public class DefaultErrorMessageLocalizer : IErrorMessageLocalizer
    {
        private static readonly Dictionary<string, string> DefaultMessages = new(StringComparer.OrdinalIgnoreCase)
        {
            // Validation errors
            [ErrorCodes.Validation.Failed] = "Validation failed.",
            [ErrorCodes.Validation.Required] = "The field '{0}' is required.",
            [ErrorCodes.Validation.ArgumentNull] = "The argument '{0}' cannot be null.",
            [ErrorCodes.Validation.ArgumentInvalid] = "The argument '{0}' is invalid.",
            [ErrorCodes.Validation.OutOfRange] = "The value is out of the valid range.",
            [ErrorCodes.Validation.MaxLength] = "The field '{0}' must not exceed {1} characters.",
            [ErrorCodes.Validation.MinLength] = "The field '{0}' must be at least {1} characters.",
            [ErrorCodes.Validation.InvalidEmail] = "The email address is invalid.",
            [ErrorCodes.Validation.InvalidPhone] = "The phone number is invalid.",
            [ErrorCodes.Validation.InvalidCpf] = "The CPF is invalid.",
            [ErrorCodes.Validation.InvalidCnpj] = "The CNPJ is invalid.",
            [ErrorCodes.Validation.InvalidUrl] = "The URL is invalid.",
            [ErrorCodes.Validation.InvalidDate] = "The date is invalid.",
            [ErrorCodes.Validation.DateInPast] = "The date cannot be in the past.",
            [ErrorCodes.Validation.DateInFuture] = "The date cannot be in the future.",
            [ErrorCodes.Validation.InvalidFormat] = "The format is invalid.",
            [ErrorCodes.Validation.DivideByZero] = "Cannot divide by zero.",
            [ErrorCodes.Validation.Overflow] = "Value overflow occurred.",
            [ErrorCodes.Validation.NestedFailed] = "Nested validation failed.",
            [ErrorCodes.Validation.EmptyCollection] = "The collection cannot be empty.",
            [ErrorCodes.Validation.Duplicate] = "A duplicate value already exists.",
            [ErrorCodes.Validation.EmptyString] = "The field cannot be empty.",
            [ErrorCodes.Validation.WeakPassword] = "The password is too weak.",
            [ErrorCodes.Validation.PasswordMismatch] = "The passwords do not match.",

            // Authentication errors
            [ErrorCodes.Auth.Unauthorized] = "Authentication required.",
            [ErrorCodes.Auth.Forbidden] = "Access denied. You don't have permission to perform this action.",
            [ErrorCodes.Auth.AccessDenied] = "Access denied.",
            [ErrorCodes.Auth.TokenExpired] = "Your session has expired. Please log in again.",
            [ErrorCodes.Auth.TokenInvalid] = "Invalid authentication token.",
            [ErrorCodes.Auth.SessionExpired] = "Your session has expired. Please log in again.",
            [ErrorCodes.Auth.InsufficientRole] = "You don't have the required role.",
            [ErrorCodes.Auth.InsufficientPermission] = "You don't have permission to perform this action.",
            [ErrorCodes.Auth.InvalidCredentials] = "Invalid credentials.",
            [ErrorCodes.Auth.AccountLocked] = "Your account has been locked.",
            [ErrorCodes.Auth.AccountDisabled] = "Your account has been disabled.",
            [ErrorCodes.Auth.InvalidApiKey] = "Invalid API key.",
            [ErrorCodes.Auth.TwoFactorRequired] = "Two-factor authentication is required.",

            // Resource errors
            [ErrorCodes.Resource.NotFound] = "The requested resource was not found.",
            [ErrorCodes.Resource.AlreadyExists] = "The resource already exists.",
            [ErrorCodes.Resource.Conflict] = "A conflict occurred. The resource may have been modified.",
            [ErrorCodes.Resource.Deleted] = "The resource has been deleted.",
            [ErrorCodes.Resource.Locked] = "The resource is locked and cannot be modified.",
            [ErrorCodes.Resource.VersionMismatch] = "The resource has been modified by another user.",
            [ErrorCodes.Resource.InvalidState] = "The resource is in an invalid state for this operation.",
            [ErrorCodes.Resource.InvalidReference] = "Invalid resource reference.",
            [ErrorCodes.Resource.DependencyNotFound] = "A required dependency was not found.",
            [ErrorCodes.Resource.HasDependencies] = "Cannot delete. The resource has dependencies.",
            [ErrorCodes.Resource.KeyNotFound] = "The specified key was not found.",
            [ErrorCodes.Resource.FileNotFound] = "The file was not found.",
            [ErrorCodes.Resource.DirectoryNotFound] = "The directory was not found.",

            // Domain errors
            [ErrorCodes.Domain.RuleViolation] = "A business rule was violated.",
            [ErrorCodes.Domain.PreconditionFailed] = "A precondition was not met.",
            [ErrorCodes.Domain.PostconditionFailed] = "A postcondition was not met.",
            [ErrorCodes.Domain.InvalidTransition] = "Invalid state transition.",
            [ErrorCodes.Domain.LimitExceeded] = "A limit has been exceeded.",
            [ErrorCodes.Domain.InsufficientBalance] = "Insufficient balance.",
            [ErrorCodes.Domain.ConstraintViolation] = "A constraint was violated.",
            [ErrorCodes.Domain.ApprovalRequired] = "This operation requires approval.",
            [ErrorCodes.Domain.Rejected] = "The operation was rejected.",
            [ErrorCodes.Domain.AlreadyProcessed] = "This operation has already been processed.",
            [ErrorCodes.Domain.Irreversible] = "This operation cannot be reversed.",
            [ErrorCodes.Domain.BusinessError] = "A business error occurred.",
            [ErrorCodes.Domain.InvariantViolation] = "An invariant was violated.",

            // Operation errors
            [ErrorCodes.Operation.InvalidState] = "Invalid operation state.",
            [ErrorCodes.Operation.NotSupported] = "This operation is not supported.",
            [ErrorCodes.Operation.NotImplemented] = "This operation is not implemented.",
            [ErrorCodes.Operation.Cancelled] = "The operation was cancelled.",
            [ErrorCodes.Operation.InProgress] = "The operation is already in progress.",
            [ErrorCodes.Operation.Failed] = "The operation failed.",

            // System errors
            [ErrorCodes.System.InternalError] = "An internal error occurred. Please try again later.",
            [ErrorCodes.System.Timeout] = "The operation timed out. Please try again.",
            [ErrorCodes.System.ServiceUnavailable] = "The service is temporarily unavailable.",
            [ErrorCodes.System.DatabaseError] = "A database error occurred.",
            [ErrorCodes.System.NetworkError] = "A network error occurred.",
            [ErrorCodes.System.ConfigurationError] = "A configuration error occurred.",
            [ErrorCodes.System.SerializationError] = "A serialization error occurred.",
            [ErrorCodes.System.JsonError] = "Invalid JSON format.",
            [ErrorCodes.System.FileSystemError] = "A file system error occurred.",
            [ErrorCodes.System.IoError] = "An I/O error occurred.",
            [ErrorCodes.System.CircuitBreakerOpen] = "The service is temporarily unavailable. Please try again later.",
            [ErrorCodes.System.RateLimitExceeded] = "Rate limit exceeded. Please wait and try again.",
            [ErrorCodes.System.MessagingError] = "A messaging error occurred.",
            [ErrorCodes.System.CacheError] = "A cache error occurred.",
            [ErrorCodes.System.PipelineError] = "A pipeline error occurred.",
            [ErrorCodes.System.ConcurrencyError] = "A concurrency error occurred.",
            [ErrorCodes.System.HttpRequestError] = "An HTTP request error occurred.",
            [ErrorCodes.System.TaskCancelled] = "The task was cancelled.",
            [ErrorCodes.System.NullReference] = "A null reference error occurred.",
            [ErrorCodes.System.TransientError] = "A transient error occurred. Please try again."
        };

        /// <inheritdoc/>
        public string GetMessage(string errorCode, params object[] args)
        {
            return GetMessage(errorCode, CultureInfo.CurrentCulture, args);
        }

        /// <inheritdoc/>
        public string GetMessage(string errorCode, CultureInfo culture, params object[] args)
        {
            if (string.IsNullOrEmpty(errorCode))
            {
                return "An error occurred.";
            }

            if (DefaultMessages.TryGetValue(errorCode, out var message))
            {
                try
                {
                    return args.Length > 0
                        ? string.Format(culture, message, args)
                        : message;
                }
                catch (FormatException)
                {
                    return message;
                }
            }

            // Return the error code itself as fallback
            return errorCode;
        }

        /// <inheritdoc/>
        public bool HasMessage(string errorCode)
        {
            return !string.IsNullOrEmpty(errorCode) && DefaultMessages.ContainsKey(errorCode);
        }

        /// <inheritdoc/>
        public string GetPropertyMessage(string errorCode, string propertyName, params object[] args)
        {
            // Prepend property name to args if message expects it
            var allArgs = new object[args.Length + 1];
            allArgs[0] = propertyName;
            Array.Copy(args, 0, allArgs, 1, args.Length);

            return GetMessage(errorCode, allArgs);
        }
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Mvp24Hours.Application.Logic.Resilience
{
    /// <summary>
    /// Implementation of <see cref="IBusinessResultWithStatus{T}"/> with status codes
    /// and error/warning distinction.
    /// </summary>
    [DataContract, Serializable]
    public class BusinessResultWithStatus<T> : BaseVO, IBusinessResultWithStatus<T>
    {
        #region [ Fields ]

        private readonly List<IResultMessage> _extendedMessages;
        private IReadOnlyCollection<IResultMessage>? _errors;
        private IReadOnlyCollection<IResultMessage>? _warnings;
        private IReadOnlyCollection<IResultMessage>? _infos;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of <see cref="BusinessResultWithStatus{T}"/>.
        /// </summary>
        [JsonConstructor]
        public BusinessResultWithStatus(
            T? data = default,
            ResultStatusCode statusCode = ResultStatusCode.Success,
            IEnumerable<IResultMessage>? messages = null,
            string? token = null)
        {
            Data = data;
            StatusCode = statusCode;
            Token = token;
            _extendedMessages = messages?.ToList() ?? new List<IResultMessage>();
        }

        #endregion

        #region [ Properties ]

        /// <inheritdoc/>
        [DataMember]
        public T? Data { get; }

        /// <inheritdoc/>
        [DataMember]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ResultStatusCode StatusCode { get; }

        /// <inheritdoc/>
        [DataMember]
        public string? Token { get; private set; }

        /// <inheritdoc/>
        [DataMember]
        public IReadOnlyCollection<IResultMessage> ExtendedMessages =>
            new ReadOnlyCollection<IResultMessage>(_extendedMessages);

        /// <inheritdoc/>
        [IgnoreDataMember]
        public IReadOnlyCollection<IMessageResult>? Messages =>
            _extendedMessages.Count > 0
                ? new ReadOnlyCollection<IMessageResult>(_extendedMessages.Cast<IMessageResult>().ToList())
                : null;

        /// <inheritdoc/>
        [DataMember]
        public bool HasErrors =>
            _extendedMessages.Any(m => m.Severity == MessageSeverity.Error);

        /// <inheritdoc/>
        [DataMember]
        public bool HasWarnings =>
            _extendedMessages.Any(m => m.Severity == MessageSeverity.Warning);

        /// <inheritdoc/>
        [IgnoreDataMember]
        public bool IsSuccess => !HasErrors;

        /// <inheritdoc/>
        [DataMember]
        public IReadOnlyCollection<IResultMessage> Errors =>
            _errors ??= new ReadOnlyCollection<IResultMessage>(
                _extendedMessages.Where(m => m.Severity == MessageSeverity.Error).ToList());

        /// <inheritdoc/>
        [DataMember]
        public IReadOnlyCollection<IResultMessage> Warnings =>
            _warnings ??= new ReadOnlyCollection<IResultMessage>(
                _extendedMessages.Where(m => m.Severity == MessageSeverity.Warning).ToList());

        /// <inheritdoc/>
        [DataMember]
        public IReadOnlyCollection<IResultMessage> Infos =>
            _infos ??= new ReadOnlyCollection<IResultMessage>(
                _extendedMessages.Where(m => m.Severity == MessageSeverity.Info).ToList());

        /// <inheritdoc/>
        [DataMember]
        public string? PrimaryErrorCode =>
            _extendedMessages.FirstOrDefault(m => m.Severity == MessageSeverity.Error)?.ErrorCode;

        #endregion

        #region [ Methods ]

        /// <inheritdoc/>
        public void SetToken(string? token)
        {
            if (string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(token))
            {
                Token = token;
            }
        }

        /// <inheritdoc/>
        public bool HasErrorCode(string errorCode)
        {
            if (string.IsNullOrEmpty(errorCode)) return false;
            return _extendedMessages.Any(m =>
                m.Severity == MessageSeverity.Error &&
                string.Equals(m.ErrorCode, errorCode, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public bool HasPropertyError(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return false;
            return _extendedMessages.Any(m =>
                m.Severity == MessageSeverity.Error &&
                string.Equals(m.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return StatusCode;
            yield return Data!;
            yield return Token ?? string.Empty;
            foreach (var msg in _extendedMessages)
            {
                yield return msg;
            }
        }

        /// <summary>
        /// Returns a string representation of the result.
        /// </summary>
        public override string ToString()
        {
            var status = IsSuccess ? "Success" : "Failure";
            var errorCount = Errors.Count;
            var warningCount = Warnings.Count;

            return $"BusinessResultWithStatus<{typeof(T).Name}> [{status}] " +
                   $"StatusCode={StatusCode}, Errors={errorCount}, Warnings={warningCount}";
        }

        #endregion

        #region [ Implicit Operators ]

        /// <summary>
        /// Implicitly converts data to a success result.
        /// </summary>
        public static implicit operator BusinessResultWithStatus<T>(T data)
        {
            return new BusinessResultWithStatus<T>(data: data);
        }

        /// <summary>
        /// Implicitly extracts data from a result.
        /// </summary>
        public static implicit operator T?(BusinessResultWithStatus<T>? result)
        {
            return result != null ? result.Data : default(T?);
        }

        /// <summary>
        /// Implicitly converts result to boolean (true if success).
        /// </summary>
        public static implicit operator bool(BusinessResultWithStatus<T>? result)
        {
            return result != null && result.IsSuccess;
        }

        #endregion
    }

    /// <summary>
    /// Static factory for creating <see cref="BusinessResultWithStatus{T}"/> instances.
    /// </summary>
    public static class BusinessResultWithStatus
    {
        #region [ Success Methods ]

        /// <summary>
        /// Creates a success result with data.
        /// </summary>
        public static IBusinessResultWithStatus<T> Success<T>(T data, string? token = null)
        {
            return new BusinessResultWithStatus<T>(
                data: data,
                statusCode: ResultStatusCode.Success,
                token: token);
        }

        /// <summary>
        /// Creates a success result with data and an info message.
        /// </summary>
        public static IBusinessResultWithStatus<T> Success<T>(
            T data,
            string infoMessage,
            string? token = null)
        {
            var messages = new List<IResultMessage>
            {
                ResultMessage.Info(infoMessage)
            };

            return new BusinessResultWithStatus<T>(
                data: data,
                statusCode: ResultStatusCode.Success,
                messages: messages,
                token: token);
        }

        /// <summary>
        /// Creates a success result with warnings.
        /// </summary>
        public static IBusinessResultWithStatus<T> SuccessWithWarnings<T>(
            T data,
            IEnumerable<IResultMessage> warnings,
            string? token = null)
        {
            return new BusinessResultWithStatus<T>(
                data: data,
                statusCode: ResultStatusCode.Success,
                messages: warnings,
                token: token);
        }

        /// <summary>
        /// Creates a success result with a single warning.
        /// </summary>
        public static IBusinessResultWithStatus<T> SuccessWithWarning<T>(
            T data,
            string warningMessage,
            string? warningCode = null,
            string? token = null)
        {
            var warnings = new List<IResultMessage>
            {
                ResultMessage.Warning(warningMessage, warningCode)
            };

            return new BusinessResultWithStatus<T>(
                data: data,
                statusCode: ResultStatusCode.Success,
                messages: warnings,
                token: token);
        }

        #endregion

        #region [ Failure Methods ]

        /// <summary>
        /// Creates a failure result with status code and error message.
        /// </summary>
        public static IBusinessResultWithStatus<T> Failure<T>(
            ResultStatusCode statusCode,
            string errorMessage,
            string? errorCode = null,
            string? propertyName = null,
            string? token = null)
        {
            var messages = new List<IResultMessage>
            {
                ResultMessage.Error(errorMessage, errorCode, propertyName)
            };

            return new BusinessResultWithStatus<T>(
                data: default,
                statusCode: statusCode,
                messages: messages,
                token: token);
        }

        /// <summary>
        /// Creates a failure result with multiple errors.
        /// </summary>
        public static IBusinessResultWithStatus<T> Failure<T>(
            ResultStatusCode statusCode,
            IEnumerable<IResultMessage> errors,
            string? token = null)
        {
            return new BusinessResultWithStatus<T>(
                data: default,
                statusCode: statusCode,
                messages: errors,
                token: token);
        }

        /// <summary>
        /// Creates a Not Found failure result.
        /// </summary>
        public static IBusinessResultWithStatus<T> NotFound<T>(
            string message = "Resource not found",
            string? errorCode = null,
            string? token = null)
        {
            return Failure<T>(
                ResultStatusCode.NotFound,
                message,
                errorCode ?? "RESOURCE.NOT_FOUND",
                token: token);
        }

        /// <summary>
        /// Creates a Not Found failure result for a specific entity.
        /// </summary>
        public static IBusinessResultWithStatus<T> NotFound<T>(
            string entityName,
            object id,
            string? token = null)
        {
            return Failure<T>(
                ResultStatusCode.NotFound,
                $"{entityName} with ID '{id}' was not found.",
                $"{entityName.ToUpperInvariant()}.NOT_FOUND",
                token: token);
        }

        /// <summary>
        /// Creates a Validation Failed result.
        /// </summary>
        public static IBusinessResultWithStatus<T> ValidationFailed<T>(
            string message,
            string? propertyName = null,
            object? attemptedValue = null,
            string? token = null)
        {
            var messages = new List<IResultMessage>
            {
                ResultMessage.Error(message, "VALIDATION.FAILED", propertyName, attemptedValue)
            };

            return new BusinessResultWithStatus<T>(
                data: default,
                statusCode: ResultStatusCode.ValidationFailed,
                messages: messages,
                token: token);
        }

        /// <summary>
        /// Creates a Validation Failed result with multiple errors.
        /// </summary>
        public static IBusinessResultWithStatus<T> ValidationFailed<T>(
            IEnumerable<(string propertyName, string message, string? errorCode)> validationErrors,
            string? token = null)
        {
            var messages = validationErrors
                .Select(e => ResultMessage.Error(e.message, e.errorCode ?? "VALIDATION.FAILED", e.propertyName))
                .ToList();

            return new BusinessResultWithStatus<T>(
                data: default,
                statusCode: ResultStatusCode.ValidationFailed,
                messages: messages,
                token: token);
        }

        /// <summary>
        /// Creates a Conflict result.
        /// </summary>
        public static IBusinessResultWithStatus<T> Conflict<T>(
            string message,
            string? errorCode = null,
            string? token = null)
        {
            return Failure<T>(
                ResultStatusCode.Conflict,
                message,
                errorCode ?? "RESOURCE.CONFLICT",
                token: token);
        }

        /// <summary>
        /// Creates an Unauthorized result.
        /// </summary>
        public static IBusinessResultWithStatus<T> Unauthorized<T>(
            string message = "Authentication required",
            string? errorCode = null,
            string? token = null)
        {
            return Failure<T>(
                ResultStatusCode.Unauthorized,
                message,
                errorCode ?? "AUTH.UNAUTHORIZED",
                token: token);
        }

        /// <summary>
        /// Creates a Forbidden result.
        /// </summary>
        public static IBusinessResultWithStatus<T> Forbidden<T>(
            string message = "Access denied",
            string? errorCode = null,
            string? token = null)
        {
            return Failure<T>(
                ResultStatusCode.Forbidden,
                message,
                errorCode ?? "AUTH.FORBIDDEN",
                token: token);
        }

        /// <summary>
        /// Creates a Domain Rule Violation result.
        /// </summary>
        public static IBusinessResultWithStatus<T> DomainRuleViolation<T>(
            string message,
            string? errorCode = null,
            string? token = null)
        {
            return Failure<T>(
                ResultStatusCode.DomainRuleViolation,
                message,
                errorCode ?? "DOMAIN.RULE_VIOLATION",
                token: token);
        }

        /// <summary>
        /// Creates an Internal Error result.
        /// </summary>
        public static IBusinessResultWithStatus<T> InternalError<T>(
            string message = "An internal error occurred",
            string? errorCode = null,
            string? token = null)
        {
            return Failure<T>(
                ResultStatusCode.InternalError,
                message,
                errorCode ?? "SYSTEM.INTERNAL_ERROR",
                token: token);
        }

        #endregion

        #region [ From Methods ]

        /// <summary>
        /// Creates a result from nullable data.
        /// Returns NotFound if data is null.
        /// </summary>
        public static IBusinessResultWithStatus<T> From<T>(
            T? data,
            string notFoundMessage = "Resource not found",
            string? token = null) where T : class
        {
            return data == null
                ? NotFound<T>(notFoundMessage, token: token)
                : Success(data, token);
        }

        /// <summary>
        /// Creates a result from nullable value type.
        /// Returns NotFound if value is null.
        /// </summary>
        public static IBusinessResultWithStatus<T> FromValue<T>(
            T? data,
            string notFoundMessage = "Resource not found",
            string? token = null) where T : struct
        {
            return !data.HasValue
                ? NotFound<T>(notFoundMessage, token: token)
                : Success(data.Value, token);
        }

        /// <summary>
        /// Creates a result from a standard IBusinessResult.
        /// </summary>
        public static IBusinessResultWithStatus<T> FromBusinessResult<T>(
            IBusinessResult<T> result,
            ResultStatusCode? statusCodeOnError = null)
        {
            if (!result.HasErrors)
            {
                return Success(result.Data!, result.Token);
            }

            var messages = result.Messages?
                .Select(m => ResultMessage.Error(m.Message, m.Key))
                .ToList() ?? new List<ResultMessage>();

            return new BusinessResultWithStatus<T>(
                data: default,
                statusCode: statusCodeOnError ?? ResultStatusCode.InternalError,
                messages: messages,
                token: result.Token);
        }

        #endregion
    }
}


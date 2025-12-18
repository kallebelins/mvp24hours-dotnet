//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Core.Exceptions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Resilience
{
    /// <summary>
    /// Default implementation of <see cref="IExceptionToResultMapper"/>.
    /// Maps common exceptions to appropriate status codes and error messages.
    /// </summary>
    public class ExceptionToResultMapper : IExceptionToResultMapper
    {
        private readonly ExceptionMappingOptions _options;
        private readonly Dictionary<Type, ExceptionMapping> _defaultMappings;

        /// <summary>
        /// Initializes a new instance of <see cref="ExceptionToResultMapper"/>.
        /// </summary>
        public ExceptionToResultMapper(IOptions<ExceptionMappingOptions>? options = null)
        {
            _options = options?.Value ?? new ExceptionMappingOptions();
            _defaultMappings = BuildDefaultMappings();
        }

        /// <inheritdoc/>
        public IBusinessResultWithStatus<T> Map<T>(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            var statusCode = GetStatusCode(exception);
            var errorCode = GetErrorCode(exception);
            var message = GetMessage(exception);

            var messages = new List<IResultMessage>
            {
                ResultMessage.Error(message, errorCode)
            };

            // Add exception details if configured
            if (ShouldIncludeDetails(exception))
            {
                AddExceptionDetails(exception, messages);
            }

            return new BusinessResultWithStatus<T>(
                data: default,
                statusCode: statusCode,
                messages: messages);
        }

        /// <inheritdoc/>
        public IBusinessResultWithStatus<T> Map<T>(Exception exception, string customMessage)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            if (string.IsNullOrEmpty(customMessage)) throw new ArgumentNullException(nameof(customMessage));

            var statusCode = GetStatusCode(exception);
            var errorCode = GetErrorCode(exception);

            var messages = new List<IResultMessage>
            {
                ResultMessage.Error(customMessage, errorCode)
            };

            if (ShouldIncludeDetails(exception))
            {
                AddExceptionDetails(exception, messages);
            }

            return new BusinessResultWithStatus<T>(
                data: default,
                statusCode: statusCode,
                messages: messages);
        }

        /// <inheritdoc/>
        public ResultStatusCode GetStatusCode(Exception exception)
        {
            var mapping = GetMapping(exception);
            return mapping?.StatusCode ?? ResultStatusCode.InternalError;
        }

        /// <inheritdoc/>
        public string GetErrorCode(Exception exception)
        {
            var mapping = GetMapping(exception);
            return mapping?.ErrorCode ?? "SYSTEM.INTERNAL_ERROR";
        }

        /// <inheritdoc/>
        public bool ShouldLog(Exception exception)
        {
            var mapping = GetMapping(exception);
            if (mapping?.ShouldLog.HasValue == true)
            {
                return mapping.ShouldLog.Value;
            }

            var statusCode = GetStatusCode(exception);
            var statusCodeInt = (int)statusCode;

            // Server errors (500+) should be logged
            if (statusCodeInt >= 500)
            {
                return _options.LogServerErrors;
            }

            // Client errors (4xx equivalent in our scheme: 100-499)
            return _options.LogClientErrors;
        }

        /// <inheritdoc/>
        public bool ShouldIncludeDetails(Exception exception)
        {
            var mapping = GetMapping(exception);
            if (mapping?.IncludeDetails.HasValue == true)
            {
                return mapping.IncludeDetails.Value;
            }

            return _options.IncludeExceptionDetails;
        }

        #region [ Private Methods ]

        private ExceptionMapping? GetMapping(Exception exception)
        {
            var exceptionType = exception.GetType();

            // Check custom mappings first
            if (_options.CustomMappings.TryGetValue(exceptionType, out var customMapping))
            {
                return customMapping;
            }

            // Check default mappings
            if (_defaultMappings.TryGetValue(exceptionType, out var defaultMapping))
            {
                return defaultMapping;
            }

            // Check base types for inheritance-based mapping
            var baseType = exceptionType.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (_options.CustomMappings.TryGetValue(baseType, out customMapping))
                {
                    return customMapping;
                }

                if (_defaultMappings.TryGetValue(baseType, out defaultMapping))
                {
                    return defaultMapping;
                }

                baseType = baseType.BaseType;
            }

            return null;
        }

        private string GetMessage(Exception exception)
        {
            var mapping = GetMapping(exception);

            if (mapping?.MessageFactory != null)
            {
                return mapping.MessageFactory(exception);
            }

            // For server errors, use default message in production
            var statusCode = GetStatusCode(exception);
            if ((int)statusCode >= 500 && !_options.IncludeExceptionDetails)
            {
                return _options.DefaultErrorMessage;
            }

            return exception.Message;
        }

        private void AddExceptionDetails(Exception exception, List<IResultMessage> messages)
        {
            if (exception.InnerException != null)
            {
                messages.Add(ResultMessage.Info(
                    $"Inner Exception: {exception.InnerException.Message}",
                    "EXCEPTION.INNER"));
            }

            if (_options.IncludeStackTrace && exception.StackTrace != null)
            {
                messages.Add(ResultMessage.Info(
                    exception.StackTrace,
                    "EXCEPTION.STACK_TRACE"));
            }
        }

        private static Dictionary<Type, ExceptionMapping> BuildDefaultMappings()
        {
            return new Dictionary<Type, ExceptionMapping>
            {
                // Mvp24Hours Exceptions
                [typeof(NotFoundException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.NotFound,
                    ErrorCode = "RESOURCE.NOT_FOUND",
                    ShouldLog = false
                },
                [typeof(ConflictException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.Conflict,
                    ErrorCode = "RESOURCE.CONFLICT",
                    ShouldLog = false
                },
                [typeof(UnauthorizedException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.Unauthorized,
                    ErrorCode = "AUTH.UNAUTHORIZED",
                    ShouldLog = false
                },
                [typeof(ForbiddenException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.Forbidden,
                    ErrorCode = "AUTH.FORBIDDEN",
                    ShouldLog = false
                },
                [typeof(ValidationException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.ValidationFailed,
                    ErrorCode = "VALIDATION.FAILED",
                    ShouldLog = false
                },
                [typeof(DomainException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.DomainRuleViolation,
                    ErrorCode = "DOMAIN.RULE_VIOLATION",
                    ShouldLog = false
                },
                [typeof(BusinessException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.DomainRuleViolation,
                    ErrorCode = "BUSINESS.ERROR",
                    ShouldLog = false
                },
                [typeof(ConfigurationException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.ConfigurationError,
                    ErrorCode = "SYSTEM.CONFIGURATION_ERROR",
                    ShouldLog = true
                },
                [typeof(DataException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.DatabaseError,
                    ErrorCode = "SYSTEM.DATABASE_ERROR",
                    ShouldLog = true
                },
                [typeof(PipelineException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.InternalError,
                    ErrorCode = "SYSTEM.PIPELINE_ERROR",
                    ShouldLog = true
                },

                // .NET Standard Exceptions
                [typeof(ArgumentNullException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.ValidationFailed,
                    ErrorCode = "VALIDATION.ARGUMENT_NULL",
                    ShouldLog = false
                },
                [typeof(ArgumentException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.ValidationFailed,
                    ErrorCode = "VALIDATION.ARGUMENT_INVALID",
                    ShouldLog = false
                },
                [typeof(ArgumentOutOfRangeException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.OutOfRange,
                    ErrorCode = "VALIDATION.OUT_OF_RANGE",
                    ShouldLog = false
                },
                [typeof(InvalidOperationException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.InvalidStateTransition,
                    ErrorCode = "OPERATION.INVALID_STATE",
                    ShouldLog = false
                },
                [typeof(TimeoutException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.Timeout,
                    ErrorCode = "SYSTEM.TIMEOUT",
                    ShouldLog = true
                },
                [typeof(OperationCanceledException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.OperationCancelled,
                    ErrorCode = "SYSTEM.OPERATION_CANCELLED",
                    ShouldLog = false
                },
                [typeof(TaskCanceledException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.OperationCancelled,
                    ErrorCode = "SYSTEM.TASK_CANCELLED",
                    ShouldLog = false
                },
                [typeof(NotSupportedException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.OperationNotSupported,
                    ErrorCode = "OPERATION.NOT_SUPPORTED",
                    ShouldLog = false
                },
                [typeof(NotImplementedException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.OperationNotSupported,
                    ErrorCode = "OPERATION.NOT_IMPLEMENTED",
                    ShouldLog = true
                },
                [typeof(UnauthorizedAccessException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.Forbidden,
                    ErrorCode = "AUTH.ACCESS_DENIED",
                    ShouldLog = false
                },
                [typeof(KeyNotFoundException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.NotFound,
                    ErrorCode = "RESOURCE.KEY_NOT_FOUND",
                    ShouldLog = false
                },
                [typeof(FormatException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.InvalidFormat,
                    ErrorCode = "VALIDATION.INVALID_FORMAT",
                    ShouldLog = false
                },
                [typeof(OverflowException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.OutOfRange,
                    ErrorCode = "VALIDATION.OVERFLOW",
                    ShouldLog = false
                },
                [typeof(DivideByZeroException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.ValidationFailed,
                    ErrorCode = "VALIDATION.DIVIDE_BY_ZERO",
                    ShouldLog = false
                },
                [typeof(NullReferenceException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.InternalError,
                    ErrorCode = "SYSTEM.NULL_REFERENCE",
                    ShouldLog = true,
                    IncludeDetails = false
                },

                // IO Exceptions
                [typeof(System.IO.IOException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.FileSystemError,
                    ErrorCode = "SYSTEM.IO_ERROR",
                    ShouldLog = true
                },
                [typeof(System.IO.FileNotFoundException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.NotFound,
                    ErrorCode = "RESOURCE.FILE_NOT_FOUND",
                    ShouldLog = false
                },
                [typeof(System.IO.DirectoryNotFoundException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.NotFound,
                    ErrorCode = "RESOURCE.DIRECTORY_NOT_FOUND",
                    ShouldLog = false
                },

                // Network Exceptions
                [typeof(System.Net.WebException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.NetworkError,
                    ErrorCode = "SYSTEM.NETWORK_ERROR",
                    ShouldLog = true
                },
                [typeof(System.Net.Http.HttpRequestException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.ServiceUnavailable,
                    ErrorCode = "SYSTEM.HTTP_REQUEST_ERROR",
                    ShouldLog = true
                },

                // Serialization Exceptions
                [typeof(System.Text.Json.JsonException)] = new ExceptionMapping
                {
                    StatusCode = ResultStatusCode.SerializationError,
                    ErrorCode = "SYSTEM.JSON_ERROR",
                    ShouldLog = false
                }
            };
        }

        #endregion
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using MvpProblemDetailsOptions = Mvp24Hours.WebAPI.Configuration.MvpProblemDetailsOptions;

namespace Mvp24Hours.WebAPI.Exceptions
{
    /// <summary>
    /// Default implementation of <see cref="IExceptionToProblemDetailsMapper"/> that maps
    /// Mvp24Hours exceptions and common exceptions to ProblemDetails responses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This mapper provides default mappings for the following exception types:
    /// <list type="bullet">
    /// <item><see cref="NotFoundException"/> → HTTP 404 Not Found</item>
    /// <item><see cref="ValidationException"/> → HTTP 400 Bad Request</item>
    /// <item><see cref="UnauthorizedException"/> → HTTP 401 Unauthorized</item>
    /// <item><see cref="ForbiddenException"/> → HTTP 403 Forbidden</item>
    /// <item><see cref="ConflictException"/> → HTTP 409 Conflict</item>
    /// <item><see cref="DomainException"/> → HTTP 422 Unprocessable Entity</item>
    /// <item><see cref="BusinessException"/> → HTTP 422 Unprocessable Entity</item>
    /// <item><see cref="ArgumentException"/> → HTTP 400 Bad Request</item>
    /// <item><see cref="ArgumentNullException"/> → HTTP 400 Bad Request</item>
    /// <item><see cref="InvalidOperationException"/> → HTTP 409 Conflict</item>
    /// <item><see cref="UnauthorizedAccessException"/> → HTTP 403 Forbidden</item>
    /// <item><see cref="TimeoutException"/> → HTTP 408 Request Timeout</item>
    /// <item><see cref="OperationCanceledException"/> → HTTP 499 Client Closed Request</item>
    /// <item>Other exceptions → HTTP 500 Internal Server Error</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class DefaultExceptionToProblemDetailsMapper : IExceptionToProblemDetailsMapper
    {
        private readonly MvpProblemDetailsOptions _options;
        private readonly Dictionary<Type, (HttpStatusCode StatusCode, string Title, string Type)> _defaultMappings;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultExceptionToProblemDetailsMapper"/> class.
        /// </summary>
        /// <param name="options">The ProblemDetails configuration options.</param>
        public DefaultExceptionToProblemDetailsMapper(IOptions<MvpProblemDetailsOptions> options)
        {
            _options = options?.Value ?? new MvpProblemDetailsOptions();
            _defaultMappings = InitializeDefaultMappings();
        }

        private Dictionary<Type, (HttpStatusCode StatusCode, string Title, string Type)> InitializeDefaultMappings()
        {
            return new Dictionary<Type, (HttpStatusCode, string, string)>
            {
                // Mvp24Hours Core Exceptions
                [typeof(NotFoundException)] = (HttpStatusCode.NotFound, "Resource Not Found", "not-found"),
                [typeof(ValidationException)] = (HttpStatusCode.BadRequest, "Validation Failed", "validation-error"),
                [typeof(UnauthorizedException)] = (HttpStatusCode.Unauthorized, "Authentication Required", "unauthorized"),
                [typeof(ForbiddenException)] = (HttpStatusCode.Forbidden, "Access Denied", "forbidden"),
                [typeof(ConflictException)] = (HttpStatusCode.Conflict, "Resource Conflict", "conflict"),
                [typeof(DomainException)] = (HttpStatusCode.UnprocessableEntity, "Domain Rule Violation", "domain-error"),
                [typeof(BusinessException)] = (HttpStatusCode.UnprocessableEntity, "Business Rule Violation", "business-error"),
                [typeof(DataException)] = (HttpStatusCode.InternalServerError, "Data Error", "data-error"),
                [typeof(ConfigurationException)] = (HttpStatusCode.InternalServerError, "Configuration Error", "configuration-error"),
                [typeof(PipelineException)] = (HttpStatusCode.InternalServerError, "Pipeline Error", "pipeline-error"),

                // .NET Framework Exceptions
                [typeof(ArgumentException)] = (HttpStatusCode.BadRequest, "Invalid Argument", "invalid-argument"),
                [typeof(ArgumentNullException)] = (HttpStatusCode.BadRequest, "Missing Required Value", "missing-argument"),
                [typeof(ArgumentOutOfRangeException)] = (HttpStatusCode.BadRequest, "Argument Out of Range", "argument-out-of-range"),
                [typeof(InvalidOperationException)] = (HttpStatusCode.Conflict, "Invalid Operation", "invalid-operation"),
                [typeof(UnauthorizedAccessException)] = (HttpStatusCode.Forbidden, "Access Denied", "access-denied"),
                [typeof(TimeoutException)] = (HttpStatusCode.RequestTimeout, "Request Timeout", "timeout"),
                [typeof(OperationCanceledException)] = ((HttpStatusCode)499, "Request Cancelled", "request-cancelled"),
                [typeof(NotImplementedException)] = (HttpStatusCode.NotImplemented, "Not Implemented", "not-implemented"),
                [typeof(NotSupportedException)] = (HttpStatusCode.NotImplemented, "Not Supported", "not-supported"),
                [typeof(KeyNotFoundException)] = (HttpStatusCode.NotFound, "Key Not Found", "key-not-found"),
            };
        }

        /// <inheritdoc />
        public ProblemDetails Map(Exception exception, HttpContext context)
        {
            // Check for custom mapper first
            if (_options.CustomMapper != null)
            {
                var customResult = _options.CustomMapper(exception, context);
                if (customResult != null)
                {
                    return customResult;
                }
            }

            var statusCode = GetStatusCode(exception);
            var (title, type) = GetTitleAndType(exception);

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = GetDetail(exception),
                Type = BuildTypeUri(type),
                Instance = context.Request.Path
            };

            // Add extensions based on exception type
            AddExceptionSpecificExtensions(problemDetails, exception);

            // Add correlation ID
            if (_options.IncludeCorrelationId)
            {
                var correlationId = GetCorrelationId(context);
                if (!string.IsNullOrEmpty(correlationId))
                {
                    problemDetails.Extensions["correlationId"] = correlationId;
                }
            }

            // Add trace ID
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            // Include exception details if configured
            if (_options.IncludeExceptionDetails)
            {
                problemDetails.Extensions["exception"] = new
                {
                    type = exception.GetType().FullName,
                    message = exception.Message,
                    innerException = exception.InnerException?.Message
                };
            }

            // Include stack trace if configured
            if (_options.IncludeStackTrace)
            {
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            }

            // Allow custom enrichment
            _options.EnrichProblemDetails?.Invoke(problemDetails, exception, context);

            return problemDetails;
        }

        /// <inheritdoc />
        public int GetStatusCode(Exception exception)
        {
            // Check custom status code mapper
            if (_options.StatusCodeMapper != null)
            {
                return _options.StatusCodeMapper(exception);
            }

            // Check custom mappings from options
            var exceptionType = exception.GetType();
            if (_options.ExceptionMappings.TryGetValue(exceptionType, out var customStatusCode))
            {
                return (int)customStatusCode;
            }

            // Check default mappings (including base types)
            foreach (var mapping in _defaultMappings)
            {
                if (mapping.Key.IsAssignableFrom(exceptionType))
                {
                    return (int)mapping.Value.StatusCode;
                }
            }

            return _options.FallbackStatusCode;
        }

        /// <inheritdoc />
        public bool CanHandle(Exception exception) => true;

        private (string Title, string Type) GetTitleAndType(Exception exception)
        {
            var exceptionType = exception.GetType();

            // Check default mappings
            foreach (var mapping in _defaultMappings)
            {
                if (mapping.Key.IsAssignableFrom(exceptionType))
                {
                    return (mapping.Value.Title, mapping.Value.Type);
                }
            }

            return (_options.FallbackTitle, "internal-error");
        }

        private string GetDetail(Exception exception)
        {
            if (_options.IncludeExceptionDetails)
            {
                return exception.Message;
            }

            // For safe exceptions (domain exceptions), we can show the message
            if (exception is Mvp24HoursException or ArgumentException or InvalidOperationException)
            {
                return exception.Message;
            }

            return _options.FallbackDetail;
        }

        private string BuildTypeUri(string type)
        {
            if (!string.IsNullOrEmpty(_options.ProblemTypeBaseUri))
            {
                var baseUri = _options.ProblemTypeBaseUri.TrimEnd('/');
                return $"{baseUri}/{type}";
            }

            return $"https://httpstatuses.com/{type}";
        }

        private void AddExceptionSpecificExtensions(ProblemDetails problemDetails, Exception exception)
        {
            switch (exception)
            {
                case NotFoundException notFoundEx:
                    if (notFoundEx.EntityName != null)
                    {
                        problemDetails.Extensions["entityName"] = notFoundEx.EntityName;
                    }
                    if (notFoundEx.EntityId != null)
                    {
                        problemDetails.Extensions["entityId"] = notFoundEx.EntityId;
                    }
                    break;

                case ValidationException validationEx:
                    if (validationEx.ValidationErrors != null && validationEx.ValidationErrors.Count > 0)
                    {
                        var errors = new List<object>();
                        foreach (var error in validationEx.ValidationErrors)
                        {
                            errors.Add(new
                            {
                                key = error.Key,
                                message = error.Message,
                                type = error.Type.ToString()
                            });
                        }
                        problemDetails.Extensions["validationErrors"] = errors;
                    }
                    break;

                case ConflictException conflictEx:
                    if (conflictEx.EntityName != null)
                    {
                        problemDetails.Extensions["entityName"] = conflictEx.EntityName;
                    }
                    if (conflictEx.PropertyName != null)
                    {
                        problemDetails.Extensions["propertyName"] = conflictEx.PropertyName;
                    }
                    if (conflictEx.ConflictingValue != null)
                    {
                        problemDetails.Extensions["conflictingValue"] = conflictEx.ConflictingValue;
                    }
                    break;

                case ForbiddenException forbiddenEx:
                    if (forbiddenEx.ResourceName != null)
                    {
                        problemDetails.Extensions["resourceName"] = forbiddenEx.ResourceName;
                    }
                    if (forbiddenEx.ActionName != null)
                    {
                        problemDetails.Extensions["actionName"] = forbiddenEx.ActionName;
                    }
                    if (forbiddenEx.RequiredPermission != null)
                    {
                        problemDetails.Extensions["requiredPermission"] = forbiddenEx.RequiredPermission;
                    }
                    break;

                case UnauthorizedException unauthorizedEx:
                    if (unauthorizedEx.AuthenticationScheme != null)
                    {
                        problemDetails.Extensions["authenticationScheme"] = unauthorizedEx.AuthenticationScheme;
                    }
                    break;

                case DomainException domainEx:
                    if (domainEx.EntityName != null)
                    {
                        problemDetails.Extensions["entityName"] = domainEx.EntityName;
                    }
                    if (domainEx.RuleName != null)
                    {
                        problemDetails.Extensions["ruleName"] = domainEx.RuleName;
                    }
                    break;

                case Mvp24HoursException mvpEx:
                    if (mvpEx.ErrorCode != null)
                    {
                        problemDetails.Extensions["errorCode"] = mvpEx.ErrorCode;
                    }
                    if (mvpEx.Context != null && mvpEx.Context.Count > 0)
                    {
                        problemDetails.Extensions["context"] = mvpEx.Context;
                    }
                    break;
            }
        }

        private string? GetCorrelationId(HttpContext context)
        {
            // Try to get from header
            if (context.Request.Headers.TryGetValue(_options.CorrelationIdHeaderName, out var headerValue))
            {
                return headerValue.ToString();
            }

            // Try to get from items (might be set by middleware)
            if (context.Items.TryGetValue("CorrelationId", out var itemValue) && itemValue is string correlationId)
            {
                return correlationId;
            }

            return null;
        }
    }
}


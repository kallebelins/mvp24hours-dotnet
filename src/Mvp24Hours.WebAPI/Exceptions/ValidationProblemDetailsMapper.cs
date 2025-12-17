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
    /// Specialized mapper for validation exceptions that produces ValidationProblemDetails.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This mapper handles <see cref="ValidationException"/> and produces a
    /// <see cref="ValidationProblemDetails"/> response that includes detailed
    /// information about each validation error.
    /// </para>
    /// <para>
    /// The response follows the RFC 7807 format extended with the "errors" dictionary
    /// as commonly used in ASP.NET Core validation responses.
    /// </para>
    /// </remarks>
    public class ValidationProblemDetailsMapper : IExceptionToProblemDetailsMapper
    {
        private readonly MvpProblemDetailsOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationProblemDetailsMapper"/> class.
        /// </summary>
        /// <param name="options">The ProblemDetails configuration options.</param>
        public ValidationProblemDetailsMapper(IOptions<MvpProblemDetailsOptions> options)
        {
            _options = options?.Value ?? new MvpProblemDetailsOptions();
        }

        /// <inheritdoc />
        public bool CanHandle(Exception exception)
        {
            return exception is ValidationException;
        }

        /// <inheritdoc />
        public int GetStatusCode(Exception exception)
        {
            return (int)HttpStatusCode.BadRequest;
        }

        /// <inheritdoc />
        public ProblemDetails Map(Exception exception, HttpContext context)
        {
            if (exception is not ValidationException validationException)
            {
                throw new ArgumentException("Exception must be of type ValidationException", nameof(exception));
            }

            var errors = new Dictionary<string, string[]>();

            if (validationException.ValidationErrors != null)
            {
                foreach (var error in validationException.ValidationErrors)
                {
                    var key = error.Key ?? "General";
                    if (!errors.ContainsKey(key))
                    {
                        errors[key] = new[] { error.Message ?? "Validation error" };
                    }
                    else
                    {
                        var existingErrors = errors[key];
                        var newErrors = new string[existingErrors.Length + 1];
                        Array.Copy(existingErrors, newErrors, existingErrors.Length);
                        newErrors[existingErrors.Length] = error.Message ?? "Validation error";
                        errors[key] = newErrors;
                    }
                }
            }

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "One or more validation errors occurred.",
                Detail = validationException.Message,
                Type = BuildTypeUri("validation-error"),
                Instance = context.Request.Path
            };

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

            // Add error code if available
            if (!string.IsNullOrEmpty(validationException.ErrorCode))
            {
                problemDetails.Extensions["errorCode"] = validationException.ErrorCode;
            }

            // Allow custom enrichment
            _options.EnrichProblemDetails?.Invoke(problemDetails, exception, context);

            return problemDetails;
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

        private string? GetCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(_options.CorrelationIdHeaderName, out var headerValue))
            {
                return headerValue.ToString();
            }

            if (context.Items.TryGetValue("CorrelationId", out var itemValue) && itemValue is string correlationId)
            {
                return correlationId;
            }

            return null;
        }
    }
}


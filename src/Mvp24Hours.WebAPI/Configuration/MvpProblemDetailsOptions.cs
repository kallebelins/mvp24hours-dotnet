//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;

namespace Mvp24Hours.WebAPI.Configuration
{
    /// <summary>
    /// Configuration options for ProblemDetails handling following RFC 7807.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ProblemDetails provides a standardized way to communicate errors in HTTP APIs.
    /// This options class allows customization of how exceptions are mapped to ProblemDetails responses.
    /// </para>
    /// <para>
    /// See <see href="https://datatracker.ietf.org/doc/html/rfc7807">RFC 7807</see> for more information.
    /// </para>
    /// <para>
    /// This class is named <c>MvpProblemDetailsOptions</c> to avoid conflict with 
    /// <c>Microsoft.AspNetCore.Http.ProblemDetailsOptions</c>.
    /// </para>
    /// </remarks>
    public class MvpProblemDetailsOptions
    {
        /// <summary>
        /// Gets or sets whether to include exception details in the response.
        /// </summary>
        /// <remarks>
        /// Should be set to false in production environments to avoid exposing sensitive information.
        /// </remarks>
        public bool IncludeExceptionDetails { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include the stack trace in the response.
        /// </summary>
        /// <remarks>
        /// Should be set to false in production environments.
        /// </remarks>
        public bool IncludeStackTrace { get; set; } = false;

        /// <summary>
        /// Gets or sets the base URI for problem type documentation.
        /// </summary>
        /// <remarks>
        /// This URI is used to construct the "type" field of ProblemDetails.
        /// Example: "https://api.example.com/errors/"
        /// </remarks>
        public string? ProblemTypeBaseUri { get; set; }

        /// <summary>
        /// Gets or sets a custom function to determine the HTTP status code for an exception.
        /// </summary>
        /// <remarks>
        /// If not provided, the default mapping will be used:
        /// <list type="bullet">
        /// <item>NotFoundException → 404</item>
        /// <item>ValidationException → 400</item>
        /// <item>UnauthorizedException → 401</item>
        /// <item>ForbiddenException → 403</item>
        /// <item>ConflictException → 409</item>
        /// <item>DomainException → 422</item>
        /// <item>Other exceptions → 500</item>
        /// </list>
        /// </remarks>
        public Func<Exception, int>? StatusCodeMapper { get; set; }

        /// <summary>
        /// Gets or sets a custom function to map exceptions to ProblemDetails.
        /// </summary>
        /// <remarks>
        /// This allows full customization of the ProblemDetails response for specific exceptions.
        /// Return null to use the default mapping.
        /// </remarks>
        public Func<Exception, HttpContext, ProblemDetails?>? CustomMapper { get; set; }

        /// <summary>
        /// Gets or sets the default title for unhandled exceptions.
        /// </summary>
        public string DefaultTitle { get; set; } = "An error occurred while processing your request.";

        /// <summary>
        /// Gets or sets additional metadata to include in all ProblemDetails responses.
        /// </summary>
        public Action<ProblemDetails, Exception, HttpContext>? EnrichProblemDetails { get; set; }

        /// <summary>
        /// Gets or sets custom exception type mappings.
        /// </summary>
        /// <remarks>
        /// Maps exception types to HTTP status codes.
        /// </remarks>
        public Dictionary<Type, HttpStatusCode> ExceptionMappings { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to use RFC 7807 content type (application/problem+json).
        /// </summary>
        public bool UseRfc7807ContentType { get; set; } = true;

        /// <summary>
        /// Gets or sets the fallback status code for unmapped exceptions.
        /// </summary>
        public int FallbackStatusCode { get; set; } = (int)HttpStatusCode.InternalServerError;

        /// <summary>
        /// Gets or sets the fallback title for unmapped exceptions.
        /// </summary>
        public string FallbackTitle { get; set; } = "Internal Server Error";

        /// <summary>
        /// Gets or sets the fallback detail message for unmapped exceptions.
        /// </summary>
        public string FallbackDetail { get; set; } = "An unexpected error has occurred. Please try again later.";

        /// <summary>
        /// Gets or sets whether to log exceptions before returning the response.
        /// </summary>
        public bool LogExceptions { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include correlation ID in the response.
        /// </summary>
        public bool IncludeCorrelationId { get; set; } = true;

        /// <summary>
        /// Gets or sets the header name for correlation ID.
        /// </summary>
        public string CorrelationIdHeaderName { get; set; } = "X-Correlation-ID";
    }
}


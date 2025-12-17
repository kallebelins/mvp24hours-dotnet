//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using MvpProblemDetailsOptions = Mvp24Hours.WebAPI.Configuration.MvpProblemDetailsOptions;

namespace Mvp24Hours.WebAPI.Filters
{
    /// <summary>
    /// Result filter that converts error responses to ProblemDetails format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This filter intercepts action results and converts non-success status codes
    /// to ProblemDetails responses when appropriate.
    /// </para>
    /// <para>
    /// It handles cases where the action returns a status code result directly
    /// without a body, ensuring consistent error responses across the API.
    /// </para>
    /// </remarks>
    public class ProblemDetailsResultFilter : IResultFilter
    {
        private readonly MvpProblemDetailsOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProblemDetailsResultFilter"/> class.
        /// </summary>
        /// <param name="options">The ProblemDetails configuration options.</param>
        public ProblemDetailsResultFilter(IOptions<MvpProblemDetailsOptions> options)
        {
            _options = options?.Value ?? new MvpProblemDetailsOptions();
        }

        /// <summary>
        /// Called before the result executes.
        /// </summary>
        /// <param name="context">The result executing context.</param>
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is ObjectResult objectResult &&
                objectResult.StatusCode >= 400 &&
                objectResult.Value == null)
            {
                var statusCode = objectResult.StatusCode.Value;
                var problemDetails = CreateProblemDetails(statusCode, context.HttpContext);

                objectResult.Value = problemDetails;
                objectResult.ContentTypes.Clear();
                objectResult.ContentTypes.Add(_options.UseRfc7807ContentType
                    ? "application/problem+json"
                    : "application/json");
            }
            else if (context.Result is StatusCodeResult statusCodeResult &&
                     statusCodeResult.StatusCode >= 400)
            {
                var statusCode = statusCodeResult.StatusCode;
                var problemDetails = CreateProblemDetails(statusCode, context.HttpContext);

                context.Result = new ObjectResult(problemDetails)
                {
                    StatusCode = statusCode,
                    ContentTypes = { _options.UseRfc7807ContentType ? "application/problem+json" : "application/json" }
                };
            }
        }

        /// <summary>
        /// Called after the result executes.
        /// </summary>
        /// <param name="context">The result executed context.</param>
        public void OnResultExecuted(ResultExecutedContext context)
        {
            // No action needed after execution
        }

        private ProblemDetails CreateProblemDetails(int statusCode, HttpContext httpContext)
        {
            var (title, type) = GetTitleAndType(statusCode);

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Type = BuildTypeUri(type),
                Instance = httpContext.Request.Path
            };

            // Add correlation ID
            if (_options.IncludeCorrelationId)
            {
                var correlationId = GetCorrelationId(httpContext);
                if (!string.IsNullOrEmpty(correlationId))
                {
                    problemDetails.Extensions["correlationId"] = correlationId;
                }
            }

            // Add trace ID
            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

            return problemDetails;
        }

        private static (string Title, string Type) GetTitleAndType(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => ("Bad Request", "bad-request"),
                StatusCodes.Status401Unauthorized => ("Unauthorized", "unauthorized"),
                StatusCodes.Status403Forbidden => ("Forbidden", "forbidden"),
                StatusCodes.Status404NotFound => ("Not Found", "not-found"),
                StatusCodes.Status405MethodNotAllowed => ("Method Not Allowed", "method-not-allowed"),
                StatusCodes.Status406NotAcceptable => ("Not Acceptable", "not-acceptable"),
                StatusCodes.Status408RequestTimeout => ("Request Timeout", "timeout"),
                StatusCodes.Status409Conflict => ("Conflict", "conflict"),
                StatusCodes.Status410Gone => ("Gone", "gone"),
                StatusCodes.Status413PayloadTooLarge => ("Payload Too Large", "payload-too-large"),
                StatusCodes.Status415UnsupportedMediaType => ("Unsupported Media Type", "unsupported-media-type"),
                StatusCodes.Status422UnprocessableEntity => ("Unprocessable Entity", "unprocessable-entity"),
                StatusCodes.Status429TooManyRequests => ("Too Many Requests", "too-many-requests"),
                StatusCodes.Status500InternalServerError => ("Internal Server Error", "internal-error"),
                StatusCodes.Status501NotImplemented => ("Not Implemented", "not-implemented"),
                StatusCodes.Status502BadGateway => ("Bad Gateway", "bad-gateway"),
                StatusCodes.Status503ServiceUnavailable => ("Service Unavailable", "service-unavailable"),
                StatusCodes.Status504GatewayTimeout => ("Gateway Timeout", "gateway-timeout"),
                _ => ("Error", "error")
            };
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


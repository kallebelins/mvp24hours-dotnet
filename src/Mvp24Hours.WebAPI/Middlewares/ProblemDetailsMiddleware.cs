//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Exceptions;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MvpProblemDetailsOptions = Mvp24Hours.WebAPI.Configuration.MvpProblemDetailsOptions;

namespace Mvp24Hours.WebAPI.Middlewares
{
    /// <summary>
    /// Middleware that handles exceptions and returns RFC 7807 ProblemDetails responses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This middleware catches unhandled exceptions and converts them to standardized
    /// ProblemDetails responses following RFC 7807. It uses <see cref="IExceptionToProblemDetailsMapper"/>
    /// to determine the appropriate HTTP status code and response content.
    /// </para>
    /// <para>
    /// The middleware should be registered early in the pipeline to catch exceptions from
    /// all downstream middleware and handlers.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// app.UseMvp24HoursProblemDetails();
    /// </code>
    /// </example>
    public class ProblemDetailsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IExceptionToProblemDetailsMapper _mapper;
        private readonly MvpProblemDetailsOptions _options;
        private readonly ILogger<ProblemDetailsMiddleware> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProblemDetailsMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="mapper">The exception to ProblemDetails mapper.</param>
        /// <param name="options">The ProblemDetails configuration options.</param>
        /// <param name="logger">The logger instance.</param>
        public ProblemDetailsMiddleware(
            RequestDelegate next,
            IExceptionToProblemDetailsMapper mapper,
            IOptions<MvpProblemDetailsOptions> options,
            ILogger<ProblemDetailsMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _options = options?.Value ?? new MvpProblemDetailsOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("The response has already started. Cannot write ProblemDetails.");
                    throw;
                }

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Log the exception
            if (_options.LogExceptions)
            {
                LogException(exception, context);
            }

            // Map exception to ProblemDetails
            var problemDetails = _mapper.Map(exception, context);

            // Set response properties
            context.Response.StatusCode = problemDetails.Status ?? _options.FallbackStatusCode;

            if (_options.UseRfc7807ContentType)
            {
                context.Response.ContentType = "application/problem+json";
            }
            else
            {
                context.Response.ContentType = "application/json";
            }

            // Add correlation ID to response header if available
            if (_options.IncludeCorrelationId &&
                problemDetails.Extensions.TryGetValue("correlationId", out var correlationId) &&
                correlationId != null)
            {
                context.Response.Headers[_options.CorrelationIdHeaderName] = correlationId.ToString();
            }

            // Write the response
            var json = JsonSerializer.Serialize(problemDetails, _jsonOptions);
            await context.Response.WriteAsync(json);
        }

        private void LogException(Exception exception, HttpContext context)
        {
            var statusCode = _mapper.GetStatusCode(exception);
            var requestPath = context.Request.Path;
            var requestMethod = context.Request.Method;
            var traceId = context.TraceIdentifier;

            if (statusCode >= 500)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception occurred while processing {Method} {Path}. TraceId: {TraceId}",
                    requestMethod,
                    requestPath,
                    traceId);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning(
                    exception,
                    "Client error ({StatusCode}) while processing {Method} {Path}. TraceId: {TraceId}",
                    statusCode,
                    requestMethod,
                    requestPath,
                    traceId);
            }
            else
            {
                _logger.LogInformation(
                    "Exception handled for {Method} {Path}. StatusCode: {StatusCode}. TraceId: {TraceId}",
                    requestMethod,
                    requestPath,
                    statusCode,
                    traceId);
            }
        }
    }
}


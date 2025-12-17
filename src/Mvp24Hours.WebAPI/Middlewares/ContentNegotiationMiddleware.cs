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
using Mvp24Hours.WebAPI.ContentNegotiation;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares
{
    /// <summary>
    /// Middleware that performs content negotiation and transforms responses
    /// based on the Accept header or format parameter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This middleware intercepts responses and transforms them to the format
    /// requested by the client. It works in conjunction with the
    /// <see cref="ContentNegotiationResultFilter"/> for MVC responses.
    /// </para>
    /// <para>
    /// For direct response writing (non-MVC), this middleware will serialize
    /// the response body to the negotiated format.
    /// </para>
    /// </remarks>
    public class ContentNegotiationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ContentNegotiationOptions _options;
        private readonly ILogger<ContentNegotiationMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentNegotiationMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="options">The content negotiation options.</param>
        /// <param name="logger">The logger.</param>
        public ContentNegotiationMiddleware(
            RequestDelegate next,
            IOptions<ContentNegotiationOptions> options,
            ILogger<ContentNegotiationMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options?.Value ?? new ContentNegotiationOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes the HTTP request.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="negotiator">The content negotiator.</param>
        public async Task InvokeAsync(HttpContext context, AcceptHeaderNegotiator negotiator)
        {
            if (!_options.Enabled)
            {
                await _next(context);
                return;
            }

            // Perform content negotiation early
            var negotiationResult = negotiator.Negotiate(context);

            // Handle 406 Not Acceptable
            if (!negotiationResult.Success)
            {
                _logger.LogWarning(
                    "Content negotiation failed for Accept: {AcceptHeader}. Requested: {RequestedMediaType}",
                    context.Request.Headers.Accept.ToString(),
                    negotiationResult.RequestedMediaType);

                await WriteNotAcceptableResponse(context, negotiationResult);
                return;
            }

            // Store negotiation result for use by filters and downstream components
            context.Items["ContentNegotiationResult"] = negotiationResult;
            context.Items["ContentFormatter"] = negotiationResult.Formatter;
            context.Items["NegotiatedMediaType"] = negotiationResult.MediaType;

            await _next(context);

            // Add Vary header if configured
            if (_options.AddVaryHeader && !context.Response.Headers.ContainsKey("Vary"))
            {
                context.Response.Headers.Append("Vary", "Accept");
            }
        }

        private async Task WriteNotAcceptableResponse(HttpContext context, ContentNegotiationResult result)
        {
            context.Response.StatusCode = StatusCodes.Status406NotAcceptable;

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status406NotAcceptable,
                Title = "Not Acceptable",
                Detail = result.ErrorMessage ?? $"The requested media type '{result.RequestedMediaType}' is not supported.",
                Type = "https://httpstatuses.com/406",
                Instance = context.Request.Path
            };

            // Default to JSON for error response
            context.Response.ContentType = "application/problem+json; charset=utf-8";
            
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(problemDetails, jsonOptions),
                Encoding.UTF8);
        }
    }

    /// <summary>
    /// Middleware that transforms response bodies to the negotiated content format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This middleware intercepts response bodies and transforms them to the format
    /// negotiated by <see cref="ContentNegotiationMiddleware"/>.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> This middleware should be placed after
    /// <see cref="ContentNegotiationMiddleware"/> in the pipeline.
    /// </para>
    /// </remarks>
    public class ResponseTransformMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ContentNegotiationOptions _options;
        private readonly ILogger<ResponseTransformMiddleware> _logger;
        private readonly IContentFormatterRegistry _registry;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseTransformMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="options">The content negotiation options.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="registry">The formatter registry.</param>
        public ResponseTransformMiddleware(
            RequestDelegate next,
            IOptions<ContentNegotiationOptions> options,
            ILogger<ResponseTransformMiddleware> logger,
            IContentFormatterRegistry registry)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options?.Value ?? new ContentNegotiationOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// Processes the HTTP request and transforms the response body if needed.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            if (!_options.Enabled)
            {
                await _next(context);
                return;
            }

            // Get the negotiated formatter from context
            if (context.Items.TryGetValue("ContentFormatter", out var formatterObj) &&
                formatterObj is IContentFormatter formatter)
            {
                // Check if we need to transform the response
                var originalContentType = context.Response.ContentType;
                
                // Only transform if content types don't match
                var normalizedContentType = NormalizeContentType(originalContentType);
                var contentTypeMatches = false;
                foreach (var mt in formatter.SupportedMediaTypes)
                {
                    if (mt == normalizedContentType)
                    {
                        contentTypeMatches = true;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(originalContentType) && !contentTypeMatches)
                {
                    // Capture the response for transformation
                    var originalBody = context.Response.Body;
                    using var newBody = new MemoryStream();
                    context.Response.Body = newBody;

                    try
                    {
                        await _next(context);

                        // Transform if needed
                        newBody.Seek(0, SeekOrigin.Begin);
                        var responseContent = await new StreamReader(newBody).ReadToEndAsync();

                        // Set new content type
                        context.Response.ContentType = formatter.GetContentType(_options.Charset);
                        
                        // Copy to original body
                        newBody.Seek(0, SeekOrigin.Begin);
                        await newBody.CopyToAsync(originalBody);
                    }
                    finally
                    {
                        context.Response.Body = originalBody;
                    }

                    return;
                }
            }

            await _next(context);
        }

        private static string NormalizeContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return string.Empty;
            }

            var semicolonIndex = contentType.IndexOf(';');
            return semicolonIndex >= 0
                ? contentType.Substring(0, semicolonIndex).Trim().ToLowerInvariant()
                : contentType.Trim().ToLowerInvariant();
        }
    }
}


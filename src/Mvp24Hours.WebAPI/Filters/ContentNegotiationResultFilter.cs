//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.ContentNegotiation;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Filters
{
    /// <summary>
    /// Result filter that applies content negotiation to action results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This filter intercepts action results and converts them to the format
    /// requested by the client via Accept header or format parameter.
    /// </para>
    /// <para>
    /// Supported formats include JSON, XML, and custom formats registered
    /// in the <see cref="IContentFormatterRegistry"/>.
    /// </para>
    /// </remarks>
    public class ContentNegotiationResultFilter : IAsyncResultFilter
    {
        private readonly AcceptHeaderNegotiator _negotiator;
        private readonly ContentNegotiationOptions _options;
        private readonly ILogger<ContentNegotiationResultFilter> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentNegotiationResultFilter"/> class.
        /// </summary>
        /// <param name="negotiator">The content negotiator.</param>
        /// <param name="options">The content negotiation options.</param>
        /// <param name="logger">The logger.</param>
        public ContentNegotiationResultFilter(
            AcceptHeaderNegotiator negotiator,
            IOptions<ContentNegotiationOptions> options,
            ILogger<ContentNegotiationResultFilter> logger)
        {
            _negotiator = negotiator ?? throw new ArgumentNullException(nameof(negotiator));
            _options = options?.Value ?? new ContentNegotiationOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Called asynchronously before the action result executes.
        /// </summary>
        /// <param name="context">The result executing context.</param>
        /// <param name="next">The delegate to execute the next filter or the result itself.</param>
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (!_options.Enabled)
            {
                await next();
                return;
            }

            // Perform content negotiation
            var negotiationResult = _negotiator.Negotiate(context.HttpContext);

            // Handle negotiation failure (406 Not Acceptable)
            if (!negotiationResult.Success)
            {
                _logger.LogWarning(
                    "Content negotiation failed for media type {MediaType}",
                    negotiationResult.RequestedMediaType);

                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status406NotAcceptable,
                    Title = "Not Acceptable",
                    Detail = negotiationResult.ErrorMessage,
                    Type = "https://httpstatuses.com/406"
                })
                {
                    StatusCode = StatusCodes.Status406NotAcceptable
                };

                await next();
                return;
            }

            var formatter = negotiationResult.Formatter!;

            // Store the formatter in HttpContext for downstream use
            context.HttpContext.Items["ContentFormatter"] = formatter;
            context.HttpContext.Items["NegotiatedMediaType"] = negotiationResult.MediaType;

            // Process object results
            if (context.Result is ObjectResult objectResult)
            {
                await ProcessObjectResult(context, objectResult, formatter, next);
            }
            else
            {
                await next();
            }

            // Add Vary header if configured
            if (_options.AddVaryHeader && !context.HttpContext.Response.Headers.ContainsKey("Vary"))
            {
                context.HttpContext.Response.Headers.Append("Vary", "Accept");
            }
        }

        private async Task ProcessObjectResult(
            ResultExecutingContext context,
            ObjectResult objectResult,
            IContentFormatter formatter,
            ResultExecutionDelegate next)
        {
            var value = objectResult.Value;
            var response = context.HttpContext.Response;

            // Handle ProblemDetails specially
            if (value is ProblemDetails problemDetails && formatter is IProblemDetailsFormatter pdFormatter)
            {
                var contentType = pdFormatter.GetProblemDetailsContentType(_options.Charset);
                objectResult.ContentTypes.Clear();
                objectResult.ContentTypes.Add(contentType);

                await next();
                return;
            }

            // Set content type
            var mediaType = formatter.GetContentType(_options.Charset);
            objectResult.ContentTypes.Clear();
            objectResult.ContentTypes.Add(mediaType);

            await next();
        }
    }

    /// <summary>
    /// Action filter that validates Accept header and returns 406 if not acceptable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this filter on specific actions that require strict content negotiation.
    /// The filter will return 406 Not Acceptable if the client requests an unsupported format.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireAcceptableMediaTypeAttribute : ActionFilterAttribute
    {
        private readonly string[] _acceptableMediaTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireAcceptableMediaTypeAttribute"/> class.
        /// </summary>
        /// <param name="acceptableMediaTypes">The acceptable media types.</param>
        public RequireAcceptableMediaTypeAttribute(params string[] acceptableMediaTypes)
        {
            _acceptableMediaTypes = acceptableMediaTypes ?? new[] { "application/json", "application/xml" };
        }

        /// <inheritdoc />
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var acceptHeader = context.HttpContext.Request.Headers.Accept.ToString();

            if (string.IsNullOrEmpty(acceptHeader) || acceptHeader == "*/*")
            {
                // Accept any format or no preference
                base.OnActionExecuting(context);
                return;
            }

            var acceptedTypes = AcceptHeaderNegotiator.ParseAcceptHeader(acceptHeader);

            foreach (var accepted in acceptedTypes)
            {
                foreach (var acceptable in _acceptableMediaTypes)
                {
                    if (IsMediaTypeMatch(accepted.MediaType, acceptable))
                    {
                        base.OnActionExecuting(context);
                        return;
                    }
                }
            }

            // No acceptable media type found
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status406NotAcceptable,
                Title = "Not Acceptable",
                Detail = $"This endpoint only accepts: {string.Join(", ", _acceptableMediaTypes)}",
                Type = "https://httpstatuses.com/406"
            })
            {
                StatusCode = StatusCodes.Status406NotAcceptable,
                ContentTypes = { "application/problem+json" }
            };
        }

        private static bool IsMediaTypeMatch(string requested, string acceptable)
        {
            if (requested.Equals(acceptable, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (requested == "*/*")
            {
                return true;
            }

            if (requested.EndsWith("/*", StringComparison.OrdinalIgnoreCase))
            {
                var typePrefix = requested.Substring(0, requested.Length - 2);
                return acceptable.StartsWith(typePrefix + "/", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }

    /// <summary>
    /// Attribute to specify that an action produces a specific content type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class ProducesContentTypeAttribute : Attribute
    {
        /// <summary>
        /// Gets the content type.
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// Gets or sets whether this is the default content type.
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProducesContentTypeAttribute"/> class.
        /// </summary>
        /// <param name="contentType">The content type.</param>
        public ProducesContentTypeAttribute(string contentType)
        {
            ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        }
    }
}


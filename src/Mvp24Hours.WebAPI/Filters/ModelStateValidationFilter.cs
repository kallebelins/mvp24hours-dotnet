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
using System.Collections.Generic;
using System.Linq;
using MvpProblemDetailsOptions = Mvp24Hours.WebAPI.Configuration.MvpProblemDetailsOptions;

namespace Mvp24Hours.WebAPI.Filters
{
    /// <summary>
    /// Action filter that validates model state and returns ProblemDetails for invalid models.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This filter automatically validates the model state before the action executes.
    /// If the model state is invalid, it returns a <see cref="ValidationProblemDetails"/>
    /// response following RFC 7807.
    /// </para>
    /// <para>
    /// The filter integrates with the ProblemDetails configuration options to ensure
    /// consistent error responses across the API.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register globally in Program.cs
    /// services.AddControllers(options =>
    /// {
    ///     options.Filters.Add&lt;ModelStateValidationFilter&gt;();
    /// });
    /// 
    /// // Or apply to specific controllers/actions
    /// [ServiceFilter(typeof(ModelStateValidationFilter))]
    /// public class MyController : ControllerBase { }
    /// </code>
    /// </example>
    public class ModelStateValidationFilter : IActionFilter
    {
        private readonly MvpProblemDetailsOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelStateValidationFilter"/> class.
        /// </summary>
        /// <param name="options">The ProblemDetails configuration options.</param>
        public ModelStateValidationFilter(IOptions<MvpProblemDetailsOptions> options)
        {
            _options = options?.Value ?? new MvpProblemDetailsOptions();
        }

        /// <summary>
        /// Called before the action executes, after model binding is complete.
        /// </summary>
        /// <param name="context">The action executing context.</param>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = new Dictionary<string, string[]>();

                foreach (var key in context.ModelState.Keys)
                {
                    var state = context.ModelState[key];
                    if (state?.Errors != null && state.Errors.Count > 0)
                    {
                        errors[key] = state.Errors
                            .Select(e => !string.IsNullOrEmpty(e.ErrorMessage)
                                ? e.ErrorMessage
                                : e.Exception?.Message ?? "Invalid value")
                            .ToArray();
                    }
                }

                var problemDetails = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Type = BuildTypeUri("validation-error"),
                    Instance = context.HttpContext.Request.Path
                };

                // Add correlation ID
                if (_options.IncludeCorrelationId)
                {
                    var correlationId = GetCorrelationId(context.HttpContext);
                    if (!string.IsNullOrEmpty(correlationId))
                    {
                        problemDetails.Extensions["correlationId"] = correlationId;
                    }
                }

                // Add trace ID
                problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                context.Result = new BadRequestObjectResult(problemDetails)
                {
                    ContentTypes = { _options.UseRfc7807ContentType ? "application/problem+json" : "application/json" }
                };
            }
        }

        /// <summary>
        /// Called after the action executes, before the result executes.
        /// </summary>
        /// <param name="context">The action executed context.</param>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed after execution
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


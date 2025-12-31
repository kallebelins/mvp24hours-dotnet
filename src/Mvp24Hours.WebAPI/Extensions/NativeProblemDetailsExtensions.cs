//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace Mvp24Hours.WebAPI.Extensions;

/// <summary>
/// Extension methods for configuring native ASP.NET Core ProblemDetails support (RFC 7807).
/// </summary>
/// <remarks>
/// <para>
/// This provides integration with the built-in <see cref="IProblemDetailsService"/> from .NET 8/9,
/// which offers:
/// <list type="bullet">
/// <item>Automatic exception handling with consistent ProblemDetails responses</item>
/// <item>Integration with ASP.NET Core's exception handling pipeline</item>
/// <item>Support for both MVC and Minimal APIs</item>
/// <item>Customizable exception-to-status-code mappings</item>
/// </list>
/// </para>
/// <para>
/// This is the recommended approach for .NET 8+ applications. For applications using
/// the custom middleware approach, see <see cref="ServiceCollectionExtentions.AddMvp24HoursProblemDetails"/>.
/// </para>
/// </remarks>
public static class NativeProblemDetailsExtensions
{
    /// <summary>
    /// Adds native ASP.NET Core ProblemDetails support with Mvp24Hours exception mappings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method uses the built-in <see cref="IProblemDetailsService"/> and configures it
    /// with Mvp24Hours-specific exception mappings for consistent error handling.
    /// </para>
    /// <para>
    /// The native ProblemDetails service is the recommended approach for .NET 8/9 applications
    /// as it provides:
    /// <list type="bullet">
    /// <item>Better integration with ASP.NET Core middleware</item>
    /// <item>Automatic content negotiation</item>
    /// <item>Status code pages integration</item>
    /// <item>Exception handler middleware integration</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Program.cs
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// // Add native ProblemDetails
    /// builder.Services.AddNativeProblemDetails(options =>
    /// {
    ///     options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    ///     options.ProblemTypeBaseUri = "https://api.example.com/errors";
    /// });
    /// 
    /// var app = builder.Build();
    /// 
    /// // Use exception handler with ProblemDetails
    /// app.UseExceptionHandler();
    /// app.UseStatusCodePages();
    /// </code>
    /// </example>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure ProblemDetails options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNativeProblemDetails(
        this IServiceCollection services,
        Action<MvpProblemDetailsOptions>? configureOptions = null)
    {
        // Configure Mvp24Hours options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<MvpProblemDetailsOptions>(_ => { });
        }

        // Register Mvp24Hours mappers
        services.AddSingleton<DefaultExceptionToProblemDetailsMapper>();
        services.AddSingleton<ValidationProblemDetailsMapper>();
        services.AddSingleton<IExceptionToProblemDetailsMapper, DefaultExceptionToProblemDetailsMapper>();

        // Add native ProblemDetails service
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var problemDetails = context.ProblemDetails;
                var httpContext = context.HttpContext;

                // Add trace ID
                problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

                // Add correlation ID if available
                if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) &&
                    !string.IsNullOrEmpty(correlationId))
                {
                    problemDetails.Extensions["correlationId"] = correlationId.ToString();
                }
                else if (httpContext.Items.TryGetValue("CorrelationId", out var itemValue) &&
                         itemValue is string correlationIdValue)
                {
                    problemDetails.Extensions["correlationId"] = correlationIdValue;
                }

                // Add instance path
                problemDetails.Instance ??= httpContext.Request.Path;

                // Add request ID
                if (!string.IsNullOrEmpty(httpContext.TraceIdentifier))
                {
                    problemDetails.Extensions["requestId"] = httpContext.TraceIdentifier;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Adds native ProblemDetails with full Mvp24Hours integration including exception handling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a convenience method that combines:
    /// <list type="bullet">
    /// <item>Native ProblemDetails registration</item>
    /// <item>Exception handler configuration</item>
    /// <item>Status code pages configuration</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple setup in Program.cs
    /// builder.Services.AddNativeProblemDetailsAll(builder.Environment);
    /// 
    /// var app = builder.Build();
    /// app.UseNativeProblemDetailsHandling();
    /// </code>
    /// </example>
    /// <param name="services">The service collection.</param>
    /// <param name="environment">The host environment for determining exception detail exposure.</param>
    /// <param name="configureOptions">Optional action to configure additional options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNativeProblemDetailsAll(
        this IServiceCollection services,
        IHostEnvironment environment,
        Action<MvpProblemDetailsOptions>? configureOptions = null)
    {
        services.AddNativeProblemDetails(options =>
        {
            options.IncludeExceptionDetails = environment.IsDevelopment();
            options.IncludeStackTrace = environment.IsDevelopment();
            configureOptions?.Invoke(options);
        });

        return services;
    }

    /// <summary>
    /// Configures the application to use native ProblemDetails exception handling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method configures:
    /// <list type="bullet">
    /// <item>Exception handler middleware with ProblemDetails responses</item>
    /// <item>Status code pages for non-exception errors</item>
    /// <item>Mvp24Hours exception-to-HTTP-status-code mapping</item>
    /// </list>
    /// </para>
    /// <para>
    /// Should be called early in the middleware pipeline, before routing.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// 
    /// // Add exception handling early in the pipeline
    /// app.UseNativeProblemDetailsHandling();
    /// 
    /// app.UseRouting();
    /// app.MapControllers();
    /// </code>
    /// </example>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseNativeProblemDetailsHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(exceptionApp =>
        {
            exceptionApp.Run(async context =>
            {
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionHandlerFeature?.Error;

                if (exception is null)
                {
                    return;
                }

                var problemDetailsService = context.RequestServices.GetService<IProblemDetailsService>();
                var mapper = context.RequestServices.GetService<IExceptionToProblemDetailsMapper>();
                var options = context.RequestServices.GetService<IOptions<MvpProblemDetailsOptions>>()?.Value
                    ?? new MvpProblemDetailsOptions();
                var logger = context.RequestServices.GetService<ILogger<IExceptionHandlerFeature>>();

                // Log the exception
                LogException(logger, exception, context, options);

                // Get status code from mapper or use default
                var statusCode = mapper?.GetStatusCode(exception) ?? GetDefaultStatusCode(exception);
                context.Response.StatusCode = statusCode;

                // Try to use IProblemDetailsService
                if (problemDetailsService is not null)
                {
                    var problemDetails = CreateProblemDetails(exception, context, statusCode, options, mapper);

                    await problemDetailsService.WriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = context,
                        ProblemDetails = problemDetails
                    });
                }
                else
                {
                    // Fallback: write ProblemDetails directly
                    var problemDetails = CreateProblemDetails(exception, context, statusCode, options, mapper);
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsJsonAsync(problemDetails);
                }
            });
        });

        // Add status code pages for non-exception errors
        app.UseStatusCodePages(async context =>
        {
            var problemDetailsService = context.HttpContext.RequestServices.GetService<IProblemDetailsService>();

            if (problemDetailsService is not null)
            {
                var statusCode = context.HttpContext.Response.StatusCode;
                var problemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = GetTitleForStatusCode(statusCode),
                    Type = GetTypeForStatusCode(statusCode),
                    Instance = context.HttpContext.Request.Path
                };

                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = problemDetails
                });
            }
        });

        return app;
    }

    /// <summary>
    /// Configures the WebApplication to use native ProblemDetails exception handling.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseNativeProblemDetailsHandling(this WebApplication app)
    {
        ((IApplicationBuilder)app).UseNativeProblemDetailsHandling();
        return app;
    }

    private static ProblemDetails CreateProblemDetails(
        Exception exception,
        HttpContext context,
        int statusCode,
        MvpProblemDetailsOptions options,
        IExceptionToProblemDetailsMapper? mapper)
    {
        // If we have a mapper, use it
        if (mapper is not null)
        {
            return mapper.Map(exception, context);
        }

        // Otherwise, create a basic ProblemDetails
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitleForException(exception),
            Detail = GetDetailForException(exception, options),
            Type = GetTypeForException(exception, options),
            Instance = context.Request.Path
        };

        // Add trace ID
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        // Add exception details if configured
        if (options.IncludeExceptionDetails)
        {
            problemDetails.Extensions["exception"] = new
            {
                type = exception.GetType().FullName,
                message = exception.Message,
                innerException = exception.InnerException?.Message
            };
        }

        if (options.IncludeStackTrace)
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        // Add exception-specific extensions
        AddExceptionSpecificExtensions(problemDetails, exception);

        return problemDetails;
    }

    private static void AddExceptionSpecificExtensions(ProblemDetails problemDetails, Exception exception)
    {
        switch (exception)
        {
            case NotFoundException notFoundEx:
                if (notFoundEx.EntityName != null)
                    problemDetails.Extensions["entityName"] = notFoundEx.EntityName;
                if (notFoundEx.EntityId != null)
                    problemDetails.Extensions["entityId"] = notFoundEx.EntityId;
                break;

            case ValidationException validationEx:
                if (validationEx.ValidationErrors?.Count > 0)
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
                    problemDetails.Extensions["entityName"] = conflictEx.EntityName;
                if (conflictEx.PropertyName != null)
                    problemDetails.Extensions["propertyName"] = conflictEx.PropertyName;
                if (conflictEx.ConflictingValue != null)
                    problemDetails.Extensions["conflictingValue"] = conflictEx.ConflictingValue;
                break;

            case ForbiddenException forbiddenEx:
                if (forbiddenEx.ResourceName != null)
                    problemDetails.Extensions["resourceName"] = forbiddenEx.ResourceName;
                if (forbiddenEx.ActionName != null)
                    problemDetails.Extensions["actionName"] = forbiddenEx.ActionName;
                if (forbiddenEx.RequiredPermission != null)
                    problemDetails.Extensions["requiredPermission"] = forbiddenEx.RequiredPermission;
                break;

            case UnauthorizedException unauthorizedEx:
                if (unauthorizedEx.AuthenticationScheme != null)
                    problemDetails.Extensions["authenticationScheme"] = unauthorizedEx.AuthenticationScheme;
                break;

            case DomainException domainEx:
                if (domainEx.EntityName != null)
                    problemDetails.Extensions["entityName"] = domainEx.EntityName;
                if (domainEx.RuleName != null)
                    problemDetails.Extensions["ruleName"] = domainEx.RuleName;
                break;

            case Mvp24HoursException mvpEx:
                if (mvpEx.ErrorCode != null)
                    problemDetails.Extensions["errorCode"] = mvpEx.ErrorCode;
                if (mvpEx.Context?.Count > 0)
                    problemDetails.Extensions["context"] = mvpEx.Context;
                break;
        }
    }

    private static int GetDefaultStatusCode(Exception exception)
    {
        return exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            ConflictException => StatusCodes.Status409Conflict,
            DomainException => StatusCodes.Status422UnprocessableEntity,
            BusinessException => StatusCodes.Status422UnprocessableEntity,
            ArgumentNullException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status409Conflict,
            TimeoutException => StatusCodes.Status408RequestTimeout,
            OperationCanceledException => 499, // Client Closed Request
            NotImplementedException => StatusCodes.Status501NotImplemented,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string GetTitleForException(Exception exception)
    {
        return exception switch
        {
            NotFoundException => "Resource Not Found",
            ValidationException => "Validation Failed",
            UnauthorizedException => "Authentication Required",
            ForbiddenException => "Access Denied",
            ConflictException => "Resource Conflict",
            DomainException => "Domain Rule Violation",
            BusinessException => "Business Rule Violation",
            ArgumentNullException => "Missing Required Value",
            ArgumentException => "Invalid Argument",
            InvalidOperationException => "Invalid Operation",
            TimeoutException => "Request Timeout",
            OperationCanceledException => "Request Cancelled",
            NotImplementedException => "Not Implemented",
            _ => "Internal Server Error"
        };
    }

    private static string GetDetailForException(Exception exception, MvpProblemDetailsOptions options)
    {
        if (options.IncludeExceptionDetails)
        {
            return exception.Message;
        }

        // For safe exceptions, show the message
        if (exception is Mvp24HoursException or ArgumentException or InvalidOperationException)
        {
            return exception.Message;
        }

        return options.FallbackDetail;
    }

    private static string GetTypeForException(Exception exception, MvpProblemDetailsOptions options)
    {
        var type = exception switch
        {
            NotFoundException => "not-found",
            ValidationException => "validation-error",
            UnauthorizedException => "unauthorized",
            ForbiddenException => "forbidden",
            ConflictException => "conflict",
            DomainException => "domain-error",
            BusinessException => "business-error",
            ArgumentException => "invalid-argument",
            InvalidOperationException => "invalid-operation",
            TimeoutException => "timeout",
            OperationCanceledException => "request-cancelled",
            NotImplementedException => "not-implemented",
            _ => "internal-error"
        };

        if (!string.IsNullOrEmpty(options.ProblemTypeBaseUri))
        {
            return $"{options.ProblemTypeBaseUri.TrimEnd('/')}/{type}";
        }

        return $"https://httpstatuses.com/{type}";
    }

    private static string GetTitleForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            405 => "Method Not Allowed",
            408 => "Request Timeout",
            409 => "Conflict",
            415 => "Unsupported Media Type",
            422 => "Unprocessable Entity",
            429 => "Too Many Requests",
            500 => "Internal Server Error",
            501 => "Not Implemented",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            504 => "Gateway Timeout",
            _ => "Error"
        };
    }

    private static string GetTypeForStatusCode(int statusCode)
    {
        return $"https://httpstatuses.com/{statusCode}";
    }

    private static void LogException(
        ILogger? logger,
        Exception exception,
        HttpContext context,
        MvpProblemDetailsOptions options)
    {
        if (logger is null || !options.LogExceptions)
        {
            return;
        }

        var statusCode = GetDefaultStatusCode(exception);
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var traceId = context.TraceIdentifier;

        if (statusCode >= 500)
        {
            logger.LogError(
                exception,
                "Unhandled exception occurred while processing {Method} {Path}. TraceId: {TraceId}",
                requestMethod,
                requestPath,
                traceId);
        }
        else if (statusCode >= 400)
        {
            logger.LogWarning(
                exception,
                "Client error ({StatusCode}) while processing {Method} {Path}. TraceId: {TraceId}",
                statusCode,
                requestMethod,
                requestPath,
                traceId);
        }
        else
        {
            logger.LogInformation(
                "Exception handled for {Method} {Path}. StatusCode: {StatusCode}. TraceId: {TraceId}",
                requestMethod,
                requestPath,
                statusCode,
                traceId);
        }
    }
}


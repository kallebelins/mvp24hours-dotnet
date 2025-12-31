//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mvp24Hours.WebAPI.Endpoints;

/// <summary>
/// Extension methods for converting <see cref="IBusinessResult{T}"/> to <see cref="TypedResults"/>.
/// Provides type-safe conversion from business results to HTTP responses.
/// </summary>
/// <remarks>
/// <para>
/// These extensions automatically convert BusinessResult to appropriate HTTP responses:
/// <list type="bullet">
/// <item>Success results → 200 OK with data</item>
/// <item>Results with errors → 400 Bad Request with error details</item>
/// <item>Results with null data → 404 Not Found (for queries)</item>
/// <item>Results with specific error codes → Appropriate HTTP status codes</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In an endpoint handler
/// var result = await _sender.SendAsync&lt;IBusinessResult&lt;OrderDto&gt;&gt;(command);
/// return result.ToTypedResult();
/// 
/// // Or use directly in MapCommandWithResult/MapQueryWithResult
/// app.MapCommandWithResult&lt;CreateOrderCommand, OrderDto&gt;("/api/orders");
/// </code>
/// </example>
public static class TypedResultsExtensions
{
    /// <summary>
    /// Converts a <see cref="IBusinessResult{T}"/> to an appropriate HTTP response.
    /// </summary>
    /// <typeparam name="T">The type of data in the result.</typeparam>
    /// <param name="result">The business result to convert.</param>
    /// <param name="allowNullData">Whether to allow null data (default: false). If false, null data returns 404.</param>
    /// <returns>A typed HTTP result (Ok, BadRequest, NotFound, etc.).</returns>
    /// <remarks>
    /// <para>
    /// Conversion rules:
    /// <list type="bullet">
    /// <item>If result has errors → 400 Bad Request with ProblemDetails</item>
    /// <item>If result is successful but data is null and allowNullData is false → 404 Not Found</item>
    /// <item>If result is successful with data → 200 OK</item>
    /// <item>If result has specific error codes (NOT_FOUND, CONFLICT, etc.) → Appropriate status codes</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await GetOrderAsync(id);
    /// return result.ToTypedResult(); // Returns Ok or NotFound automatically
    /// </code>
    /// </example>
    public static IResult ToTypedResult<T>(this IBusinessResult<T> result, bool allowNullData = false)
    {
        ArgumentNullException.ThrowIfNull(result);

        // If result has errors, return BadRequest with error details
        if (result.HasErrors)
        {
            return MapErrorsToResult(result);
        }

        // If result is successful but data is null
        if (result.Data is null && !allowNullData)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Resource not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        // Success with data
        return Results.Ok(result.Data);
    }

    /// <summary>
    /// Converts a <see cref="IBusinessResult{T}"/> to an HTTP response, allowing null data.
    /// Useful for queries that may legitimately return null.
    /// </summary>
    /// <typeparam name="T">The type of data in the result.</typeparam>
    /// <param name="result">The business result to convert.</param>
    /// <returns>A typed HTTP result.</returns>
    /// <remarks>
    /// <para>
    /// This overload allows null data, returning 200 OK even if data is null.
    /// Use this for queries where null is a valid response.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await CheckOrderExistsAsync(id);
    /// return result.ToTypedResultAllowNull(); // Returns Ok even if data is null
    /// </code>
    /// </example>
    public static IResult ToTypedResultAllowNull<T>(this IBusinessResult<T> result)
    {
        return result.ToTypedResult(allowNullData: true);
    }

    /// <summary>
    /// Maps business result errors to appropriate HTTP status codes and ProblemDetails.
    /// </summary>
    /// <typeparam name="T">The type of data in the result.</typeparam>
    /// <param name="result">The business result with errors.</param>
    /// <returns>An HTTP result with appropriate status code.</returns>
    private static IResult MapErrorsToResult<T>(IBusinessResult<T> result)
    {
        if (result.Messages is null || !result.Messages.Any())
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "An error occurred",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Check for specific error codes that map to HTTP status codes
        var errorMessages = result.Messages.Where(m => m.Type == MessageType.Error).ToList();

        // Check for structured messages with error codes
        var structuredMessages = errorMessages.OfType<Mvp24Hours.Core.Contract.ValueObjects.Logic.IStructuredMessageResult>().ToList();
        
        // Check for NOT_FOUND error code
        if (structuredMessages.Any(m => m.ErrorCode == "NOT_FOUND" || m.ErrorCode?.Contains("NOT_FOUND") == true) ||
            errorMessages.Any(m => m.Key == "NOT_FOUND" || m.CustomType == "NOT_FOUND"))
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Resource not found",
                Detail = string.Join("; ", errorMessages.Select(m => m.Message)),
                Status = StatusCodes.Status404NotFound
            });
        }

        // Check for CONFLICT error code
        if (structuredMessages.Any(m => m.ErrorCode == "CONFLICT" || m.ErrorCode?.Contains("CONFLICT") == true) ||
            errorMessages.Any(m => m.Key == "CONFLICT" || m.CustomType == "CONFLICT"))
        {
            return Results.Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Detail = string.Join("; ", errorMessages.Select(m => m.Message)),
                Status = StatusCodes.Status409Conflict
            });
        }

        // Check for UNAUTHORIZED error code
        if (structuredMessages.Any(m => m.ErrorCode == "UNAUTHORIZED" || m.ErrorCode?.Contains("UNAUTHORIZED") == true) ||
            errorMessages.Any(m => m.Key == "UNAUTHORIZED" || m.CustomType == "UNAUTHORIZED"))
        {
            return Results.Unauthorized();
        }

        // Check for FORBIDDEN error code
        if (structuredMessages.Any(m => m.ErrorCode == "FORBIDDEN" || m.ErrorCode?.Contains("FORBIDDEN") == true) ||
            errorMessages.Any(m => m.Key == "FORBIDDEN" || m.CustomType == "FORBIDDEN"))
        {
            return Results.Forbid();
        }

        // Check for VALIDATION error code
        if (structuredMessages.Any(m => m.ErrorCode == "VALIDATION" || m.ErrorCode?.Contains("VALIDATION") == true) ||
            errorMessages.Any(m => m.Key == "VALIDATION" || m.CustomType == "VALIDATION"))
        {
            var validationErrors = structuredMessages
                .Where(m => !string.IsNullOrEmpty(m.PropertyName))
                .GroupBy(m => m.PropertyName!)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(m => m.Message ?? string.Empty).ToArray()
                );

            if (validationErrors.Any())
            {
                return Results.ValidationProblem(validationErrors);
            }
        }

        // Default: BadRequest with all error messages
        return Results.BadRequest(new ProblemDetails
        {
            Title = "Validation failed",
            Detail = string.Join("; ", errorMessages.Select(m => m.Message)),
            Status = StatusCodes.Status400BadRequest,
            Extensions =
            {
                ["errors"] = errorMessages.Select(m => new
                {
                    code = (m as Mvp24Hours.Core.Contract.ValueObjects.Logic.IStructuredMessageResult)?.ErrorCode ?? m.Key ?? m.CustomType,
                    message = m.Message,
                    property = (m as Mvp24Hours.Core.Contract.ValueObjects.Logic.IStructuredMessageResult)?.PropertyName ?? m.Key
                }).ToArray()
            }
        });
    }

    #region TypedResults.Problem() Extensions

    /// <summary>
    /// Converts an exception to a ProblemDetails result using <see cref="TypedResults.Problem"/>.
    /// </summary>
    /// <param name="exception">The exception to convert.</param>
    /// <param name="includeDetails">Whether to include exception details (default: false).</param>
    /// <param name="instance">The request instance path (optional).</param>
    /// <returns>A ProblemHttpResult with appropriate status code.</returns>
    /// <remarks>
    /// <para>
    /// This method maps common Mvp24Hours exceptions to appropriate HTTP status codes:
    /// <list type="bullet">
    /// <item><see cref="NotFoundException"/> → 404 Not Found</item>
    /// <item><see cref="ValidationException"/> → 400 Bad Request</item>
    /// <item><see cref="UnauthorizedException"/> → 401 Unauthorized</item>
    /// <item><see cref="ForbiddenException"/> → 403 Forbidden</item>
    /// <item><see cref="ConflictException"/> → 409 Conflict</item>
    /// <item><see cref="DomainException"/> → 422 Unprocessable Entity</item>
    /// <item>Other exceptions → 500 Internal Server Error</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapPost("/orders", async (CreateOrderCommand command) =>
    /// {
    ///     try
    ///     {
    ///         var result = await handler.HandleAsync(command);
    ///         return TypedResults.Ok(result);
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         return ex.ToProblem();
    ///     }
    /// });
    /// </code>
    /// </example>
    public static ProblemHttpResult ToProblem(
        this Exception exception,
        bool includeDetails = false,
        string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var (statusCode, title, type) = GetExceptionMapping(exception);
        var detail = includeDetails ? exception.Message : GetSafeDetail(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = instance
        };

        // Add trace ID
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        // Add exception-specific extensions
        AddExceptionExtensions(problemDetails, exception);

        // Add exception details if requested
        if (includeDetails)
        {
            problemDetails.Extensions["exception"] = new
            {
                type = exception.GetType().FullName,
                message = exception.Message,
                innerException = exception.InnerException?.Message
            };
        }

        return TypedResults.Problem(problemDetails);
    }

    /// <summary>
    /// Converts an exception to a ProblemDetails result with stack trace included.
    /// </summary>
    /// <param name="exception">The exception to convert.</param>
    /// <param name="instance">The request instance path (optional).</param>
    /// <returns>A ProblemHttpResult with exception details and stack trace.</returns>
    /// <remarks>
    /// <para>
    /// <b>Warning:</b> Only use this in development environments.
    /// Including stack traces in production responses is a security risk.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapPost("/orders", async (CreateOrderCommand command) =>
    /// {
    ///     try
    ///     {
    ///         return TypedResults.Ok(await handler.HandleAsync(command));
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         return env.IsDevelopment() 
    ///             ? ex.ToProblemWithStackTrace() 
    ///             : ex.ToProblem();
    ///     }
    /// });
    /// </code>
    /// </example>
    public static ProblemHttpResult ToProblemWithStackTrace(
        this Exception exception,
        string? instance = null)
    {
        var result = exception.ToProblem(includeDetails: true, instance);
        result.ProblemDetails.Extensions["stackTrace"] = exception.StackTrace;
        return result;
    }

    /// <summary>
    /// Creates a ProblemDetails result for a not found resource.
    /// </summary>
    /// <param name="entityName">The name of the entity that was not found.</param>
    /// <param name="entityId">The ID of the entity that was not found (optional).</param>
    /// <param name="instance">The request instance path (optional).</param>
    /// <returns>A ProblemHttpResult with 404 status code.</returns>
    /// <example>
    /// <code>
    /// app.MapGet("/orders/{id}", async (Guid id) =>
    /// {
    ///     var order = await repository.GetByIdAsync(id);
    ///     return order is null 
    ///         ? TypedResultsExtensions.NotFoundProblem("Order", id)
    ///         : TypedResults.Ok(order);
    /// });
    /// </code>
    /// </example>
    public static ProblemHttpResult NotFoundProblem(
        string entityName,
        object? entityId = null,
        string? instance = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Resource Not Found",
            Detail = entityId is not null
                ? $"{entityName} with ID '{entityId}' was not found."
                : $"{entityName} was not found.",
            Type = "https://httpstatuses.com/not-found",
            Instance = instance
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        problemDetails.Extensions["entityName"] = entityName;

        if (entityId is not null)
        {
            problemDetails.Extensions["entityId"] = entityId;
        }

        return TypedResults.Problem(problemDetails);
    }

    /// <summary>
    /// Creates a ProblemDetails result for validation errors.
    /// </summary>
    /// <param name="errors">Dictionary of validation errors (property name → error messages).</param>
    /// <param name="instance">The request instance path (optional).</param>
    /// <returns>A ValidationProblem with 400 status code.</returns>
    /// <example>
    /// <code>
    /// app.MapPost("/orders", async (CreateOrderCommand command) =>
    /// {
    ///     var errors = validator.Validate(command);
    ///     if (errors.Any())
    ///     {
    ///         return TypedResultsExtensions.ValidationProblem(errors);
    ///     }
    ///     return TypedResults.Ok(await handler.HandleAsync(command));
    /// });
    /// </code>
    /// </example>
    public static ValidationProblem ValidationProblem(
        IDictionary<string, string[]> errors,
        string? instance = null)
    {
        return TypedResults.ValidationProblem(errors, instance: instance);
    }

    /// <summary>
    /// Creates a ProblemDetails result for validation errors from a list of tuples.
    /// </summary>
    /// <param name="errors">List of validation errors as (property, message) tuples.</param>
    /// <param name="instance">The request instance path (optional).</param>
    /// <returns>A ValidationProblem with 400 status code.</returns>
    /// <example>
    /// <code>
    /// var errors = new List&lt;(string, string)&gt;
    /// {
    ///     ("Name", "Name is required"),
    ///     ("Email", "Email format is invalid")
    /// };
    /// return TypedResultsExtensions.ValidationProblem(errors);
    /// </code>
    /// </example>
    public static ValidationProblem ValidationProblem(
        IEnumerable<(string Property, string Message)> errors,
        string? instance = null)
    {
        var errorDict = errors
            .GroupBy(e => e.Property)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Message).ToArray()
            );

        return TypedResults.ValidationProblem(errorDict, instance: instance);
    }

    /// <summary>
    /// Creates a ProblemDetails result for a conflict.
    /// </summary>
    /// <param name="detail">The conflict detail message.</param>
    /// <param name="entityName">The name of the entity with the conflict (optional).</param>
    /// <param name="instance">The request instance path (optional).</param>
    /// <returns>A ProblemHttpResult with 409 status code.</returns>
    /// <example>
    /// <code>
    /// app.MapPost("/orders", async (CreateOrderCommand command) =>
    /// {
    ///     if (await repository.ExistsAsync(command.OrderNumber))
    ///     {
    ///         return TypedResultsExtensions.ConflictProblem(
    ///             $"Order with number '{command.OrderNumber}' already exists.",
    ///             "Order");
    ///     }
    ///     return TypedResults.Created($"/orders/{order.Id}", order);
    /// });
    /// </code>
    /// </example>
    public static ProblemHttpResult ConflictProblem(
        string detail,
        string? entityName = null,
        string? instance = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Resource Conflict",
            Detail = detail,
            Type = "https://httpstatuses.com/conflict",
            Instance = instance
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        if (entityName is not null)
        {
            problemDetails.Extensions["entityName"] = entityName;
        }

        return TypedResults.Problem(problemDetails);
    }

    /// <summary>
    /// Creates a ProblemDetails result for forbidden access.
    /// </summary>
    /// <param name="detail">The detail message explaining why access is forbidden.</param>
    /// <param name="resourceName">The name of the resource (optional).</param>
    /// <param name="requiredPermission">The required permission (optional).</param>
    /// <param name="instance">The request instance path (optional).</param>
    /// <returns>A ProblemHttpResult with 403 status code.</returns>
    /// <example>
    /// <code>
    /// app.MapDelete("/orders/{id}", async (Guid id, ClaimsPrincipal user) =>
    /// {
    ///     if (!user.HasPermission("orders:delete"))
    ///     {
    ///         return TypedResultsExtensions.ForbiddenProblem(
    ///             "You do not have permission to delete orders.",
    ///             "Order",
    ///             "orders:delete");
    ///     }
    ///     await repository.DeleteAsync(id);
    ///     return TypedResults.NoContent();
    /// });
    /// </code>
    /// </example>
    public static ProblemHttpResult ForbiddenProblem(
        string detail,
        string? resourceName = null,
        string? requiredPermission = null,
        string? instance = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Access Denied",
            Detail = detail,
            Type = "https://httpstatuses.com/forbidden",
            Instance = instance
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        if (resourceName is not null)
        {
            problemDetails.Extensions["resourceName"] = resourceName;
        }

        if (requiredPermission is not null)
        {
            problemDetails.Extensions["requiredPermission"] = requiredPermission;
        }

        return TypedResults.Problem(problemDetails);
    }

    /// <summary>
    /// Creates a ProblemDetails result for unauthorized access.
    /// </summary>
    /// <param name="detail">The detail message explaining why authentication is required.</param>
    /// <param name="authenticationScheme">The authentication scheme required (optional).</param>
    /// <param name="instance">The request instance path (optional).</param>
    /// <returns>A ProblemHttpResult with 401 status code.</returns>
    /// <example>
    /// <code>
    /// app.MapGet("/protected", (ClaimsPrincipal user) =>
    /// {
    ///     if (!user.Identity?.IsAuthenticated ?? true)
    ///     {
    ///         return TypedResultsExtensions.UnauthorizedProblem(
    ///             "Authentication is required to access this resource.",
    ///             "Bearer");
    ///     }
    ///     return TypedResults.Ok(new { message = "Welcome!" });
    /// });
    /// </code>
    /// </example>
    public static ProblemHttpResult UnauthorizedProblem(
        string? detail = null,
        string? authenticationScheme = null,
        string? instance = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Authentication Required",
            Detail = detail ?? "Authentication is required to access this resource.",
            Type = "https://httpstatuses.com/unauthorized",
            Instance = instance
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        if (authenticationScheme is not null)
        {
            problemDetails.Extensions["authenticationScheme"] = authenticationScheme;
        }

        return TypedResults.Problem(problemDetails);
    }

    /// <summary>
    /// Creates a ProblemDetails result for a domain rule violation.
    /// </summary>
    /// <param name="detail">The detail message explaining the domain rule violation.</param>
    /// <param name="entityName">The name of the entity (optional).</param>
    /// <param name="ruleName">The name of the violated rule (optional).</param>
    /// <param name="instance">The request instance path (optional).</param>
    /// <returns>A ProblemHttpResult with 422 status code.</returns>
    /// <example>
    /// <code>
    /// app.MapPost("/orders/{id}/cancel", async (Guid id) =>
    /// {
    ///     var order = await repository.GetByIdAsync(id);
    ///     if (order.Status == OrderStatus.Shipped)
    ///     {
    ///         return TypedResultsExtensions.DomainProblem(
    ///             "Cannot cancel an order that has already been shipped.",
    ///             "Order",
    ///             "OrderMustNotBeShipped");
    ///     }
    ///     order.Cancel();
    ///     return TypedResults.Ok(order);
    /// });
    /// </code>
    /// </example>
    public static ProblemHttpResult DomainProblem(
        string detail,
        string? entityName = null,
        string? ruleName = null,
        string? instance = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Domain Rule Violation",
            Detail = detail,
            Type = "https://httpstatuses.com/domain-error",
            Instance = instance
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        if (entityName is not null)
        {
            problemDetails.Extensions["entityName"] = entityName;
        }

        if (ruleName is not null)
        {
            problemDetails.Extensions["ruleName"] = ruleName;
        }

        return TypedResults.Problem(problemDetails);
    }

    /// <summary>
    /// Creates a ProblemDetails result for an internal server error.
    /// </summary>
    /// <param name="detail">The detail message (default: generic error message).</param>
    /// <param name="instance">The request instance path (optional).</param>
    /// <returns>A ProblemHttpResult with 500 status code.</returns>
    /// <example>
    /// <code>
    /// app.MapGet("/data", async () =>
    /// {
    ///     try
    ///     {
    ///         return TypedResults.Ok(await GetDataAsync());
    ///     }
    ///     catch
    ///     {
    ///         return TypedResultsExtensions.InternalServerErrorProblem();
    ///     }
    /// });
    /// </code>
    /// </example>
    public static ProblemHttpResult InternalServerErrorProblem(
        string? detail = null,
        string? instance = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = detail ?? "An unexpected error has occurred. Please try again later.",
            Type = "https://httpstatuses.com/internal-error",
            Instance = instance
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        return TypedResults.Problem(problemDetails);
    }

    /// <summary>
    /// Creates a ProblemDetails result with a custom status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="title">The problem title.</param>
    /// <param name="detail">The problem detail.</param>
    /// <param name="type">The problem type URI (optional).</param>
    /// <param name="instance">The request instance path (optional).</param>
    /// <param name="extensions">Additional extension data (optional).</param>
    /// <returns>A ProblemHttpResult with the specified status code.</returns>
    /// <example>
    /// <code>
    /// app.MapPost("/upload", async () =>
    /// {
    ///     if (fileSizeLimitExceeded)
    ///     {
    ///         return TypedResultsExtensions.CustomProblem(
    ///             StatusCodes.Status413PayloadTooLarge,
    ///             "Payload Too Large",
    ///             "The uploaded file exceeds the maximum allowed size of 10MB.",
    ///             extensions: new Dictionary&lt;string, object?&gt;
    ///             {
    ///                 ["maxSize"] = "10MB",
    ///                 ["actualSize"] = "15MB"
    ///             });
    ///     }
    ///     return TypedResults.Ok();
    /// });
    /// </code>
    /// </example>
    public static ProblemHttpResult CustomProblem(
        int statusCode,
        string title,
        string detail,
        string? type = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type ?? $"https://httpstatuses.com/{statusCode}",
            Instance = instance
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problemDetails.Extensions[extension.Key] = extension.Value;
            }
        }

        return TypedResults.Problem(problemDetails);
    }

    #endregion

    #region Private Helper Methods

    private static (int StatusCode, string Title, string Type) GetExceptionMapping(Exception exception)
    {
        return exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found", "https://httpstatuses.com/not-found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Failed", "https://httpstatuses.com/validation-error"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Authentication Required", "https://httpstatuses.com/unauthorized"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Access Denied", "https://httpstatuses.com/forbidden"),
            ConflictException => (StatusCodes.Status409Conflict, "Resource Conflict", "https://httpstatuses.com/conflict"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Domain Rule Violation", "https://httpstatuses.com/domain-error"),
            BusinessException => (StatusCodes.Status422UnprocessableEntity, "Business Rule Violation", "https://httpstatuses.com/business-error"),
            ArgumentNullException => (StatusCodes.Status400BadRequest, "Missing Required Value", "https://httpstatuses.com/invalid-argument"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid Argument", "https://httpstatuses.com/invalid-argument"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Invalid Operation", "https://httpstatuses.com/invalid-operation"),
            TimeoutException => (StatusCodes.Status408RequestTimeout, "Request Timeout", "https://httpstatuses.com/timeout"),
            OperationCanceledException => (499, "Request Cancelled", "https://httpstatuses.com/request-cancelled"),
            NotImplementedException => (StatusCodes.Status501NotImplemented, "Not Implemented", "https://httpstatuses.com/not-implemented"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "https://httpstatuses.com/internal-error")
        };
    }

    private static string GetSafeDetail(Exception exception)
    {
        // For safe exceptions (domain exceptions), we can show the message
        if (exception is Mvp24HoursException or ArgumentException or InvalidOperationException)
        {
            return exception.Message;
        }

        return "An unexpected error has occurred. Please try again later.";
    }

    private static void AddExceptionExtensions(ProblemDetails problemDetails, Exception exception)
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

    #endregion
}


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
/// Extension methods for converting <see cref="IBusinessResult{T}"/> to native .NET 9 <see cref="TypedResults"/>.
/// Provides strongly-typed, AOT-friendly conversion from business results to HTTP responses.
/// </summary>
/// <remarks>
/// <para>
/// <strong>.NET 9 Features:</strong>
/// <list type="bullet">
/// <item><c>TypedResults.Ok&lt;T&gt;()</c> - Strongly-typed OK response</item>
/// <item><c>TypedResults.NotFound()</c> - 404 Not Found</item>
/// <item><c>TypedResults.BadRequest()</c> - 400 Bad Request</item>
/// <item><c>TypedResults.InternalServerError()</c> - 500 Internal Server Error (NEW in .NET 9)</item>
/// <item><c>TypedResults.Problem()</c> - RFC 7807 ProblemDetails</item>
/// </list>
/// </para>
/// <para>
/// <strong>Benefits over Results.*:</strong>
/// <list type="bullet">
/// <item>Compile-time type checking</item>
/// <item>Better OpenAPI documentation generation</item>
/// <item>AOT compilation friendly</item>
/// <item>Improved IntelliSense support</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// app.MapGet("/orders/{id}", async (Guid id, ISender sender) =>
/// {
///     var result = await sender.SendAsync&lt;IBusinessResult&lt;OrderDto&gt;&gt;(new GetOrderQuery(id));
///     return result.ToNativeTypedResult();
/// });
/// </code>
/// </example>
public static class NativeTypedResultsExtensions
{
    #region IBusinessResult<T> to TypedResults Conversions

    /// <summary>
    /// Converts a <see cref="IBusinessResult{T}"/> to a strongly-typed HTTP response using .NET 9 TypedResults.
    /// </summary>
    /// <typeparam name="T">The type of data in the result.</typeparam>
    /// <param name="result">The business result to convert.</param>
    /// <param name="allowNullData">Whether to allow null data (default: false). If false, null data returns 404.</param>
    /// <returns>A strongly-typed HTTP result.</returns>
    /// <remarks>
    /// <para>
    /// Conversion rules:
    /// <list type="bullet">
    /// <item>If result has errors → Appropriate status based on error codes</item>
    /// <item>If result is successful but data is null and allowNullData is false → 404 Not Found</item>
    /// <item>If result is successful with data → 200 OK</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await GetOrderAsync(id);
    /// return result.ToNativeTypedResult(); // Returns Ok&lt;OrderDto&gt; or NotFound
    /// </code>
    /// </example>
    public static IResult
        ToNativeTypedResult<T>(this IBusinessResult<T> result, bool allowNullData = false)
    {
        ArgumentNullException.ThrowIfNull(result);

        // If result has errors, map to appropriate status code
        if (result.HasErrors)
        {
            return MapErrorsToTypedResult<T>(result);
        }

        // If result is successful but data is null
        if (result.Data is null && !allowNullData)
        {
            return TypedResults.NotFound(CreateProblemDetails(
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                "The requested resource was not found."));
        }

        // Success with data
        return TypedResults.Ok(result.Data!);
    }

    /// <summary>
    /// Converts a <see cref="IBusinessResult{T}"/> to a strongly-typed HTTP response, allowing null data.
    /// Useful for queries that may legitimately return null.
    /// </summary>
    /// <typeparam name="T">The type of data in the result.</typeparam>
    /// <param name="result">The business result to convert.</param>
    /// <returns>A strongly-typed HTTP result.</returns>
    public static IResult
        ToNativeTypedResultAllowNull<T>(this IBusinessResult<T> result)
    {
        return result.ToNativeTypedResult(allowNullData: true);
    }

    /// <summary>
    /// Converts a <see cref="IBusinessResult{T}"/> to a simple Ok/BadRequest result.
    /// Use when you only need basic success/failure handling.
    /// </summary>
    /// <typeparam name="T">The type of data in the result.</typeparam>
    /// <param name="result">The business result to convert.</param>
    /// <returns>Either Ok with data or BadRequest with errors.</returns>
    /// <example>
    /// <code>
    /// var result = await CreateOrderAsync(command);
    /// return result.ToSimpleTypedResult();
    /// </code>
    /// </example>
    public static IResult
        ToSimpleTypedResult<T>(this IBusinessResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.HasErrors)
        {
            var errorMessages = result.Messages?
                .Where(m => m.Type == MessageType.Error)
                .Select(m => m.Message)
                .ToList() ?? [];

            return TypedResults.BadRequest(CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                string.Join("; ", errorMessages)));
        }

        return TypedResults.Ok(result.Data!);
    }

    /// <summary>
    /// Converts a <see cref="IBusinessResult{T}"/> to a Created response for POST operations.
    /// </summary>
    /// <typeparam name="T">The type of data in the result.</typeparam>
    /// <param name="result">The business result to convert.</param>
    /// <param name="uri">The URI of the created resource.</param>
    /// <returns>Either Created with data/location or BadRequest with errors.</returns>
    /// <example>
    /// <code>
    /// var result = await CreateOrderAsync(command);
    /// return result.ToCreatedTypedResult($"/api/orders/{result.Data?.Id}");
    /// </code>
    /// </example>
    public static IResult
        ToCreatedTypedResult<T>(this IBusinessResult<T> result, string? uri = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.HasErrors)
        {
            var structuredMessages = result.Messages?
                .OfType<IStructuredMessageResult>()
                .ToList() ?? [];

            // Check for conflict
            if (structuredMessages.Any(m => m.ErrorCode == "CONFLICT" || m.ErrorCode?.Contains("CONFLICT") == true))
            {
                return TypedResults.Conflict(CreateProblemDetails(
                    StatusCodes.Status409Conflict,
                    "Resource Conflict",
                    string.Join("; ", result.Messages?.Where(m => m.Type == MessageType.Error).Select(m => m.Message) ?? [])));
            }

            return TypedResults.BadRequest(CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Creation Failed",
                string.Join("; ", result.Messages?.Where(m => m.Type == MessageType.Error).Select(m => m.Message) ?? [])));
        }

        return TypedResults.Created(uri, result.Data!);
    }

    /// <summary>
    /// Converts a <see cref="IBusinessResult{T}"/> to a NoContent response for DELETE/PUT operations.
    /// </summary>
    /// <typeparam name="T">The type of data in the result.</typeparam>
    /// <param name="result">The business result to convert.</param>
    /// <returns>Either NoContent on success or appropriate error response.</returns>
    /// <example>
    /// <code>
    /// var result = await DeleteOrderAsync(command);
    /// return result.ToNoContentTypedResult();
    /// </code>
    /// </example>
    public static IResult
        ToNoContentTypedResult<T>(this IBusinessResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.HasErrors)
        {
            var structuredMessages = result.Messages?
                .OfType<IStructuredMessageResult>()
                .ToList() ?? [];

            // Check for not found
            if (structuredMessages.Any(m => m.ErrorCode == "NOT_FOUND" || m.ErrorCode?.Contains("NOT_FOUND") == true))
            {
                return TypedResults.NotFound(CreateProblemDetails(
                    StatusCodes.Status404NotFound,
                    "Resource Not Found",
                    string.Join("; ", result.Messages?.Where(m => m.Type == MessageType.Error).Select(m => m.Message) ?? [])));
            }

            return TypedResults.BadRequest(CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Operation Failed",
                string.Join("; ", result.Messages?.Where(m => m.Type == MessageType.Error).Select(m => m.Message) ?? [])));
        }

        return TypedResults.NoContent();
    }

    /// <summary>
    /// Converts a <see cref="IBusinessResult{T}"/> to an Accepted response for async operations.
    /// </summary>
    /// <typeparam name="T">The type of data in the result.</typeparam>
    /// <param name="result">The business result to convert.</param>
    /// <param name="uri">The URI to check the status of the operation (optional).</param>
    /// <returns>Either Accepted with location or BadRequest with errors.</returns>
    /// <example>
    /// <code>
    /// var result = await StartProcessAsync(command);
    /// return result.ToAcceptedTypedResult($"/api/processes/{result.Data?.ProcessId}");
    /// </code>
    /// </example>
    public static IResult
        ToAcceptedTypedResult<T>(this IBusinessResult<T> result, string? uri = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.HasErrors)
        {
            return TypedResults.BadRequest(CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Operation Failed",
                string.Join("; ", result.Messages?.Where(m => m.Type == MessageType.Error).Select(m => m.Message) ?? [])));
        }

        return TypedResults.Accepted(uri, result.Data!);
    }

    #endregion

    #region Exception to TypedResults Conversions

    /// <summary>
    /// Converts an exception to a strongly-typed ProblemHttpResult using .NET 9 TypedResults.
    /// </summary>
    /// <param name="exception">The exception to convert.</param>
    /// <param name="includeDetails">Whether to include exception details (default: false).</param>
    /// <param name="instance">The request instance path (optional).</param>
    /// <returns>A ProblemHttpResult with appropriate status code.</returns>
    /// <remarks>
    /// <para>
    /// Exception mapping:
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
    public static ProblemHttpResult ToNativeTypedProblem(
        this Exception exception,
        bool includeDetails = false,
        string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var (statusCode, title, type) = MapExceptionToStatusCode(exception);
        var detail = includeDetails ? exception.Message : GetSafeExceptionDetail(exception);

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
        AddExceptionSpecificExtensions(problemDetails, exception);

        // Add exception details if requested (development only!)
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
    /// Converts an exception to a ProblemHttpResult with stack trace included.
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
    public static ProblemHttpResult ToNativeTypedProblemWithStackTrace(
        this Exception exception,
        string? instance = null)
    {
        var result = exception.ToNativeTypedProblem(includeDetails: true, instance);
        result.ProblemDetails.Extensions["stackTrace"] = exception.StackTrace;
        return result;
    }

    #endregion

    #region .NET 9 TypedResults Helpers

    /// <summary>
    /// Creates a 200 OK response with strongly-typed data.
    /// </summary>
    /// <typeparam name="T">The type of data.</typeparam>
    /// <param name="data">The response data.</param>
    /// <returns>A strongly-typed Ok result.</returns>
    /// <example>
    /// <code>
    /// return NativeTypedResultsExtensions.Ok(orderDto);
    /// </code>
    /// </example>
    public static Ok<T> Ok<T>(T data) => TypedResults.Ok(data);

    /// <summary>
    /// Creates a 201 Created response with strongly-typed data.
    /// </summary>
    /// <typeparam name="T">The type of data.</typeparam>
    /// <param name="uri">The URI of the created resource.</param>
    /// <param name="data">The created resource data.</param>
    /// <returns>A strongly-typed Created result.</returns>
    public static Created<T> Created<T>(string? uri, T data) => TypedResults.Created(uri, data);

    /// <summary>
    /// Creates a 202 Accepted response with strongly-typed data.
    /// </summary>
    /// <typeparam name="T">The type of data.</typeparam>
    /// <param name="uri">The URI to check operation status.</param>
    /// <param name="data">The operation data.</param>
    /// <returns>A strongly-typed Accepted result.</returns>
    public static Accepted<T> Accepted<T>(string? uri, T data) => TypedResults.Accepted(uri, data);

    /// <summary>
    /// Creates a 204 No Content response.
    /// </summary>
    /// <returns>A NoContent result.</returns>
    public static NoContent NoContent() => TypedResults.NoContent();

    /// <summary>
    /// Creates a 400 Bad Request response with ProblemDetails.
    /// </summary>
    /// <param name="detail">The error detail message.</param>
    /// <returns>A BadRequest result with ProblemDetails.</returns>
    public static BadRequest<ProblemDetails> BadRequest(string detail)
        => TypedResults.BadRequest(CreateProblemDetails(StatusCodes.Status400BadRequest, "Bad Request", detail));

    /// <summary>
    /// Creates a 401 Unauthorized response.
    /// </summary>
    /// <returns>An Unauthorized result.</returns>
    public static UnauthorizedHttpResult Unauthorized() => TypedResults.Unauthorized();

    /// <summary>
    /// Creates a 403 Forbidden response.
    /// </summary>
    /// <returns>A Forbid result.</returns>
    public static ForbidHttpResult Forbid() => TypedResults.Forbid();

    /// <summary>
    /// Creates a 404 Not Found response with ProblemDetails.
    /// </summary>
    /// <param name="entityName">The name of the entity not found.</param>
    /// <param name="entityId">The ID of the entity not found (optional).</param>
    /// <returns>A NotFound result with ProblemDetails.</returns>
    public static NotFound<ProblemDetails> NotFound(string entityName, object? entityId = null)
    {
        var detail = entityId is not null
            ? $"{entityName} with ID '{entityId}' was not found."
            : $"{entityName} was not found.";

        var problemDetails = CreateProblemDetails(StatusCodes.Status404NotFound, "Resource Not Found", detail);
        problemDetails.Extensions["entityName"] = entityName;
        if (entityId is not null)
            problemDetails.Extensions["entityId"] = entityId;

        return TypedResults.NotFound(problemDetails);
    }

    /// <summary>
    /// Creates a 409 Conflict response with ProblemDetails.
    /// </summary>
    /// <param name="detail">The conflict detail message.</param>
    /// <param name="entityName">The name of the entity with conflict (optional).</param>
    /// <returns>A Conflict result with ProblemDetails.</returns>
    public static Conflict<ProblemDetails> Conflict(string detail, string? entityName = null)
    {
        var problemDetails = CreateProblemDetails(StatusCodes.Status409Conflict, "Resource Conflict", detail);
        if (entityName is not null)
            problemDetails.Extensions["entityName"] = entityName;

        return TypedResults.Conflict(problemDetails);
    }

    /// <summary>
    /// Creates a 422 Unprocessable Entity response with ProblemDetails for domain rule violations.
    /// </summary>
    /// <param name="detail">The domain error detail message.</param>
    /// <param name="entityName">The name of the entity (optional).</param>
    /// <param name="ruleName">The name of the violated rule (optional).</param>
    /// <returns>A Problem result with 422 status.</returns>
    public static ProblemHttpResult UnprocessableEntity(string detail, string? entityName = null, string? ruleName = null)
    {
        var problemDetails = CreateProblemDetails(StatusCodes.Status422UnprocessableEntity, "Domain Rule Violation", detail);
        problemDetails.Type = "https://httpstatuses.com/domain-error";
        if (entityName is not null)
            problemDetails.Extensions["entityName"] = entityName;
        if (ruleName is not null)
            problemDetails.Extensions["ruleName"] = ruleName;

        return TypedResults.Problem(problemDetails);
    }

    /// <summary>
    /// Creates a 500 Internal Server Error response with ProblemDetails.
    /// </summary>
    /// <param name="detail">The error detail message (optional, defaults to generic message).</param>
    /// <returns>A Problem result with 500 status.</returns>
    /// <remarks>
    /// This method uses the new .NET 9 TypedResults.InternalServerError() pattern.
    /// </remarks>
    public static ProblemHttpResult InternalServerError(string? detail = null)
    {
        return TypedResults.Problem(CreateProblemDetails(
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            detail ?? "An unexpected error has occurred. Please try again later."));
    }

    /// <summary>
    /// Creates a ValidationProblem response for validation errors.
    /// </summary>
    /// <param name="errors">Dictionary of validation errors (property name → error messages).</param>
    /// <returns>A ValidationProblem result.</returns>
    /// <example>
    /// <code>
    /// var errors = new Dictionary&lt;string, string[]&gt;
    /// {
    ///     ["Name"] = ["Name is required"],
    ///     ["Email"] = ["Email format is invalid"]
    /// };
    /// return NativeTypedResultsExtensions.ValidationProblem(errors);
    /// </code>
    /// </example>
    public static ValidationProblem ValidationProblem(IDictionary<string, string[]> errors)
        => TypedResults.ValidationProblem(errors);

    /// <summary>
    /// Creates a ValidationProblem response from a list of error tuples.
    /// </summary>
    /// <param name="errors">List of validation errors as (property, message) tuples.</param>
    /// <returns>A ValidationProblem result.</returns>
    public static ValidationProblem ValidationProblem(IEnumerable<(string Property, string Message)> errors)
    {
        var errorDict = errors
            .GroupBy(e => e.Property)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Message).ToArray()
            );

        return TypedResults.ValidationProblem(errorDict);
    }

    /// <summary>
    /// Creates a custom ProblemDetails response with specified status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="title">The problem title.</param>
    /// <param name="detail">The problem detail.</param>
    /// <param name="extensions">Additional extension data (optional).</param>
    /// <returns>A Problem result with the specified status code.</returns>
    public static ProblemHttpResult Problem(
        int statusCode,
        string title,
        string detail,
        IDictionary<string, object?>? extensions = null)
    {
        var problemDetails = CreateProblemDetails(statusCode, title, detail);

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

    private static IResult
        MapErrorsToTypedResult<T>(IBusinessResult<T> result)
    {
        if (result.Messages is null || !result.Messages.Any())
        {
            return TypedResults.BadRequest(CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "An error occurred",
                "An unknown error occurred."));
        }

        var errorMessages = result.Messages.Where(m => m.Type == MessageType.Error).ToList();
        var structuredMessages = errorMessages.OfType<IStructuredMessageResult>().ToList();
        var errorDetail = string.Join("; ", errorMessages.Select(m => m.Message));

        // Check for NOT_FOUND error code
        if (structuredMessages.Any(m => m.ErrorCode == "NOT_FOUND" || m.ErrorCode?.Contains("NOT_FOUND") == true) ||
            errorMessages.Any(m => m.Key == "NOT_FOUND" || m.CustomType == "NOT_FOUND"))
        {
            return TypedResults.NotFound(CreateProblemDetails(
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                errorDetail));
        }

        // Check for CONFLICT error code
        if (structuredMessages.Any(m => m.ErrorCode == "CONFLICT" || m.ErrorCode?.Contains("CONFLICT") == true) ||
            errorMessages.Any(m => m.Key == "CONFLICT" || m.CustomType == "CONFLICT"))
        {
            return TypedResults.Conflict(CreateProblemDetails(
                StatusCodes.Status409Conflict,
                "Resource Conflict",
                errorDetail));
        }

        // Check for UNAUTHORIZED error code
        if (structuredMessages.Any(m => m.ErrorCode == "UNAUTHORIZED" || m.ErrorCode?.Contains("UNAUTHORIZED") == true) ||
            errorMessages.Any(m => m.Key == "UNAUTHORIZED" || m.CustomType == "UNAUTHORIZED"))
        {
            return TypedResults.Unauthorized();
        }

        // Check for FORBIDDEN error code
        if (structuredMessages.Any(m => m.ErrorCode == "FORBIDDEN" || m.ErrorCode?.Contains("FORBIDDEN") == true) ||
            errorMessages.Any(m => m.Key == "FORBIDDEN" || m.CustomType == "FORBIDDEN"))
        {
            return TypedResults.Forbid();
        }

        // Check for INTERNAL_ERROR error code
        if (structuredMessages.Any(m => m.ErrorCode == "INTERNAL_ERROR" || m.ErrorCode?.Contains("INTERNAL_ERROR") == true) ||
            errorMessages.Any(m => m.Key == "INTERNAL_ERROR" || m.CustomType == "INTERNAL_ERROR"))
        {
            return TypedResults.Problem(CreateProblemDetails(
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                errorDetail));
        }

        // Default: BadRequest
        var problemDetails = CreateProblemDetails(
            StatusCodes.Status400BadRequest,
            "Validation Failed",
            errorDetail);

        // Add structured error details
        problemDetails.Extensions["errors"] = errorMessages.Select(m => new
        {
            code = (m as IStructuredMessageResult)?.ErrorCode ?? m.Key ?? m.CustomType,
            message = m.Message,
            property = (m as IStructuredMessageResult)?.PropertyName ?? m.Key
        }).ToArray();

        return TypedResults.BadRequest(problemDetails);
    }

    private static ProblemDetails CreateProblemDetails(int statusCode, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Extensions = { ["traceId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString() }
        };
    }

    private static (int StatusCode, string Title, string Type) MapExceptionToStatusCode(Exception exception)
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

    private static string GetSafeExceptionDetail(Exception exception)
    {
        // For safe exceptions (domain exceptions), we can show the message
        if (exception is Mvp24HoursException or ArgumentException or InvalidOperationException)
        {
            return exception.Message;
        }

        return "An unexpected error has occurred. Please try again later.";
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
                    problemDetails.Extensions["validationErrors"] = validationEx.ValidationErrors
                        .Select(e => new { key = e.Key, message = e.Message, type = e.Type.ToString() })
                        .ToList();
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


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
}


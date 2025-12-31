//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Endpoints.Filters;

/// <summary>
/// Enhanced validation endpoint filter using .NET 9 TypedResults.
/// Validates requests using FluentValidation and returns strongly-typed responses.
/// </summary>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
/// <remarks>
/// <para>
/// This filter provides:
/// <list type="bullet">
/// <item>FluentValidation integration with TypedResults</item>
/// <item>Strongly-typed ValidationProblem responses</item>
/// <item>Automatic validator discovery from DI container</item>
/// <item>Tracing support with Activity</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register validators
/// builder.Services.AddValidatorsFromAssemblyContaining&lt;CreateOrderCommandValidator&gt;();
/// 
/// // Use filter in endpoint
/// app.MapPost("/api/orders", handler)
///    .AddEndpointFilter&lt;NativeValidationEndpointFilter&lt;CreateOrderCommand&gt;&gt;();
/// </code>
/// </example>
public class NativeValidationEndpointFilter<TRequest> : IEndpointFilter
    where TRequest : class
{
    /// <summary>
    /// Validates the request and returns ValidationProblem if validation fails.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (request is null)
        {
            return await next(context);
        }

        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();

        if (validator is null)
        {
            return await next(context);
        }

        var validationResult = await validator.ValidateAsync(request, CancellationToken.None);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            // Add trace ID
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            context.HttpContext.Response.Headers.Append("X-Trace-Id", traceId);

            return TypedResults.ValidationProblem(errors);
        }

        return await next(context);
    }
}

/// <summary>
/// Exception handling endpoint filter that converts exceptions to TypedResults ProblemDetails.
/// </summary>
/// <remarks>
/// <para>
/// This filter catches exceptions and converts them to appropriate HTTP responses:
/// <list type="bullet">
/// <item><see cref="NotFoundException"/> → 404 Not Found</item>
/// <item><see cref="ValidationException"/> → 400 Bad Request</item>
/// <item><see cref="ConflictException"/> → 409 Conflict</item>
/// <item><see cref="UnauthorizedException"/> → 401 Unauthorized</item>
/// <item><see cref="ForbiddenException"/> → 403 Forbidden</item>
/// <item><see cref="DomainException"/> → 422 Unprocessable Entity</item>
/// <item>Other exceptions → 500 Internal Server Error</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// app.MapPost("/api/orders", handler)
///    .AddEndpointFilter&lt;ExceptionHandlingEndpointFilter&gt;();
/// </code>
/// </example>
public class ExceptionHandlingEndpointFilter : IEndpointFilter
{
    private readonly ILogger<ExceptionHandlingEndpointFilter> _logger;
    private readonly bool _includeExceptionDetails;

    /// <summary>
    /// Initializes a new instance of the exception handling filter.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="includeExceptionDetails">Whether to include exception details in responses (development only!).</param>
    public ExceptionHandlingEndpointFilter(
        ILogger<ExceptionHandlingEndpointFilter> logger,
        bool includeExceptionDetails = false)
    {
        _logger = logger;
        _includeExceptionDetails = includeExceptionDetails;
    }

    /// <summary>
    /// Catches exceptions and converts them to ProblemDetails responses.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in endpoint filter: {ExceptionType} - {Message}",
                ex.GetType().Name, ex.Message);

            return ex.ToNativeTypedProblem(_includeExceptionDetails, context.HttpContext.Request.Path);
        }
    }
}

/// <summary>
/// Factory for creating exception handling filters with configuration.
/// </summary>
public class ExceptionHandlingEndpointFilterFactory : IEndpointFilter
{
    /// <summary>
    /// Catches exceptions and converts them to ProblemDetails responses.
    /// Uses environment to determine if exception details should be included.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (Exception ex)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ExceptionHandlingEndpointFilterFactory>>();
            var env = context.HttpContext.RequestServices.GetService<Microsoft.Extensions.Hosting.IHostEnvironment>();
            var includeDetails = env?.EnvironmentName == "Development";

            logger.LogError(ex, "Exception caught in endpoint filter: {ExceptionType} - {Message}",
                ex.GetType().Name, ex.Message);

            return ex.ToNativeTypedProblem(includeDetails, context.HttpContext.Request.Path);
        }
    }
}

/// <summary>
/// Logging endpoint filter that logs request/response information.
/// </summary>
/// <remarks>
/// <para>
/// This filter logs:
/// <list type="bullet">
/// <item>Request method and path</item>
/// <item>Request duration</item>
/// <item>Response status code</item>
/// <item>Exception information (if any)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// app.MapPost("/api/orders", handler)
///    .AddEndpointFilter&lt;LoggingEndpointFilter&gt;();
/// </code>
/// </example>
public class LoggingEndpointFilter : IEndpointFilter
{
    /// <summary>
    /// Logs request/response information and timing.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<LoggingEndpointFilter>>();
        var request = context.HttpContext.Request;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation(
            "Starting request {Method} {Path}",
            request.Method,
            request.Path);

        try
        {
            var result = await next(context);

            stopwatch.Stop();
            logger.LogInformation(
                "Completed request {Method} {Path} in {ElapsedMs}ms",
                request.Method,
                request.Path,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Failed request {Method} {Path} in {ElapsedMs}ms: {ExceptionType}",
                request.Method,
                request.Path,
                stopwatch.ElapsedMilliseconds,
                ex.GetType().Name);
            throw;
        }
    }
}

/// <summary>
/// Correlation ID endpoint filter that ensures correlation ID propagation.
/// </summary>
/// <remarks>
/// <para>
/// This filter:
/// <list type="bullet">
/// <item>Reads correlation ID from request header (X-Correlation-ID)</item>
/// <item>Generates new correlation ID if not present</item>
/// <item>Sets correlation ID in response header</item>
/// <item>Creates Activity with correlation ID</item>
/// </list>
/// </para>
/// </remarks>
public class CorrelationIdEndpointFilter : IEndpointFilter
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    /// <summary>
    /// Ensures correlation ID is present and propagated.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var correlationId = context.HttpContext.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Activity.Current?.Id
            ?? Guid.NewGuid().ToString();

        // Set correlation ID in response
        context.HttpContext.Response.Headers[CorrelationIdHeader] = correlationId;

        // Set baggage for distributed tracing
        Activity.Current?.SetBaggage("CorrelationId", correlationId);

        return await next(context);
    }
}

/// <summary>
/// Idempotency endpoint filter for POST/PUT/PATCH requests.
/// </summary>
/// <remarks>
/// <para>
/// This filter:
/// <list type="bullet">
/// <item>Reads idempotency key from request header (Idempotency-Key)</item>
/// <item>Returns cached response if request was already processed</item>
/// <item>Caches response for successful requests</item>
/// </list>
/// </para>
/// </remarks>
public class IdempotencyEndpointFilter : IEndpointFilter
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private static readonly Dictionary<string, (object? Result, DateTimeOffset ExpiresAt)> _cache = new();
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(24);

    /// <summary>
    /// Handles idempotency for the request.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var idempotencyKey = context.HttpContext.Request.Headers[IdempotencyKeyHeader].FirstOrDefault();

        // If no idempotency key, proceed without caching
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            return await next(context);
        }

        // Check cache
        lock (_cache)
        {
            // Clean up expired entries
            var expiredKeys = _cache.Where(kvp => kvp.Value.ExpiresAt < DateTimeOffset.UtcNow).Select(kvp => kvp.Key).ToList();
            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }

            // Check if we have a cached response
            if (_cache.TryGetValue(idempotencyKey, out var cached))
            {
                context.HttpContext.Response.Headers["X-Idempotency-Replay"] = "true";
                return cached.Result;
            }
        }

        // Execute the request
        var result = await next(context);

        // Cache the result
        lock (_cache)
        {
            _cache[idempotencyKey] = (result, DateTimeOffset.UtcNow.Add(DefaultExpiration));
        }

        return result;
    }
}

/// <summary>
/// Request timeout endpoint filter.
/// </summary>
/// <remarks>
/// <para>
/// This filter:
/// <list type="bullet">
/// <item>Applies a timeout to the request processing</item>
/// <item>Returns 408 Request Timeout if timeout is exceeded</item>
/// <item>Uses CancellationToken for cooperative cancellation</item>
/// </list>
/// </para>
/// </remarks>
public class TimeoutEndpointFilter : IEndpointFilter
{
    private readonly TimeSpan _timeout;

    /// <summary>
    /// Initializes a new instance of the timeout filter.
    /// </summary>
    /// <param name="timeout">The request timeout.</param>
    public TimeoutEndpointFilter(TimeSpan timeout)
    {
        _timeout = timeout;
    }

    /// <summary>
    /// Applies timeout to the request.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.HttpContext.RequestAborted);
        cts.CancelAfter(_timeout);

        try
        {
            return await next(context).AsTask().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            return TypedResults.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status408RequestTimeout,
                Title = "Request Timeout",
                Detail = $"The request exceeded the timeout of {_timeout.TotalSeconds} seconds.",
                Type = "https://httpstatuses.com/408",
                Extensions = { ["traceId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString() }
            });
        }
    }
}

/// <summary>
/// Factory for creating timeout endpoint filters with configurable duration.
/// </summary>
/// <param name="timeout">The request timeout duration.</param>
public class TimeoutEndpointFilterFactory(TimeSpan timeout) : IEndpointFilter
{
    private readonly TimeSpan _timeout = timeout;

    /// <summary>
    /// Creates a timeout filter with the specified duration.
    /// </summary>
    /// <param name="seconds">The timeout in seconds.</param>
    /// <returns>A new timeout filter factory.</returns>
    public static TimeoutEndpointFilterFactory FromSeconds(int seconds)
        => new(TimeSpan.FromSeconds(seconds));

    /// <summary>
    /// Creates a timeout filter with the specified duration.
    /// </summary>
    /// <param name="milliseconds">The timeout in milliseconds.</param>
    /// <returns>A new timeout filter factory.</returns>
    public static TimeoutEndpointFilterFactory FromMilliseconds(int milliseconds)
        => new(TimeSpan.FromMilliseconds(milliseconds));

    /// <summary>
    /// Applies timeout to the request.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.HttpContext.RequestAborted);
        cts.CancelAfter(_timeout);

        try
        {
            return await next(context).AsTask().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            return TypedResults.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status408RequestTimeout,
                Title = "Request Timeout",
                Detail = $"The request exceeded the timeout of {_timeout.TotalSeconds} seconds.",
                Type = "https://httpstatuses.com/408",
                Extensions = { ["traceId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString() }
            });
        }
    }
}

/// <summary>
/// Extension methods for adding endpoint filters.
/// </summary>
public static class EndpointFilterExtensions
{
    /// <summary>
    /// Adds validation filter with FluentValidation using TypedResults.
    /// </summary>
    /// <typeparam name="TRequest">The request type to validate.</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder WithNativeValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class
    {
        return builder.AddEndpointFilter<NativeValidationEndpointFilter<TRequest>>();
    }

    /// <summary>
    /// Adds exception handling filter that converts exceptions to ProblemDetails.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder WithExceptionHandling(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter<ExceptionHandlingEndpointFilterFactory>();
    }

    /// <summary>
    /// Adds logging filter for request/response logging.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder WithLogging(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter<LoggingEndpointFilter>();
    }

    /// <summary>
    /// Adds correlation ID filter for distributed tracing.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder WithCorrelationId(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter<CorrelationIdEndpointFilter>();
    }

    /// <summary>
    /// Adds idempotency filter for POST/PUT/PATCH requests.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder WithIdempotency(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter<IdempotencyEndpointFilter>();
    }

    /// <summary>
    /// Adds timeout filter with the specified duration.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="timeout">The request timeout.</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder WithTimeout(this RouteHandlerBuilder builder, TimeSpan timeout)
    {
        return builder.AddEndpointFilter(new TimeoutEndpointFilterFactory(timeout));
    }

    /// <summary>
    /// Adds timeout filter with the specified duration in seconds.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="seconds">The request timeout in seconds.</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder WithTimeout(this RouteHandlerBuilder builder, int seconds)
    {
        return builder.WithTimeout(TimeSpan.FromSeconds(seconds));
    }

    /// <summary>
    /// Adds all standard filters: correlation ID, logging, validation, and exception handling.
    /// </summary>
    /// <typeparam name="TRequest">The request type to validate.</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder WithStandardFilters<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class
    {
        return builder
            .WithCorrelationId()
            .WithLogging()
            .WithNativeValidation<TRequest>()
            .WithExceptionHandling();
    }
}


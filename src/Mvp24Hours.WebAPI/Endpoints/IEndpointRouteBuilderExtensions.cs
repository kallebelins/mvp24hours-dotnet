//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.WebAPI.Endpoints.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Endpoints;

/// <summary>
/// Extension methods for <see cref="IEndpointRouteBuilder"/> to add Minimal API support for Mvp24Hours.
/// Provides helpers for mapping CQRS commands and queries to HTTP endpoints with automatic validation and error handling.
/// </summary>
/// <remarks>
/// <para>
/// These extensions simplify the creation of Minimal APIs by:
/// <list type="bullet">
/// <item>Automatically mapping CQRS commands/queries to HTTP endpoints</item>
/// <item>Handling validation errors and converting them to ProblemDetails</item>
/// <item>Converting BusinessResult to appropriate HTTP responses</item>
/// <item>Providing type-safe endpoint registration</item>
/// </list>
/// </para>
/// <para>
/// <strong>Prerequisites:</strong>
/// <list type="bullet">
/// <item>Register CQRS services via <c>services.AddMvpMediator()</c></item>
/// <item>Register validation services (FluentValidation)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs
/// builder.Services.AddMvpMediator();
/// 
/// var app = builder.Build();
/// 
/// // Map a command endpoint
/// app.MapCommand&lt;CreateOrderCommand, OrderDto&gt;("/api/orders", HttpMethod.Post);
/// 
/// // Map a query endpoint
/// app.MapQuery&lt;GetOrderByIdQuery, OrderDto&gt;("/api/orders/{id}");
/// </code>
/// </example>
public static class IEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a CQRS command to an HTTP endpoint with automatic validation and error handling.
    /// </summary>
    /// <typeparam name="TCommand">The command type implementing <see cref="IMediatorCommand{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The response type returned by the command handler.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern (e.g., "/api/orders").</param>
    /// <param name="method">The HTTP method (default: POST).</param>
    /// <param name="configure">Optional configuration for the endpoint.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    /// <remarks>
    /// <para>
    /// This method:
    /// <list type="bullet">
    /// <item>Binds the command from the request body</item>
    /// <item>Validates the command using FluentValidation</item>
    /// <item>Sends the command via <see cref="ISender"/></item>
    /// <item>Converts the response to appropriate HTTP status codes</item>
    /// <item>Handles exceptions and converts them to ProblemDetails</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapCommand&lt;CreateOrderCommand, OrderDto&gt;(
    ///     "/api/orders",
    ///     HttpMethod.Post,
    ///     endpoint => endpoint
    ///         .RequireAuthorization()
    ///         .WithTags("Orders")
    ///         .WithSummary("Create a new order")
    /// );
    /// </code>
    /// </example>
    public static RouteHandlerBuilder MapCommand<TCommand, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        HttpMethod? method = null,
        Action<RouteHandlerBuilder>? configure = null)
        where TCommand : class, IMediatorCommand<TResponse>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        method ??= HttpMethod.Post;

        var builder = endpoints.MapMethods(pattern, [method.Method], async (
            [FromBody] TCommand command,
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(command);

            try
            {
                var response = await sender.SendAsync<TResponse>(command, cancellationToken);
                return Results.Ok(response);
            }
            catch (ValidationException ex)
            {
                var errors = ConvertValidationErrors(ex.ValidationErrors);
                return Results.ValidationProblem(errors);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Resource not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (ConflictException ex)
            {
                return Results.Conflict(new ProblemDetails
                {
                    Title = "Conflict",
                    Detail = ex.Message,
                    Status = StatusCodes.Status409Conflict
                });
            }
            catch (UnauthorizedException)
            {
                return Results.Unauthorized();
            }
            catch (ForbiddenException)
            {
                return Results.Forbid();
            }
            catch (DomainException ex)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Domain error",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        })
        .AddEndpointFilter<ValidationEndpointFilter<TCommand>>()
        .Produces<TResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        configure?.Invoke(builder);

        return builder;
    }

    /// <summary>
    /// Maps a CQRS command that returns <see cref="IBusinessResult{T}"/> to an HTTP endpoint.
    /// Automatically converts BusinessResult to appropriate HTTP responses.
    /// </summary>
    /// <typeparam name="TCommand">The command type implementing <see cref="IMediatorCommand{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The response type wrapped in BusinessResult.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern (e.g., "/api/orders").</param>
    /// <param name="method">The HTTP method (default: POST).</param>
    /// <param name="configure">Optional configuration for the endpoint.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    /// <remarks>
    /// <para>
    /// This overload handles commands that return <see cref="IBusinessResult{T}"/>:
    /// <list type="bullet">
    /// <item>If result has errors, returns 400 Bad Request with error details</item>
    /// <item>If result is successful, returns 200 OK with data</item>
    /// <item>Automatically extracts messages from BusinessResult</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapCommandWithResult&lt;CreateOrderCommand, OrderDto&gt;(
    ///     "/api/orders",
    ///     HttpMethod.Post
    /// );
    /// </code>
    /// </example>
    public static RouteHandlerBuilder MapCommandWithResult<TCommand, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        HttpMethod? method = null,
        Action<RouteHandlerBuilder>? configure = null)
        where TCommand : class, IMediatorCommand<IBusinessResult<TResponse>>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        method ??= HttpMethod.Post;

        var builder = endpoints.MapMethods(pattern, [method.Method], async (
            [FromBody] TCommand command,
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(command);

            try
            {
                var result = await sender.SendAsync<IBusinessResult<TResponse>>(command, cancellationToken);
                return result.ToTypedResult();
            }
            catch (ValidationException ex)
            {
                var errors = ConvertValidationErrors(ex.ValidationErrors);
                return Results.ValidationProblem(errors);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Resource not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (ConflictException ex)
            {
                return Results.Conflict(new ProblemDetails
                {
                    Title = "Conflict",
                    Detail = ex.Message,
                    Status = StatusCodes.Status409Conflict
                });
            }
            catch (UnauthorizedException)
            {
                return Results.Unauthorized();
            }
            catch (ForbiddenException)
            {
                return Results.Forbid();
            }
            catch (DomainException ex)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Domain error",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        })
        .AddEndpointFilter<ValidationEndpointFilter<TCommand>>()
        .Produces<TResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        configure?.Invoke(builder);

        return builder;
    }

    /// <summary>
    /// Maps a CQRS query to an HTTP GET endpoint with automatic validation and error handling.
    /// </summary>
    /// <typeparam name="TQuery">The query type implementing <see cref="IMediatorQuery{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The response type returned by the query handler.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern (e.g., "/api/orders/{id}").</param>
    /// <param name="configure">Optional configuration for the endpoint.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    /// <remarks>
    /// <para>
    /// This method:
    /// <list type="bullet">
    /// <item>Binds the query from route parameters and query string</item>
    /// <item>Validates the query using FluentValidation</item>
    /// <item>Sends the query via <see cref="ISender"/></item>
    /// <item>Returns 404 if result is null</item>
    /// <item>Converts exceptions to ProblemDetails</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapQuery&lt;GetOrderByIdQuery, OrderDto&gt;(
    ///     "/api/orders/{id}",
    ///     endpoint => endpoint
    ///         .RequireAuthorization()
    ///         .WithTags("Orders")
    ///         .WithSummary("Get order by ID")
    /// );
    /// </code>
    /// </example>
    public static RouteHandlerBuilder MapQuery<TQuery, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TQuery : class, IMediatorQuery<TResponse>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var builder = endpoints.MapGet(pattern, async (
            [AsParameters] TQuery query,
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(query);

            try
            {
                var response = await sender.SendAsync<TResponse>(query, cancellationToken);
                
                if (response is null)
                {
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Resource not found",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                return Results.Ok(response);
            }
            catch (ValidationException ex)
            {
                var errors = ConvertValidationErrors(ex.ValidationErrors);
                return Results.ValidationProblem(errors);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Resource not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (UnauthorizedException)
            {
                return Results.Unauthorized();
            }
            catch (ForbiddenException)
            {
                return Results.Forbid();
            }
            catch (DomainException ex)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Domain error",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        })
        .AddEndpointFilter<ValidationEndpointFilter<TQuery>>()
        .Produces<TResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        configure?.Invoke(builder);

        return builder;
    }

    /// <summary>
    /// Maps a CQRS query that returns <see cref="IBusinessResult{T}"/> to an HTTP GET endpoint.
    /// Automatically converts BusinessResult to appropriate HTTP responses.
    /// </summary>
    /// <typeparam name="TQuery">The query type implementing <see cref="IMediatorQuery{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The response type wrapped in BusinessResult.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern (e.g., "/api/orders/{id}").</param>
    /// <param name="configure">Optional configuration for the endpoint.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    /// <remarks>
    /// <para>
    /// This overload handles queries that return <see cref="IBusinessResult{T}"/>:
    /// <list type="bullet">
    /// <item>If result has errors, returns 400 Bad Request with error details</item>
    /// <item>If result is successful but data is null, returns 404 Not Found</item>
    /// <item>If result is successful with data, returns 200 OK</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapQueryWithResult&lt;GetOrderByIdQuery, OrderDto&gt;("/api/orders/{id}");
    /// </code>
    /// </example>
    public static RouteHandlerBuilder MapQueryWithResult<TQuery, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TQuery : class, IMediatorQuery<IBusinessResult<TResponse>>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var builder = endpoints.MapGet(pattern, async (
            [AsParameters] TQuery query,
            [FromServices] ISender sender,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(query);

            try
            {
                var result = await sender.SendAsync<IBusinessResult<TResponse>>(query, cancellationToken);
                return result.ToTypedResult();
            }
            catch (ValidationException ex)
            {
                var errors = ConvertValidationErrors(ex.ValidationErrors);
                return Results.ValidationProblem(errors);
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Resource not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (UnauthorizedException)
            {
                return Results.Unauthorized();
            }
            catch (ForbiddenException)
            {
                return Results.Forbid();
            }
            catch (DomainException ex)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Domain error",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        })
        .AddEndpointFilter<ValidationEndpointFilter<TQuery>>()
        .Produces<TResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        configure?.Invoke(builder);

        return builder;
    }

    private static IDictionary<string, string[]> ConvertValidationErrors(IList<IMessageResult>? validationErrors)
    {
        if (validationErrors == null || validationErrors.Count == 0)
        {
            return new Dictionary<string, string[]>();
        }

        return validationErrors
            .Where(m => !string.IsNullOrEmpty(m.Key))
            .GroupBy(m => m.Key!)
            .ToDictionary(
                g => g.Key,
                g => g.Select(m => m.Message ?? string.Empty).ToArray()
            );
    }
}


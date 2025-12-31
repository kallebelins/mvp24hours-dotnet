//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using StatusCodes = Microsoft.AspNetCore.Http.StatusCodes;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.WebAPI.Endpoints.Filters;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Mvp24Hours.WebAPI.Endpoints;

/// <summary>
/// Extension methods for <see cref="IEndpointRouteBuilder"/> using .NET 9 native TypedResults.
/// Provides type-safe, AOT-friendly endpoint mapping for CQRS commands and queries.
/// </summary>
/// <remarks>
/// <para>
/// <strong>.NET 9 Features:</strong>
/// <list type="bullet">
/// <item>TypedResults for strongly-typed responses</item>
/// <item>Better OpenAPI metadata generation</item>
/// <item>Improved AOT compilation support</item>
/// <item>Enhanced compile-time type checking</item>
/// </list>
/// </para>
/// <para>
/// <strong>Comparison with Results.*:</strong>
/// <list type="table">
/// <listheader>
/// <term>Feature</term>
/// <description>Results.* vs TypedResults.*</description>
/// </listheader>
/// <item>
/// <term>Type Safety</term>
/// <description>TypedResults provides compile-time checking</description>
/// </item>
/// <item>
/// <term>OpenAPI</term>
/// <description>TypedResults generates better documentation</description>
/// </item>
/// <item>
/// <term>AOT</term>
/// <description>TypedResults is AOT-friendly</description>
/// </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs
/// var app = builder.Build();
/// 
/// // Map command with native TypedResults
/// app.MapNativeCommand&lt;CreateOrderCommand, OrderDto&gt;("/api/orders");
/// 
/// // Map query with native TypedResults
/// app.MapNativeQuery&lt;GetOrderByIdQuery, OrderDto&gt;("/api/orders/{id}");
/// 
/// // Map command with BusinessResult
/// app.MapNativeCommandWithResult&lt;CreateOrderCommand, OrderDto&gt;("/api/orders");
/// </code>
/// </example>
public static class NativeMinimalApiEndpointExtensions
{
    #region Command Endpoints

    /// <summary>
    /// Maps a CQRS command to an HTTP endpoint using native .NET 9 TypedResults.
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
    /// This method uses TypedResults for strongly-typed responses:
    /// <list type="bullet">
    /// <item><c>TypedResults.Ok&lt;TResponse&gt;()</c> - Success with data</item>
    /// <item><c>TypedResults.NotFound()</c> - Resource not found</item>
    /// <item><c>TypedResults.BadRequest()</c> - Validation errors</item>
    /// <item><c>TypedResults.Conflict()</c> - Conflict error</item>
    /// <item><c>TypedResults.Unauthorized()</c> - Not authenticated</item>
    /// <item><c>TypedResults.Forbid()</c> - Not authorized</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapNativeCommand&lt;CreateOrderCommand, OrderDto&gt;(
    ///     "/api/orders",
    ///     HttpMethod.Post,
    ///     endpoint => endpoint
    ///         .RequireAuthorization()
    ///         .WithTags("Orders")
    ///         .WithSummary("Create a new order")
    /// );
    /// </code>
    /// </example>
    public static RouteHandlerBuilder MapNativeCommand<TCommand, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        HttpMethod? method = null,
        Action<RouteHandlerBuilder>? configure = null)
        where TCommand : class, IMediatorCommand<TResponse>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        method ??= HttpMethod.Post;

        var builder = endpoints.MapMethods(pattern, [method.Method],
            async Task<IResult> (
                [FromBody] TCommand command,
                [FromServices] ISender sender,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(command);

                try
                {
                    var response = await sender.SendAsync<TResponse>(command, cancellationToken);
                    return TypedResults.Ok(response);
                }
                catch (ValidationException ex)
                {
                    return TypedResults.BadRequest(CreateValidationProblem(ex));
                }
                catch (NotFoundException ex)
                {
                    return TypedResults.NotFound(CreateProblemDetails(
                        StatusCodes.Status404NotFound,
                        "Resource Not Found",
                        ex.Message,
                        ex.EntityName,
                        ex.EntityId));
                }
                catch (ConflictException ex)
                {
                    return TypedResults.Conflict(CreateProblemDetails(
                        StatusCodes.Status409Conflict,
                        "Resource Conflict",
                        ex.Message,
                        ex.EntityName));
                }
                catch (UnauthorizedException)
                {
                    return TypedResults.Unauthorized();
                }
                catch (ForbiddenException)
                {
                    return TypedResults.Forbid();
                }
                catch (DomainException ex)
                {
                    return TypedResults.Problem(CreateProblemDetails(
                        StatusCodes.Status422UnprocessableEntity,
                        "Domain Rule Violation",
                        ex.Message,
                        ex.EntityName));
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(CreateProblemDetails(
                        StatusCodes.Status500InternalServerError,
                        "Internal Server Error",
                        ex.Message));
                }
            })
        .AddEndpointFilter<ValidationEndpointFilter<TCommand>>()
        .Produces<TResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        configure?.Invoke(builder);

        return builder;
    }

    /// <summary>
    /// Maps a CQRS command that returns <see cref="IBusinessResult{T}"/> to an HTTP endpoint
    /// using native .NET 9 TypedResults.
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
    /// This overload automatically converts <see cref="IBusinessResult{T}"/> to TypedResults:
    /// <list type="bullet">
    /// <item>Success with data → <c>TypedResults.Ok&lt;TResponse&gt;()</c></item>
    /// <item>Errors → Appropriate status code based on error codes</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapNativeCommandWithResult&lt;CreateOrderCommand, OrderDto&gt;("/api/orders");
    /// </code>
    /// </example>
    public static RouteHandlerBuilder MapNativeCommandWithResult<TCommand, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        HttpMethod? method = null,
        Action<RouteHandlerBuilder>? configure = null)
        where TCommand : class, IMediatorCommand<IBusinessResult<TResponse>>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        method ??= HttpMethod.Post;

        var builder = endpoints.MapMethods(pattern, [method.Method],
            async Task<IResult> (
                [FromBody] TCommand command,
                [FromServices] ISender sender,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(command);

                try
                {
                    var result = await sender.SendAsync<IBusinessResult<TResponse>>(command, cancellationToken);
                    return result.ToNativeTypedResult();
                }
                catch (ValidationException ex)
                {
                    return TypedResults.BadRequest(CreateValidationProblem(ex));
                }
                catch (NotFoundException ex)
                {
                    return TypedResults.NotFound(CreateProblemDetails(
                        StatusCodes.Status404NotFound,
                        "Resource Not Found",
                        ex.Message,
                        ex.EntityName,
                        ex.EntityId));
                }
                catch (ConflictException ex)
                {
                    return TypedResults.Conflict(CreateProblemDetails(
                        StatusCodes.Status409Conflict,
                        "Resource Conflict",
                        ex.Message,
                        ex.EntityName));
                }
                catch (UnauthorizedException)
                {
                    return TypedResults.Unauthorized();
                }
                catch (ForbiddenException)
                {
                    return TypedResults.Forbid();
                }
                catch (DomainException ex)
                {
                    return TypedResults.Problem(CreateProblemDetails(
                        StatusCodes.Status422UnprocessableEntity,
                        "Domain Rule Violation",
                        ex.Message,
                        ex.EntityName));
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(CreateProblemDetails(
                        StatusCodes.Status500InternalServerError,
                        "Internal Server Error",
                        ex.Message));
                }
            })
        .AddEndpointFilter<ValidationEndpointFilter<TCommand>>()
        .Produces<TResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        configure?.Invoke(builder);

        return builder;
    }

    /// <summary>
    /// Maps a CQRS command for creating resources to an HTTP POST endpoint
    /// that returns 201 Created on success.
    /// </summary>
    /// <typeparam name="TCommand">The command type implementing <see cref="IMediatorCommand{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The response type wrapped in BusinessResult.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern (e.g., "/api/orders").</param>
    /// <param name="locationPattern">Pattern for the created resource location (e.g., "/api/orders/{0}").</param>
    /// <param name="idSelector">Function to extract the ID from the response for the location URL.</param>
    /// <param name="configure">Optional configuration for the endpoint.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// app.MapNativeCommandCreate&lt;CreateOrderCommand, OrderDto&gt;(
    ///     "/api/orders",
    ///     "/api/orders/{0}",
    ///     dto => dto.Id);
    /// </code>
    /// </example>
    public static RouteHandlerBuilder MapNativeCommandCreate<TCommand, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        string locationPattern,
        Func<TResponse, object> idSelector,
        Action<RouteHandlerBuilder>? configure = null)
        where TCommand : class, IMediatorCommand<IBusinessResult<TResponse>>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(idSelector);

        var builder = endpoints.MapPost(pattern,
            async Task<IResult> (
                [FromBody] TCommand command,
                [FromServices] ISender sender,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(command);

                try
                {
                    var result = await sender.SendAsync<IBusinessResult<TResponse>>(command, cancellationToken);

                    if (result.HasErrors)
                    {
                        return result.ToCreatedTypedResult();
                    }

                    var id = idSelector(result.Data!);
                    var location = string.Format(locationPattern, id);
                    return TypedResults.Created(location, result.Data!);
                }
                catch (ValidationException ex)
                {
                    return TypedResults.BadRequest(CreateValidationProblem(ex));
                }
                catch (ConflictException ex)
                {
                    return TypedResults.Conflict(CreateProblemDetails(
                        StatusCodes.Status409Conflict,
                        "Resource Conflict",
                        ex.Message,
                        ex.EntityName));
                }
                catch (UnauthorizedException)
                {
                    return TypedResults.Unauthorized();
                }
                catch (ForbiddenException)
                {
                    return TypedResults.Forbid();
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(CreateProblemDetails(
                        StatusCodes.Status500InternalServerError,
                        "Internal Server Error",
                        ex.Message));
                }
            })
        .AddEndpointFilter<ValidationEndpointFilter<TCommand>>()
        .Produces<TResponse>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        configure?.Invoke(builder);

        return builder;
    }

    /// <summary>
    /// Maps a CQRS command for deleting resources to an HTTP DELETE endpoint
    /// that returns 204 No Content on success.
    /// </summary>
    /// <typeparam name="TCommand">The command type implementing <see cref="IMediatorCommand{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The response type wrapped in BusinessResult.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern (e.g., "/api/orders/{id}").</param>
    /// <param name="configure">Optional configuration for the endpoint.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// app.MapNativeCommandDelete&lt;DeleteOrderCommand, bool&gt;("/api/orders/{id}");
    /// </code>
    /// </example>
    public static RouteHandlerBuilder MapNativeCommandDelete<TCommand, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TCommand : class, IMediatorCommand<IBusinessResult<TResponse>>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var builder = endpoints.MapDelete(pattern,
            async Task<IResult> (
                [AsParameters] TCommand command,
                [FromServices] ISender sender,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(command);

                try
                {
                    var result = await sender.SendAsync<IBusinessResult<TResponse>>(command, cancellationToken);
                    return result.ToNoContentTypedResult();
                }
                catch (ValidationException ex)
                {
                    return TypedResults.BadRequest(CreateValidationProblem(ex));
                }
                catch (NotFoundException ex)
                {
                    return TypedResults.NotFound(CreateProblemDetails(
                        StatusCodes.Status404NotFound,
                        "Resource Not Found",
                        ex.Message,
                        ex.EntityName,
                        ex.EntityId));
                }
                catch (UnauthorizedException)
                {
                    return TypedResults.Unauthorized();
                }
                catch (ForbiddenException)
                {
                    return TypedResults.Forbid();
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(CreateProblemDetails(
                        StatusCodes.Status500InternalServerError,
                        "Internal Server Error",
                        ex.Message));
                }
            })
        .AddEndpointFilter<ValidationEndpointFilter<TCommand>>()
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        configure?.Invoke(builder);

        return builder;
    }

    #endregion

    #region Query Endpoints

    /// <summary>
    /// Maps a CQRS query to an HTTP GET endpoint using native .NET 9 TypedResults.
    /// </summary>
    /// <typeparam name="TQuery">The query type implementing <see cref="IMediatorQuery{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The response type returned by the query handler.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern (e.g., "/api/orders/{id}").</param>
    /// <param name="configure">Optional configuration for the endpoint.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// app.MapNativeQuery&lt;GetOrderByIdQuery, OrderDto&gt;(
    ///     "/api/orders/{id}",
    ///     endpoint => endpoint
    ///         .RequireAuthorization()
    ///         .WithTags("Orders")
    ///         .WithSummary("Get order by ID")
    /// );
    /// </code>
    /// </example>
    public static RouteHandlerBuilder MapNativeQuery<TQuery, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TQuery : class, IMediatorQuery<TResponse>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var builder = endpoints.MapGet(pattern,
            async Task<Results<Ok<TResponse>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>, UnauthorizedHttpResult, ForbidHttpResult, ProblemHttpResult>> (
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
                        return TypedResults.NotFound(CreateProblemDetails(
                            StatusCodes.Status404NotFound,
                            "Resource Not Found",
                            "The requested resource was not found."));
                    }

                    return TypedResults.Ok(response);
                }
                catch (ValidationException ex)
                {
                    return TypedResults.BadRequest(CreateValidationProblem(ex));
                }
                catch (NotFoundException ex)
                {
                    return TypedResults.NotFound(CreateProblemDetails(
                        StatusCodes.Status404NotFound,
                        "Resource Not Found",
                        ex.Message,
                        ex.EntityName,
                        ex.EntityId));
                }
                catch (UnauthorizedException)
                {
                    return TypedResults.Unauthorized();
                }
                catch (ForbiddenException)
                {
                    return TypedResults.Forbid();
                }
                catch (DomainException ex)
                {
                    return TypedResults.Problem(CreateProblemDetails(
                        StatusCodes.Status422UnprocessableEntity,
                        "Domain Rule Violation",
                        ex.Message,
                        ex.EntityName));
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(CreateProblemDetails(
                        StatusCodes.Status500InternalServerError,
                        "Internal Server Error",
                        ex.Message));
                }
            })
        .AddEndpointFilter<ValidationEndpointFilter<TQuery>>()
        .Produces<TResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        configure?.Invoke(builder);

        return builder;
    }

    /// <summary>
    /// Maps a CQRS query that returns <see cref="IBusinessResult{T}"/> to an HTTP GET endpoint
    /// using native .NET 9 TypedResults.
    /// </summary>
    /// <typeparam name="TQuery">The query type implementing <see cref="IMediatorQuery{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The response type wrapped in BusinessResult.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern (e.g., "/api/orders/{id}").</param>
    /// <param name="configure">Optional configuration for the endpoint.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// app.MapNativeQueryWithResult&lt;GetOrderByIdQuery, OrderDto&gt;("/api/orders/{id}");
    /// </code>
    /// </example>
    public static RouteHandlerBuilder MapNativeQueryWithResult<TQuery, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TQuery : class, IMediatorQuery<IBusinessResult<TResponse>>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var builder = endpoints.MapGet(pattern,
            async Task<IResult> (
                [AsParameters] TQuery query,
                [FromServices] ISender sender,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(query);

                try
                {
                    var result = await sender.SendAsync<IBusinessResult<TResponse>>(query, cancellationToken);
                    return result.ToNativeTypedResult();
                }
                catch (ValidationException ex)
                {
                    return TypedResults.BadRequest(CreateValidationProblem(ex));
                }
                catch (NotFoundException ex)
                {
                    return TypedResults.NotFound(CreateProblemDetails(
                        StatusCodes.Status404NotFound,
                        "Resource Not Found",
                        ex.Message,
                        ex.EntityName,
                        ex.EntityId));
                }
                catch (UnauthorizedException)
                {
                    return TypedResults.Unauthorized();
                }
                catch (ForbiddenException)
                {
                    return TypedResults.Forbid();
                }
                catch (DomainException ex)
                {
                    return TypedResults.Problem(CreateProblemDetails(
                        StatusCodes.Status422UnprocessableEntity,
                        "Domain Rule Violation",
                        ex.Message,
                        ex.EntityName));
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(CreateProblemDetails(
                        StatusCodes.Status500InternalServerError,
                        "Internal Server Error",
                        ex.Message));
                }
            })
        .AddEndpointFilter<ValidationEndpointFilter<TQuery>>()
        .Produces<TResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        configure?.Invoke(builder);

        return builder;
    }

    /// <summary>
    /// Maps a CQRS query for listing resources to an HTTP GET endpoint.
    /// Optimized for collections that may return empty results (not 404).
    /// </summary>
    /// <typeparam name="TQuery">The query type implementing <see cref="IMediatorQuery{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The collection response type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern (e.g., "/api/orders").</param>
    /// <param name="configure">Optional configuration for the endpoint.</param>
    /// <returns>The route handler builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// app.MapNativeQueryList&lt;GetOrdersQuery, IEnumerable&lt;OrderDto&gt;&gt;("/api/orders");
    /// </code>
    /// </example>
    public static RouteHandlerBuilder MapNativeQueryList<TQuery, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Action<RouteHandlerBuilder>? configure = null)
        where TQuery : class, IMediatorQuery<TResponse>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var builder = endpoints.MapGet(pattern,
            async Task<Results<Ok<TResponse>, BadRequest<ProblemDetails>, UnauthorizedHttpResult, ForbidHttpResult, ProblemHttpResult>> (
                [AsParameters] TQuery query,
                [FromServices] ISender sender,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(query);

                try
                {
                    var response = await sender.SendAsync<TResponse>(query, cancellationToken);
                    return TypedResults.Ok(response);
                }
                catch (ValidationException ex)
                {
                    return TypedResults.BadRequest(CreateValidationProblem(ex));
                }
                catch (UnauthorizedException)
                {
                    return TypedResults.Unauthorized();
                }
                catch (ForbiddenException)
                {
                    return TypedResults.Forbid();
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(CreateProblemDetails(
                        StatusCodes.Status500InternalServerError,
                        "Internal Server Error",
                        ex.Message));
                }
            })
        .AddEndpointFilter<ValidationEndpointFilter<TQuery>>()
        .Produces<TResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        configure?.Invoke(builder);

        return builder;
    }

    #endregion

    #region Private Helper Methods

    private static ProblemDetails CreateProblemDetails(
        int statusCode,
        string title,
        string detail,
        string? entityName = null,
        object? entityId = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Extensions = { ["traceId"] = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString() }
        };

        if (entityName is not null)
            problemDetails.Extensions["entityName"] = entityName;

        if (entityId is not null)
            problemDetails.Extensions["entityId"] = entityId;

        return problemDetails;
    }

    private static ProblemDetails CreateValidationProblem(ValidationException ex)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Detail = ex.Message,
            Type = "https://httpstatuses.com/validation-error",
            Extensions = { ["traceId"] = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString() }
        };

        if (ex.ValidationErrors?.Count > 0)
        {
            var errors = new System.Collections.Generic.Dictionary<string, string[]>();
            foreach (var group in ex.ValidationErrors.GroupBy(e => e.Key ?? ""))
            {
                errors[group.Key] = group.Select(e => e.Message ?? "").ToArray();
            }
            problemDetails.Extensions["errors"] = errors;
        }

        return problemDetails;
    }

    #endregion
}


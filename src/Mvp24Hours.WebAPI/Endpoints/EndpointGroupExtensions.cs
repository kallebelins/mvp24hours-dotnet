//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq.Expressions;

namespace Mvp24Hours.WebAPI.Endpoints;

/// <summary>
/// Extension methods for creating and configuring endpoint groups with conventions.
/// Provides helpers for organizing endpoints with common prefixes, tags, and conventions.
/// </summary>
/// <remarks>
/// <para>
/// Endpoint groups allow you to:
/// <list type="bullet">
/// <item>Apply common prefixes to multiple endpoints</item>
/// <item>Add common tags for Swagger/OpenAPI</item>
/// <item>Apply authorization policies to all endpoints in the group</item>
/// <item>Set common response types and status codes</item>
/// <item>Apply rate limiting policies</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create an endpoint group for orders
/// var ordersGroup = app.MapGroup("/api/orders")
///     .WithTags("Orders")
///     .RequireAuthorization();
/// 
/// // Add endpoints to the group
/// ordersGroup.MapGet("/", GetOrders);
/// ordersGroup.MapGet("/{id}", GetOrderById);
/// ordersGroup.MapPost("/", CreateOrder);
/// </code>
/// </example>
public static class EndpointGroupExtensions
{
    /// <summary>
    /// Creates an endpoint group with a prefix and optional configuration.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="prefix">The route prefix for all endpoints in the group (e.g., "/api/orders").</param>
    /// <param name="configure">Optional configuration action for the group.</param>
    /// <returns>The route group builder for further configuration.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a route group that applies the prefix to all endpoints added to it.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var group = app.MapMvpGroup("/api/orders", group =>
    /// {
    ///     group.WithTags("Orders");
    ///     group.RequireAuthorization();
    /// });
    /// </code>
    /// </example>
    public static RouteGroupBuilder MapMvpGroup(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        Action<RouteGroupBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var group = endpoints.MapGroup(prefix);
        configure?.Invoke(group);
        return group;
    }

    /// <summary>
    /// Creates an endpoint group with a prefix and common conventions for REST APIs.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="prefix">The route prefix (e.g., "/api/orders").</param>
    /// <param name="tag">The OpenAPI tag for the group (e.g., "Orders").</param>
    /// <param name="requireAuthorization">Whether to require authorization for all endpoints (default: false).</param>
    /// <returns>The route group builder for further configuration.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a group with:
    /// <list type="bullet">
    /// <item>Route prefix</item>
    /// <item>OpenAPI tag</item>
    /// <item>Optional authorization requirement</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var ordersGroup = app.MapMvpApiGroup("/api/orders", "Orders", requireAuthorization: true);
    /// 
    /// ordersGroup.MapGet("/", GetOrders);
    /// ordersGroup.MapGet("/{id}", GetOrderById);
    /// </code>
    /// </example>
    public static RouteGroupBuilder MapMvpApiGroup(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        string tag,
        bool requireAuthorization = false)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        var group = endpoints.MapGroup(prefix)
            .WithTags(tag);

        if (requireAuthorization)
        {
            group.RequireAuthorization();
        }

        return group;
    }

    /// <summary>
    /// Creates an endpoint group for CQRS commands with common conventions.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="prefix">The route prefix (e.g., "/api/orders").</param>
    /// <param name="tag">The OpenAPI tag (e.g., "Orders").</param>
    /// <param name="requireAuthorization">Whether to require authorization (default: true).</param>
    /// <returns>The route group builder configured for commands.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a group optimized for CQRS commands:
    /// <list type="bullet">
    /// <item>Route prefix</item>
    /// <item>OpenAPI tag</item>
    /// <item>Authorization requirement (default: true for commands)</item>
    /// <item>Common response types (200 OK, 400 BadRequest, 401 Unauthorized, 403 Forbidden)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var commandsGroup = app.MapMvpCommandsGroup("/api/orders", "Orders");
    /// 
    /// commandsGroup.MapCommand&lt;CreateOrderCommand, OrderDto&gt;("/");
    /// commandsGroup.MapCommand&lt;UpdateOrderCommand, OrderDto&gt;("/{id}");
    /// </code>
    /// </example>
    public static RouteGroupBuilder MapMvpCommandsGroup(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        string tag,
        bool requireAuthorization = true)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        var group = endpoints.MapMvpApiGroup(prefix, tag, requireAuthorization);
        return group;
    }

    /// <summary>
    /// Creates an endpoint group for CQRS queries with common conventions.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="prefix">The route prefix (e.g., "/api/orders").</param>
    /// <param name="tag">The OpenAPI tag (e.g., "Orders").</param>
    /// <param name="requireAuthorization">Whether to require authorization (default: true).</param>
    /// <returns>The route group builder configured for queries.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a group optimized for CQRS queries:
    /// <list type="bullet">
    /// <item>Route prefix</item>
    /// <item>OpenAPI tag</item>
    /// <item>Authorization requirement (default: true)</item>
    /// <item>Common response types (200 OK, 404 NotFound, 400 BadRequest, 401 Unauthorized, 403 Forbidden)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var queriesGroup = app.MapMvpQueriesGroup("/api/orders", "Orders");
    /// 
    /// queriesGroup.MapQuery&lt;GetOrderByIdQuery, OrderDto&gt;("/{id}");
    /// queriesGroup.MapQuery&lt;GetOrdersQuery, List&lt;OrderDto&gt;&gt;("/");
    /// </code>
    /// </example>
    public static RouteGroupBuilder MapMvpQueriesGroup(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        string tag,
        bool requireAuthorization = true)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        var group = endpoints.MapMvpApiGroup(prefix, tag, requireAuthorization);
        return group;
    }
}


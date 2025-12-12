//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Mvp24Hours.Core.Contract.Infrastructure
{
    /// <summary>
    /// Provides access to the current request context, including user, tenant, and correlation information.
    /// This is a fundamental infrastructure abstraction for cross-cutting concerns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Purpose:</strong>
    /// The request context provides a unified way to access contextual information
    /// that is relevant throughout the lifetime of a request, including:
    /// <list type="bullet">
    /// <item>User identity and claims</item>
    /// <item>Tenant information (for multi-tenant applications)</item>
    /// <item>Correlation ID (for distributed tracing)</item>
    /// <item>Request-scoped metadata</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Usage:</strong>
    /// Inject <see cref="IRequestContext"/> into services that need access to
    /// contextual information without depending on ASP.NET Core specific types.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class OrderService
    /// {
    ///     private readonly IRequestContext _context;
    ///     
    ///     public OrderService(IRequestContext context)
    ///     {
    ///         _context = context;
    ///     }
    ///     
    ///     public void CreateOrder(Order order)
    ///     {
    ///         order.CreatedBy = _context.UserId;
    ///         order.TenantId = _context.TenantId;
    ///         order.CorrelationId = _context.CorrelationId;
    ///         // ...
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IRequestContext
    {
        /// <summary>
        /// Gets the unique identifier for the current request.
        /// Used for distributed tracing and logging correlation.
        /// </summary>
        string CorrelationId { get; }

        /// <summary>
        /// Gets the causation ID, representing the ID of the request/event that caused this request.
        /// Useful for tracking event chains and debugging.
        /// </summary>
        string? CausationId { get; }

        /// <summary>
        /// Gets the current user's ID, or null if not authenticated.
        /// </summary>
        string? UserId { get; }

        /// <summary>
        /// Gets the current user's name, or null if not authenticated.
        /// </summary>
        string? UserName { get; }

        /// <summary>
        /// Gets the current tenant's ID, or null if not in a multi-tenant context.
        /// </summary>
        string? TenantId { get; }

        /// <summary>
        /// Gets the current user's claims principal, or null if not authenticated.
        /// </summary>
        ClaimsPrincipal? User { get; }

        /// <summary>
        /// Gets whether the current user is authenticated.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Gets the timestamp when this request context was created.
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Gets a dictionary of custom properties associated with this request context.
        /// Use this for storing request-scoped data that doesn't fit other properties.
        /// </summary>
        IDictionary<string, object> Items { get; }

        /// <summary>
        /// Gets a custom property by key.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="key">The property key.</param>
        /// <returns>The property value, or default if not found.</returns>
        T? GetItem<T>(string key);

        /// <summary>
        /// Sets a custom property.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        void SetItem<T>(string key, T value);

        /// <summary>
        /// Checks if the current user has the specified role.
        /// </summary>
        /// <param name="role">The role to check.</param>
        /// <returns>True if the user has the role; otherwise, false.</returns>
        bool IsInRole(string role);

        /// <summary>
        /// Checks if the current user has the specified claim.
        /// </summary>
        /// <param name="claimType">The claim type.</param>
        /// <param name="claimValue">The claim value (optional).</param>
        /// <returns>True if the user has the claim; otherwise, false.</returns>
        bool HasClaim(string claimType, string? claimValue = null);
    }

    /// <summary>
    /// Default implementation of <see cref="IRequestContext"/> for non-web scenarios
    /// or when no specific context is available.
    /// </summary>
    public class DefaultRequestContext : IRequestContext
    {
        private readonly Dictionary<string, object> _items = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRequestContext"/> class.
        /// </summary>
        /// <param name="correlationId">Optional correlation ID. If not provided, a new GUID is generated.</param>
        public DefaultRequestContext(string? correlationId = null)
        {
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N");
            Timestamp = DateTime.UtcNow;
        }

        /// <inheritdoc />
        public string CorrelationId { get; }

        /// <inheritdoc />
        public string? CausationId { get; set; }

        /// <inheritdoc />
        public string? UserId { get; set; }

        /// <inheritdoc />
        public string? UserName { get; set; }

        /// <inheritdoc />
        public string? TenantId { get; set; }

        /// <inheritdoc />
        public ClaimsPrincipal? User { get; set; }

        /// <inheritdoc />
        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        /// <inheritdoc />
        public DateTime Timestamp { get; }

        /// <inheritdoc />
        public IDictionary<string, object> Items => _items;

        /// <inheritdoc />
        public T? GetItem<T>(string key)
        {
            if (_items.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        /// <inheritdoc />
        public void SetItem<T>(string key, T value)
        {
            if (value is null)
            {
                _items.Remove(key);
            }
            else
            {
                _items[key] = value;
            }
        }

        /// <inheritdoc />
        public bool IsInRole(string role)
        {
            return User?.IsInRole(role) ?? false;
        }

        /// <inheritdoc />
        public bool HasClaim(string claimType, string? claimValue = null)
        {
            if (User is null) return false;

            return claimValue is null
                ? User.HasClaim(c => c.Type == claimType)
                : User.HasClaim(claimType, claimValue);
        }

        /// <summary>
        /// Creates a new context with the specified user claims.
        /// </summary>
        /// <param name="claims">The claims for the user.</param>
        /// <returns>A new configured request context.</returns>
        public static DefaultRequestContext WithUser(IEnumerable<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims, "DefaultAuth");
            var principal = new ClaimsPrincipal(identity);
            
            return new DefaultRequestContext
            {
                User = principal,
                UserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UserName = principal.FindFirst(ClaimTypes.Name)?.Value
            };
        }
    }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Marker interface for requests that require authorization.
/// Implement this interface and specify the required roles or permissions.
/// </summary>
/// <remarks>
/// <para>
/// Requests implementing this interface will be checked by
/// <see cref="AuthorizationBehavior{TRequest, TResponse}"/> before execution.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DeleteUserCommand : IMediatorCommand, IAuthorized
/// {
///     public int UserId { get; init; }
///     
///     // Require admin role
///     public IEnumerable&lt;string&gt; RequiredRoles => new[] { "Admin" };
///     
///     // Or require specific permission
///     public IEnumerable&lt;string&gt; RequiredPermissions => new[] { "Users.Delete" };
/// }
/// </code>
/// </example>
public interface IAuthorized
{
    /// <summary>
    /// Gets the roles required to execute this request.
    /// User must have at least one of the specified roles.
    /// </summary>
    IEnumerable<string> RequiredRoles => Enumerable.Empty<string>();

    /// <summary>
    /// Gets the permissions required to execute this request.
    /// User must have all of the specified permissions.
    /// </summary>
    IEnumerable<string> RequiredPermissions => Enumerable.Empty<string>();

    /// <summary>
    /// Gets the policy names required to execute this request.
    /// All policies must be satisfied.
    /// </summary>
    IEnumerable<string> RequiredPolicies => Enumerable.Empty<string>();
}

/// <summary>
/// Interface for providing information about the current user.
/// Implement this interface to integrate with your authentication system.
/// </summary>
/// <remarks>
/// <para>
/// This interface should be implemented to provide user information from
/// your authentication system (JWT, cookies, Windows Auth, etc.).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class HttpContextUserContext : IUserContext
/// {
///     private readonly IHttpContextAccessor _httpContextAccessor;
///     
///     public HttpContextUserContext(IHttpContextAccessor httpContextAccessor)
///     {
///         _httpContextAccessor = httpContextAccessor;
///     }
///     
///     public bool IsAuthenticated => 
///         _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
///         
///     public string? UserId => 
///         _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
///         
///     public IEnumerable&lt;string&gt; Roles => 
///         _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)
///             .Select(c => c.Value) ?? Enumerable.Empty&lt;string&gt;();
/// }
/// </code>
/// </example>
public interface IUserContext
{
    /// <summary>
    /// Gets whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current user's identifier.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the current user's username.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Gets the roles assigned to the current user.
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Gets the permissions granted to the current user.
    /// </summary>
    IEnumerable<string> Permissions => Enumerable.Empty<string>();

    /// <summary>
    /// Checks if the user is in the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the user is in the role, false otherwise.</returns>
    bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the user has the specified permission.
    /// </summary>
    /// <param name="permission">The permission to check.</param>
    /// <returns>True if the user has the permission, false otherwise.</returns>
    bool HasPermission(string permission) => Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Pipeline behavior that checks authorization before executing a request.
/// Only applies to requests that implement <see cref="IAuthorized"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior checks:
/// <list type="bullet">
/// <item>If the user is authenticated (throws <see cref="UnauthorizedException"/> if not)</item>
/// <item>If the user has at least one required role (throws <see cref="ForbiddenException"/> if not)</item>
/// <item>If the user has all required permissions (throws <see cref="ForbiddenException"/> if not)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddScoped&lt;IUserContext, HttpContextUserContext&gt;();
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(AuthorizationBehavior&lt;,&gt;));
/// </code>
/// </example>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IUserContext? _userContext;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>>? _logger;

    /// <summary>
    /// Creates a new instance of the AuthorizationBehavior.
    /// </summary>
    /// <param name="userContext">Optional user context for authorization checks.</param>
    /// <param name="logger">Optional logger for recording authorization operations.</param>
    /// <remarks>
    /// If no user context is provided, this behavior allows all requests.
    /// </remarks>
    public AuthorizationBehavior(
        IUserContext? userContext = null,
        ILogger<AuthorizationBehavior<TRequest, TResponse>>? logger = null)
    {
        _userContext = userContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only check authorization if the request implements IAuthorized and we have a user context
        if (request is not IAuthorized authorized || _userContext == null)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;

        // Check if user is authenticated
        if (!_userContext.IsAuthenticated)
        {
            _logger?.LogWarning(
                "[Authorization] Unauthenticated access attempt for {RequestName}",
                requestName);

            throw new UnauthorizedException(
                $"Authentication is required to execute {requestName}.");
        }

        // Check required roles (user must have at least one)
        var requiredRoles = authorized.RequiredRoles.ToList();
        if (requiredRoles.Count > 0)
        {
            var hasAnyRole = requiredRoles.Any(role => _userContext.IsInRole(role));

            if (!hasAnyRole)
            {
                _logger?.LogWarning(
                    "[Authorization] User {UserId} lacks required roles for {RequestName}. Required: {RequiredRoles}, Has: {UserRoles}",
                    _userContext.UserId,
                    requestName,
                    string.Join(", ", requiredRoles),
                    string.Join(", ", _userContext.Roles));

                throw new ForbiddenException(
                    $"You don't have permission to execute {requestName}. Required roles: {string.Join(", ", requiredRoles)}",
                    requestName,
                    "Execute",
                    string.Join(", ", requiredRoles));
            }
        }

        // Check required permissions (user must have all)
        var requiredPermissions = authorized.RequiredPermissions.ToList();
        if (requiredPermissions.Count > 0)
        {
            var missingPermissions = requiredPermissions
                .Where(p => !_userContext.HasPermission(p))
                .ToList();

            if (missingPermissions.Count > 0)
            {
                _logger?.LogWarning(
                    "[Authorization] User {UserId} lacks required permissions for {RequestName}. Missing: {MissingPermissions}",
                    _userContext.UserId,
                    requestName,
                    string.Join(", ", missingPermissions));

                throw new ForbiddenException(
                    $"You don't have permission to execute {requestName}. Missing permissions: {string.Join(", ", missingPermissions)}",
                    requestName,
                    "Execute",
                    string.Join(", ", missingPermissions));
            }
        }

        _logger?.LogDebug(
            "[Authorization] User {UserId} authorized for {RequestName}",
            _userContext.UserId,
            requestName);

        return await next();
    }
}


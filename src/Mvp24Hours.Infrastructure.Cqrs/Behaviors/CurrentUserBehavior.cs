//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.MultiTenancy;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Marker interface for requests that require an authenticated user.
/// </summary>
/// <remarks>
/// <para>
/// Requests implementing this interface will have user resolution and
/// validation performed by <see cref="CurrentUserBehavior{TRequest, TResponse}"/>.
/// </para>
/// <para>
/// This is different from <see cref="IAuthorized"/> which checks roles/permissions.
/// <see cref="IUserRequired"/> only ensures the user is authenticated.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CreateOrderCommand : IMediatorCommand&lt;int&gt;, IUserRequired
/// {
///     public string ProductName { get; init; }
///     public int Quantity { get; init; }
///     
///     // Will fail if user is not authenticated
/// }
/// </code>
/// </example>
public interface IUserRequired
{
    /// <summary>
    /// Gets whether to allow execution without an authenticated user.
    /// Defaults to false (user is required).
    /// </summary>
    bool AllowAnonymous => false;
}

/// <summary>
/// Pipeline behavior that resolves and injects the current user into the request context.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior:
/// <list type="bullet">
/// <item>Resolves the current user using <see cref="ICurrentUserFactory"/></item>
/// <item>Sets the user in <see cref="ICurrentUserAccessor"/></item>
/// <item>Validates user requirements for <see cref="IUserRequired"/> requests</item>
/// </list>
/// </para>
/// <para>
/// <strong>Execution Order:</strong>
/// <code>
/// ┌──────────────────────────────────────────────────────────────────────────────┐
/// │ 1. Create user from current context via ICurrentUserFactory                 │
/// │ 2. Set user in ICurrentUserAccessor                                         │
/// │ 3. If request implements IUserRequired and no user: throw exception         │
/// │ 4. Execute next behavior/handler                                            │
/// │ 5. Clear user context on completion                                         │
/// └──────────────────────────────────────────────────────────────────────────────┘
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(CurrentUserBehavior&lt;,&gt;));
/// services.AddScoped&lt;ICurrentUserFactory, HttpContextCurrentUserFactory&gt;();
/// services.AddScoped&lt;ICurrentUserAccessor, CurrentUserAccessor&gt;();
/// </code>
/// </example>
public sealed class CurrentUserBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ICurrentUserFactory? _currentUserFactory;
    private readonly ILogger<CurrentUserBehavior<TRequest, TResponse>>? _logger;

    /// <summary>
    /// Creates a new instance of the CurrentUserBehavior.
    /// </summary>
    /// <param name="currentUserAccessor">The current user accessor.</param>
    /// <param name="currentUserFactory">Optional factory for creating the current user.</param>
    /// <param name="logger">Optional logger.</param>
    public CurrentUserBehavior(
        ICurrentUserAccessor currentUserAccessor,
        ICurrentUserFactory? currentUserFactory = null,
        ILogger<CurrentUserBehavior<TRequest, TResponse>>? logger = null)
    {
        _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
        _currentUserFactory = currentUserFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var previousUser = _currentUserAccessor.User;

        try
        {
            // Resolve current user
            ICurrentUser? currentUser = null;
            
            if (_currentUserFactory != null)
            {
                currentUser = _currentUserFactory.CreateFromCurrentContext();
                
                if (currentUser?.IsAuthenticated == true)
                {
                    _logger?.LogDebug(
                        "[CurrentUser] User {UserId} ({UserName}) resolved for {RequestName}",
                        currentUser.Id,
                        currentUser.Name,
                        requestName);
                }
                else
                {
                    _logger?.LogDebug(
                        "[CurrentUser] No authenticated user for {RequestName}",
                        requestName);
                }
            }

            // Set the user context
            _currentUserAccessor.User = currentUser;

            // Validate user requirements
            if (request is IUserRequired userRequired)
            {
                if (!userRequired.AllowAnonymous && (currentUser == null || !currentUser.IsAuthenticated))
                {
                    _logger?.LogWarning(
                        "[CurrentUser] No authenticated user for {RequestName} which requires authentication",
                        requestName);

                    throw new UnauthorizedException(
                        $"Authentication is required to execute {requestName}.");
                }
            }

            // Execute the request
            return await next();
        }
        finally
        {
            // Restore previous user
            _currentUserAccessor.User = previousUser;
        }
    }
}


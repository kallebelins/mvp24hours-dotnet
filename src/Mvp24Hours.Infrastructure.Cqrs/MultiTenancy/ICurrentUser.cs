//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.MultiTenancy;

/// <summary>
/// Represents the current authenticated user.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a simplified view of the current user for use in
/// commands, queries, and domain logic. It focuses on identity and claims
/// rather than authentication mechanisms.
/// </para>
/// <para>
/// <strong>Relationship with IUserContext:</strong>
/// <code>
/// ┌─────────────────────────────────────────────────────────────────────────────┐
/// │ IUserContext (in AuthorizationBehavior)                                     │
/// │   - Used for authorization checks (roles, permissions)                      │
/// │   - Typically tied to authentication mechanism (JWT, cookies)               │
/// │   - May include detailed claim information                                  │
/// │                                                                             │
/// │ ICurrentUser (this interface)                                               │
/// │   - Simplified user identity for business logic                             │
/// │   - Provides Id, Name, Email for domain operations                         │
/// │   - Decoupled from authentication mechanism                                 │
/// └─────────────────────────────────────────────────────────────────────────────┘
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CreateOrderCommandHandler : IMediatorCommandHandler&lt;CreateOrderCommand, int&gt;
/// {
///     private readonly ICurrentUser _currentUser;
///     private readonly IOrderRepository _orderRepository;
///     
///     public CreateOrderCommandHandler(ICurrentUser currentUser, IOrderRepository orderRepository)
///     {
///         _currentUser = currentUser;
///         _orderRepository = orderRepository;
///     }
///     
///     public async Task&lt;int&gt; Handle(CreateOrderCommand request, CancellationToken ct)
///     {
///         var order = new Order
///         {
///             CreatedBy = _currentUser.Id,
///             CreatedByName = _currentUser.Name,
///             // ... other properties
///         };
///         
///         return await _orderRepository.AddAsync(order, ct);
///     }
/// }
/// </code>
/// </example>
public interface ICurrentUser
{
    /// <summary>
    /// Gets the unique identifier of the current user.
    /// </summary>
    /// <remarks>
    /// Returns null if the user is not authenticated.
    /// </remarks>
    string? Id { get; }

    /// <summary>
    /// Gets the name of the current user.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the email address of the current user.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the roles assigned to the current user.
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Gets additional claims for the current user.
    /// </summary>
    IReadOnlyDictionary<string, string?> Claims { get; }

    /// <summary>
    /// Checks if the user has the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the user has the role, false otherwise.</returns>
    bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a claim value by type.
    /// </summary>
    /// <param name="claimType">The claim type.</param>
    /// <returns>The claim value, or null if not found.</returns>
    string? GetClaim(string claimType) => Claims.TryGetValue(claimType, out var value) ? value : null;
}

/// <summary>
/// Mutable interface for setting the current user context.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// Gets or sets the current user.
    /// </summary>
    ICurrentUser? User { get; set; }
}

/// <summary>
/// Factory interface for creating current user instances.
/// </summary>
public interface ICurrentUserFactory
{
    /// <summary>
    /// Creates a current user from the current authentication context.
    /// </summary>
    /// <returns>The current user, or null if not authenticated.</returns>
    ICurrentUser? CreateFromCurrentContext();
}

/// <summary>
/// Marker interface for entities that track user creation and modification.
/// </summary>
/// <remarks>
/// Entities implementing this interface will have user tracking automatically applied.
/// </remarks>
/// <example>
/// <code>
/// public class Order : IHasUserTracking
/// {
///     public int Id { get; set; }
///     public string? CreatedBy { get; set; }
///     public string? CreatedByName { get; set; }
///     public DateTimeOffset CreatedAt { get; set; }
///     public string? ModifiedBy { get; set; }
///     public string? ModifiedByName { get; set; }
///     public DateTimeOffset? ModifiedAt { get; set; }
/// }
/// </code>
/// </example>
public interface IHasUserTracking
{
    /// <summary>
    /// Gets or sets the ID of the user who created this entity.
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who created this entity.
    /// </summary>
    string? CreatedByName { get; set; }

    /// <summary>
    /// Gets or sets when this entity was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who last modified this entity.
    /// </summary>
    string? ModifiedBy { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who last modified this entity.
    /// </summary>
    string? ModifiedByName { get; set; }

    /// <summary>
    /// Gets or sets when this entity was last modified.
    /// </summary>
    DateTimeOffset? ModifiedAt { get; set; }
}


//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.MultiTenancy;

/// <summary>
/// Default implementation of <see cref="ICurrentUser"/>.
/// </summary>
/// <remarks>
/// This is an immutable record that contains user identity information.
/// </remarks>
public sealed record CurrentUser : ICurrentUser
{
    /// <summary>
    /// Creates a new current user with the specified values.
    /// </summary>
    public CurrentUser(
        string? id = null,
        string? name = null,
        string? email = null,
        bool isAuthenticated = false,
        IEnumerable<string>? roles = null,
        IReadOnlyDictionary<string, string?>? claims = null)
    {
        Id = id;
        Name = name;
        Email = email;
        IsAuthenticated = isAuthenticated;
        Roles = roles?.ToList() ?? new List<string>();
        Claims = claims ?? new Dictionary<string, string?>();
    }

    /// <inheritdoc />
    public string? Id { get; }

    /// <inheritdoc />
    public string? Name { get; }

    /// <inheritdoc />
    public string? Email { get; }

    /// <inheritdoc />
    public bool IsAuthenticated { get; }

    /// <inheritdoc />
    public IEnumerable<string> Roles { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string?> Claims { get; }

    /// <inheritdoc />
    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetClaim(string claimType) => Claims.TryGetValue(claimType, out var value) ? value : null;

    /// <summary>
    /// Creates an anonymous (non-authenticated) user.
    /// </summary>
    public static CurrentUser Anonymous => new();

    /// <summary>
    /// Creates an authenticated user with the specified ID.
    /// </summary>
    public static CurrentUser FromId(string id, string? name = null) => 
        new(id: id, name: name, isAuthenticated: true);

    /// <summary>
    /// Creates an authenticated user with ID, name, and email.
    /// </summary>
    public static CurrentUser Create(string id, string name, string? email = null, IEnumerable<string>? roles = null) => 
        new(id: id, name: name, email: email, isAuthenticated: true, roles: roles);
}

/// <summary>
/// Default implementation of <see cref="ICurrentUserAccessor"/> using AsyncLocal for ambient context.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses <see cref="AsyncLocal{T}"/> to maintain the current user
/// across async operations within the same logical execution flow.
/// </para>
/// </remarks>
public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private static readonly AsyncLocal<UserHolder> _userHolder = new();

    /// <inheritdoc />
    public ICurrentUser? User
    {
        get => _userHolder.Value?.User;
        set
        {
            var holder = _userHolder.Value;
            if (holder != null)
            {
                holder.User = null;
            }

            if (value != null)
            {
                _userHolder.Value = new UserHolder { User = value };
            }
        }
    }

    private sealed class UserHolder
    {
        public ICurrentUser? User;
    }
}


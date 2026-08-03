namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;

/// <summary>
/// Request to assign realm or client roles to a user.
/// </summary>
public sealed record AssignRolesRequest
{
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Optional client UUID when assigning client roles. Null means realm roles.
    /// </summary>
    public string? ClientUuid { get; init; }

    public IReadOnlyList<RoleRepresentation> Roles { get; init; } = [];

    public bool IsValid => Validate().Count == 0;

    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(UserId))
        {
            errors.Add($"{nameof(UserId)} is required.");
        }

        if (Roles is null || Roles.Count == 0)
        {
            errors.Add($"{nameof(Roles)} must contain at least one role.");
        }
        else if (Roles.Any(role => string.IsNullOrWhiteSpace(role.Name) && string.IsNullOrWhiteSpace(role.Id)))
        {
            errors.Add("Each role must have an Id or Name.");
        }

        return errors;
    }
}

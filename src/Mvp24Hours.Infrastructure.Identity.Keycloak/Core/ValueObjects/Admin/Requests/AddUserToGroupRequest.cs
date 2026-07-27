namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;

/// <summary>
/// Request to add a user to a group via the Keycloak Admin API.
/// </summary>
public sealed record AddUserToGroupRequest
{
    public string UserId { get; init; } = string.Empty;

    public string GroupId { get; init; } = string.Empty;

    public bool IsValid => Validate().Count == 0;

    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(UserId))
        {
            errors.Add($"{nameof(UserId)} is required.");
        }

        if (string.IsNullOrWhiteSpace(GroupId))
        {
            errors.Add($"{nameof(GroupId)} is required.");
        }

        return errors;
    }
}

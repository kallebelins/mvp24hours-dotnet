namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;

/// <summary>
/// Request to create a group via the Keycloak Admin API.
/// </summary>
public sealed record CreateGroupRequest
{
    public string Name { get; init; } = string.Empty;

    public string? ParentGroupId { get; init; }

    public Dictionary<string, IReadOnlyList<string>>? Attributes { get; init; }

    public IReadOnlyList<string>? RealmRoles { get; init; }

    public Dictionary<string, IReadOnlyList<string>>? ClientRoles { get; init; }

    public bool IsValid => Validate().Count == 0;

    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add($"{nameof(Name)} is required.");
        }

        return errors;
    }
}

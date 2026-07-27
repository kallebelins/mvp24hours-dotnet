namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;

/// <summary>
/// Request to update an existing user via the Keycloak Admin API.
/// </summary>
public sealed record UpdateUserRequest
{
    public string UserId { get; init; } = string.Empty;

    public string? Username { get; init; }

    public string? Email { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public bool? Enabled { get; init; }

    public bool? EmailVerified { get; init; }

    public IReadOnlyList<string>? RequiredActions { get; init; }

    public Dictionary<string, IReadOnlyList<string>>? Attributes { get; init; }

    public bool IsValid => Validate().Count == 0;

    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(UserId))
        {
            errors.Add($"{nameof(UserId)} is required.");
        }

        return errors;
    }
}

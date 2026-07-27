namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;

/// <summary>
/// Request to create a user via the Keycloak Admin API.
/// </summary>
public sealed record CreateUserRequest
{
    public string Username { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public bool Enabled { get; init; } = true;

    public bool EmailVerified { get; init; }

    public string? TemporaryPassword { get; init; }

    public bool TemporaryPasswordRequired { get; init; } = true;

    public IReadOnlyList<string>? RequiredActions { get; init; }

    public Dictionary<string, IReadOnlyList<string>>? Attributes { get; init; }

    public IReadOnlyList<string>? Groups { get; init; }

    public bool IsValid => Validate().Count == 0;

    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(Username))
        {
            errors.Add($"{nameof(Username)} is required.");
        }

        return errors;
    }
}

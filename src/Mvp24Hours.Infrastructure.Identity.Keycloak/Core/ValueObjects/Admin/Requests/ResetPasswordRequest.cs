namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;

/// <summary>
/// Request to reset a user's password via the Keycloak Admin API.
/// </summary>
public sealed record ResetPasswordRequest
{
    public string UserId { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public bool Temporary { get; init; } = true;

    public string Type { get; init; } = "password";

    public bool IsValid => Validate().Count == 0;

    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(UserId))
        {
            errors.Add($"{nameof(UserId)} is required.");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            errors.Add($"{nameof(Password)} is required.");
        }

        if (string.IsNullOrWhiteSpace(Type))
        {
            errors.Add($"{nameof(Type)} is required.");
        }

        return errors;
    }
}

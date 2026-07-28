namespace CustomerAPI.Core.DTOs.Admin;

/// <summary>
/// Request body for creating a Keycloak user via the Admin API.
/// </summary>
public sealed record CreateKeycloakUserDto(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string TemporaryPassword,
    bool Enabled = true);

namespace CustomerAPI.Core.DTOs.Admin;

/// <summary>
/// Request body for resetting a Keycloak user's password.
/// </summary>
public sealed record ResetPasswordDto(
    string UserId,
    string NewPassword,
    bool Temporary = true);

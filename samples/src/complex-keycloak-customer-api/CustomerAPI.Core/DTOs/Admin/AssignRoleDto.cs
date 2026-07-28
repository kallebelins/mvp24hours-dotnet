namespace CustomerAPI.Core.DTOs.Admin;

/// <summary>
/// Request body for assigning a realm role to a Keycloak user.
/// </summary>
public sealed record AssignRoleDto(
    string UserId,
    string RoleId,
    string RoleName);

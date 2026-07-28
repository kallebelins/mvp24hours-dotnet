using CustomerAPI.Core.DTOs.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;

namespace CustomerAPI.WebAPI.Controllers;

/// <summary>
/// Internal Keycloak Admin API flows: create user, reset password, assign realm role.
/// Requires the <c>realm-admin</c> role — restrict to internal networks in production.
/// </summary>
[ApiController]
[Route("api/admin/keycloak")]
[Authorize(Roles = "realm-admin")]
public sealed class AdminController(
    IKeycloakUserService userService,
    ILogger<AdminController> logger) : ControllerBase
{
    /// <summary>
    /// Creates a new user in the Keycloak realm.
    /// </summary>
    [HttpPost("users")]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateKeycloakUserDto dto,
        CancellationToken cancellationToken)
    {
        var request = new CreateUserRequest
        {
            Username = dto.Username,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Enabled = dto.Enabled,
            TemporaryPassword = dto.TemporaryPassword,
            TemporaryPasswordRequired = true,
            EmailVerified = false
        };

        if (!request.IsValid)
        {
            return BadRequest(request.Validate());
        }

        var result = await userService.CreateUserAsync(request, cancellationToken);

        if (result.HasErrors)
        {
            logger.LogWarning("Keycloak CreateUser failed: {Errors}", result.Messages);
            return BadRequest(result.Messages);
        }

        string userId = result.GetDataValue() ?? string.Empty;
        logger.LogInformation("Keycloak user created: {UserId}", userId);

        return CreatedAtAction(
            nameof(GetUser),
            new { userId },
            new { userId });
    }

    /// <summary>
    /// Gets a Keycloak user by identifier.
    /// </summary>
    [HttpGet("users/{userId}")]
    [ProducesResponseType(typeof(UserRepresentation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUser(
        string userId,
        CancellationToken cancellationToken)
    {
        var result = await userService.GetUserByIdAsync(userId, cancellationToken);

        if (result.HasErrors || result.GetDataValue() is null)
        {
            return NotFound();
        }

        return Ok(result.GetDataValue());
    }

    /// <summary>
    /// Resets a user's password in the Keycloak realm.
    /// </summary>
    [HttpPost("users/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto dto,
        CancellationToken cancellationToken)
    {
        var request = new ResetPasswordRequest
        {
            UserId = dto.UserId,
            Password = dto.NewPassword,
            Temporary = dto.Temporary
        };

        if (!request.IsValid)
        {
            return BadRequest(request.Validate());
        }

        var result = await userService.ResetPasswordAsync(request, cancellationToken);

        if (result.HasErrors)
        {
            logger.LogWarning("Keycloak ResetPassword failed: {Errors}", result.Messages);
            return BadRequest(result.Messages);
        }

        return NoContent();
    }

    /// <summary>
    /// Assigns a realm role to an existing Keycloak user.
    /// </summary>
    [HttpPost("users/assign-role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignRole(
        [FromBody] AssignRoleDto dto,
        CancellationToken cancellationToken)
    {
        var request = new AssignRolesRequest
        {
            UserId = dto.UserId,
            Roles = [new RoleRepresentation { Id = dto.RoleId, Name = dto.RoleName }]
        };

        if (!request.IsValid)
        {
            return BadRequest(request.Validate());
        }

        var result = await userService.AssignRealmRolesAsync(request, cancellationToken);

        if (result.HasErrors)
        {
            logger.LogWarning("Keycloak AssignRole failed: {Errors}", result.Messages);
            return BadRequest(result.Messages);
        }

        return NoContent();
    }
}

using Microsoft.AspNetCore.Builder;
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Middlewares;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;

/// <summary>
/// Keycloak middleware pipeline extensions.
/// </summary>
public static class KeycloakApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the current Keycloak user middleware. Call after <c>UseAuthentication</c>.
    /// </summary>
    public static IApplicationBuilder UseKeycloakCurrentUser(
        this IApplicationBuilder application)
    {
        return application.UseMiddleware<KeycloakCurrentUserMiddleware>();
    }
}

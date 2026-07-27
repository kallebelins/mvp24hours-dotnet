using Microsoft.AspNetCore.Http;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;

/// <summary>
/// Resolves the parsed Keycloak user associated with the current HTTP request.
/// </summary>
public sealed class KeycloakCurrentUser(
    IHttpContextAccessor httpContextAccessor) : IKeycloakCurrentUser
{
    public UserToken? User =>
        httpContextAccessor.HttpContext?.Items[KeycloakHttpContextKeys.User] as UserToken;

    public Guid? UserId => User?.Id;

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}

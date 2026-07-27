using Microsoft.AspNetCore.Http;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Middlewares;

/// <summary>
/// Parses the bearer token once and exposes its user through the current request.
/// </summary>
public sealed class KeycloakCurrentUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IKeycloakJwtTokenParser parser)
    {
        string? authorization = context.Request.Headers.Authorization.FirstOrDefault();
        UserToken? user = parser.ParseUserToken(authorization);
        if (user is not null)
        {
            context.Items[KeycloakHttpContextKeys.User] = user;
        }

        await next(context);
    }
}

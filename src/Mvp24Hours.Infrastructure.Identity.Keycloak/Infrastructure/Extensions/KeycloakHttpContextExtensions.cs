using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Extensions;

/// <summary>
/// Helpers that resolve Keycloak authorization headers and parsed users from the current request.
/// </summary>
public static class KeycloakHttpContextExtensions
{
    /// <summary>
    /// Gets the raw Authorization header value for the current HTTP request.
    /// </summary>
    public static string? GetAuthorization(this IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault();
    }

    /// <summary>
    /// Gets the parsed Keycloak user from the current HTTP request services.
    /// </summary>
    public static UserToken? GetUserToken(this IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<IHttpContextAccessor>()?.GetUserToken();
    }

    /// <summary>
    /// Gets the parsed Keycloak user from request items, or parses the bearer token when needed.
    /// </summary>
    public static UserToken? GetUserToken(this IHttpContextAccessor httpContextAccessor)
    {
        HttpContext? context = httpContextAccessor.HttpContext;
        if (context?.Items[KeycloakHttpContextKeys.User] is UserToken currentUser)
        {
            return currentUser;
        }

        IKeycloakJwtTokenParser? parser = context?
            .RequestServices
            .GetService<IKeycloakJwtTokenParser>();
        return parser?.ParseUserToken(httpContextAccessor.GetAuthorization());
    }

    /// <summary>
    /// Gets the Keycloak subject identifier from the current HTTP request services.
    /// </summary>
    public static Guid? GetUserId(this IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<IHttpContextAccessor>()?.GetUserId();
    }

    /// <summary>
    /// Gets the Keycloak subject identifier from the parsed current-user token.
    /// </summary>
    public static Guid? GetUserId(this IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.GetUserToken()?.Id;
    }
}

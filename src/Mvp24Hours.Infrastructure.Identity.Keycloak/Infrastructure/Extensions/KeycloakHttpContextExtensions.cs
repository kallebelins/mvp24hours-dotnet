using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Extensions;

public static class KeycloakHttpContextExtensions
{
    public static string? GetAuthorization(this IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault();
    }

    public static UserToken? GetUserToken(this IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<IHttpContextAccessor>()?.GetUserToken();
    }

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

    public static Guid? GetUserId(this IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<IHttpContextAccessor>()?.GetUserId();
    }

    public static Guid? GetUserId(this IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.GetUserToken()?.Id;
    }
}

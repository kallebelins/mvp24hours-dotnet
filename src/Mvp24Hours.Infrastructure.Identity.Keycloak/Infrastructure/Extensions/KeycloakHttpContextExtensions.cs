using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
        string? jwt = GetRawToken(httpContextAccessor.GetAuthorization());
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return null;
        }

        JwtSecurityToken token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
        return new UserToken
        {
            Id = GetGuid(token, "sub"),
            Name = GetString(token, "name"),
            PreferredUserName = GetString(token, "preferred_username"),
            Email = GetString(token, "email"),
            EmailVerified = GetBoolean(token, "email_verified"),
            Scope = GetString(token, "scope"),
            SessionId = GetGuid(token, "sid"),
            SessionState = GetGuid(token, "session_state"),
            AuthorizedParty = GetString(token, "azp"),
            AllowedOrigins = GetStringArray(token, "allowed-origins"),
            RealmRoles = GetNestedStringArray(token, "realm_access", "roles"),
            ResourceRoles = GetNestedStringArray(token, "resource_access", "account", "roles")
        };
    }

    public static Guid? GetUserId(this IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<IHttpContextAccessor>()?.GetUserId();
    }

    public static Guid? GetUserId(this IHttpContextAccessor httpContextAccessor)
    {
        string? jwt = GetRawToken(httpContextAccessor.GetAuthorization());
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return null;
        }

        return GetGuid(new JwtSecurityTokenHandler().ReadJwtToken(jwt), "sub");
    }

    private static string? GetRawToken(string? authorizationValue)
    {
        if (string.IsNullOrWhiteSpace(authorizationValue))
        {
            return null;
        }

        const string bearerPrefix = "Bearer ";
        return authorizationValue.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorizationValue[bearerPrefix.Length..].Trim()
            : authorizationValue.Trim();
    }

    private static string? GetString(JwtSecurityToken token, string claim)
    {
        return token.Payload.TryGetValue(claim, out object? value) ? value?.ToString() : null;
    }

    private static Guid? GetGuid(JwtSecurityToken token, string claim)
    {
        return Guid.TryParse(GetString(token, claim), out Guid value) ? value : null;
    }

    private static bool? GetBoolean(JwtSecurityToken token, string claim)
    {
        return bool.TryParse(GetString(token, claim), out bool value) ? value : null;
    }

    private static List<string>? GetStringArray(JwtSecurityToken token, string claim)
    {
        if (!token.Payload.TryGetValue(claim, out object? value) || value is null)
        {
            return null;
        }

        JsonElement element = JsonSerializer.SerializeToElement(value);
        return element.ValueKind == JsonValueKind.Array
            ? [.. element.EnumerateArray()
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!)]
            : null;
    }

    private static List<string>? GetNestedStringArray(
        JwtSecurityToken token,
        string claim,
        params string[] path)
    {
        if (!token.Payload.TryGetValue(claim, out object? value) || value is null)
        {
            return null;
        }

        JsonElement element = JsonSerializer.SerializeToElement(value);
        foreach (string segment in path)
        {
            if (element.ValueKind != JsonValueKind.Object
                || !element.TryGetProperty(segment, out element))
            {
                return null;
            }
        }

        return element.ValueKind == JsonValueKind.Array
            ? [.. element.EnumerateArray()
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!)]
            : null;
    }
}

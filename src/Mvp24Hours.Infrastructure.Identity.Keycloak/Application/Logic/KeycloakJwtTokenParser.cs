using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;

/// <summary>
/// Parses claims from Keycloak JSON Web Tokens.
/// </summary>
public sealed class KeycloakJwtTokenParser(
    IOptions<KeycloakOptions> options,
    ILogger<KeycloakJwtTokenParser> logger) : IKeycloakJwtTokenParser
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly KeycloakOptions _options = options.Value;

    public UserToken? ParseUserToken(string? jwt)
    {
        using JsonDocument? payload = ReadPayload(jwt);
        return payload is null ? null : UserToken.FromJsonElement(payload.RootElement);
    }

    public Guid? ParseUserId(string? jwt)
    {
        return ParseUserToken(jwt)?.Id;
    }

    public IReadOnlyDictionary<string, JsonElement>? ParseClaims(string? jwt)
    {
        using JsonDocument? payload = ReadPayload(jwt);
        if (payload is null || payload.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return payload.RootElement
            .EnumerateObject()
            .ToDictionary(
                property => property.Name,
                property => property.Value.Clone(),
                StringComparer.Ordinal);
    }

    public bool IsExpired(string? jwt)
    {
        DateTimeOffset? expiration = GetExpiration(jwt);
        return expiration is null
            || DateTimeOffset.UtcNow > expiration.Value.Add(_options.TokenClockSkew);
    }

    public DateTimeOffset? GetExpiration(string? jwt)
    {
        return ParseUserToken(jwt)?.ExpiresAt;
    }

    private JsonDocument? ReadPayload(string? jwt)
    {
        string? rawToken = GetRawToken(jwt);
        if (string.IsNullOrWhiteSpace(rawToken) || !_tokenHandler.CanReadToken(rawToken))
        {
            return null;
        }

        try
        {
            JwtSecurityToken token = _tokenHandler.ReadJwtToken(rawToken);
            string payloadJson = JsonSerializer.Serialize(token.Payload);
            return JsonDocument.Parse(payloadJson);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or JsonException
            or SecurityTokenException)
        {
            logger.LogDebug(exception, "Unable to parse the Keycloak JWT payload.");
            return null;
        }
    }

    private static string? GetRawToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        const string bearerPrefix = "Bearer ";
        return value.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? value[bearerPrefix.Length..].Trim()
            : value.Trim();
    }
}

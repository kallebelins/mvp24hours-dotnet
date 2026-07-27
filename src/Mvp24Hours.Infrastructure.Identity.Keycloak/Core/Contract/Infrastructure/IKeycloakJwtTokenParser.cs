using System.Text.Json;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;

/// <summary>
/// Reads Keycloak JWT payloads without validating their signature.
/// </summary>
/// <remarks>
/// Token validation remains the responsibility of the JWT bearer authentication handler.
/// </remarks>
public interface IKeycloakJwtTokenParser
{
    /// <summary>
    /// Parses the token payload into a Keycloak user representation.
    /// </summary>
    UserToken? ParseUserToken(string? jwt);

    /// <summary>
    /// Parses the Keycloak subject identifier.
    /// </summary>
    Guid? ParseUserId(string? jwt);

    /// <summary>
    /// Parses all payload claims while preserving their JSON value types.
    /// </summary>
    IReadOnlyDictionary<string, JsonElement>? ParseClaims(string? jwt);

    /// <summary>
    /// Returns whether the token is expired, including the configured clock skew.
    /// Invalid tokens are treated as expired.
    /// </summary>
    bool IsExpired(string? jwt);

    /// <summary>
    /// Gets the token expiration time, when present and valid.
    /// </summary>
    DateTimeOffset? GetExpiration(string? jwt);
}

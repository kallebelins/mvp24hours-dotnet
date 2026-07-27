using System.Text.Json.Serialization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;

/// <summary>
/// OAuth 2.0 token introspection response (RFC 7662) from Keycloak.
/// </summary>
public class TokenIntrospectionResponse
{
    [JsonPropertyName("active")]
    public bool Active { get; init; }

    [JsonPropertyName("sub")]
    public string? Subject { get; init; }

    [JsonPropertyName("exp")]
    public long? Expiration { get; init; }

    [JsonPropertyName("iat")]
    public long? IssuedAt { get; init; }

    [JsonPropertyName("nbf")]
    public long? NotBefore { get; init; }

    [JsonPropertyName("jti")]
    public string? JwtId { get; init; }

    [JsonPropertyName("iss")]
    public string? Issuer { get; init; }

    [JsonPropertyName("aud")]
    public object? Audience { get; init; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [JsonPropertyName("azp")]
    public string? AuthorizedParty { get; init; }

    [JsonPropertyName("session_state")]
    public string? SessionState { get; init; }

    [JsonPropertyName("sid")]
    public string? SessionId { get; init; }
}

using System.Text.Json.Serialization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;

/// <summary>
/// Subset of OpenID Connect Discovery document used by this library.
/// </summary>
public class OpenIdConnectConfigurationDocument
{
    [JsonPropertyName("issuer")]
    public string? Issuer { get; init; }

    [JsonPropertyName("authorization_endpoint")]
    public string? AuthorizationEndpoint { get; init; }

    [JsonPropertyName("token_endpoint")]
    public string? TokenEndpoint { get; init; }

    [JsonPropertyName("introspection_endpoint")]
    public string? IntrospectionEndpoint { get; init; }

    [JsonPropertyName("revocation_endpoint")]
    public string? RevocationEndpoint { get; init; }

    [JsonPropertyName("userinfo_endpoint")]
    public string? UserInfoEndpoint { get; init; }

    [JsonPropertyName("jwks_uri")]
    public string? JwksUri { get; init; }

    [JsonPropertyName("end_session_endpoint")]
    public string? EndSessionEndpoint { get; init; }

    [JsonPropertyName("grant_types_supported")]
    public IReadOnlyList<string>? GrantTypesSupported { get; init; }

    [JsonPropertyName("response_types_supported")]
    public IReadOnlyList<string>? ResponseTypesSupported { get; init; }

    [JsonPropertyName("scopes_supported")]
    public IReadOnlyList<string>? ScopesSupported { get; init; }

    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public IReadOnlyList<string>? TokenEndpointAuthMethodsSupported { get; init; }
}

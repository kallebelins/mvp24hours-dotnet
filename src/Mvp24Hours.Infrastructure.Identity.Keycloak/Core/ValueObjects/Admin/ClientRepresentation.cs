using System.Text.Json.Serialization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;

/// <summary>
/// Keycloak Admin API client representation.
/// </summary>
public class ClientRepresentation
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("clientId")]
    public string? ClientId { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    [JsonPropertyName("protocol")]
    public string? Protocol { get; init; }

    [JsonPropertyName("publicClient")]
    public bool? PublicClient { get; init; }

    [JsonPropertyName("bearerOnly")]
    public bool? BearerOnly { get; init; }

    [JsonPropertyName("confidentialPort")]
    public int? ConfidentialPort { get; init; }

    [JsonPropertyName("secret")]
    public string? Secret { get; init; }

    [JsonPropertyName("serviceAccountsEnabled")]
    public bool? ServiceAccountsEnabled { get; init; }

    [JsonPropertyName("authorizationServicesEnabled")]
    public bool? AuthorizationServicesEnabled { get; init; }

    [JsonPropertyName("directAccessGrantsEnabled")]
    public bool? DirectAccessGrantsEnabled { get; init; }

    [JsonPropertyName("standardFlowEnabled")]
    public bool? StandardFlowEnabled { get; init; }

    [JsonPropertyName("implicitFlowEnabled")]
    public bool? ImplicitFlowEnabled { get; init; }

    [JsonPropertyName("frontchannelLogout")]
    public bool? FrontchannelLogout { get; init; }

    [JsonPropertyName("fullScopeAllowed")]
    public bool? FullScopeAllowed { get; init; }

    [JsonPropertyName("rootUrl")]
    public string? RootUrl { get; init; }

    [JsonPropertyName("baseUrl")]
    public string? BaseUrl { get; init; }

    [JsonPropertyName("adminUrl")]
    public string? AdminUrl { get; init; }

    [JsonPropertyName("redirectUris")]
    public IReadOnlyList<string>? RedirectUris { get; init; }

    [JsonPropertyName("webOrigins")]
    public IReadOnlyList<string>? WebOrigins { get; init; }

    [JsonPropertyName("defaultClientScopes")]
    public IReadOnlyList<string>? DefaultClientScopes { get; init; }

    [JsonPropertyName("optionalClientScopes")]
    public IReadOnlyList<string>? OptionalClientScopes { get; init; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, string>? Attributes { get; init; }
}

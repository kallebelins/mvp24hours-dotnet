using System.Text.Json.Serialization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;

/// <summary>
/// Keycloak Admin API user representation.
/// </summary>
public class UserRepresentation
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("emailVerified")]
    public bool? EmailVerified { get; init; }

    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    [JsonPropertyName("createdTimestamp")]
    public long? CreatedTimestamp { get; init; }

    [JsonPropertyName("totp")]
    public bool? Totp { get; init; }

    [JsonPropertyName("federationLink")]
    public string? FederationLink { get; init; }

    [JsonPropertyName("serviceAccountClientId")]
    public string? ServiceAccountClientId { get; init; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, IReadOnlyList<string>>? Attributes { get; init; }

    [JsonPropertyName("requiredActions")]
    public IReadOnlyList<string>? RequiredActions { get; init; }

    [JsonPropertyName("realmRoles")]
    public IReadOnlyList<string>? RealmRoles { get; init; }

    [JsonPropertyName("clientRoles")]
    public Dictionary<string, IReadOnlyList<string>>? ClientRoles { get; init; }

    [JsonPropertyName("groups")]
    public IReadOnlyList<string>? Groups { get; init; }

    [JsonPropertyName("credentials")]
    public IReadOnlyList<CredentialRepresentation>? Credentials { get; init; }

    [JsonPropertyName("disableableCredentialTypes")]
    public IReadOnlyList<string>? DisableableCredentialTypes { get; init; }

    [JsonPropertyName("access")]
    public Dictionary<string, bool>? Access { get; init; }
}

using System.Text.Json.Serialization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;

/// <summary>
/// Keycloak Admin API credential representation (password, OTP, etc.).
/// </summary>
public class CredentialRepresentation
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("userLabel")]
    public string? UserLabel { get; init; }

    [JsonPropertyName("createdDate")]
    public long? CreatedDate { get; init; }

    [JsonPropertyName("secretData")]
    public string? SecretData { get; init; }

    [JsonPropertyName("credentialData")]
    public string? CredentialData { get; init; }

    [JsonPropertyName("priority")]
    public int? Priority { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("temporary")]
    public bool? Temporary { get; init; }
}

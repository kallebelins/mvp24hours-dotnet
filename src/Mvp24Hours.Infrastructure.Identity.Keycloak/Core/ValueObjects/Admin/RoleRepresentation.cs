using System.Text.Json.Serialization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;

/// <summary>
/// Keycloak Admin API role representation.
/// </summary>
public class RoleRepresentation
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("composite")]
    public bool? Composite { get; init; }

    [JsonPropertyName("clientRole")]
    public bool? ClientRole { get; init; }

    [JsonPropertyName("containerId")]
    public string? ContainerId { get; init; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, IReadOnlyList<string>>? Attributes { get; init; }
}

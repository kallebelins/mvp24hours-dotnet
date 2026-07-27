using System.Text.Json.Serialization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;

/// <summary>
/// Keycloak Admin API group representation.
/// </summary>
public class GroupRepresentation
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("parentId")]
    public string? ParentId { get; init; }

    [JsonPropertyName("subGroupCount")]
    public long? SubGroupCount { get; init; }

    [JsonPropertyName("subGroups")]
    public IReadOnlyList<GroupRepresentation>? SubGroups { get; init; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, IReadOnlyList<string>>? Attributes { get; init; }

    [JsonPropertyName("realmRoles")]
    public IReadOnlyList<string>? RealmRoles { get; init; }

    [JsonPropertyName("clientRoles")]
    public Dictionary<string, IReadOnlyList<string>>? ClientRoles { get; init; }

    [JsonPropertyName("access")]
    public Dictionary<string, bool>? Access { get; init; }
}

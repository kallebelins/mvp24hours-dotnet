using System.Text.Json.Serialization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

public class Resource(string name, string[] scopes)
{
    public string Name { get; } = name;

    public string? Type { get; set; }

    [JsonPropertyName("resource_scopes")]
    public string[] Scopes { get; } = scopes;

    public Dictionary<string, string> Attributes { get; } = [];
}

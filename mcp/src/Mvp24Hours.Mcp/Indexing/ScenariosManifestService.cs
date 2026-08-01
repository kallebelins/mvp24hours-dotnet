using System.Text.Json;
using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Models;

namespace Mvp24Hours.Mcp.Indexing;

public sealed class ScenariosManifestService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly RepoRootResolver _paths;
    private readonly Lazy<(ScenariosManifest Manifest, string RawJson)> _manifest;

    public ScenariosManifestService(RepoRootResolver paths)
    {
        _paths = paths;
        _manifest = new Lazy<(ScenariosManifest, string)>(LoadManifest);
    }

    public ScenariosManifest Manifest => _manifest.Value.Manifest;

    public string RawJson => _manifest.Value.RawJson;

    public ScenarioDefinition? GetScenario(string id) =>
        Manifest.Scenarios.FirstOrDefault(s =>
            string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<ScenarioDefinition> GetAllScenarios() => Manifest.Scenarios;

    public string GetDiscoveryPlaybookPath() => Manifest.DiscoveryPlaybook;

    public void Warmup() => _ = Manifest;

    private (ScenariosManifest Manifest, string RawJson) LoadManifest()
    {
        var json = File.ReadAllText(_paths.ScenariosManifestPath);
        var manifest = JsonSerializer.Deserialize<ScenariosManifest>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse scenarios-manifest.json");

        var ids = manifest.Scenarios.Select(s => s.Id).ToList();
        if (ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count)
        {
            throw new InvalidOperationException("Duplicate scenario IDs in scenarios-manifest.json");
        }

        return (manifest, json);
    }
}

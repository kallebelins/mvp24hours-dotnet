using System.Text.Json;
using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Models;

namespace Mvp24Hours.Mcp.Indexing;

public sealed class ManifestService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly RepoRootResolver _paths;
    private readonly Lazy<(TemplatesManifest Manifest, string RawJson)> _manifest;

    public ManifestService(RepoRootResolver paths)
    {
        _paths = paths;
        _manifest = new Lazy<(TemplatesManifest, string)>(LoadManifest);
    }

    public TemplatesManifest Manifest => _manifest.Value.Manifest;

    public string RawJson => _manifest.Value.RawJson;

    public ArchitectureTemplate? GetTemplate(string id) =>
        Manifest.Templates.FirstOrDefault(t =>
            string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<ArchitectureTemplate> GetAllTemplates() => Manifest.Templates;

    public void Warmup() => _ = Manifest;

    private (TemplatesManifest Manifest, string RawJson) LoadManifest()
    {
        var json = File.ReadAllText(_paths.ManifestPath);
        var manifest = JsonSerializer.Deserialize<TemplatesManifest>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse templates-manifest.json");

        var ids = manifest.Templates.Select(t => t.Id).ToList();
        if (ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count)
        {
            throw new InvalidOperationException("Duplicate template IDs in templates-manifest.json");
        }

        foreach (var template in manifest.Templates)
        {
            var docFull = _paths.ResolveRepoRelative(template.DocPath);
            if (!File.Exists(docFull))
            {
                throw new InvalidOperationException($"Manifest docPath not found: {template.DocPath}");
            }
        }

        return (manifest, json);
    }
}

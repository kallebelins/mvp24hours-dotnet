using System.Text.Json;
using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Models;

namespace Mvp24Hours.Mcp.Indexing;

public sealed class CapabilitiesManifestService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly RepoRootResolver _paths;
    private readonly Lazy<(CapabilitiesManifest Manifest, string RawJson)> _manifest;

    public CapabilitiesManifestService(RepoRootResolver paths)
    {
        _paths = paths;
        _manifest = new Lazy<(CapabilitiesManifest, string)>(LoadManifest);
    }

    public CapabilitiesManifest Manifest => _manifest.Value.Manifest;

    public string RawJson => _manifest.Value.RawJson;

    public CapabilityDefinition? GetCapability(string id) =>
        Manifest.Capabilities.FirstOrDefault(c =>
            string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<CapabilityDefinition> GetAllCapabilities() => Manifest.Capabilities;

    public CapabilityResolution? Resolve(string featureKeyword, string? templateId = null)
    {
        var text = featureKeyword.ToLowerInvariant();
        var matches = Manifest.Capabilities
            .Select(c => new
            {
                Capability = c,
                Score = c.Keywords.Count(k => text.Contains(k, StringComparison.OrdinalIgnoreCase))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Capability.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matches.Count == 0 &&
            Manifest.Capabilities.FirstOrDefault(c =>
                string.Equals(c.Id, featureKeyword, StringComparison.OrdinalIgnoreCase)) is { } exact)
        {
            matches = [new { Capability = exact, Score = 1 }];
        }

        var best = matches.FirstOrDefault();
        if (best is null)
        {
            return null;
        }

        var capability = best.Capability;
        var rationale = $"Matched capability '{capability.Id}' from keyword '{featureKeyword}'.";

        if (!string.IsNullOrWhiteSpace(templateId) &&
            !string.IsNullOrWhiteSpace(capability.TemplateId) &&
            !string.Equals(capability.TemplateId, templateId, StringComparison.OrdinalIgnoreCase))
        {
            rationale += $" Note: capability default template is '{capability.TemplateId}', current template is '{templateId}'.";
        }

        return new CapabilityResolution
        {
            CapabilityId = capability.Id,
            DocPath = capability.DocPath,
            ReferenceSample = capability.ReferenceSample,
            TemplateId = capability.TemplateId,
            Symbols = capability.Symbols,
            RelatedDocs = capability.RelatedDocs,
            ComplianceRules = capability.ComplianceRules,
            Rationale = rationale
        };
    }

    public void Warmup() => _ = Manifest;

    private (CapabilitiesManifest Manifest, string RawJson) LoadManifest()
    {
        var json = File.ReadAllText(_paths.CapabilitiesManifestPath);
        var manifest = JsonSerializer.Deserialize<CapabilitiesManifest>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse capabilities-manifest.json");

        var ids = manifest.Capabilities.Select(c => c.Id).ToList();
        if (ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count)
        {
            throw new InvalidOperationException("Duplicate capability IDs in capabilities-manifest.json");
        }

        return (manifest, json);
    }
}

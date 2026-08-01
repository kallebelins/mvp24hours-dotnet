using System.Text.Json;
using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Models;

namespace Mvp24Hours.Mcp.Indexing;

public sealed class MigrationPlaybookService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly RepoRootResolver _paths;
    private readonly ManifestService _templates;
    private readonly Lazy<(MigrationPlaybooksManifest Manifest, string RawJson)> _playbooks;

    public MigrationPlaybookService(RepoRootResolver paths, ManifestService templates)
    {
        _paths = paths;
        _templates = templates;
        _playbooks = new Lazy<(MigrationPlaybooksManifest, string)>(LoadManifest);
    }

    public MigrationPlaybooksManifest Manifest => _playbooks.Value.Manifest;

    public string RawJson => _playbooks.Value.RawJson;

    public MigrationPlaybook? GetPlaybook(string id) =>
        Manifest.Playbooks.FirstOrDefault(p =>
            string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    public ArchitectureMigrationPlan? PlanMigration(string sourceTemplateId, string targetTemplateId)
    {
        var source = _templates.GetTemplate(sourceTemplateId);
        var target = _templates.GetTemplate(targetTemplateId);

        if (source is null || target is null)
        {
            return null;
        }

        var pairKey = $"{sourceTemplateId}:{targetTemplateId}";
        Manifest.TemplatePairs.TryGetValue(pairKey, out var playbookId);

        var playbook = playbookId is not null ? GetPlaybook(playbookId) : null;

        var sourceLayers = source.Layers.ToDictionary(l => l.Name, StringComparer.OrdinalIgnoreCase);
        var targetLayers = target.Layers.ToDictionary(l => l.Name, StringComparer.OrdinalIgnoreCase);

        var changes = new List<LayerMigrationChange>();

        foreach (var layer in targetLayers.Values)
        {
            if (!sourceLayers.ContainsKey(layer.Name))
            {
                changes.Add(new LayerMigrationChange
                {
                    ChangeType = "Add",
                    LayerName = layer.Name,
                    Pattern = layer.Pattern,
                    Responsibilities = layer.Responsibilities
                });
            }
        }

        foreach (var layer in sourceLayers.Values)
        {
            if (!targetLayers.ContainsKey(layer.Name))
            {
                changes.Add(new LayerMigrationChange
                {
                    ChangeType = "Remove",
                    LayerName = layer.Name,
                    Pattern = layer.Pattern,
                    Responsibilities = layer.Responsibilities
                });
            }
            else
            {
                var targetLayer = targetLayers[layer.Name];
                if (!string.Equals(layer.Pattern, targetLayer.Pattern, StringComparison.OrdinalIgnoreCase) ||
                    !layer.Responsibilities.SequenceEqual(targetLayer.Responsibilities))
                {
                    changes.Add(new LayerMigrationChange
                    {
                        ChangeType = "Modify",
                        LayerName = layer.Name,
                        Pattern = targetLayer.Pattern,
                        Responsibilities = targetLayer.Responsibilities
                    });
                }
            }
        }

        var rationale = new List<string>
        {
            $"Migrating from '{source.Id}' ({source.Shape}) to '{target.Id}' ({target.Shape}).",
            $"Source reference sample: {source.ReferenceSample}",
            $"Target reference sample: {target.ReferenceSample}"
        };

        if (playbook is not null)
        {
            rationale.Add($"Matched playbook: {playbook.Id} — {playbook.Title}");
        }
        else
        {
            rationale.Add("No predefined playbook for this template pair — use layer diff and samples.");
        }

        return new ArchitectureMigrationPlan
        {
            SourceTemplateId = source.Id,
            TargetTemplateId = target.Id,
            PlaybookId = playbook?.Id,
            SourceSample = playbook?.SourceSample ?? source.ReferenceSample,
            TargetSample = playbook?.TargetSample ?? target.ReferenceSample,
            LayerChanges = changes,
            Rationale = rationale,
            Steps = playbook?.Steps ?? []
        };
    }

    public void Warmup() => _ = Manifest;

    private (MigrationPlaybooksManifest Manifest, string RawJson) LoadManifest()
    {
        var json = File.ReadAllText(_paths.MigrationPlaybooksPath);
        var manifest = JsonSerializer.Deserialize<MigrationPlaybooksManifest>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse migration-playbooks.json");

        var ids = manifest.Playbooks.Select(p => p.Id).ToList();
        if (ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count)
        {
            throw new InvalidOperationException("Duplicate playbook IDs in migration-playbooks.json");
        }

        return (manifest, json);
    }
}

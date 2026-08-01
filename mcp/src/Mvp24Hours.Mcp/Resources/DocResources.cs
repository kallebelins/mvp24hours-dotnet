using System.ComponentModel;
using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Indexing;
using ModelContextProtocol.Server;

namespace Mvp24Hours.Mcp.Resources;

[McpServerResourceType]
public static class DocResources
{
    [McpServerResource(UriTemplate = "mvp24hours://manifest", Name = "Templates Manifest", MimeType = "application/json")]
    public static string GetManifest(ManifestService manifest) =>
        manifest.RawJson;

    [McpServerResource(UriTemplate = "mvp24hours://docs/{*path}", Name = "Documentation Page", MimeType = "text/markdown")]
    public static string GetDocumentation(DocIndexService docIndex, string path)
    {
        var content = docIndex.GetDoc(path);
        return content ?? $"Document not found: docs/en-us/{path}";
    }

    [McpServerResource(UriTemplate = "mvp24hours://templates/{id}", Name = "Architecture Template", MimeType = "text/markdown")]
    public static string GetTemplate(ManifestService manifest, DocIndexService docIndex, string id)
    {
        var template = manifest.GetTemplate(id);
        if (template is null)
        {
            return $"Template '{id}' not found.";
        }

        return docIndex.GetDocByRepoPath(template.DocPath) ?? $"Doc not found: {template.DocPath}";
    }

    [McpServerResource(UriTemplate = "mvp24hours://layers/{name}", Name = "Layer Template", MimeType = "text/markdown")]
    public static string GetLayer(ManifestService manifest, RepoRootResolver paths, string name)
    {
        if (manifest.Manifest.LayerDocs.TryGetValue(name, out var docPath))
        {
            var full = paths.ResolveRepoRelative(docPath);
            if (File.Exists(full))
            {
                return File.ReadAllText(full);
            }
        }

        var fallback = Path.Combine(paths.LayersPath, $"layer-{name}.md");
        return File.Exists(fallback) ? File.ReadAllText(fallback) : $"Layer '{name}' not found.";
    }

    [McpServerResource(UriTemplate = "mvp24hours://samples/{id}/readme", Name = "Sample README", MimeType = "text/markdown")]
    public static string GetSampleReadme(SampleCatalogService samples, RepoRootResolver paths, string id)
    {
        var readmePath = samples.FindReadmePath(id);
        if (readmePath is null)
        {
            return $"README not found for sample '{id}'.";
        }

        var full = paths.ResolveRepoRelative(readmePath);
        return File.ReadAllText(full);
    }

    [McpServerResource(UriTemplate = "mvp24hours://scenarios", Name = "Scenarios Manifest", MimeType = "application/json")]
    public static string GetScenarios(ScenariosManifestService scenarios) =>
        scenarios.RawJson;

    [McpServerResource(UriTemplate = "mvp24hours://capabilities", Name = "Capabilities Manifest", MimeType = "application/json")]
    public static string GetCapabilities(CapabilitiesManifestService capabilities) =>
        capabilities.RawJson;

    [McpServerResource(UriTemplate = "mvp24hours://migration/{id}", Name = "Migration Playbook", MimeType = "application/json")]
    public static string GetMigrationPlaybookResource(MigrationPlaybookService migration, string id)
    {
        var playbook = migration.GetPlaybook(id);
        return playbook is null
            ? $"{{\"error\":\"Playbook '{id}' not found.\"}}"
            : System.Text.Json.JsonSerializer.Serialize(playbook, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerResource(UriTemplate = "mvp24hours://discovery", Name = "Discovery Playbook", MimeType = "text/markdown")]
    public static string GetDiscovery(RepoRootResolver paths) =>
        File.Exists(paths.DiscoveryPlaybookPath)
            ? File.ReadAllText(paths.DiscoveryPlaybookPath)
            : "Discovery playbook not found.";
}

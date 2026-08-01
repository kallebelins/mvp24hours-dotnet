using System.Text;
using System.Text.Json;
using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Indexing;
using ModelContextProtocol.Server;

namespace Mvp24Hours.Mcp.Tools;

[McpServerToolType]
public static class ArchitectureTools
{
    [McpServerTool, Description("Recommend an architecture template from constraints and situation description.")]
    public static string ResolveArchitecture(
        ArchitectureResolver resolver,
        [Description("Describe the situation, e.g. small CRUD API, event-driven with RabbitMQ")] string situation,
        [Description("Team size (optional)")] string? teamSize = null,
        [Description("Requires messaging integration")] bool messaging = false,
        [Description("Requires CQRS")] bool cqrs = false)
    {
        var resolution = resolver.Resolve(situation, teamSize, messaging, cqrs);
        return JsonSerializer.Serialize(resolution, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Return architecture template markdown and metadata by manifest id.")]
    public static string GetArchitectureTemplate(
        ManifestService manifest,
        DocIndexService docIndex,
        McpOptions options,
        [Description("Template id, e.g. simple-nlayers, cqrs, clean-architecture")] string templateId)
    {
        var template = manifest.GetTemplate(templateId);
        if (template is null)
        {
            return $"Template '{templateId}' not found. Known ids: {string.Join(", ", manifest.GetAllTemplates().Select(t => t.Id))}";
        }

        var doc = docIndex.GetDocByRepoPath(template.DocPath);
        var sb = new StringBuilder();
        sb.AppendLine($"# Template: {template.Id} ({template.Tier})");
        sb.AppendLine($"Shape: {template.Shape}");
        sb.AppendLine($"Reference sample: {template.ReferenceSample}");
        sb.AppendLine();
        sb.AppendLine("## Layers");
        sb.AppendLine(JsonSerializer.Serialize(template.Layers, new JsonSerializerOptions { WriteIndented = true }));
        sb.AppendLine();
        sb.AppendLine("## Documentation");
        if (doc is null)
        {
            sb.AppendLine($"Doc not found at {template.DocPath}");
        }
        else if (doc.Length > options.MaxFileBytes)
        {
            sb.AppendLine(doc[..options.MaxFileBytes]);
            sb.AppendLine($"\n[Truncated at {options.MaxFileBytes} bytes]");
        }
        else
        {
            sb.AppendLine(doc);
        }

        return sb.ToString();
    }

    [McpServerTool, Description("List layers and dependencies for an architecture template.")]
    public static string ListLayers(
        ManifestService manifest,
        [Description("Template id")] string templateId)
    {
        var template = manifest.GetTemplate(templateId);
        if (template is null)
        {
            return $"Template '{templateId}' not found.";
        }

        return JsonSerializer.Serialize(template.Layers, new JsonSerializerOptions { WriteIndented = true });
    }
}

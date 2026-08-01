using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Indexing;
using Mvp24Hours.Mcp.Models;
using ModelContextProtocol.Server;

namespace Mvp24Hours.Mcp.Tools;

[McpServerToolType]
public static class ReferenceTools
{
    [McpServerTool, Description("Full-text search across docs/en-us markdown files.")]
    public static string SearchDocs(
        DocIndexService docIndex,
        McpOptions options,
        [Description("Search query")] string query)
    {
        var hits = docIndex.Search(query, options.MaxSearchResults);
        if (hits.Count == 0)
        {
            return "No matches found.";
        }

        var sb = new StringBuilder();
        foreach (var hit in hits)
        {
            sb.AppendLine($"## docs/en-us/{hit.RelativePath} (score {hit.Score})");
            sb.AppendLine($"Title: {hit.Title}");
            sb.AppendLine(hit.Snippet);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    [McpServerTool, Description("Read a markdown document from docs/en-us by relative path.")]
    public static string GetDoc(
        DocIndexService docIndex,
        McpOptions options,
        [Description("Path under docs/en-us, e.g. webapi.md or guides/architecture/home.md")] string path)
    {
        var content = docIndex.GetDoc(path);
        if (content is null)
        {
            return $"Document not found: {path}";
        }

        if (content.Length > options.MaxFileBytes)
        {
            return content[..options.MaxFileBytes] + $"\n\n[Truncated at {options.MaxFileBytes} bytes. Full path: docs/en-us/{path}]";
        }

        return content;
    }

    [McpServerTool, Description("List runnable samples under samples/src with optional filters.")]
    public static string ListSamples(
        SampleCatalogService samples,
        [Description("Filter by tier: Minimal, Simple, Complex, Blueprint, Capability")] string? tier = null,
        [Description("Filter by architecture shape keyword")] string? shape = null,
        [Description("Filter by technology keyword (ef, mongodb, rabbitmq, etc.)")] string? technology = null)
    {
        var list = samples.Filter(tier, shape, technology);
        return JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Return a directory tree for a sample by id.")]
    public static string GetSampleTree(
        SampleCatalogService samples,
        [Description("Sample id, e.g. complex-crud-ef-customer-api")] string sampleId,
        [Description("Max depth (default 4)")] int maxDepth = 4)
    {
        return samples.BuildTree(sampleId, maxDepth);
    }

    [McpServerTool, Description("Read a file from a sample directory.")]
    public static string GetSampleFile(
        SampleCatalogService samples,
        McpOptions options,
        [Description("Sample id")] string sampleId,
        [Description("File path relative to sample root")] string relativeFilePath)
    {
        var content = samples.ReadSampleFile(sampleId, relativeFilePath, options.MaxFileBytes);
        return content ?? $"File not found: {relativeFilePath} in sample {sampleId}";
    }
}

using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Indexing;
using ModelContextProtocol.Server;

namespace Mvp24Hours.Mcp.Tools;

[McpServerToolType]
public static class ScaffoldTools
{
    [McpServerTool, Description("Suggest solution/project tree from an architecture template.")]
    public static string SuggestProjectStructure(
        ManifestService manifest,
        [Description("Template id")] string templateId,
        [Description("Product name prefix, e.g. CustomerAPI or Product")] string productName = "Product")
    {
        var template = manifest.GetTemplate(templateId);
        if (template is null)
        {
            return $"Template '{templateId}' not found.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{productName}.slnx");
        sb.AppendLine($"Reference sample: {template.ReferenceSample}");
        sb.AppendLine();

        foreach (var layer in template.Layers)
        {
            var projectName = layer.Pattern?.Replace("{Product}", productName) ?? layer.Name;
            sb.AppendLine($"├── {projectName}/");
            if (layer.Responsibilities.Count > 0)
            {
                sb.AppendLine($"│   └── {string.Join(", ", layer.Responsibilities)}");
            }

            if (layer.DependsOn.Count > 0)
            {
                sb.AppendLine($"│   depends on: {string.Join(", ", layer.DependsOn)}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Suggested csproj skeleton:");
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk.Web\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>net10.0</TargetFramework>");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("</Project>");

        return sb.ToString();
    }

    [McpServerTool, Description("Return parameterized test templates from samples/templates/.")]
    public static string GetTestScaffold(
        ManifestService manifest,
        RepoRootResolver paths,
        McpOptions options,
        [Description("Template id for default test templates")] string templateId,
        [Description("Test project namespace")] string testNamespace = "CustomerAPI.Test",
        [Description("DbContext type name")] string dbContext = "EFDBContext",
        [Description("Template file name under samples/templates/, e.g. SAMPLE_TEST_CustomerApiFactory.cs.template")] string? templateFile = null)
    {
        var template = manifest.GetTemplate(templateId);
        var files = templateFile is not null
            ? [templateFile]
            : (template?.TestTemplates.Count > 0
                ? template.TestTemplates
                : manifest.Manifest.TestTemplates);

        if (files.Count == 0)
        {
            files = ["samples/templates/SAMPLE_TEST_CustomerApiFactory.cs.template"];
        }

        var sb = new StringBuilder();
        foreach (var file in files.Take(5))
        {
            var full = paths.ResolveRepoRelative(file);
            if (!File.Exists(full))
            {
                sb.AppendLine($"# Missing: {file}");
                continue;
            }

            var content = File.ReadAllText(full)
                .Replace("{Namespace}", testNamespace, StringComparison.Ordinal)
                .Replace("{DbContext}", dbContext, StringComparison.Ordinal);

            sb.AppendLine($"# {file}");
            if (content.Length > options.MaxFileBytes)
            {
                sb.AppendLine(content[..options.MaxFileBytes]);
                sb.AppendLine("[Truncated]");
            }
            else
            {
                sb.AppendLine(content);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    [McpServerTool, Description("Return parameterized README scaffold from samples/templates/.")]
    public static string GetReadmeScaffold(
        RepoRootResolver paths,
        [Description("Sample display name")] string sampleName = "Customer API",
        [Description("One-paragraph description")] string description = "Demonstrates Mvp24Hours patterns.")
    {
        var path = paths.ResolveRepoRelative("samples/templates/SAMPLE_README.template.md");
        if (!File.Exists(path))
        {
            return "README template not found at samples/templates/SAMPLE_README.template.md";
        }

        return File.ReadAllText(path)
            .Replace("<Sample name>", sampleName, StringComparison.Ordinal)
            .Replace("<One-paragraph description of the problem demonstrated by this sample.>", description, StringComparison.Ordinal);
    }

    [McpServerTool, Description("DI registration hints from reference sample Program.cs and related module docs.")]
    public static string GetDiRegistrationHints(
        ManifestService manifest,
        SampleCatalogService samples,
        McpOptions options,
        [Description("Architecture template id")] string templateId)
    {
        var template = manifest.GetTemplate(templateId);
        if (template is null)
        {
            return $"Template '{templateId}' not found.";
        }

        var sampleId = Path.GetFileName(template.ReferenceSample.TrimEnd('/'));
        var programFiles = new[] { "Program.cs", "CustomerAPI.WebAPI/Program.cs", "CustomerAPI/Program.cs" };
        var sb = new StringBuilder();
        sb.AppendLine($"Reference sample: {template.ReferenceSample}");
        sb.AppendLine();

        foreach (var file in programFiles)
        {
            var content = samples.ReadSampleFile(sampleId, file, options.MaxFileBytes);
            if (content is not null && !content.StartsWith("[Truncated", StringComparison.Ordinal))
            {
                sb.AppendLine($"## {file}");
                sb.AppendLine(content);
                sb.AppendLine();
            }
        }

        sb.AppendLine("## Related module documentation");
        foreach (var doc in template.RelatedDocs)
        {
            sb.AppendLine($"- {doc}");
        }

        return sb.ToString();
    }
}

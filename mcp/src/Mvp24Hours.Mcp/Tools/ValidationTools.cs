using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Mvp24Hours.Mcp.Indexing;
using ModelContextProtocol.Server;

namespace Mvp24Hours.Mcp.Tools;

[McpServerToolType]
public static class ValidationTools
{
    [McpServerTool, Description("Search for a type or API name under src/Mvp24Hours.*/")]
    public static string FindSourceSymbol(
        SourceIndexService sourceIndex,
        [Description("Symbol or API name, e.g. AddMvpMediator")] string symbol,
        [Description("Max results")] int maxResults = 20)
    {
        var hits = sourceIndex.FindSymbol(symbol, maxResults);
        if (hits.Count == 0)
        {
            return $"No matches for '{symbol}' under src/.";
        }

        return JsonSerializer.Serialize(hits, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Map a module name to test projects under src/Tests/.")]
    public static string FindTestsForModule(
        SourceIndexService sourceIndex,
        [Description("Module name, e.g. Mvp24Hours.Core or WebAPI")] string moduleName)
    {
        var tests = sourceIndex.FindTestsForModule(moduleName);
        if (tests.Count == 0)
        {
            return $"No test projects found for module '{moduleName}'.";
        }

        return string.Join(Environment.NewLine, tests);
    }

    [McpServerTool, Description("Check repo-relative paths against Mvp24Hours compliance rules.")]
    public static string RunComplianceCheck(
        ComplianceService compliance,
        [Description("Comma-separated repo-relative paths (files or directories)")] string paths,
        [Description("Optional architecture template id for template-specific rules")] string? templateId = null,
        [Description("Optional scenario id for scenario-specific context")] string? scenarioId = null)
    {
        var pathList = paths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = compliance.CheckPaths(pathList, templateId, scenarioId);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Verify that an API name mentioned in docs exists in src/ or src/Tests/.")]
    public static string VerifyDocClaim(
        SourceIndexService sourceIndex,
        [Description("API or type name from documentation")] string apiName)
    {
        var inSource = sourceIndex.SymbolExists(apiName);
        var inTests = sourceIndex.FindTestsForModule(apiName).Count > 0;

        var sb = new StringBuilder();
        sb.AppendLine($"API '{apiName}':");
        sb.AppendLine($"  Found in src/: {inSource}");
        sb.AppendLine($"  Related tests: {inTests}");

        if (!inSource && !inTests)
        {
            sb.AppendLine("  Status: NOT VERIFIED — check src/ and src/Tests/ manually.");
        }
        else
        {
            sb.AppendLine("  Status: VERIFIED (partial — confirm behavior in tests).");
        }

        return sb.ToString();
    }
}

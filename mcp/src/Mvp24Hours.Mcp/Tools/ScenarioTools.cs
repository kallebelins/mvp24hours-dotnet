using System.Text;
using System.Text.Json;
using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Indexing;
using ModelContextProtocol.Server;

namespace Mvp24Hours.Mcp.Tools;

[McpServerToolType]
public static class ScenarioTools
{
    [McpServerTool, Description("List available development scenarios from the scenarios manifest.")]
    public static string ListScenarios(ScenariosManifestService scenarios)
    {
        var list = scenarios.GetAllScenarios().Select(s => new
        {
            s.Id,
            s.Title,
            s.Prompt,
            s.DiscoveryRequired,
            s.Inputs
        });

        return JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Return the step-by-step playbook for a development scenario.")]
    public static string GetScenarioPlaybook(
        ScenariosManifestService scenarios,
        [Description("Scenario id, e.g. greenfield-api, port-to-mvp24hours")] string scenarioId)
    {
        var scenario = scenarios.GetScenario(scenarioId);
        if (scenario is null)
        {
            return $"Scenario '{scenarioId}' not found. Known ids: {string.Join(", ", scenarios.GetAllScenarios().Select(s => s.Id))}";
        }

        return JsonSerializer.Serialize(scenario, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Return the language-agnostic discovery playbook for porting external code to Mvp24Hours.")]
    public static string GetDiscoveryPlaybook(
        ScenariosManifestService scenarios,
        RepoRootResolver paths,
        DocIndexService docIndex,
        McpOptions options)
    {
        var playbookPath = scenarios.GetDiscoveryPlaybookPath();
        var content = docIndex.GetDocByRepoPath(playbookPath)
            ?? (File.Exists(paths.DiscoveryPlaybookPath) ? File.ReadAllText(paths.DiscoveryPlaybookPath) : null);

        if (content is null)
        {
            return "Discovery playbook not found.";
        }

        var sb = new StringBuilder();
        sb.AppendLine(content);

        if (content.Length > options.MaxFileBytes)
        {
            return content[..options.MaxFileBytes] + $"\n[Truncated at {options.MaxFileBytes} bytes]";
        }

        sb.AppendLine();
        sb.AppendLine("## Recommended tools after discovery");
        sb.AppendLine("- resolve_architecture");
        sb.AppendLine("- list_layers");
        sb.AppendLine("- search_sample_patterns");
        sb.AppendLine("- get_sample_file");
        sb.AppendLine("- get_di_registration_hints");
        sb.AppendLine("- suggest_project_structure");
        sb.AppendLine("- verify_doc_claim");
        sb.AppendLine("- run_compliance_check");

        return sb.ToString();
    }

    [McpServerTool, Description("Resolve a feature keyword to docs, sample, symbols, and compliance rules.")]
    public static string ResolveFeature(
        CapabilitiesManifestService capabilities,
        [Description("Feature keyword, e.g. cqrs, rabbitmq, keycloak")] string featureKeyword,
        [Description("Optional current architecture template id")] string? templateId = null)
    {
        var resolution = capabilities.Resolve(featureKeyword, templateId);
        if (resolution is null)
        {
            return $"No capability matched for '{featureKeyword}'. Try search_docs or list_samples.";
        }

        return JsonSerializer.Serialize(resolution, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Plan migration between two architecture templates with layer diff and playbook steps.")]
    public static string PlanArchitectureMigration(
        MigrationPlaybookService migration,
        [Description("Source template id, e.g. simple-nlayers")] string sourceTemplateId,
        [Description("Target template id, e.g. complex-nlayers")] string targetTemplateId)
    {
        var plan = migration.PlanMigration(sourceTemplateId, targetTemplateId);
        if (plan is null)
        {
            return $"Could not plan migration from '{sourceTemplateId}' to '{targetTemplateId}'. Check template ids.";
        }

        return JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Return a structured migration playbook by id.")]
    public static string GetMigrationPlaybook(
        MigrationPlaybookService migration,
        [Description("Playbook id, e.g. simple-to-complex-nlayers, legacy-to-native-apis")] string playbookId)
    {
        var playbook = migration.GetPlaybook(playbookId);
        if (playbook is null)
        {
            return $"Playbook '{playbookId}' not found. Known ids: {string.Join(", ", migration.Manifest.Playbooks.Select(p => p.Id))}";
        }

        return JsonSerializer.Serialize(playbook, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Search for API patterns across sample implementations.")]
    public static string SearchSamplePatterns(
        SamplePatternIndexService patternIndex,
        [Description("Pattern to search, e.g. AddMvpMediator, DbContext, MapOpenApi")] string pattern,
        [Description("Max results")] int maxResults = 20)
    {
        var hits = patternIndex.Search(pattern, maxResults);
        if (hits.Count == 0)
        {
            return $"No matches for '{pattern}' in samples/.";
        }

        return JsonSerializer.Serialize(hits, new JsonSerializerOptions { WriteIndented = true });
    }
}

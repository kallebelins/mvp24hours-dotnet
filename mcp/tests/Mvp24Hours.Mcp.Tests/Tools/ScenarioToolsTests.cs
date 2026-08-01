using Mvp24Hours.Mcp.Indexing;
using Mvp24Hours.Mcp.Tools;

namespace Mvp24Hours.Mcp.Tests.Tools;

public class ScenarioToolsTests : McpTestFixture
{
    [Fact]
    public void GetDiscoveryPlaybook_returns_content()
    {
        var paths = CreatePaths();
        var scenarios = new ScenariosManifestService(paths);
        var docIndex = new DocIndexService(paths);
        var options = new Mvp24Hours.Mcp.Configuration.McpOptions { RepoRoot = paths.RepoRoot };

        var result = ScenarioTools.GetDiscoveryPlaybook(scenarios, paths, docIndex, options);
        Assert.Contains("Phase A", result, StringComparison.Ordinal);
        Assert.Contains("resolve_architecture", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveFeature_returns_cqrs_capability()
    {
        var capabilities = new CapabilitiesManifestService(CreatePaths());
        var result = ScenarioTools.ResolveFeature(capabilities, "cqrs");
        Assert.Contains("complex-cqrs-ef-customer-api", result, StringComparison.Ordinal);
    }
}

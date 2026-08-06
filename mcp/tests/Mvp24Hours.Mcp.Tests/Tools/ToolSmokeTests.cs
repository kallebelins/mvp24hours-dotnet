using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Indexing;
using Mvp24Hours.Mcp.Tools;

namespace Mvp24Hours.Mcp.Tests.Tools;

public class ToolSmokeTests : McpTestFixture
{
    [Fact]
    public void GetArchitectureTemplate_returns_content_for_simple_nlayers()
    {
        var paths = CreatePaths();
        var options = new McpOptions { RepoRoot = paths.RepoRoot };
        var manifest = new ManifestService(paths);
        var docIndex = new DocIndexService(paths);

        var result = ArchitectureTools.GetArchitectureTemplate(manifest, docIndex, options, "simple-nlayers");
        Assert.Contains("Simple N-Layers", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Reference sample", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetTestScaffold_substitutes_placeholders()
    {
        var paths = CreatePaths();
        var options = new McpOptions { RepoRoot = paths.RepoRoot };
        var manifest = new ManifestService(paths);

        var result = ScaffoldTools.GetTestScaffold(
            manifest,
            paths,
            options,
            "simple-nlayers",
            "MyProduct.Test",
            "MyDbContext",
            "samples/templates/SAMPLE_TEST_CustomerApiFactory.cs.template");

        Assert.Contains("MyProduct.Test", result, StringComparison.Ordinal);
        Assert.Contains("MyDbContext", result, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyDocClaim_returns_usage_when_api_name_is_missing()
    {
        var source = new SourceIndexService(CreatePaths());

        var result = ValidationTools.VerifyDocClaim(source);

        Assert.Contains("Usage: verify_doc_claim", result, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyDocClaim_reports_known_symbol_status()
    {
        var source = new SourceIndexService(CreatePaths());

        var result = ValidationTools.VerifyDocClaim(source, "IMediator");

        Assert.Contains("API 'IMediator':", result, StringComparison.Ordinal);
        Assert.Contains("Status:", result, StringComparison.Ordinal);
    }
}

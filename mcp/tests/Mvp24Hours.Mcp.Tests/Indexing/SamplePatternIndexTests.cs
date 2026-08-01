using Mvp24Hours.Mcp.Indexing;

namespace Mvp24Hours.Mcp.Tests.Indexing;

public class SamplePatternIndexTests : McpTestFixture
{
    [Fact]
    public void Search_finds_AddMvpMediator_in_cqrs_sample()
    {
        var paths = CreatePaths();
        var manifest = new ManifestService(paths);
        var samples = new SampleCatalogService(paths, manifest);
        var index = new SamplePatternIndexService(paths, samples);
        index.Warmup();

        var hits = index.Search("AddMvpMediator", 10);
        Assert.NotEmpty(hits);
        Assert.Contains(hits, h => h.SampleId.Contains("cqrs", StringComparison.OrdinalIgnoreCase));
    }
}

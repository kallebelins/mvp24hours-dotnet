using Mvp24Hours.Mcp.Indexing;

namespace Mvp24Hours.Mcp.Tests.Indexing;

public class SampleCatalogServiceTests : McpTestFixture
{
    [Fact]
    public void GetAll_indexes_at_least_32_samples()
    {
        var paths = CreatePaths();
        var manifest = new ManifestService(paths);
        var samples = new SampleCatalogService(paths, manifest);

        Assert.True(samples.GetAll().Count >= 32);
    }

    [Fact]
    public void GetById_finds_canonical_complex_sample()
    {
        var paths = CreatePaths();
        var manifest = new ManifestService(paths);
        var samples = new SampleCatalogService(paths, manifest);

        var sample = samples.GetById("complex-crud-ef-customer-api");
        Assert.NotNull(sample);
        Assert.Equal("Complex", sample!.Tier);
    }

    [Fact]
    public void BuildTree_returns_directory_structure()
    {
        var paths = CreatePaths();
        var manifest = new ManifestService(paths);
        var samples = new SampleCatalogService(paths, manifest);

        var tree = samples.BuildTree("minimal-crud-ef-customer-api");
        Assert.Contains("CustomerAPI", tree, StringComparison.OrdinalIgnoreCase);
    }
}

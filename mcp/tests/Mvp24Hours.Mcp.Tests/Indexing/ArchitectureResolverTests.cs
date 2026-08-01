using Mvp24Hours.Mcp.Indexing;

namespace Mvp24Hours.Mcp.Tests.Indexing;

public class ArchitectureResolverTests : McpTestFixture
{
    [Fact]
    public void Resolve_small_crud_suggests_minimal_or_simple()
    {
        var paths = CreatePaths();
        var manifest = new ManifestService(paths);
        var resolver = new ArchitectureResolver(manifest);

        var result = resolver.Resolve("small CRUD API");
        Assert.True(
            result.TemplateId is "minimal-api" or "simple-nlayers",
            $"Expected minimal-api or simple-nlayers but got {result.TemplateId}");
    }

    [Fact]
    public void Resolve_cqrs_suggests_cqrs_template()
    {
        var paths = CreatePaths();
        var manifest = new ManifestService(paths);
        var resolver = new ArchitectureResolver(manifest);

        var result = resolver.Resolve("commands and queries", cqrs: true);
        Assert.Equal("cqrs", result.TemplateId);
    }
}

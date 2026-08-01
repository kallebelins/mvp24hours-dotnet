using Mvp24Hours.Mcp.Indexing;

namespace Mvp24Hours.Mcp.Tests.Indexing;

public class ManifestServiceTests : McpTestFixture
{
    [Fact]
    public void Manifest_loads_with_unique_ids_and_existing_doc_paths()
    {
        var paths = CreatePaths();
        var manifest = new ManifestService(paths);

        Assert.True(manifest.GetAllTemplates().Count >= 9);
        Assert.All(manifest.GetAllTemplates(), t => Assert.False(string.IsNullOrWhiteSpace(t.Id)));
        Assert.NotNull(manifest.GetTemplate("simple-nlayers"));
        Assert.NotNull(manifest.GetTemplate("complex-nlayers"));
        Assert.NotNull(manifest.GetTemplate("minimal-api"));
    }

    [Fact]
    public void GetTemplate_is_case_insensitive()
    {
        var manifest = new ManifestService(CreatePaths());
        Assert.NotNull(manifest.GetTemplate("CQRS"));
        Assert.NotNull(manifest.GetTemplate("clean-architecture"));
    }
}

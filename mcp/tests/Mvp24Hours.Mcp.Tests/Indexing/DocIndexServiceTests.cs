using Mvp24Hours.Mcp.Indexing;

namespace Mvp24Hours.Mcp.Tests.Indexing;

public class DocIndexServiceTests : McpTestFixture
{
    [Fact]
    public void Search_finds_mvp_mediator()
    {
        var docIndex = new DocIndexService(CreatePaths());
        var hits = docIndex.Search("AddMvpMediator");
        Assert.NotEmpty(hits);
    }

    [Fact]
    public void Search_finds_web_application_factory()
    {
        var docIndex = new DocIndexService(CreatePaths());
        var hits = docIndex.Search("WebApplicationFactory");
        Assert.NotEmpty(hits);
    }

    [Fact]
    public void GetDoc_returns_known_page()
    {
        var docIndex = new DocIndexService(CreatePaths());
        var content = docIndex.GetDoc("getting-started.md");
        Assert.NotNull(content);
        Assert.Contains("Mvp24Hours", content, StringComparison.OrdinalIgnoreCase);
    }
}

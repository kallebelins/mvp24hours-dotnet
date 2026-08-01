using Mvp24Hours.Mcp.Indexing;

namespace Mvp24Hours.Mcp.Tests.Indexing;

public class SourceIndexServiceTests : McpTestFixture
{
    [Fact]
    public void FindSymbol_locates_known_type()
    {
        var source = new SourceIndexService(CreatePaths());
        var hits = source.FindSymbol("IMediator");
        Assert.NotEmpty(hits);
    }

    [Fact]
    public void FindTestsForModule_finds_webapi_tests()
    {
        var source = new SourceIndexService(CreatePaths());
        var tests = source.FindTestsForModule("WebAPI");
        Assert.NotEmpty(tests);
    }
}

using Mvp24Hours.Mcp.Indexing;

namespace Mvp24Hours.Mcp.Tests.Indexing;

public class ScenariosManifestServiceTests : McpTestFixture
{
    [Fact]
    public void Manifest_loads_known_scenarios()
    {
        var service = new ScenariosManifestService(CreatePaths());
        service.Warmup();

        var scenarios = service.GetAllScenarios();
        Assert.True(scenarios.Count >= 5);
        Assert.NotNull(service.GetScenario("port-to-mvp24hours"));
        Assert.NotNull(service.GetScenario("greenfield-api"));
    }
}

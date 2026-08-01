using Mvp24Hours.Mcp.Indexing;

namespace Mvp24Hours.Mcp.Tests.Indexing;

public class CapabilitiesManifestServiceTests : McpTestFixture
{
    [Theory]
    [InlineData("cqrs", "cqrs")]
    [InlineData("rabbitmq", "rabbitmq")]
    [InlineData("keycloak", "keycloak")]
    public void Resolve_matches_known_capabilities(string keyword, string expectedId)
    {
        var service = new CapabilitiesManifestService(CreatePaths());
        service.Warmup();

        var result = service.Resolve(keyword);
        Assert.NotNull(result);
        Assert.Equal(expectedId, result!.CapabilityId);
    }
}

using Microsoft.AspNetCore.Mvc.Testing;

namespace ProductAPI.Test.Integration;

[Trait("Category", "Integration")]
public class OpenApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OpenApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOpenApiDocument_WhenTestingHost_ReturnsNon5xx()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");

        ((int)response.StatusCode).Should().BeLessThan(500);
    }
}

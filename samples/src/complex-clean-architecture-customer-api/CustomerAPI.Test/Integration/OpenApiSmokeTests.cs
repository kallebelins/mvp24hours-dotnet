namespace CustomerAPI.Test.Integration;

[Trait("Category", "Integration")]
public class OpenApiSmokeTests : IClassFixture<CustomerApiFactory>
{
    private readonly CustomerApiFactory _factory;

    public OpenApiSmokeTests(CustomerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOpenApiDocument_WhenTestingHost_ReturnsNon5xx()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        ((int)response.StatusCode).Should().BeLessThan(500);
    }
}

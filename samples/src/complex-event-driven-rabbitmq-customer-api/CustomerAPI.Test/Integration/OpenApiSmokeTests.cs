namespace CustomerAPI.Test.Integration;

[Trait("Category", "Integration")]
public class OpenApiSmokeTests : IClassFixture<EventDrivenApiFactory>
{
    private readonly EventDrivenApiFactory _factory;

    public OpenApiSmokeTests(EventDrivenApiFactory factory)
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

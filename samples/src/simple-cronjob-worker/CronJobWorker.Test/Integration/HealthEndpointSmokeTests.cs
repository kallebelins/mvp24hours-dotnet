using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CronJobWorker.Test.Integration;

[Trait("Category", "Integration")]
public class HealthEndpointSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthLive_Get_ReturnsOk()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

using CustomerAPI.Test.Support;

namespace CustomerAPI.Test.Integration;

[Collection("MongoDb")]
[Trait("Category", "Integration")]
public sealed class CustomerMongoDbIntegrationTests(MongoDbContainerFixture fixture)
{
    [DockerFact]
    public async Task HealthCheck_WhenMongoDbContainerRunning_ReturnsNon5xx()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        await using MongoDbCustomerApiFactory factory = new(fixture.ConnectionString);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/hc");

        ((int)response.StatusCode).Should().BeLessThan(500);
    }
}

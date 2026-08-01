using CustomerAPI.Infrastructure.Data;
using CustomerAPI.Test.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerAPI.Test.Integration;

[Collection("EventDrivenContainers")]
[Trait("Category", "Integration")]
public sealed class EventDrivenContainersIntegrationTests(EventDrivenContainersFixture fixture)
{
    [DockerFact]
    public async Task HealthCheck_WhenSqlServerAndRabbitMqContainersRunning_ReturnsNon5xx()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        await using EventDrivenContainerApiFactory factory = new(
            fixture.SqlConnectionString,
            fixture.RabbitConnectionString);

        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            EFDBContext db = scope.ServiceProvider.GetRequiredService<EFDBContext>();
            await db.Database.EnsureCreatedAsync();
        }

        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/hc");

        ((int)response.StatusCode).Should().BeLessThan(500);
    }
}

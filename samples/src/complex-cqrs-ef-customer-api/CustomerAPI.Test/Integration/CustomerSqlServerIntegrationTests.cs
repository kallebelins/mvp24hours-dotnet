using System.Net.Http.Json;
using CustomerAPI.Core.ValueObjects.Customers;
using CustomerAPI.Infrastructure.Data;
using CustomerAPI.Test.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerAPI.Test.Integration;

[Collection("SqlServer")]
[Trait("Category", "Integration")]
public sealed class CustomerSqlServerIntegrationTests(SqlServerContainerFixture fixture)
{
    [DockerFact]
    public async Task CreateCustomer_WhenSqlServerContainerRunning_PersistsAndReadsBack()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        await using SqlServerCustomerApiFactory factory = new(fixture.ConnectionString);

        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            EFDBContext db = scope.ServiceProvider.GetRequiredService<EFDBContext>();
            await db.Database.EnsureCreatedAsync();
        }

        using HttpClient client = factory.CreateClient();
        var payload = new CustomerCreate { Name = "Testcontainers Customer", Note = "integration" };

        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/customer", payload);
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        HttpResponseMessage listResponse = await client.GetAsync("/api/customer?Limit=10&Offset=0");
        listResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        string body = await listResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Testcontainers Customer");
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CustomerAPI.WebAPI.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CustomerAPI.Test.Integration;

[Trait("Category", "Integration")]
public class CustomersApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CustomersApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
    }

    [Fact]
    public async Task Customers_CreateThenGetById_ReturnsSuccess()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest("Ada Lovelace", "ada@example.com"));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        string createBody = await createResponse.Content.ReadAsStringAsync();
        using JsonDocument created = JsonDocument.Parse(createBody);
        Guid id = created.RootElement.GetProperty("id").GetGuid();

        HttpResponseMessage getResponse = await client.GetAsync($"/api/customers/{id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        string getBody = await getResponse.Content.ReadAsStringAsync();
        using JsonDocument fetched = JsonDocument.Parse(getBody);
        fetched.RootElement.GetProperty("name").GetString().Should().Be("Ada Lovelace");
        fetched.RootElement.GetProperty("email").GetString().Should().Be("ada@example.com");
    }
}

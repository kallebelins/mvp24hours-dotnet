using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace App.Test;

public class ItemApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ItemApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Testing");
        }).CreateClient();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Create_Then_GetById_ReturnsItem()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/Item", new { Name = "Sample", Note = "bff-template" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await _client.GetAsync("/api/Item");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HealthCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("/hc");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

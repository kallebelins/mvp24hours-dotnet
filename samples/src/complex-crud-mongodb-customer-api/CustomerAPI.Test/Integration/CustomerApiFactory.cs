using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CustomerAPI.Test.Integration;

/// <summary>
/// Default smoke-test factory. OpenAPI does not require a live MongoDB instance at startup.
/// </summary>
public sealed class CustomerApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MongoDbContext"] = "mongodb://localhost:27017"
            });
        });
    }
}

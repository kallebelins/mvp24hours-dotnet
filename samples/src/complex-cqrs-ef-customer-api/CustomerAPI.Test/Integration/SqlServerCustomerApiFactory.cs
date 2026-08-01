using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CustomerAPI.Test.Integration;

/// <summary>
/// WebApplicationFactory wired to a real SQL Server Testcontainer connection string.
/// </summary>
public sealed class SqlServerCustomerApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EFDBContext"] = connectionString
            });
        });
    }
}

using CustomerAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CustomerAPI.Test.Integration;

/// <summary>
/// WebApplicationFactory wired to real SQL Server and RabbitMQ Testcontainers.
/// Background consumers/outbox processors are removed so the test stays deterministic.
/// </summary>
public sealed class EventDrivenContainerApiFactory(string sqlConnectionString, string rabbitConnectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EFDBContext"] = sqlConnectionString,
                ["ConnectionStrings:RabbitMQContext"] = rabbitConnectionString
            });
        });

        builder.ConfigureTestServices(services =>
        {
            foreach (ServiceDescriptor descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
            {
                services.Remove(descriptor);
            }

            var dbDescriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<EFDBContext>) ||
                d.ServiceType == typeof(EFDBContext) ||
                (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)) ||
                d.ServiceType == typeof(IDbContextOptionsConfiguration<EFDBContext>)).ToList();

            foreach (ServiceDescriptor? descriptor in dbDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<EFDBContext>(options => options.UseSqlServer(sqlConnectionString));
        });
    }
}

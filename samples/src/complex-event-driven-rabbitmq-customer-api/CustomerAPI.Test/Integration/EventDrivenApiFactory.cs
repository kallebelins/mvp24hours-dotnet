using CustomerAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Extensions;

namespace CustomerAPI.Test.Integration;

public class EventDrivenApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
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

            services.AddDbContext<EFDBContext>(options =>
                options.UseInMemoryDatabase("EventDrivenSmoke_" + Guid.NewGuid()));

            services.ReplaceRabbitMQWithInMemory();
        });
    }
}

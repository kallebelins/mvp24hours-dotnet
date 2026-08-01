using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Extensions;

namespace WebStatus.Test.Integration;

public class WebStatusFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.ReplaceRabbitMQWithInMemory();
        });
    }
}

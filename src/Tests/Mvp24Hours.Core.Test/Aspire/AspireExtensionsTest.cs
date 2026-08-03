using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Core.Aspire;

namespace Mvp24Hours.Core.Test.Aspire;

[Trait("Category", "Unit")]
public class AspireExtensionsTest
{
    [Fact]
    public void AddMvp24HoursAspireDefaults_Should_RegisterOptionsAndCorrelationAccessor()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration["Aspire:ServiceName"] = "ConfiguredService";

        builder.AddMvp24HoursAspireDefaults(options =>
        {
            options.EnableHealthChecks = true;
            options.EnableResilience = true;
            options.ServiceVersion = "9.9.9";
        });

        ServiceProvider provider = builder.Services.BuildServiceProvider();

        AspireOptions options = provider.GetRequiredService<AspireOptions>();
        options.ServiceName.Should().Be("ConfiguredService");
        options.ServiceVersion.Should().Be("9.9.9");
        options.Environment.Should().NotBeNullOrEmpty();
        provider.GetRequiredService<ICorrelationIdAccessor>().Should().BeOfType<CorrelationIdAccessor>();
    }

    [Fact]
    public void AddMvp24HoursAspireDefaults_WithConfigurationSection_ShouldBindOptions()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration["CustomAspire:EnableHealthChecks"] = "false";
        builder.Configuration["CustomAspire:EnableResilience"] = "false";

        builder.AddMvp24HoursAspireDefaults("CustomAspire");

        AspireOptions options = builder.Services.BuildServiceProvider().GetRequiredService<AspireOptions>();
        options.EnableHealthChecks.Should().BeFalse();
        options.EnableResilience.Should().BeFalse();
    }

    [Fact]
    public void AddMvp24HoursAspireDefaults_WithHealthChecksDisabled_ShouldNotRegisterHealthChecks()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddMvp24HoursAspireDefaults(options => options.EnableHealthChecks = false);

        ServiceDescriptor[] descriptors = [.. builder.Services];
        descriptors.Should().NotContain(d => d.ServiceType == typeof(HealthCheckService));
    }

    [Fact]
    public async Task MapMvp24HoursAspireHealthChecks_Should_ExposeStandardEndpoints()
    {
        (HttpClient client, IHost host) = await CreateHealthCheckHost();
        using (host)
        {
            (await client.GetAsync("/health/live")).StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            (await client.GetAsync("/health/ready")).StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            (await client.GetAsync("/health/startup")).StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            (await client.GetAsync("/health")).StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }
    }

    [Fact]
    public void UseAspireDashboardSupport_Should_ReturnSameApplication()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();

        WebApplication result = app.UseAspireDashboardSupport();

        result.Should().BeSameAs(app);
    }

    private static async Task<(HttpClient Client, IHost Host)> CreateHealthCheckHost()
    {
        IHost host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddLogging();
                    services.AddHealthChecks();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapMvp24HoursAspireHealthChecks());
                }))
            .StartAsync();

        return (host.GetTestClient(), host);
    }
}

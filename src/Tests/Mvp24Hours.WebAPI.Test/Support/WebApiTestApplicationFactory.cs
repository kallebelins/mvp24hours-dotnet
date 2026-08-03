using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mvp24Hours.WebAPI.Test.Support;

internal sealed class WebApiTestMarker;

internal sealed class WebApiTestApplicationFactory : WebApplicationFactory<WebApiTestMarker>
{
    private Action<IServiceCollection>? _configureServices;
    private Action<IEndpointRouteBuilder>? _configureEndpoints;
    private Action<IApplicationBuilder>? _configurePipeline;

    public WebApiTestApplicationFactory ConfigureServices(Action<IServiceCollection> configure)
    {
        _configureServices = configure;
        return this;
    }

    public WebApiTestApplicationFactory ConfigurePipeline(Action<IApplicationBuilder> configure)
    {
        _configurePipeline = configure;
        return this;
    }

    public WebApiTestApplicationFactory ConfigureEndpoints(Action<IEndpointRouteBuilder> configure)
    {
        _configureEndpoints = configure;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(AppContext.BaseDirectory);
    }

    protected override IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddLogging();
                    _configureServices?.Invoke(services);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    _configurePipeline?.Invoke(app);
                    app.UseEndpoints(endpoints => _configureEndpoints?.Invoke(endpoints));
                }));
    }
}

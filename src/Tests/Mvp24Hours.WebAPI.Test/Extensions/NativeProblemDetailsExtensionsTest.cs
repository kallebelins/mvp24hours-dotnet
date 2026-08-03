using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Exceptions;
using Mvp24Hours.WebAPI.Extensions;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Extensions;

[Trait("Category", "Unit")]
public class NativeProblemDetailsExtensionsTest
{
    [Fact]
    public void AddNativeProblemDetails_Should_RegisterMappersAndProblemDetailsService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNativeProblemDetails(options => options.ProblemTypeBaseUri = "https://errors.test");

        services.Should().Contain(x => x.ServiceType == typeof(DefaultExceptionToProblemDetailsMapper));
        services.Should().Contain(x => x.ServiceType == typeof(ValidationProblemDetailsMapper));
        services.Should().Contain(x => x.ServiceType == typeof(IExceptionToProblemDetailsMapper));
        services.Should().Contain(x => x.ServiceType.Name.Contains("IProblemDetailsService"));
    }

    [Fact]
    public void AddNativeProblemDetailsAll_Should_SetDevelopmentOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };

        services.AddNativeProblemDetailsAll(environment, options => options.FallbackDetail = "fallback");

        MvpProblemDetailsOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<MvpProblemDetailsOptions>>().Value;

        options.IncludeExceptionDetails.Should().BeTrue();
        options.IncludeStackTrace.Should().BeTrue();
        options.FallbackDetail.Should().Be("fallback");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_Return404ProblemDetails_ForNotFoundException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) => throw new NotFoundException("missing", "Order", 1)));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("missing");
        body.Should().Contain("traceId");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_IncludeCorrelationId_FromHeader()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) => throw new Core.Exceptions.ValidationException("invalid")));

        HttpClient client = host.GetTestClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-ID", "corr-99");
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("corr-99");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_WriteStatusCodePage_ForNonException404()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/missing", async context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await Task.CompletedTask;
            }));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/missing");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Not Found");
    }

    [Fact]
    public void UseNativeProblemDetailsHandling_Should_ReturnWebApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddNativeProblemDetails();
        WebApplication app = builder.Build();

        WebApplication result = app.UseNativeProblemDetailsHandling();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void AddNativeProblemDetailsAll_ProductionEnvironment_ShouldNotIncludeStackTrace()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Production };

        services.AddNativeProblemDetailsAll(environment);

        MvpProblemDetailsOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<MvpProblemDetailsOptions>>().Value;

        options.IncludeExceptionDetails.Should().BeFalse();
        options.IncludeStackTrace.Should().BeFalse();
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_Return409ProblemDetails_ForConflictException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) =>
                throw new ConflictException("duplicate", "Order", "Name")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("duplicate");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_Return422ProblemDetails_ForDomainException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) =>
                throw new DomainException("rule violated", "Order")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be((HttpStatusCode)422);
        (await response.Content.ReadAsStringAsync()).Should().Contain("rule violated");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_UseCorrelationId_FromHttpContextItems()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/fail", (HttpContext context) =>
            {
                context.Items["CorrelationId"] = "item-corr-1";
                throw new Core.Exceptions.ValidationException("invalid");
            }));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("item-corr-1");
    }

    private static async Task<IHost> CreateHost(
        Action<IApplicationBuilder> configureApp,
        Action<IServiceCollection> configureServices,
        Action<IEndpointRouteBuilder> configureEndpoints)
    {
        return await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddLogging();
                    configureServices(services);
                })
                .Configure(app =>
                {
                    configureApp(app);
                    app.UseRouting();
                    app.UseEndpoints(configureEndpoints);
                }))
            .StartAsync();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Mvp24Hours.WebAPI.Test";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
